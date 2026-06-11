using ReleaseGuard.Editor.Config;
using UnityEditor;
// The DI namespace and the DI class share the same name, which would be ambiguous inside
// ReleaseGuard.Editor.Core.* — use an alias to make the static class unambiguous here.
using RelGuardDI = ReleaseGuard.Editor.Core.DI.DI;

namespace ReleaseGuard.Editor.Core.Runtime
{
    /// <summary>Unity Editor load hook for Release Guard runtime initialization.</summary>
    [InitializeOnLoad]
    internal static class ReleaseGuardStartup
    {
        static ReleaseGuardStartup()
        {
            // Dispose the container when the domain is about to reload so IDisposable singletons
            // are cleaned up deterministically.
            AssemblyReloadEvents.beforeAssemblyReload += RelGuardDI.Clear;

            // Initialize synchronously. Assembly dependency ordering guarantees this static ctor
            // runs before any [InitializeOnLoad] in assemblies that reference ReleaseGuard.Editor,
            // so the environment is fully initialized by the time plugin bootstrappers run.
            Reload();
        }

        /// <summary>
        /// Reinitialize Release Guard immediately (e.g. from the Settings "Reload" button or
        /// after dismissing an advisory). Replaces the environment registered in the DI container.
        /// </summary>
        public static void Reload()
        {
            var environment = new ReleaseGuardEnvironment();
            RelGuardDI.Configure(c => c.RegisterInstance(environment));
            environment.Initialize(ReleaseGuardSettings.LoadOrCreate());
        }
    }
}
