using ReleaseGuard.Editor.Core.Plugins;

namespace ReleaseGuard.Editor.Core.Config
{
    internal static class PluginSettingsPageRenderer
    {
        public static void Draw(ReleaseGuardPluginSettings settings)
        {
            if (settings is null)
                return;

            (settings.Renderer ?? SettingsRenderer.Default).Draw(settings);
        }
    }
}