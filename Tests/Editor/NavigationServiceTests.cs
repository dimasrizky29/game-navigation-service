using System;
using NUnit.Framework;
using UnityEngine;
using YatechStudio.Navigation.Domain;
using YatechStudio.Navigation.Infrastructure;

namespace YatechStudio.Navigation.Tests
{
    public class NavigationServiceTests
    {
        private sealed class MockAuthBridge : INavigationAuthBridge
        {
            public bool IsAuthenticated { get; set; }
            public event Action<bool> OnAuthenticationChanged;

            public void Raise(bool value) => OnAuthenticationChanged?.Invoke(value);
        }

        private sealed class MockUIBridge : INavigationUIBridge
        {
            public bool TransitionShown { get; private set; }
            public float LastProgress { get; private set; }

            public void TransitionPanel(bool show, string routeId) => TransitionShown = show;

            public void UpdateTransitionProgress(float progress) => LastProgress = progress;

            public Transform GetLayer(NavType navType) => null;
        }

        private NavigationConfig _config;
        private MockAuthBridge _auth;
        private MockUIBridge _ui;
        private NavigationService _service;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<NavigationConfig>();
            _config.Routes.Clear();
            _auth = new MockAuthBridge();
            _ui = new MockUIBridge();
            _service = new NavigationService(_config, _ui, _auth, NullNavigationLogger.Instance);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
            UnityEngine.Object.DestroyImmediate(_config);
        }

        private RouteDefinition Scene(string id, string scene, RouteFlags flags = RouteFlags.None)
        {
            var route = ScriptableObject.CreateInstance<RouteDefinition>();
            route.RouteId = id;
            route.Type = NavType.ScenePage;
            route.TargetScene = scene;
            route.Flags = flags;
            return route;
        }

        [Test]
        public void NavigateTo_UnknownRoute_DoesNotThrowAndKeepsCurrentRoute()
        {
            Assert.DoesNotThrow(() => _service.NavigateTo("Missing").GetAwaiter().GetResult());
            Assert.IsEmpty(_service.CurrentRoute);
            Assert.IsFalse(_service.CanGoBack);
        }

        [Test]
        public void NavigateTo_ProtectedRouteWhileSignedOut_RedirectsInsteadOfNavigating()
        {
            _config.Routes.Add(Scene("Shop", "ShopScene", RouteFlags.RequiresAuth));
            _auth.IsAuthenticated = false;

            Assert.DoesNotThrow(() => _service.NavigateTo("Shop").GetAwaiter().GetResult());

            Assert.AreNotEqual("Shop", _service.CurrentRoute, "Signed-out user must not reach a protected route.");
        }

        [Test]
        public void GoBack_EmptyHistory_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _service.GoBack().GetAwaiter().GetResult());
            Assert.IsFalse(_service.CanGoBack);
        }

        [Test]
        public void HandleHardwareBack_EmptyHistory_ReturnsFalse()
        {
            var handled = _service.HandleHardwareBack().GetAwaiter().GetResult();
            Assert.IsFalse(handled);
        }

        [Test]
        public void ConcurrentNavigations_AreSerializedWithoutThrowing()
        {
            _config.Routes.Add(Scene("Lobby", "LobbyScene"));
            _config.Routes.Add(Scene("Shop", "ShopScene"));

            var t1 = _service.NavigateTo("Lobby");
            var t2 = _service.NavigateTo("Shop");
            var t3 = _service.NavigateTo("Missing");

            Assert.DoesNotThrow(() =>
            {
                t1.GetAwaiter().GetResult();
                t2.GetAwaiter().GetResult();
                t3.GetAwaiter().GetResult();
            });
        }

        [Test]
        public void Dispose_UnsubscribesAuthEvents()
        {
            _service.Dispose();

            // Must not throw after disposal — the handler was removed.
            Assert.DoesNotThrow(() => _auth.Raise(true));
        }

        [Test]
        public void NavigateTo_UnknownRouteWhileDisposed_DoesNotThrow()
        {
            _service.Dispose();

            Assert.DoesNotThrow(() => _service.NavigateTo("Missing").GetAwaiter().GetResult());
        }
    }
}
