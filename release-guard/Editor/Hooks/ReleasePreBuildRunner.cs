using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.Runtime;
using ReleaseGuard.Editor.Core.Build;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Hooks
{
    /// <summary>
    /// Build hook. Dispatches the pre-build event before a build and aborts with a clear message if any issue
    /// is at or above the failure threshold.
    /// The active ReleaseGuard profile is determined at build time by the build's options and
    /// environment; the profile selected in Project Settings has no effect on builds.
    /// </summary>
    internal sealed class ReleasePreBuildRunner : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var env = ReleaseGuardDI.Resolve<ReleaseGuardEnvironment>();
            var logger = env.Logger;

            var buildEnv = BuildEnvironmentDetector.Detect();
            var profileSettings = ProfileSettingsResolver.ResolveForBuild(env.Registry.profiles, report, buildEnv);
            var configuration = ReleaseGuardConfiguration.Resolve(profileSettings, report);

            if (!configuration.Enabled)
            {
                logger.LogVerbose("Skipped: disabled in the active profile's settings.");
                return;
            }

            var result = env.Pipeline.DispatchWithResult(
                ReleaseGuardPreBuildEvent.ForBuild(profileSettings, configuration, logger, report),
                releaseEvent => releaseEvent.Report);
            logger.LogReport(result, configuration.FailureThreshold);

            if (!result.ShouldFailBuild(configuration.FailureThreshold)) return;
            var blocking = result.CountAtOrAbove(configuration.FailureThreshold);
            throw new BuildFailedException(
                $"[ReleaseGuard] Build blocked: {blocking} issue(s) at or above {configuration.FailureThreshold}. " +
                "See the Console (or the Release Guard window) for details and fixes.");
        }
    }
}
