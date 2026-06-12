using ReleaseGuard.Editor.Core.Config.Renderer;

namespace ReleaseGuard.Editor.Core.Config.Components
{
    public sealed class SectionHeaderComponent : SettingsComponent
    {
        public string Header { get; init; }

        public override void Render(SettingsRenderer renderer)
        {
            renderer.Section(Header);
        }
    }
}