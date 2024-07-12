using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Editor
{
    public class WindowLoadLevel : EditorWindow
    {
        [MenuItem("Tools/Level Toolkit")]
        static void ShowWindow()
        {
            GetWindow(typeof(WindowLoadLevel));
        }

        void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();
        }
    }
}