using UnityEditor;
using UnityEngine;
using YatechStudio.Navigation.Infrastructure;

namespace YatechStudio.Navigation.Editor
{
    /// <summary>
    /// Adds "Open Route Manager" and "+ Add Route" buttons to the NavigationConfig
    /// inspector, plus a live validity summary footer.
    /// </summary>
    [CustomEditor(typeof(NavigationConfig))]
    public class NavigationConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var config = (NavigationConfig)target;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Route Manager", GUILayout.Height(24f)))
                {
                    var window = EditorWindow.GetWindow<RouteManagerWindow>();
                    window.Open(config);
                }
                if (GUILayout.Button("+ Add Route", GUILayout.Height(24f)))
                {
                    var win = EditorWindow.GetWindow<RouteEditWindow>("Add Route");
                    win.OpenCreate(config);
                }
            }

            EditorGUILayout.Space();

            // Default inspector draws Routes list + all config fields.
            DrawDefaultInspector();

            EditorGUILayout.Space();
            DrawValiditySummary(config);
        }

        private static void DrawValiditySummary(NavigationConfig config)
        {
            var valid = 0;
            var invalid = 0;
            foreach (var route in config.Routes)
            {
                if (route == null) { invalid++; continue; }
                if (route.IsValid(out _)) valid++; else invalid++;
            }

            if (invalid == 0)
            {
                EditorGUILayout.HelpBox($"All {valid} routes valid.", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                $"{invalid} of {valid + invalid} routes are incomplete. " +
                "Open the Route Manager and look for the warning icon.",
                MessageType.Warning);
        }
    }
}