namespace ReleaseGuard.Editor.Core.Config
{
    public sealed class ExclusionListComponent : SerializedFieldComponent
    {
        private readonly ExclusionListRenderer _exclusionListRenderer = new();

        protected override void DrawValue(SettingsRenderer renderer)
            => _exclusionListRenderer.RenderField(Property, DisplayName, Tooltip, renderer);
    }
}
