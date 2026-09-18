using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using YatechStudio.Navigation.Domain;
using YatechStudio.Navigation.Infrastructure;

namespace YatechStudio.Navigation.Editor
{
    /// <summary>
    /// Modal-style form for creating or editing a single <see cref="RouteDefinition"/>.
    /// </summary>
    public class RouteEditWindow : EditorWindow
    {
        private NavigationConfig _config;
        private RouteDefinition _existing;
        private bool _create;

        private string _routeId = string.Empty;
        private NavType _type = NavType.ScenePage;
        private RouteFlags _flags = RouteFlags.None;
        private string _targetScene = string.Empty;
        private GameObject _prefab;

        public NavigationConfig Config => _config;
        public bool IsCreateMode => _create;
        public string RouteId => _routeId;
        public NavType Type => _type;
        public RouteFlags Flags => _flags;

        /// <summary>Public entry point called from RouteManagerWindow.</summary>
        public void OpenCreate(NavigationConfig config)
        {
            _config = config;
            _existing = null;
            _create = true;
            _routeId = string.Empty;
            _type = NavType.ScenePage;
            _flags = RouteFlags.None;
            _targetScene = string.Empty;
            _prefab = null;
            titleContent = new GUIContent("Add Route");
            ShowUtility();
        }

        /// <summary>Public entry point called from RouteManagerWindow.</summary>
        public void OpenEdit(NavigationConfig config, RouteDefinition route)
        {
            _config = config;
            _existing = route;
            _create = false;
            _routeId = route.RouteId;
            _type = route.Type;
            _flags = route.Flags;
            _targetScene = route.TargetScene;
            _prefab = route.Prefab != null && route.Prefab.RuntimeKeyIsValid()
                ? AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(route.Prefab.RuntimeKey.ToString()))
                : null;
            titleContent = new GUIContent($"Edit Route — {route.RouteId}");
            ShowUtility();
        }

        private void OnGUI()
        {
            if (_config == null) { Close(); return; }

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                GUILayout.Label(_create ? "Create Route" : "Edit Route",
                    NavigationEditorStyles.HeaderStyle);
                EditorGUILayout.Space();

                _routeId = EditorGUILayout.TextField("Route Id", _routeId);
                _type = (NavType)EditorGUILayout.EnumPopup("Type", _type);

                EditorGUILayout.LabelField("Flags");
                _flags = DrawFlags(_flags);

                EditorGUILayout.Space();
                if (_type == NavType.ScenePage)
                {
                    _targetScene = EditorGUILayout.TextField("Target Scene", _targetScene);
                }
                else
                {
                    var obj = EditorGUILayout.ObjectField("Prefab", _prefab, typeof(GameObject), false);
                    _prefab = obj as GameObject;
                }

                EditorGUILayout.Space();
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Cancel", GUILayout.Width(NavigationEditorStyles.ButtonWidth))) Close();
                    if (GUILayout.Button("Save", GUILayout.Width(NavigationEditorStyles.ButtonWidth))) Save();
                }
            }
        }

        private static RouteFlags DrawFlags(RouteFlags flags)
        {
            flags = ToggleFlag(flags, RouteFlags.RequiresAuth, "Requires Auth");
            flags = ToggleFlag(flags, RouteFlags.IsAdditive, "Is Additive");
            flags = ToggleFlag(flags, RouteFlags.IsTransient, "Is Transient");
            flags = ToggleFlag(flags, RouteFlags.ClearHistory, "Clear History");
            flags = ToggleFlag(flags, RouteFlags.BlockBack, "Block Back");
            return flags;
        }

        private static RouteFlags ToggleFlag(RouteFlags flags, RouteFlags flag, string label)
            => EditorGUILayout.Toggle(label, flags.HasFlag(flag)) ? flags | flag : flags & ~flag;

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_routeId))
            {
                EditorUtility.DisplayDialog("Route Manager", "Route Id is required.", "OK");
                return;
            }

            var route = _create ? CreateInstance<RouteDefinition>() : _existing;
            route.RouteId = _routeId.Trim();
            route.Type = _type;
            route.Flags = _flags;
            route.TargetScene = _type == NavType.ScenePage ? _targetScene : string.Empty;

            // Window/Popup routes need an Addressable prefab reference.
            if (_type != NavType.ScenePage && _prefab != null)
            {
                var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_prefab));
                route.Prefab = new AssetReference(guid);
            }
            else if (_type == NavType.ScenePage)
            {
                route.Prefab = null;
            }

            if (_create)
            {
                RouteAssetUtility.CreateRouteAsset(_config, route);
            }
            else
            {
                EditorUtility.SetDirty(route);
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
                _config.InvalidateCache();
            }

            if (!route.IsValid(out var reason))
                Debug.LogWarning($"[Nav] Saved route '{route.RouteId}' is incomplete: {reason}");

            Close();
        }
    }
}