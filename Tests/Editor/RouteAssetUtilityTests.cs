using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using YatechStudio.Navigation.Domain;
using YatechStudio.Navigation.Editor;
using YatechStudio.Navigation.Infrastructure;

namespace YatechStudio.Navigation.Tests
{
    public class RouteAssetUtilityTests
    {
        private NavigationConfig _config;
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = "Assets/TempRouteTests";
            if (!AssetDatabase.IsValidFolder(_tempDir))
                AssetDatabase.CreateFolder("Assets", "TempRouteTests");

            _config = ScriptableObject.CreateInstance<NavigationConfig>();
            _config.Routes.Clear();
            AssetDatabase.CreateAsset(_config, Path.Combine(_tempDir, "TestConfig.asset"));
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(_tempDir);
        }

        [Test]
        public void GetRoutesFolder_IsConfigSibling_AndNamedAfterConfig()
        {
            var folder = RouteAssetUtility.GetRoutesFolder(_config);
            Assert.That(folder, Does.EndWith("TestConfigRoutes"));
            Assert.That(Path.GetDirectoryName(AssetDatabase.GetAssetPath(_config)),
                        Is.EqualTo(Path.GetDirectoryName(folder)));
        }

        private RouteDefinition MakeRoute(string id)
        {
            var route = ScriptableObject.CreateInstance<RouteDefinition>();
            route.RouteId = id;
            route.Type = NavType.ScenePage;
            route.TargetScene = id + "Scene";
            return route;
        }

        [Test]
        public void UniqueAssetPath_AvoidsCollisions()
        {
            var first = RouteAssetUtility.UniqueAssetPath(_config, "Shop");
            RouteAssetUtility.CreateRouteAsset(_config, MakeRoute("Shop"));

            var second = RouteAssetUtility.UniqueAssetPath(_config, "Shop");

            Assert.AreNotEqual(first, second);
            Assert.That(second, Does.EndWith("Shop_1.asset"));
        }

        [Test]
        public void CreateRouteAsset_AddsToList_AndSavesToDisk()
        {
            var route = ScriptableObject.CreateInstance<RouteDefinition>();
            route.RouteId = "Lobby";
            route.Type = NavType.ScenePage;
            route.TargetScene = "LobbyScene";

            RouteAssetUtility.CreateRouteAsset(_config, route);

            Assert.AreEqual(1, _config.Routes.Count);
            Assert.AreSame(route, _config.Routes[0]);
            var path = AssetDatabase.GetAssetPath(route);
            Assert.IsFalse(string.IsNullOrEmpty(path), "Route must be saved to disk.");
            Assert.That(path, Does.EndWith(".asset"));
        }

        [Test]
        public void SafeDeleteRoute_RemovesFromList_AndDestroysAsset()
        {
            var route = ScriptableObject.CreateInstance<RouteDefinition>();
            route.RouteId = "Shop";
            route.Type = NavType.Popup;
            RouteAssetUtility.CreateRouteAsset(_config, route);
            var path = AssetDatabase.GetAssetPath(route);

            RouteAssetUtility.SafeDeleteRoute(_config, 0);

            Assert.AreEqual(0, _config.Routes.Count);
            Assert.IsTrue(AssetDatabase.LoadAssetAtPath<RouteDefinition>(path) == null,
                          "Asset file must be deleted, not orphaned.");
        }
    }
}
