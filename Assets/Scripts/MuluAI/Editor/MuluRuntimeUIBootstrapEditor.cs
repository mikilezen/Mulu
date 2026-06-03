using UnityEditor;
using UnityEngine;

namespace MuluAI.Editor
{
    [CustomEditor(typeof(MuluRuntimeUIBootstrap))]
    public class MuluRuntimeUIBootstrapEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(12f);

            MuluRuntimeUIBootstrap bootstrap = (MuluRuntimeUIBootstrap)target;

            if (GUILayout.Button("Build / Rebuild Chat UI", GUILayout.Height(36f)))
            {
                bootstrap.Build();
                EditorUtility.SetDirty(bootstrap.gameObject);
            }

            if (GUILayout.Button("Clear Generated UI", GUILayout.Height(30f)))
            {
                bootstrap.ClearGeneratedUi();
                EditorUtility.SetDirty(bootstrap.gameObject);
            }

            EditorGUILayout.HelpBox(
                "Use Build / Rebuild Chat UI in Edit Mode to create the Canvas so you can move, resize, and restyle it in the Unity scene. Press Play to use the prompt.",
                MessageType.Info);
        }
    }
}
