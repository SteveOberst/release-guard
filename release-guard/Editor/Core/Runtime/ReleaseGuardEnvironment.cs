using System;
using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Builtins;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.Plugins;
using ReleaseGuard.Editor.Core.PostProcessing;
using ReleaseGuard.Editor.Core.Registries;
using ReleaseGuard.Editor.Core.Transforming;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Core.Runtime
{
    /// <summary>
    /// Runtime state for the current Editor domain. Created and initialized synchronously by
    /// <see cref="ReleaseGuardStartup"/>, registered in <see cref="DI.DI"/>, and shared by hooks,
    /// UI, and plugins.
    ///
    /// Plugins in dependent assemblies register via their own <c>[InitializeOnLoad]</c>:
    /// <code>
    /// DI.Resolve&lt;ReleaseGuardEnvironment&gt;().RegisterPlugin(new MyPlugin());
    /// </code>
    /// Assembly dependency order guarantees Release Guard initializes first, so the environment
    /// is fully initialized when the call arrives.
    /// </summary>
    public sealed class ReleaseGuardEnvironment
    {
        private readonly string _packageAssemblyName =
            typeof(ReleaseGuardEnvironment).Assembly.GetName().Name;

        public ReleaseGuardSettings Settings { get; private set; }
        public ReleaseGuardLogger Logger { get; private set; }
        public ReleaseGuardRegistries Registries { get; private set; }
        public IReadOnlyList<ReleaseGuardPlugin> Plugins { get; private set; } = Array.Empty<ReleaseGuardPlugin>();
        public ReleaseGuardExecutor AuditPipeline { get; private set; }
        public ReleasePostProcessExecutor PostProcessPipeline { get; private set; }
        public ReleaseTransformExecutor TransformPipeline { get; private set; }

        /// <summary>
        /// Dynamically register a plugin post-initialization. Safe to call from a dependent
        /// assembly's <c>[InitializeOnLoad]</c> — assembly dependency order guarantees this
        /// environment is fully initialized before that code runs.
        ///
        /// Returns <c>false</c> without throwing when: plugin is null, has a duplicate id,
        /// is disabled in settings, or is called before <see cref="Initialize"/> has run.
        /// </summary>
        public bool RegisterPlugin(ReleaseGuardPlugin plugin)
        {
            if (plugin == null) return false;

            if (Settings == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[ReleaseGuard] RegisterPlugin('{plugin.GetType().Name}') was called before " +
                    "initialization completed. Ensure your assembly has an explicit asmdef dependency " +
                    "on ReleaseGuard.Editor so [InitializeOnLoad] ordering is deterministic.");
                return false;
            }

            var id = plugin.PluginId;
            if (string.IsNullOrEmpty(id))
            {
                Logger.LogWarning($"Plugin '{plugin.GetType().FullName}' returned an empty PluginId; skipped.");
                return false;
            }

            if (Plugins.Any(p => string.Equals(p.PluginId, id, StringComparison.OrdinalIgnoreCase)))
            {
                Logger.LogVerbose($"Plugin '{id}' is already registered; skipping duplicate.");
                return false;
            }

            if (Settings.IsPluginDisabled(id))
            {
                Logger.LogVerbose($"Plugin '{id}' is disabled in settings; skipping.");
                return false;
            }

            PluginLoader.WireSettings(plugin);

            try
            {
                plugin.Register(new PluginRegistrationContext(this));
            }
            catch (Exception e)
            {
                Logger.LogException(
                    $"Plugin '{id}' ({plugin.GetType().FullName}) threw during Register().", e);
            }

            var list = new List<ReleaseGuardPlugin>(Plugins) { plugin };
            list.Sort((a, b) =>
                string.Compare(a.PluginId, b.PluginId, StringComparison.OrdinalIgnoreCase));
            Plugins = list;

            Logger.LogVerbose($"Plugin '{id}' registered dynamically.");
            return true;
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        public ReleaseGuardEnvironment Initialize(ReleaseGuardSettings settings)
        {
            var logger = new ReleaseGuardLogger(settings.general.verboseLogging);
            return Initialize(settings, logger);
        }

        internal ReleaseGuardEnvironment Initialize(
            ReleaseGuardSettings settings,
            ReleaseGuardLogger logger)
        {
            Settings = settings;
            Logger = logger;

            var registries = new ReleaseGuardRegistries();
            Registries = registries;

            AuditPipeline = new ReleaseGuardExecutor(this);
            PostProcessPipeline = new ReleasePostProcessExecutor(this);
            TransformPipeline = new ReleaseTransformExecutor(this);

            var typeActivator = new TypeCacheActivator(_packageAssemblyName, logger);
            var pluginLoader = new PluginLoader(this, typeActivator);

            Plugins = pluginLoader.Load();

            var registryLoader = new RegistryLoader(typeActivator, logger);
            registryLoader.Load(RegistryDefinitions(settings, registries, logger));
            pluginLoader.Register();

            logger.LogVerbose(
                $"Release Guard environment initialized: {Plugins.Count} plugin(s), " +
                $"{registries.Auditors.Items.Count} auditor(s), " +
                $"{registries.PostProcessors.Items.Count} post-processor(s), " +
                $"{registries.Transformers.Items.Count} transformer(s).");

            return this;
        }

        public ReleaseGuardConfiguration ResolveConfiguration(BuildReport report = null) =>
            ReleaseGuardConfiguration.Resolve(Settings, report);

        private static IEnumerable<IRegistryDefinition> RegistryDefinitions(
            ReleaseGuardSettings settings,
            ReleaseGuardRegistries registries,
            ReleaseGuardLogger logger)
        {
            return new IRegistryDefinition[]
            {
                new RegistryDefinition<ReleaseAuditor>(
                    registries.Auditors,
                    BuiltInAuditorRegistry.GetAll(),
                    settings.auditors.autoDiscoverAuditors,
                    settings.IsAuditorDisabled,
                    logger),
                new RegistryDefinition<ReleasePostProcessor>(
                    registries.PostProcessors,
                    BuiltInPostProcessorRegistry.GetAll(),
                    settings.postProcessors.autoDiscoverPostProcessors,
                    settings.IsPostProcessorDisabled,
                    logger),
                new RegistryDefinition<ReleaseTransformer>(
                    registries.Transformers,
                    BuiltInTransformerRegistry.GetAll(),
                    settings.transformers.autoDiscoverTransformers,
                    settings.IsTransformerDisabled,
                    logger)
            };
        }
    }
}
