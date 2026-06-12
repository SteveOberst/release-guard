namespace ReleaseGuard.Editor.Core.Config
{
    public abstract class SettingsComponent
    {
        public string DisplayName { get; init; }
        public string Tooltip     { get; init; }
        public abstract void Render(SettingsRenderer renderer);
    }
}
