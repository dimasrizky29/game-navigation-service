using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using YatechStudio.Navigation.Domain;
using YatechStudio.Navigation.Editor;
using YatechStudio.Navigation.Infrastructure;

namespace YatechStudio.Navigation.Tests
{
    public class RouteEditWindowTests
    {
        [Test]
        public void OpenCreate_BindsConfig_AndStartsInCreateMode()
        {
            var config = ScriptableObject.CreateInstance<NavigationConfig>();
            config.Routes.Clear();
            var window = ScriptableObject.CreateInstance<RouteEditWindow>();

            window.OpenCreate(config);

            Assert.AreSame(config, window.Config);
            Assert.IsTrue(window.IsCreateMode);
            Assert.IsEmpty(window.RouteId);

            Object.DestroyImmediate(window);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void OpenEdit_PrefillsFromExistingRoute()
        {
            var config = ScriptableObject.CreateInstance<NavigationConfig>();
            config.Routes.Clear();
            var route = ScriptableObject.CreateInstance<RouteDefinition>();
            route.RouteId = "Shop";
            route.Type = NavType.Window;
            route.Flags = RouteFlags.RequiresAuth;
            config.Routes.Add(route);

            var window = ScriptableObject.CreateInstance<RouteEditWindow>();
            window.OpenEdit(config, route);

            Assert.IsFalse(window.IsCreateMode);
            Assert.AreEqual("Shop", window.RouteId);
            Assert.AreEqual(NavType.Window, window.Type);
            Assert.IsTrue(window.Flags.HasFlag(RouteFlags.RequiresAuth));

            Object.DestroyImmediate(window);
            Object.DestroyImmediate(route);
            Object.DestroyImmediate(config);
        }
    }
}