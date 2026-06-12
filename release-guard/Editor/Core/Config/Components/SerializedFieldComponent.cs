using System.Reflection;
using ReleaseGuard.Editor.Core.Config.Renderer;
using UnityEditor;

namespace ReleaseGuard.Editor.Core.Config.Components
{
    public abstract class SerializedFieldComponent : SettingsComponent
    {
        public FieldInfo FieldInfo { get; init; }
        internal SerializedProperty Property { get; set; }

        public override void Render(SettingsRenderer renderer)
        {
            DrawValue(renderer);
        }

        protected abstract void DrawValue(SettingsRenderer renderer);
    }
}