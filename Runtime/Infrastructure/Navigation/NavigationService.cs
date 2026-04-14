using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public struct RouteState
{
    public string RouteId;
    public string Environment;
    public object Payload;

    public RouteState(string routeId, string environment = "", object payload = null)
    {
        RouteId = routeId;
        Environment = environment;
        Payload = payload;
    }
}

public class NavigationService : INavigationService, IDisposable
{
    private readonly NavigationConfig _routeConfigObj;
    private readonly Dictionary<string, RouteDefinition> _routeConfig;
    private readonly INavigationUIBridge _uiBridge;
    private readonly INavigationAuthBridge _authBridge;

    // Cache untuk Scene View (Local)
    private IPageContainer _currentSceneContainer;
    private UniTaskCompletionSource<IPageContainer> _sceneLoadTcs;

    private RouteState _currentState = new ();
    private readonly Stack<RouteState> _history = new Stack<RouteState>();
    private RouteState? _pendingState; // Simpan payload jika user harus login dulu

    private CancellationTokenSource _navCts;

    // Cache untuk Window/Popup yang sudah di-spawn (biar gak spawn double)
    private Dictionary<string, IViewPanel> _activeWindows = new();
    private Dictionary<string, AsyncOperationHandle<GameObject>> _loadedHandles = new();

    private bool _isGoingBack; // Flag agar saat 'Back', kita tidak menambah histori baru

    public bool CanGoBack => _history.Count > 0;

    public NavigationService(NavigationConfig config, INavigationUIBridge uiBridge, INavigationAuthBridge authBridge)
    {
        _routeConfigObj = config;
        _routeConfig = config.ToDictionary();
        _uiBridge = uiBridge;
        _authBridge = authBridge;

        _authBridge.OnAuthenticationChanged += HandleSessionChanged;
    }

    // Method untuk Scene lokal mendaftarkan UI-nya
    public void RegisterView(IPageContainer container)
    {
        _currentSceneContainer = container;
        _sceneLoadTcs?.TrySetResult(container); // Beritahu NavigateTo bahwa scene sudah siap
    }

    // Method untuk banyak Scene dengan 1 routeId yang sama (misal: Shop_Season1, Shop_Season2)
    public async UniTask NavigateToInEnv(string routeId, string env, object payload = null)
    {
        var additionalNameScene = $"_{env}";
        await NavigateTo(routeId, additionalNameScene, payload);
    }

    public async UniTask NavigateTo(string routeId, string additionalNameScene = "", object payload = null)
    {
        // Cek Config
        if (!_routeConfig.TryGetValue(routeId, out var config))
        {
            LoggerService.Error($"[Nav] Route {routeId} tidak terdaftar di Config!");
            return;
        }

        // --- LOGIC ROOT NAVIGATION ---
        if (config.HasFlag(RouteFlags.ClearHistory))
        {
            LoggerService.LogDebug($"[Nav] {routeId} is a Root Route. Clearing history stack.");
            _history.Clear();
        }

        // Auth Guard
        if (config.HasFlag(RouteFlags.RequiresAuth) && !_authBridge.IsAuthenticated)
        {
            // Simpan seluruh state (Route, Env, dan Payload) secara utuh
            _pendingState = new RouteState(routeId, additionalNameScene, payload);

            LoggerService.LogDebug($"Access denied to {routeId}{additionalNameScene}. Redirecting to Login...");
            await NavigateTo(_routeConfigObj.LoginRouteId);
            return;
        }

        // Logic Histori Management
        // Jangan push ke history jika: 
        // 1. Sedang Back
        // 2. Rute tujuan adalah Transient
        // 3. BARU SAJA melakukan ClearHistory (Root Route tidak butuh back ke sebelumnya)
        if (!_isGoingBack && !config.HasFlag(RouteFlags.IsTransient) && !config.HasFlag(RouteFlags.ClearHistory))
        {
            // Jangan push rute yang sama dua kali berturut-turut
            if (_history.Count == 0 || _history.Peek().RouteId != _currentState.RouteId)
                _history.Push(_currentState); // Push state utuh
        }
        _isGoingBack = false;

        // Cancel navigasi sebelumnya jika ada
        _navCts?.Cancel();
        _navCts = new CancellationTokenSource();
        var token = _navCts.Token;

        try
        {
            // 4. Close previous UI if NOT Additive
            if (!config.HasFlag(RouteFlags.IsAdditive))
            {
                await CloseCurrentState(token);
            }

            // 5. Execution
            IViewPanel targetPanel = null;

            // LOGIKA UTAMA
            if (config.Type == NavType.ScenePage) // Scene Page (Local)
                targetPanel = await HandleSceneNavigation(config, additionalNameScene, token);
            else // Window / Popup (Addressables)
                targetPanel = await HandleWindowNavigation(config, token);

            // Inject Payload ke Panel sebelum Show
            if (targetPanel != null)
            {
                // Kita kirim datanya ke Panel. 
                // Panel/Presenter nanti yang akan memutuskan sub-panel mana yang aktif.
                if (targetPanel is IPagePanel page)
                    page.SetPayload(payload);
                LoggerService.LogDebug($"[Nav] Navigated to {routeId} with payload: {payload}");

                await targetPanel.Show(token);
                _currentState = new RouteState(routeId, additionalNameScene, payload);
            }

            LoggerService.LogDebug($"[Nav] Navigated to {routeId}");
        }
        catch (OperationCanceledException)
        {
            // Handle jika navigasi dibatalkan
        }
        catch (Exception e)
        {
            LoggerService.Exception(e, $"[Nav] Failed to navigate to {routeId}");
        }
    }

    private async UniTask<IViewPanel> HandleSceneNavigation(RouteDefinition config, string additionalNameScene, CancellationToken token)
    {
        string currentScene = SceneManager.GetActiveScene().name;
        string targetSceneName = config.TargetScene + additionalNameScene;

        // A. Pindah Scene jika perlu
        if (currentScene != targetSceneName)
        {
            _uiBridge.ShowLoading(true);

            try
            {
                //clear cache window yang mungkin masih tersisa dari scene sebelumnya
                //(sangat penting untuk mencegah memory leak)
                ReleaseAllWindows();

                _currentSceneContainer = null;
                _sceneLoadTcs = new UniTaskCompletionSource<IPageContainer>();

                await SceneManager.LoadSceneAsync(targetSceneName).ToUniTask(cancellationToken: token);
                await _sceneLoadTcs.Task.Timeout(TimeSpan.FromSeconds(5)); // Tunggu RegisterView

                await UniTask.Delay(2000);
            }
            catch (TimeoutException)
            {
                LoggerService.Error($"[Nav] Scene {targetSceneName} gagal register container dalam 5 detik!");
                return null;
            }
            finally
            {
                _uiBridge.ShowLoading(false);
            }
        }
        
        // B. Tampilkan Panel Local
        if (_currentSceneContainer != null)
        {
            return _currentSceneContainer.GetPanel(config.RouteId);
        }
        return null;
    }

    private async UniTask<IViewPanel> HandleWindowNavigation(RouteDefinition config, CancellationToken token)
    {
        if (_activeWindows.TryGetValue(config.RouteId, out var panel)) return panel;

        // LOAD / GET PREFAB HANDLE
        if (!_loadedHandles.TryGetValue(config.RouteId, out var handle))
        {
            handle = config.Prefab.LoadAssetAsync<GameObject>();
            _loadedHandles.Add(config.RouteId, handle);
        }

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("Gagal load Addressable");
            return null;
        }

        // 3. INSTANTIATE
        var layer = _uiBridge.GetLayer(config.Type);
        var instance = UnityEngine.Object.Instantiate(handle.Result, layer);
        if (instance.TryGetComponent<IViewPanel>(out panel))
        {
            _activeWindows.Add(config.RouteId, panel);
            return panel;
        }

        return null;
    }

    private async UniTask CloseCurrentState(CancellationToken token)
    {
        // Tutup semua window addressable yang sedang aktif
        foreach (var window in _activeWindows.Values)
        {
            if (window.IsActive) await window.Hide(token);
        }

        // Matikan semua panel lokal di scene
        if (_currentSceneContainer != null)
            _currentSceneContainer.HideAllPanels();
    }

    public async UniTask GoBack()
    {
        if (_history.Count == 0)
        {
            LoggerService.LogDebug("No history to go back to.");
            return;
        }

        if (_routeConfig.TryGetValue(_currentState.RouteId, out var currentConfig) &&
        currentConfig.HasFlag(RouteFlags.BlockBack)) // Buat flag baru jika perlu
        {
            LoggerService.LogDebug("Back action is blocked for this route.");
            return;
        }

        // Jika rute saat ini adalah ADDITIVE, kita cukup menutupnya tanpa pindah rute
        if (_routeConfig.TryGetValue(_currentState.RouteId, out var config) && config.HasFlag(RouteFlags.IsAdditive))
        {
            var currentPanel = GetPanelInstance(_currentState.RouteId);
            if (currentPanel != null) await currentPanel.Hide();

            if(_history.Count > 0)
                _currentState = _history.Pop(); // Balik ke state sebelumnya tanpa NavigateTo
            return;
        }

        _isGoingBack = true;
        var previousState = _history.Pop();
        LoggerService.LogDebug($"Go back {previousState.RouteId}");
        await NavigateTo(previousState.RouteId, previousState.Environment, previousState.Payload);
    }

    public async UniTask<bool> HandleHardwareBack()
    {
        // 1. Dapatkan panel yang sedang aktif di layar
        var currentPanel = GetPanelInstance(_currentState.RouteId);

        // 2. Cek apakah panel tersebut adalah IPagePanel
        if (currentPanel is IPagePanel pagePanel)
        {
            // Jika panel memiliki sub-panel, ia akan mundur 1 langkah dan mengembalikan true.
            if (pagePanel.TryBackSubPanel())
            {
                LoggerService.LogDebug("[Nav] Back action handled by Page's SubPanel.");
                return true;
            }
        }

        // 3. Jika tidak ada sub-panel (false), cek apakah histori RUTE masih ada
        if (CanGoBack)
        {
            LoggerService.LogDebug("[Nav] Back action handled by Route History.");
            await GoBack();
            return true;
        }

        // 4. Jika Sub-panel habis AND Route history habis (sedang di Root/MainMenu)
        return false;
    }

    // Helper untuk mencari instance panel di mana pun dia berada
    private IViewPanel GetPanelInstance(string routeId)
    {
        if (string.IsNullOrEmpty(routeId)) return null;
        if (_activeWindows.TryGetValue(routeId, out var window)) return window;
        if (_currentSceneContainer != null) return _currentSceneContainer.GetPanel(routeId);
        return null;
    }

    private void HandleSessionChanged(bool isAuthenticated)
    {
        if (isAuthenticated)
        {
            // LOGIN SUKSES: Cek apakah ada rute yang tertunda?
            if (_pendingState.HasValue)
            {
                var targetState = _pendingState.Value;
                _pendingState = null; // Clear memori

                LoggerService.LogDebug($"[Nav] Resuming pending route: {targetState.RouteId}{targetState.Environment}");
                NavigateTo(targetState.RouteId, targetState.Environment, targetState.Payload).Forget(); // Lanjutkan ke tujuan awal
            }
            // Prioritas 2: Jika tidak ada janji, mungkin user baru login biasa, bawa ke Lobby
            else if (_currentState.RouteId == _routeConfigObj.LoginRouteId)
            {
                NavigateTo(_routeConfigObj.DefaultRouteId).Forget(); // rute fallback sesuai config
            }
        }
        else
        {
            // Logout logic
            _pendingState = null; // Bersihkan janji jika user memang sengaja logout
            if (_routeConfig.TryGetValue(_currentState.RouteId, out var config) && config.HasFlag(RouteFlags.RequiresAuth))
            {
                NavigateTo(_routeConfigObj.LoginRouteId).Forget();
            }
        }
    }

    public void Dispose()
    {
        if (_authBridge != null)
            _authBridge.OnAuthenticationChanged -= HandleSessionChanged;
            
        _navCts?.Dispose();

        // Lepaskan memori Asset via Handle (Sangat Aman)
        ReleaseAllWindows();
    }

    private void ReleaseAllWindows()
    {
        // 1. Destroy Instance secara fisik
        foreach (var window in _activeWindows.Values)
        {
            if (window != null && window.gameObject != null)
                UnityEngine.Object.Destroy(window.gameObject);
        }
        _activeWindows.Clear();

        // 2. Release Asset dari RAM
        foreach (var handle in _loadedHandles.Values)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }
        _loadedHandles.Clear();

        Debug.Log("Addressables UI Released: RAM Cleared for new scene.");
    }

    public string CurrentRoute => _currentState.RouteId;
}