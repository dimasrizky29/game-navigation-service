using UnityEditor;
using UnityEngine;

namespace YatechStudio.Navigation.Editor
{
    /// <summary>
    /// Shared layout constants and icons so the Route Manager window and the
    /// NavigationConfig inspector buttons draw identically.
    /// </summary>
    public static class NavigationEditorStyles
    {
        public const int Padding = 8;
        public const float ButtonWidth = 80f;
        public const float RowHeight = 22f;

        private static GUIStyle _headerStyle;
        public static GUIStyle HeaderStyle
        {
            get
            {
                if (_headerStyle == null)
                    _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };
                return _headerStyle;
            }
        }

        public static Texture WarningIcon => EditorGUIUtility.IconContent("console.warnicon.sml").image;
        public static Texture ErrorIcon => EditorGUIUtility.IconContent("console.erroricon.sml").image;
    }
}
