using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using YatechStudio.Navigation.Domain;
using YatechStudio.Navigation.Editor;
using YatechStudio.Navigation.Infrastructure;

namespace YatechStudio.Navigation.Tests
{
    public class RouteManagerWindowTests
    {
        private NavigationConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<NavigationConfig>();
            _config.Routes.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null) Object.DestroyImmediate(_config);
        }

        [Test]
        public void Open_BindsConfig_AndCreatesSerializedObject()
        {
            var window = ScriptableObject.CreateInstance<RouteManagerWindow>();
            window.Open(_config);

            Assert.AreSame(_config, window.Config);
            Assert.IsNotNull(window.SerializedConfig);
            Assert.AreEqual(0, window.RouteCount);

            Object.DestroyImmediate(window);
        }

        [Test]
        public void RouteCount_TracksConfigRoutes()
        {
            var route = ScriptableObject.CreateInstance<RouteDefinition>();
            route.RouteId = "Lobby";
            route.Type = NavType.ScenePage;
            route.TargetScene = "LobbyScene";
            _config.Routes.Add(route);

            var window = ScriptableObject.CreateInstance<RouteManagerWindow>();
            window.Open(_config);

            Assert.AreEqual(1, window.RouteCount);

            Object.DestroyImmediate(route);
            Object.DestroyImmediate(window);
        }
    }
}