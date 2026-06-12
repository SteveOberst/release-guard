using ReleaseGuard.Editor.Core.Config.Renderer;

namespace ReleaseGuard.Editor.Core.Config.Components
{
    public sealed class StringListComponent : SerializedFieldComponent
    {
        protected override void DrawValue(SettingsRenderer renderer)
        {
            renderer.LineListField(Property, Tooltip);
        }
    }
}