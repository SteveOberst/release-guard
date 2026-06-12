using UnityEditor;

namespace ReleaseGuard.Editor.Core.Config
{
    public sealed class PrimitiveComponent : SerializedFieldComponent
    {
        protected override void DrawValue(SettingsRenderer renderer)
            => EditorGUILayout.PropertyField(Property);
    }
}
