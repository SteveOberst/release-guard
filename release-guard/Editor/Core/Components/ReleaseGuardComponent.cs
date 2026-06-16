using System;
using ReleaseGuard.Editor.Core.Registries;

namespace ReleaseGuard.Editor.Core.Components
{
    /// <summary>
    /// Base class for a Release Guard component. A component can participate in one or more
    /// pipeline phases by subscribing handlers through <see cref="Register"/>.
    /// </summary>
    public abstract class ReleaseGuardComponent : IReleaseGuardRegistryItem
    {
        /// <summary>Stable, unique id (snake_case). Used for logging, disabling, and de-duplication.</summary>
        public abstract string Id { get; }

        /// <summary>Human-friendly name shown in the Release Guard window.</summary>
        public virtual string DisplayName => GetType().Name;

        /// <summary>
        /// The type of this component's settings class, or null for toggle-only components.
        /// Defaults to null; components with configurable settings expose this via the generic
        /// <see cref="ReleaseGuardComponent{TSettings}"/> base.
        /// </summary>
        public virtual Type SettingsType => null;

        /// <summary>
        /// Called before <see cref="Register"/> with a delegate that looks up the component's
        /// serialized settings by id. Override to read settings into a local field.
        /// The default implementation is a no-op.
        /// </summary>
        public virtual void Initialize(Func<string, ReleaseGuardComponentSettings> settingsLookup)
        {
        }

        /// <summary>
        /// Creates a default settings instance for this component. Used when pre-populating the
        /// component toggle list. Returns a base <see cref="ReleaseGuardComponentSettings"/> by
        /// default; components that default to disabled (e.g. build manifest) override this.
        /// </summary>
        public virtual ReleaseGuardComponentSettings CreateDefaultSettings() =>
            new ReleaseGuardComponentSettings();

        /// <summary>
        /// Register this component's lifecycle handlers. Components may subscribe to any
        /// combination of pre-build, build, and post-build events.
        /// </summary>
        public abstract void Register(ReleaseGuardComponentBinder binder);
    }

    /// <summary>
    /// Base class for components that have configurable settings beyond the enabled toggle.
    /// Populates the <see cref="Settings"/> property automatically during
    /// <see cref="Initialize"/> by reading from the component toggle list.
    /// </summary>
    public abstract class ReleaseGuardComponent<TSettings> : ReleaseGuardComponent
        where TSettings : ReleaseGuardComponentSettings, new()
    {
        /// <summary>
        /// Typed settings for this component. Available after <see cref="Initialize"/> has been
        /// called. Guaranteed non-null: returns a default instance if no entry exists in the
        /// settings asset yet.
        /// </summary>
        public TSettings Settings { get; private set; }

        public override Type SettingsType => typeof(TSettings);

        public override void Initialize(Func<string, ReleaseGuardComponentSettings> settingsLookup)
        {
            Settings = settingsLookup(Id) as TSettings ?? new TSettings { componentId = Id };
        }

        public override ReleaseGuardComponentSettings CreateDefaultSettings() =>
            new TSettings { componentId = Id };
    }
}