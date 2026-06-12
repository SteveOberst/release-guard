namespace ReleaseGuard.Editor.Core.Config
{
    public sealed class SectionHeaderComponent : SettingsComponent
    {
        public string Header { get; init; }
        public override void Render(SettingsRenderer renderer) => renderer.Section(Header);
    }
}
