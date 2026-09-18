using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using YatechStudio.Navigation.Domain;
using YatechStudio.Navigation.Infrastructure;

namespace YatechStudio.Navigation.Editor
{
    /// <summary>
    /// Window for visually managing the routes of a <see cref="NavigationConfig"/>.
    /// Open via "Window > Yatech Studio > Route Manager" or the inspector button.
    /// </summary>
    public class RouteManagerWindow : EditorWindow
    {
        private NavigationConfig _config;
        private SerializedObject _serialized;
        private SerializedProperty _routesProp;
        private Vector2 _scroll;

        public NavigationConfig Config => _config;
        public SerializedObject SerializedConfig => _serialized;
        public int RouteCount => _config != null ? _config.Routes.Count : 0;

        [MenuItem("Window/Yatech Studio/Route Manager")]
        public static void OpenMenu()
        {
            var window = GetWindow<RouteManagerWindow>("Route Manager");
            window.minSize = new Vector2(520f, 320f);
            window.Show();
        }

        /// <summary>Opens the window bound to a specific config. Public so the inspector button can call it.</summary>
        public void Open(NavigationConfig config)
        {
            _config = config;
            Rebind();
            titleContent = new GUIContent($"Route Manager — {(config != null ? config.name : "None")}");
            Repaint();
        }

        private void Rebind()
        {
            if (_config == null) return;
            _serialized = new SerializedObject(_config);
            _routesProp = _serialized.FindProperty(nameof(NavigationConfig.Routes));
        }

        private void OnSelectionChange()
        {
            if (_config == null && Selection.activeObject is NavigationConfig selected)
                Open(selected);
        }

        private void OnGUI()
        {
            if (_config == null)
            {
                DrawNoConfigState();
                return;
            }

            _serialized.Update();

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                DrawToolbar();
                EditorGUILayout.Space();
                DrawRouteTable();
                EditorGUILayout.Space();
                DrawConfigDefaults();
            }

            if (_serialized.ApplyModifiedProperties())
                _config.InvalidateCache();
        }

        private void DrawNoConfigState()
        {
            GUILayout.Label("No NavigationConfig selected.", NavigationEditorStyles.HeaderStyle);
            EditorGUILayout.Space();
            _config = (NavigationConfig)EditorGUILayout.ObjectField(
                "Config", null, typeof(NavigationConfig), false);
            if (_config != null) Open(_config);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _config = (NavigationConfig)EditorGUILayout.ObjectField(
                    "Config", _config, typeof(NavigationConfig), false);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add Route", GUILayout.Width(NavigationEditorStyles.ButtonWidth)))
                {
                    var win = GetWindow<RouteEditWindow>("Add Route");
                    win.OpenCreate(_config);
                }
                if (GUILayout.Button("Validate All", GUILayout.Width(NavigationEditorStyles.ButtonWidth)))
                    ValidateAll();
            }
        }

        private void DrawRouteTable()
        {
            if (_routesProp == null || _routesProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox(
                    "No routes yet. Click \"Add Route\" to create the first one.",
                    MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("RouteId", NavigationEditorStyles.HeaderStyle, GUILayout.Width(140));
                GUILayout.Label("Type", NavigationEditorStyles.HeaderStyle, GUILayout.Width(90));
                GUILayout.Label("Flags", NavigationEditorStyles.HeaderStyle, GUILayout.Width(160));
                GUILayout.Label("Target / Prefab", NavigationEditorStyles.HeaderStyle);
                GUILayout.Space(NavigationEditorStyles.ButtonWidth * 2 + NavigationEditorStyles.Padding);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (var i = 0; i < _routesProp.arraySize; i++)
                DrawRouteRow(i);
            EditorGUILayout.EndScrollView();
        }

        private void DrawRouteRow(int index)
        {
            var element = _routesProp.GetArrayElementAtIndex(index);
            var route = element.objectReferenceValue as RouteDefinition;
            if (route == null) return;

            using (new EditorGUILayout.HorizontalScope(GUI.skin.box, GUILayout.Height(NavigationEditorStyles.RowHeight)))
            {
                GUILayout.Label(route.RouteId, GUILayout.Width(140));
                GUILayout.Label(route.Type.ToString(), GUILayout.Width(90));
                GUILayout.Label(route.Flags == RouteFlags.None ? "—" : route.Flags.ToString(), GUILayout.Width(160));

                string target;
                if (route.Type == NavType.ScenePage)
                {
                    target = route.TargetScene;
                }
                else
                {
                    target = route.Prefab != null && route.Prefab.RuntimeKeyIsValid()
                        ? route.Prefab.editorAsset != null ? route.Prefab.editorAsset.name : "<missing>"
                        : "<unset>";
                }
                GUILayout.Label(target ?? "<unset>");

                GUILayout.FlexibleSpace();
                if (!route.IsValid(out _))
                    GUILayout.Label(NavigationEditorStyles.WarningIcon, GUILayout.Width(18));

                if (GUILayout.Button("Edit", GUILayout.Width(NavigationEditorStyles.ButtonWidth)))
                {
                    var win = GetWindow<RouteEditWindow>("Edit Route");
                    win.OpenEdit(_config, route);
                }
                if (GUILayout.Button("Del", GUILayout.Width(NavigationEditorStyles.ButtonWidth)))
                    RouteAssetUtility.SafeDeleteRoute(_config, index);
            }
        }

        private void DrawConfigDefaults()
        {
            var login = _serialized.FindProperty(nameof(NavigationConfig.LoginRouteId));
            var defaultRoute = _serialized.FindProperty(nameof(NavigationConfig.DefaultRouteId));
            var timeout = _serialized.FindProperty(nameof(NavigationConfig.SceneRegisterTimeoutSeconds));
            var delay = _serialized.FindProperty(nameof(NavigationConfig.SceneTransitionDelaySeconds));

            EditorGUILayout.PropertyField(login);
            EditorGUILayout.PropertyField(defaultRoute);
            EditorGUILayout.PropertyField(timeout);
            EditorGUILayout.PropertyField(delay);
        }

        private void ValidateAll()
        {
            var problems = new List<string>();
            foreach (var route in _config.Routes)
            {
                if (route == null) { problems.Add("A null entry exists in the Routes list."); continue; }
                if (!route.IsValid(out var reason)) problems.Add($"{route.RouteId}: {reason}");
            }

            if (problems.Count == 0)
                Debug.Log($"[Nav] All {_config.Routes.Count} routes in '{_config.name}' are valid.");
            else
                foreach (var p in problems) Debug.LogWarning($"[Nav] {_config.name}: {p}");
        }
    }
}