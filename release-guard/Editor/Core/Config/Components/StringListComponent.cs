namespace ReleaseGuard.Editor.Core.Config
{
    public sealed class StringListComponent : SerializedFieldComponent
    {
        protected override void DrawValue(SettingsRenderer renderer)
            => renderer.LineListField(Property, Tooltip);
    }
}
