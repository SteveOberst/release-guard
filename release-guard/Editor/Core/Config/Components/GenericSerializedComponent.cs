using UnityEditor;

namespace ReleaseGuard.Editor.Core.Config
{
    public sealed class GenericSerializedComponent : SerializedFieldComponent
    {
        protected override void DrawValue(SettingsRenderer renderer)
            => EditorGUILayout.PropertyField(Property, includeChildren: true);
    }
}
