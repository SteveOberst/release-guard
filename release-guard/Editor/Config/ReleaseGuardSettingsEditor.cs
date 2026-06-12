using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Config
{
    /// <summary>
    /// Inspector for the <see cref="ReleaseGuardSettings"/> asset. Project Settings is the
    /// canonical edit surface, while this inspector keeps the raw data behind a foldout for
    /// debugging.
    /// </summary>
    [CustomEditor(typeof(ReleaseGuardSettings))]
    internal sealed class ReleaseGuardSettingsEditor : UnityEditor.Editor
    {
        private bool _showRaw;

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Edit these settings under Edit > Project Settings > Release Guard.",
                MessageType.Info);

            if (GUILayout.Button("Open Release Guard Settings", GUILayout.Height(24)))
                SettingsService.OpenProjectSettings("Project/Release Guard");

            EditorGUILayout.Space(8);
            _showRaw = EditorGUILayout.Foldout(_showRaw, "Raw serialized data", true);
            if (_showRaw)
                DrawDefaultInspector();
        }
    }
}