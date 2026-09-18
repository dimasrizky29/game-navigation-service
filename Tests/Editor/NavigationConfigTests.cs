using NUnit.Framework;
using UnityEngine;
using YatechStudio.Navigation.Domain;
using YatechStudio.Navigation.Infrastructure;

namespace YatechStudio.Navigation.Tests
{
    public class NavigationConfigTests
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
            Object.DestroyImmediate(_config);
        }

        private RouteDefinition Scene(string id, string scene)
        {
            var route = ScriptableObject.CreateInstance<RouteDefinition>();
            route.RouteId = id;
            route.Type = NavType.ScenePage;
            route.TargetScene = scene;
            return route;
        }

        [Test]
        public void TryGet_RegisteredRoute_ReturnsDefinition()
        {
            var route = Scene("Lobby", "LobbyScene");
            _config.Routes.Add(route);
            _config.InvalidateCache();

            Assert.IsTrue(_config.TryGet("Lobby", out var found));
            Assert.AreSame(route, found);
        }

        [Test]
        public void TryGet_UnknownRoute_ReturnsFalse()
        {
            _config.Routes.Add(Scene("Lobby", "LobbyScene"));
            _config.InvalidateCache();

            Assert.IsFalse(_config.TryGet("Missing", out var found));
            Assert.IsNull(found);
        }

        [Test]
        public void TryGet_NullOrEmptyId_ReturnsFalse()
        {
            _config.Routes.Add(Scene("Lobby", "LobbyScene"));
            _config.InvalidateCache();

            Assert.IsFalse(_config.TryGet(null, out _));
            Assert.IsFalse(_config.TryGet(string.Empty, out _));
        }

        [Test]
        public void Lookup_DuplicateRouteIds_FirstEntryWins()
        {
            var first = Scene("Shop", "ShopScene");
            var second = Scene("Shop", "ShopScene2");
            _config.Routes.Add(first);
            _config.Routes.Add(second);
            _config.InvalidateCache();

            Assert.IsTrue(_config.TryGet("Shop", out var found));
            Assert.AreSame(first, found);
            Assert.AreEqual(1, _config.Lookup.Count);
        }

        [Test]
        public void Lookup_IgnoresEntriesWithoutRouteId()
        {
            _config.Routes.Add(Scene(string.Empty, "Scene"));
            _config.Routes.Add(null);
            _config.Routes.Add(Scene("Lobby", "LobbyScene"));
            _config.InvalidateCache();

            Assert.AreEqual(1, _config.Lookup.Count);
            Assert.IsTrue(_config.TryGet("Lobby", out _));
        }

        [Test]
        public void ToDictionary_ReturnsSameContentAsLookup()
        {
            _config.Routes.Add(Scene("Lobby", "LobbyScene"));
            _config.Routes.Add(Scene("Shop", "ShopScene"));
            _config.InvalidateCache();

            var dict = _config.ToDictionary();

            Assert.AreEqual(2, dict.Count);
            Assert.IsTrue(dict.ContainsKey("Lobby"));
            Assert.IsTrue(dict.ContainsKey("Shop"));
        }

        [Test]
        public void Lookup_IsCachedUntilInvalidated()
        {
            _config.Routes.Add(Scene("Lobby", "LobbyScene"));
            _config.InvalidateCache();
            var first = _config.Lookup;

            _config.Routes.Add(Scene("Shop", "ShopScene"));

            Assert.AreSame(first, _config.Lookup, "Lookup should be cached while Routes changes are uncommitted.");

            _config.InvalidateCache();
            Assert.AreNotSame(first, _config.Lookup);
            Assert.AreEqual(2, _config.Lookup.Count);
        }
    }
}
