using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using YatechStudio.Navigation;
using YatechStudio.Navigation.Domain;
using YatechStudio.Navigation.Infrastructure;

namespace YatechStudio.Navigation.Samples
{
    /// <summary>
    /// Builds a NavigationConfig in code and registers mock bridges, so the sample
    /// runs without a real auth server or DI bootstrap in the host project.
    /// The sample uses inspector-assigned AssetReference fields for popups/windows
    /// so it compiles and runs in a player build.
    /// </summary>
    public class SampleNavigationInstaller : MonoBehaviour
    {
        [SerializeField] private AssetReference confirmPopupPrefab;
        [SerializeField] private AssetReference settingsWindowPrefab;
        [SerializeField] private bool startSignedIn = true;

        private class MockAuthBridge : INavigationAuthBridge
        {
            public bool IsAuthenticated { get; set; }
            public event System.Action<bool> OnAuthenticationChanged;
            public void Raise(bool value) => OnAuthenticationChanged?.Invoke(value);
        }

        private class MockUIBridge : INavigationUIBridge
        {
            public void TransitionPanel(bool show, string routeId) { }
            public void UpdateTransitionProgress(float progress) { }
            public Transform GetLayer(NavType navType) => null;
        }

        private class MockAudioBridge : INavigationAudioBridge
        {
            public void PlayBackSound() { }
        }

        private void Awake()
        {
            var config = ScriptableObject.CreateInstance<NavigationConfig>();
            config.LoginRouteId = "Login";
            config.DefaultRouteId = "MainMenu";
            config.SceneRegisterTimeoutSeconds = 10f;
            config.SceneTransitionDelaySeconds = 0.5f;

            config.Routes.Add(Route("MainMenu", NavType.ScenePage, RouteFlags.ClearHistory, "MainMenu"));
            config.Routes.Add(Route("Shop", NavType.ScenePage, RouteFlags.RequiresAuth, "Shop"));
            config.Routes.Add(Route("Login", NavType.ScenePage, RouteFlags.None, "MainMenu"));
            config.Routes.Add(PrefabRoute("Confirm", NavType.Popup, RouteFlags.IsTransient, confirmPopupPrefab));
            config.Routes.Add(PrefabRoute("Settings", NavType.Window, RouteFlags.IsAdditive, settingsWindowPrefab));

            var auth = new MockAuthBridge { IsAuthenticated = startSignedIn };
            var ui = new MockUIBridge();
            var audio = new MockAudioBridge();

            var builder = new ContainerBuilder();
            builder.RegisterInstance(config);
            builder.RegisterInstance<INavigationAuthBridge>(auth);
            builder.RegisterInstance<INavigationUIBridge>(ui);
            builder.RegisterInstance<INavigationAudioBridge>(audio);
            builder.RegisterInstance<INavigationLogger>(NullNavigationLogger.Instance);
            builder.Register<NavigationService>(Lifetime.Singleton)
                   .As<INavigationService>()
                   .As<System.IDisposable>();
            var container = builder.Build();

            // Boot: navigate to the default route.
            container.Resolve<INavigationService>().NavigateTo(config.DefaultRouteId).Forget();
        }

        private static RouteDefinition Route(string id, NavType type, RouteFlags flags, string scene)
        {
            var r = ScriptableObject.CreateInstance<RouteDefinition>();
            r.RouteId = id; r.Type = type; r.Flags = flags; r.TargetScene = scene;
            return r;
        }

        private static RouteDefinition PrefabRoute(string id, NavType type, RouteFlags flags, AssetReference prefab)
        {
            var r = ScriptableObject.CreateInstance<RouteDefinition>();
            r.RouteId = id; r.Type = type; r.Flags = flags;
            // Store the AssetReference directly on the RouteDefinition.
            // Runtime code will read route.Prefab (AssetReference) and load via
            // Addressables.LoadAssetAsync — the host project's addressable group
            // must contain the referenced assets.
            r.Prefab = prefab;
            return r;
        }
    }
}