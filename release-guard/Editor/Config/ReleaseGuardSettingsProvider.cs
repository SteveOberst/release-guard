using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Core.Config;
using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Plugins;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;

namespace ReleaseGuard.Editor.Config
{
    /// <summary>
    /// Registers the Release Guard pages in Project Settings.
    ///
    /// Page tree and all leaf rendering are driven entirely by attributes on
    /// <see cref="ReleaseGuardSettings"/> and its sub-objects:
    /// <list type="bullet">
    /// <item><see cref="SettingsPageAttribute"/> — which sub-objects become pages and their order.</item>
    /// <item><see cref="SettingsSectionAttribute"/> — section headings within a page.</item>
    /// <item><see cref="SettingsStatusAttribute"/> — status text on the root overview page.</item>
    /// <item><see cref="SettingsActionAttribute"/> — action buttons on the root overview page.</item>
    /// <item><see cref="SettingsConditionalWarningAttribute"/> — warning boxes on bool fields.</item>
    /// <item><see cref="SettingsExclusionPreviewAttribute"/> — live asset preview on pattern lists.</item>
    /// </list>
    ///
    /// The only registration logic here that isn't attribute-driven is the plugin sub-pages,
    /// which are generated dynamically from runtime-discovered plugins.
    /// </summary>
    internal static class ReleaseGuardSettingsProvider
    {
        private const string RootPath = "Project/Release Guard";

        private static readonly SettingsRenderer Renderer = new ReleaseGuardSettingsRenderer();

        [SettingsProviderGroup]
        public static SettingsProvider[] CreateAll()
        {
            var settings = ReleaseGuardSettings.LoadOrCreate();
            return Renderer.CreateProviders(RootPath, settings)
                .Concat(CreatePluginSubPages())
                .ToArray();
        }

        private static IEnumerable<SettingsProvider> CreatePluginSubPages()
        {
            foreach (var plugin in DI.Resolve<ReleaseGuardEnvironment>().Plugins)
            {
                if (plugin.SettingsType == null)
                    continue;

                var captured = plugin;
                yield return new SettingsProvider(
                    $"{RootPath}/5 Plugins/{plugin.DisplayName}",
                    SettingsScope.Project)
                {
                    label = captured.DisplayName,
                    guiHandler = _ =>
                    {
                        var pluginSettings = PluginSettingsRegistry.LoadOrCreate(
                            captured.PluginId, captured.SettingsType);
                        if (pluginSettings != null)
                            Renderer.DrawPaddedContent(() => PluginSettingsPageRenderer.Draw(pluginSettings));
                    }
                };
            }
        }

        private sealed class ReleaseGuardSettingsRenderer : SettingsRenderer
        {
            protected override System.Collections.Generic.HashSet<string> CreateOverviewKeywords() =>
                Keywords("release", "guard", "overview", "status", "audit", "build");
        }
    }
}
