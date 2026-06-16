using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.Runtime;
using ReleaseGuard.Editor.Core.Build;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Hooks
{
    /// <summary>
    /// Post-build hook that dispatches the post-build cleanup event after a successful build.
    /// Runs at <c>callbackOrder = int.MaxValue</c> so it executes last.
    /// A component failure here is logged but never throws <see cref="BuildFailedException"/>.
    /// </summary>
    internal sealed class ReleasePostBuildRunner : IPostprocessBuildWithReport
    {
        public int callbackOrder => int.MaxValue;

        public void OnPostprocessBuild(BuildReport report)
        {
            var env = ReleaseGuardDI.Resolve<ReleaseGuardEnvironment>();
            var logger = env.Logger;

            var buildEnv = BuildEnvironmentDetector.Detect();
            var profileSettings = ProfileSettingsResolver.ResolveForBuild(env.Registry.profiles, report, buildEnv);
            var configuration = ReleaseGuardConfiguration.Resolve(profileSettings, report);

            if (!configuration.Enabled)
            {
                logger.LogVerbose("Post-build event skipped: disabled in the active profile's settings.");
                return;
            }

            env.Pipeline.Dispatch(ReleaseGuardPostBuildEvent.ForBuild(profileSettings, report));
        }
    }
}