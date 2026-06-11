using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.PostProcessing;
using ReleaseGuard.Editor.Core.Transforming;

namespace ReleaseGuard.Editor.Core.Plugins
{
    /// <summary>
    /// Base class for Release Guard plugins. A plugin is the primary entry point for
    /// contributing custom auditors, post-processors, and transformers from a single,
    /// identifiable entry point.
    ///
    /// <para>Create a non-abstract subclass in any Editor-included assembly -- Release Guard
    /// discovers it automatically via TypeCache and calls <see cref="Register"/> once while
    /// initializing the Editor-domain <see cref="ReleaseGuardEnvironment"/>.</para>
    ///
    /// <para><b>When to use a plugin vs. a direct subclass:</b><br/>
    /// Subclassing <see cref="ReleaseAuditor"/>, <see cref="ReleasePostProcessor"/>, or
    /// <see cref="ReleaseTransformer"/> directly is simpler for single-item contributions and
    /// is still fully supported. Use a plugin when you want to contribute multiple items from
    /// one place, attach identity metadata (author, id) for tooling visibility, or read
    /// settings inside contributed items such as <c>ShouldRun</c>.</para>
    ///
    /// <para><b>Register() contract:</b><br/>
    /// Register() is called once per environment initialization. Register contributions through
    /// <c>context.ReleaseGuard.Registries</c> and keep side effects limited to registration.</para>
    /// </summary>
    public abstract class ReleaseGuardPlugin
    {
        private ReleaseGuardPluginSettings _settings;

        /// <summary>
        /// Stable, unique identifier for this plugin. Used for logging and for disabling
        /// via <c>PluginSettings.disabledPluginIds</c>.
        /// Recommended format: reverse-domain, e.g. <c>"com.example.my-plugin"</c>.
        /// </summary>
        public abstract string PluginId { get; }

        /// <summary>Human-readable name shown in the Release Guard window's Plugins foldout.</summary>
        public abstract string DisplayName { get; }

        /// <summary>Optional author name shown in the Release Guard window's Plugins foldout.</summary>
        // ReSharper disable once VirtualMemberNeverOverridden.Global
        public virtual string Author => null;

        /// <summary>
        /// The concrete <see cref="ReleaseGuardPluginSettings"/> type for this plugin, or
        /// <c>null</c> if this plugin has no configurable settings.
        ///
        /// <para>Override and return <c>typeof(YourSettings)</c> to opt into the settings
        /// system. The framework will load or create the settings asset at
        /// <c>Assets/ReleaseGuard/Plugins/{PluginId}.asset</c> and generate a Project
        /// Settings sub-page for it automatically.</para>
        /// </summary>
        // ReSharper disable once VirtualMemberNeverOverridden.Global
        public virtual System.Type SettingsType => null;

        /// <summary>
        /// Register auditors, transformers, and other contributions.
        ///
        /// <para>Called once per environment initialization -- keep it free of side-effects
        /// beyond registering contributions in <see cref="PluginRegistrationContext.Environment"/>.</para>
        /// </summary>
        public abstract void Register(PluginRegistrationContext context);

        /// <summary>
        /// Returns the settings instance for this plugin, or null if the plugin has no
        /// settings or the asset has not been loaded yet.
        /// </summary>
        public ReleaseGuardPluginSettings GetSettings() => _settings;

        /// <summary>Typed convenience overload. Returns null if no settings or wrong type.</summary>
        public T GetSettings<T>() where T : ReleaseGuardPluginSettings => _settings as T;

        internal void SetSettings(ReleaseGuardPluginSettings settings) => _settings = settings;
    }
}