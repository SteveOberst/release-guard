using System;
using System.Collections.Generic;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Runtime;

namespace ReleaseGuard.Editor.Core.Plugins
{
    internal sealed class PluginLoader
    {
        private readonly ReleaseGuardEnvironment _environment;
        private readonly ReleaseGuardSettings _settings;
        private readonly ReleaseGuardLogger _logger;
        private readonly TypeCacheActivator _activator;

        public PluginLoader(ReleaseGuardEnvironment environment, TypeCacheActivator activator)
        {
            _environment = environment;
            _activator = activator;
            _logger = environment.Logger;
            _settings = environment.Settings;
        }

        public IReadOnlyList<ReleaseGuardPlugin> Load()
        {
            if (!_settings.plugins.autoDiscoverPlugins)
                return Array.Empty<ReleaseGuardPlugin>();

            var plugins = new List<ReleaseGuardPlugin>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (plugin, typeName) in _activator.CreateDerived<ReleaseGuardPlugin>("plugin"))
            {
                var id = plugin.PluginId;
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning($"Plugin '{typeName}' returned an empty PluginId; skipped.");
                    continue;
                }

                if (_settings.IsPluginDisabled(id))
                {
                    _logger.LogVerbose($"Plugin '{id}' is disabled in settings; skipping.");
                    continue;
                }

                if (!seenIds.Add(id))
                {
                    _logger.LogVerbose($"Duplicate plugin id '{id}' from '{typeName}' ignored.");
                    continue;
                }

                WireSettings(plugin);
                plugins.Add(plugin);
            }

            plugins.Sort((a, b) =>
                string.Compare(a.PluginId, b.PluginId, StringComparison.OrdinalIgnoreCase));
            return plugins;
        }

        public void Register()
        {
            foreach (var plugin in _environment.Plugins)
            {
                try
                {
                    plugin.Register(new PluginRegistrationContext(_environment));
                }
                catch (Exception e)
                {
                    _logger.LogException(
                        $"Plugin '{plugin.PluginId}' ({plugin.GetType().FullName}) threw during Register().",
                        e);
                }
            }
        }

        internal static void WireSettings(ReleaseGuardPlugin plugin)
        {
            if (plugin.SettingsType is null)
                return;

            var settings = PluginSettingsRegistry.LoadOrCreate(plugin.PluginId, plugin.SettingsType);
            plugin.SetSettings(settings);
        }
    }
}
