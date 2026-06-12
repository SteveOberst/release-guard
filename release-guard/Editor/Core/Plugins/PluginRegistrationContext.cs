using ReleaseGuard.Editor.Core.Runtime;

namespace ReleaseGuard.Editor.Core.Plugins
{
    /// <summary>
    /// Passed to <see cref="ReleaseGuardPlugin.Register"/> during initialization.
    /// Plugins register contributions into <see cref="ReleaseGuard"/>.<see cref="ReleaseGuardEnvironment.Registries"/>.
    /// </summary>
    public sealed class PluginRegistrationContext
    {
        internal PluginRegistrationContext(ReleaseGuardEnvironment releaseGuard)
        {
            ReleaseGuard = releaseGuard;
        }

        public ReleaseGuardEnvironment ReleaseGuard { get; }
    }
}