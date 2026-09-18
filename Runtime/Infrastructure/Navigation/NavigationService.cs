using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using YatechStudio.Navigation.Domain;

namespace YatechStudio.Navigation.Infrastructure
{
    /// <summary>
    /// Default <see cref="INavigationService"/> implementation.
    ///
    /// Threading contract: all methods must be called from the Unity main thread. Navigation
    /// requests are serialized internally — a call issued while another navigation is in flight
    /// waits for it instead of interleaving — and the authentication events are handled through
    /// the same queue.
    /// </summary>
    public class NavigationService : INavigationService, IDisposable
    {
        private readonly NavigationConfig _config;
        private readonly INavigationUIBridge _uiBridge;
        private readonly INavigationAuthBridge _authBridge;
        private readonly INavigationLogger _logger;

        // Local scene-page state
        private IPageContainer _currentSceneContainer;
        private UniTaskCompletionSource<IPageContainer> _sceneLoadTcs;

        private RouteState _currentState = new RouteState();
        private readonly Stack<RouteState> _history = new Stack<RouteState>();
        private RouteState? _pendingState;

        // Serialized navigation queue (single-flight). Never navigates concurrently.
        private UniTask _navigationInFlight = UniTask.CompletedTask;

        // Cancelled whenever a newer navigation supersedes the running one.
        private CancellationTokenSource _navCts;

        // Addressable window/popup state
        private readonly Dictionary<string, IViewPanel> _activeWindows = new Dictionary<string, IViewPanel>();
        private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _loadedHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();
        private readonly Dictionary<string, UniTask<IViewPanel>> _windowLoads = new Dictionary<string, UniTask<IViewPanel>>();

        private bool _disposed;

        public bool CanGoBack => _history.Count > 0;

        public string CurrentRoute => _currentState.RouteId ?? string.Empty;

        public NavigationService(
            NavigationConfig config,
            INavigationUIBridge uiBridge,
            INavigationAuthBridge authBridge,
            INavigationLogger logger = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _uiBridge = uiBridge ?? throw new ArgumentNullException(nameof(uiBridge));
            _authBridge = authBridge ?? throw new ArgumentNullException(nameof(authBridge));
            _logger = logger ?? UnityNavigationLogger.Instance;

            _authBridge.OnAuthenticationChanged += HandleSessionChanged;
        }

        /// <summary>
        /// Called by a scene's <see cref="ScenePageContainer"/> once its panels are ready.
        /// Safe to call before or after the navigation that expects it — the latest
        /// registration always wins and any pending scene load is completed.
        /// </summary>
        public void RegisterView(IPageContainer container)
        {
            if (_disposed) return;
            if (container == null) throw new ArgumentNullException(nameof(container));

            _currentSceneContainer = container;
            _sceneLoadTcs?.TrySetResult(container);
        }

        /// <summary>
        /// Navigates to a route, then to the environment-specific variant of that route.
        /// </summary>
        public UniTask NavigateToInEnv(string routeId, string env, object payload = null)
            => NavigateTo(routeId, "_" + env, payload);

        public UniTask NavigateTo(string targetRouteId, string additionalNameScene = "", object payload = null)
            => Enqueue(() => NavigateToCore(targetRouteId, additionalNameScene, payload));

        public UniTask GoBack()
            => Enqueue(GoBackCore);

        public async UniTask<bool> HandleHardwareBack()
        {
            var currentPanel = GetPanelInstance(_currentState.RouteId);

            if (currentPanel is IPagePanel pagePanel && pagePanel.TryBackSubPanel())
            {
                _logger.Info("Back action handled by the page's sub-panel.");
                return true;
            }

            if (CanGoBack)
            {
                await GoBack();
                return true;
            }

            return false;
        }

        // ------------------------------------------------------------------ Queue plumbing

        private UniTask Enqueue(Func<UniTask> action)
        {
            var previous = _navigationInFlight;
            var completion = new UniTaskCompletionSource();
            _navigationInFlight = completion.Task;
            return RunAfter(previous, action, completion);
        }

        private async UniTask RunAfter(UniTask previous, Func<UniTask> action, UniTaskCompletionSource completion)
        {
            await previous.SuppressCancellationThrow();
            try
            {
                await action();
            }
            finally
            {
                completion.TrySetResult();
            }
        }

        // ------------------------------------------------------------------ Core navigation

        private async UniTask NavigateToCore(string routeId, string additionalNameScene, object payload, bool pushHistory = true)
        {
            if (_disposed) return;

            if (!_config.TryGet(routeId, out var route))
            {
                _logger.Error($"Route '{routeId}' is not registered in the NavigationConfig.");
                return;
            }

            // Auth guard. Never guard the login route itself (would recurse forever).
            if (route.HasFlag(RouteFlags.RequiresAuth)
                && routeId != _config.LoginRouteId
                && !_authBridge.IsAuthenticated)
            {
                _pendingState = new RouteState(routeId, additionalNameScene, payload);
                _logger.Info($"Access denied to '{routeId}'. Redirecting to Login...");
                await NavigateToCore(_config.LoginRouteId, string.Empty, null);
                return;
            }

            // Supersede any in-flight navigation. State mutation happens only after success.
            _navCts?.Cancel();
            _navCts?.Dispose();
            _navCts = new CancellationTokenSource();
            var token = _navCts.Token;

            try
            {
                if (!route.HasFlag(RouteFlags.IsAdditive))
                    await CloseCurrentState(token);

                IViewPanel targetPanel = route.Type == NavType.ScenePage
                    ? await HandleSceneNavigation(route, additionalNameScene, token)
                    : await HandleWindowNavigation(route, token);

                if (targetPanel == null)
                {
                    _logger.Warning($"Navigation to '{routeId}' produced no panel. Current route unchanged.");
                    return;
                }

                if (targetPanel is IPagePanel page)
                    page.SetPayload(payload);

                await targetPanel.Show(token);

                // History bookkeeping — only after the navigation actually succeeded.
                if (route.HasFlag(RouteFlags.ClearHistory))
                {
                    _history.Clear();
                    _logger.Info($"'{routeId}' is a root route. History cleared.");
                }
                else if (pushHistory && _currentState.RouteId != routeId)
                {
                    PushCurrentToHistory();
                }

                _currentState = new RouteState(routeId, additionalNameScene, payload);
                _logger.Info($"Navigated to '{routeId}'.");
            }
            catch (OperationCanceledException)
            {
                // Superseded by a newer navigation — intentionally silent, state untouched.
            }
            catch (Exception e)
            {
                _logger.Exception(e, $"Failed to navigate to '{routeId}'.");
            }
        }

        private void PushCurrentToHistory()
        {
            // Never remember an empty state or a transient route; back would land on garbage.
            if (string.IsNullOrEmpty(_currentState.RouteId)) return;
            if (_config.TryGet(_currentState.RouteId, out var currentRoute)
                && currentRoute.HasFlag(RouteFlags.IsTransient)) return;

            _history.Push(_currentState);
        }

        private async UniTask GoBackCore()
        {
            if (_disposed) return;

            if (_history.Count == 0)
            {
                _logger.Info("No history to go back to.");
                return;
            }

            var hasCurrent = _config.TryGet(_currentState.RouteId, out var currentRoute);

            if (hasCurrent && currentRoute.HasFlag(RouteFlags.BlockBack))
            {
                _logger.Info($"Back is blocked on route '{_currentState.RouteId}'.");
                return;
            }

            try
            {
                // Additive routes are overlays: close them and restore state without re-navigation.
                if (hasCurrent && currentRoute.HasFlag(RouteFlags.IsAdditive))
                {
                    var currentPanel = GetPanelInstance(_currentState.RouteId);
                    if (currentPanel != null)
                        await currentPanel.Hide();

                    _currentState = _history.Pop();
                    return;
                }

                var previousState = _history.Pop();
                _logger.Info($"Going back to '{previousState.RouteId}'.");
                await NavigateToCore(previousState.RouteId, previousState.Environment, previousState.Payload, pushHistory: false);
            }
            catch (Exception e)
            {
                _logger.Exception(e, "GoBack failed.");
            }
        }

        // ------------------------------------------------------------------ Scene navigation

        private async UniTask<IViewPanel> HandleSceneNavigation(RouteDefinition route, string additionalNameScene, CancellationToken token)
        {
            var currentScene = SceneManager.GetActiveScene().name;
            var targetSceneName = route.TargetScene + additionalNameScene;

            if (!string.Equals(currentScene, targetSceneName, StringComparison.Ordinal))
            {
                _uiBridge.TransitionPanel(true, route.RouteId);

                try
                {
                    // Addressable windows from the previous scene must not leak into the new one.
                    ReleaseAllWindows();

                    _currentSceneContainer = null;

                    // Created BEFORE the load starts so RegisterView can never miss it.
                    var tcs = new UniTaskCompletionSource<IPageContainer>();
                    _sceneLoadTcs = tcs;

                    var progress = Progress.Create<float>(p => _uiBridge.UpdateTransitionProgress(p));

                    await SceneManager.LoadSceneAsync(targetSceneName)
                        .ToUniTask(progress: progress, cancellationToken: token);

                    await tcs.Task
                        .AttachExternalCancellation(token)
                        .Timeout(TimeSpan.FromSeconds(Mathf.Max(1f, _config.SceneRegisterTimeoutSeconds)));

                    if (_config.SceneTransitionDelaySeconds > 0f)
                    {
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(_config.SceneTransitionDelaySeconds),
                            cancellationToken: token);
                    }
                }
                catch (TimeoutException)
                {
                    _logger.Error($"Scene '{targetSceneName}' did not register a ScenePageContainer within {_config.SceneRegisterTimeoutSeconds}s.");
                    return null;
                }
                finally
                {
                    _sceneLoadTcs = null;
                    _uiBridge.TransitionPanel(false, route.RouteId);
                }
            }

            if (_currentSceneContainer == null)
            {
                _logger.Error($"No ScenePageContainer registered for scene '{targetSceneName}'. " +
                              "Add a ScenePageContainer to the scene and make sure it is resolved by the DI container.");
                return null;
            }

            return _currentSceneContainer.GetPanel(route.RouteId);
        }

        // ------------------------------------------------------------------ Window / popup navigation

        private async UniTask<IViewPanel> HandleWindowNavigation(RouteDefinition route, CancellationToken token)
        {
            if (_activeWindows.TryGetValue(route.RouteId, out var existing))
                return existing;

            // A concurrent navigation to the same route awaits the in-flight load instead of
            // spawning a second instance.
            if (_windowLoads.TryGetValue(route.RouteId, out var inFlight))
                return await inFlight.AttachExternalCancellation(token);

            var load = LoadWindow(route);
            _windowLoads[route.RouteId] = load;
            try
            {
                return await load.AttachExternalCancellation(token);
            }
            finally
            {
                _windowLoads.Remove(route.RouteId);
            }
        }

        private async UniTask<IViewPanel> LoadWindow(RouteDefinition route)
        {
            if (!_loadedHandles.TryGetValue(route.RouteId, out var handle))
            {
                handle = route.Prefab.LoadAssetAsync<GameObject>();
                _loadedHandles.Add(route.RouteId, handle);
            }

            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                _logger.Error($"Failed to load Addressable prefab for route '{route.RouteId}': {handle.OperationException?.Message}");
                return null;
            }

            var layer = _uiBridge.GetLayer(route.Type);
            var instance = UnityEngine.Object.Instantiate(handle.Result, layer);

            if (instance.TryGetComponent<IViewPanel>(out var panel))
            {
                _activeWindows.Add(route.RouteId, panel);
                return panel;
            }

            _logger.Error($"Prefab for route '{route.RouteId}' has no IViewPanel component. Instance destroyed.");
            UnityEngine.Object.Destroy(instance);
            return null;
        }

        // ------------------------------------------------------------------ Teardown / cleanup

        private async UniTask CloseCurrentState(CancellationToken token)
        {
            // Snapshot: Hide may trigger callbacks that mutate the collection.
            var windows = new List<IViewPanel>(_activeWindows.Values);
            foreach (var window in windows)
            {
                if (window != null && window.gameObject != null && window.IsActive)
                    await window.Hide(token);
            }

            _currentSceneContainer?.HideAllPanels();
        }

        private IViewPanel GetPanelInstance(string routeId)
        {
            if (string.IsNullOrEmpty(routeId)) return null;
            if (_activeWindows.TryGetValue(routeId, out var window)) return window;
            return _currentSceneContainer?.GetPanel(routeId);
        }

        private void HandleSessionChanged(bool isAuthenticated)
        {
            if (_disposed) return;

            if (isAuthenticated)
            {
                if (_pendingState.HasValue)
                {
                    var target = _pendingState.Value;
                    _pendingState = null;
                    _logger.Info($"Resuming pending route '{target.RouteId}{target.Environment}'.");
                    NavigateTo(target.RouteId, target.Environment, target.Payload).Forget(OnForgetException);
                }
                else if (_currentState.RouteId == _config.LoginRouteId)
                {
                    _logger.Info("Signed in on the Login route. Navigating to the default route.");
                    NavigateTo(_config.DefaultRouteId).Forget(OnForgetException);
                }
            }
            else
            {
                _pendingState = null;
                if (_config.TryGet(_currentState.RouteId, out var route) && route.HasFlag(RouteFlags.RequiresAuth))
                {
                    _logger.Info("Signed out on a protected route. Redirecting to Login.");
                    NavigateTo(_config.LoginRouteId).Forget(OnForgetException);
                }
            }
        }

        private void OnForgetException(Exception e)
            => _logger.Exception(e, "Unhandled exception in fire-and-forget navigation.");

        private void ReleaseAllWindows()
        {
            foreach (var window in _activeWindows.Values)
            {
                if (window != null && window.gameObject != null)
                    UnityEngine.Object.Destroy(window.gameObject);
            }
            _activeWindows.Clear();

            foreach (var handle in _loadedHandles.Values)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
            _loadedHandles.Clear();
            _windowLoads.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _authBridge.OnAuthenticationChanged -= HandleSessionChanged;

            _navCts?.Cancel();
            _navCts?.Dispose();
            _navCts = null;

            ReleaseAllWindows();
        }
    }
}
