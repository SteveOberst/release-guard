using ReleaseGuard.Editor.Core.Config.Renderer;

namespace ReleaseGuard.Editor.Core.Config.Components
{
    public abstract class SettingsComponent
    {
        public string DisplayName { get; internal set; }
        public string Tooltip { get; internal set; }
        public abstract void Render(SettingsRenderer renderer);
    }
}