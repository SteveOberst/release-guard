using System.Reflection;
using UnityEditor;

namespace ReleaseGuard.Editor.Core.Config
{
    public abstract class SerializedFieldComponent : SettingsComponent
    {
        public    FieldInfo          FieldInfo { get; init; }
        internal  SerializedProperty Property  { get; set; }

        public override void Render(SettingsRenderer renderer) => DrawValue(renderer);
        protected abstract void DrawValue(SettingsRenderer renderer);
    }
}
