using ReleaseGuard.Editor.Core.Config.Renderer;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Core.Config.Components
{
    public sealed class GenericSerializedComponent : SerializedFieldComponent
    {
        protected override void DrawValue(SettingsRenderer renderer)
        {
            EditorGUILayout.PropertyField(Property, new GUIContent(DisplayName, Tooltip), true);
        }
    }
}