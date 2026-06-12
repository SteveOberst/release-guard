namespace ReleaseGuard.Editor.Core.Config
{
    /// <summary>
    /// Wraps a <see cref="SettingsComponentReader"/> and exposes it for use by
    /// <see cref="SettingsRenderer"/>. Construct via <see cref="SettingsRenderer"/> --
    /// direct construction is only needed for plugin settings readers.
    /// </summary>
    public sealed class SettingsComponentRenderer
    {
        public SettingsComponentReader ComponentReader { get; }

        public SettingsComponentRenderer()
        {
            ComponentReader = new SettingsComponentReader();
            BuiltinComponents.RegisterAll(ComponentReader);
        }
    }
}
