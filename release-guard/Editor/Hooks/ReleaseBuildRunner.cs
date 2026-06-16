using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.Runtime;
using ReleaseGuard.Editor.Core.Build;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Hooks
{
    /// <summary>
    /// Post-build hook that dispatches the build event after a successful build.
    /// Runs at <c>callbackOrder = 0</c>, before the post-build cleanup event.
    /// A component failure here is logged but never throws <see cref="BuildFailedException"/>.
    /// </summary>
    internal sealed class ReleaseBuildRunner : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            var env = ReleaseGuardDI.Resolve<ReleaseGuardEnvironment>();
            var logger = env.Logger;

            var buildEnv = BuildEnvironmentDetector.Detect();
            var profileSettings = ProfileSettingsResolver.ResolveForBuild(
                env.Registry.profiles, report, buildEnv);
            var configuration = ReleaseGuardConfiguration.Resolve(profileSettings, report);

            if (!configuration.Enabled)
            {
                logger.LogVerbose("Build event skipped: disabled in the active profile's settings.");
                return;
            }

            env.Pipeline.Dispatch(ReleaseGuardBuildEvent.ForBuild(profileSettings, report));
        }
    }
}