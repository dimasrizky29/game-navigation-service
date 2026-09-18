using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using YatechStudio.Navigation.Infrastructure;

namespace YatechStudio.Navigation.Editor
{
    /// <summary>
    /// Pure asset-IO helpers for <see cref="RouteDefinition"/> assets. Kept free of
    /// EditorWindow state so it can be exercised by EditMode tests.
    /// </summary>
    public static class RouteAssetUtility
    {
        /// <summary>Folder that holds this config's route assets: a sibling named <c>&lt;ConfigName&gt;Routes</c>.</summary>
        public static string GetRoutesFolder(NavigationConfig config)
        {
            var configPath = AssetDatabase.GetAssetPath(config);
            var dir = Path.GetDirectoryName(configPath);
            return Path.Combine(dir, $"{config.name}Routes").Replace('\\', '/');
        }

        /// <summary>
        /// Path for a new route asset, de-duplicated by appending a numeric suffix.
        /// Considers assets already on disk AND route ids already used by the config,
        /// so two consecutive calls for the same RouteId never collide.
        /// </summary>
        public static string UniqueAssetPath(NavigationConfig config, string routeId)
        {
            var folder = GetRoutesFolder(config);
            var safe = string.IsNullOrWhiteSpace(routeId) ? "Route" : routeId;
            foreach (var c in Path.GetInvalidFileNameChars())
                safe = safe.Replace(c, '_');

            var taken = new HashSet<string>();
            foreach (var existing in config.Routes)
            {
                if (existing == null) continue;
                var p = AssetDatabase.GetAssetPath(existing);
                if (!string.IsNullOrEmpty(p))
                    taken.Add(p.Replace('\\', '/'));
            }

            var n = 0;
            string candidate;
            do
            {
                candidate = n == 0
                    ? $"{folder}/{safe}.asset"
                    : $"{folder}/{safe}_{n}.asset";
                n++;
            }
            while (taken.Contains(candidate) || AssetExistsOnDisk(candidate));

            return candidate;
        }

        /// <summary>True if an asset already occupies this Unity-relative path.</summary>
        private static bool AssetExistsOnDisk(string unityRelativePath)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(unityRelativePath) != null)
                return true;

            var absolute = Path.GetFullPath(unityRelativePath);
            return File.Exists(absolute);
        }

        /// <summary>Saves <paramref name="route"/> to disk and appends it to the config's Routes list.</summary>
        public static void CreateRouteAsset(NavigationConfig config, RouteDefinition route)
        {
            var folder = GetRoutesFolder(config);
            if (!AssetDatabase.IsValidFolder(folder))
            {
                var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
                var name = Path.GetFileName(folder);
                if (!AssetDatabase.IsValidFolder(parent))
                    AssetDatabase.CreateFolder("Assets", "_GeneratedRoutes");
                AssetDatabase.CreateFolder(parent, name);
            }

            Undo.RecordObject(config, "Add Route");
            Undo.RecordObject(route, "Add Route");

            AssetDatabase.StartAssetEditing();
            try
            {
                AssetDatabase.CreateAsset(route, UniqueAssetPath(config, route.RouteId));
                config.Routes.Add(route);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            config.InvalidateCache();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Removes the route at <paramref name="index"/> from the config and deletes its asset file.</summary>
        public static void SafeDeleteRoute(NavigationConfig config, int index)
        {
            if (index < 0 || index >= config.Routes.Count) return;

            var route = config.Routes[index];
            Undo.RecordObject(config, "Delete Route");

            AssetDatabase.StartAssetEditing();
            try
            {
                config.Routes.RemoveAt(index);
                if (route != null)
                {
                    var path = AssetDatabase.GetAssetPath(route);
                    if (!string.IsNullOrEmpty(path))
                        AssetDatabase.DeleteAsset(path);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            config.InvalidateCache();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }
}
