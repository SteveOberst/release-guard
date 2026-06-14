using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.DI;
using UnityEditor;

namespace ReleaseGuard.Editor.Core.Runtime
{
    /// <summary>Unity Editor load hook for Release Guard runtime initialization.</summary>
    [InitializeOnLoad]
    internal static class ReleaseGuardStartup
    {
        static ReleaseGuardStartup()
        {
            AssemblyReloadEvents.beforeAssemblyReload += ReleaseGuardDI.Clear;

            // When the user switches editing profile, reinitialize so the environment reflects
            // the newly selected profile's settings.
            ActiveProfileState.Changed += Reload;

            Reload();
        }

        /// <summary>
        /// Reinitialize Release Guard immediately (e.g. from the Settings "Reload" button,
        /// after dismissing an advisory, or after switching the active editing profile).
        /// </summary>
        public static void Reload()
        {
            ProfileMigration.Run();

            var registry = ReleaseGuardRegistry.LoadOrCreate();
            var settings = ActiveProfileState.CurrentSettings();

            var environment = new ReleaseGuardEnvironment();
            ReleaseGuardDI.Configure(c => c.RegisterInstance(environment));
            environment.Initialize(settings);
            environment.SetRegistry(registry);
        }
    }
}