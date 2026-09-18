using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using YatechStudio.Navigation.Domain;
using YatechStudio.Navigation.Infrastructure;

namespace YatechStudio.Navigation.Tests
{
    public class RouteDefinitionTests
    {
        private RouteDefinition _route;

        [SetUp]
        public void SetUp() => _route = ScriptableObject.CreateInstance<RouteDefinition>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_route);

        [Test]
        public void HasFlag_ReturnsTrueOnlyForSetFlags()
        {
            _route.Flags = RouteFlags.RequiresAuth | RouteFlags.IsTransient;

            Assert.IsTrue(_route.HasFlag(RouteFlags.RequiresAuth));
            Assert.IsTrue(_route.HasFlag(RouteFlags.IsTransient));
            Assert.IsFalse(_route.HasFlag(RouteFlags.ClearHistory));
        }

        [Test]
        public void HasFlag_None_IsAlwaysFalse()
        {
            _route.Flags = RouteFlags.None;
            Assert.IsFalse(_route.HasFlag(RouteFlags.RequiresAuth));
        }

        [Test]
        public void IsValid_EmptyRouteId_IsInvalid()
        {
            _route.RouteId = "  ";
            _route.Type = NavType.Popup;
            Assert.IsFalse(_route.IsValid(out var reason));
            StringAssert.Contains("RouteId", reason);
        }

        [Test]
        public void IsValid_ScenePageWithoutScene_IsInvalid()
        {
            _route.RouteId = "Lobby";
            _route.Type = NavType.ScenePage;
            _route.TargetScene = string.Empty;

            Assert.IsFalse(_route.IsValid(out var reason));
            StringAssert.Contains("TargetScene", reason);
        }

        [Test]
        public void IsValid_ScenePageWithScene_IsValid()
        {
            _route.RouteId = "Lobby";
            _route.Type = NavType.ScenePage;
            _route.TargetScene = "LobbyScene";

            Assert.IsTrue(_route.IsValid(out var reason));
            Assert.IsNull(reason);
        }

        [Test]
        public void IsValid_PopupWithoutPrefab_IsInvalid()
        {
            _route.RouteId = "Confirm";
            _route.Type = NavType.Popup;
            _route.Prefab = null;

            Assert.IsFalse(_route.IsValid(out var reason));
            StringAssert.Contains("Prefab", reason);
        }
    }
}
