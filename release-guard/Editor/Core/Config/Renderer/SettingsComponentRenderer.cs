using ReleaseGuard.Editor.Core.Config.Reader;

namespace ReleaseGuard.Editor.Core.Config.Renderer
{
    /// <summary>
    ///     Wraps a <see cref="SettingsComponentReader" /> and exposes it for use by
    ///     <see cref="SettingsRenderer" />. Construct via <see cref="SettingsRenderer" /> --
    ///     direct construction is only needed for plugin settings readers.
    /// </summary>
    public sealed class SettingsComponentRenderer
    {
        public SettingsComponentRenderer()
        {
            ComponentReader = new SettingsComponentReader();
            BuiltinComponents.RegisterAll(ComponentReader);
        }

        public SettingsComponentReader ComponentReader { get; }
    }
}