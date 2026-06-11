using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Hooks
{
    /// <summary>
    /// Build hook. Runs the audit before a build and aborts with a clear message if any issue
    /// is at or above the failure threshold. Skips automatically for development builds (when
    /// configured) and for disabled Build Profiles.
    /// </summary>
    internal sealed class ReleaseBuildPreprocessor : IPreprocessBuildWithReport
    {
        // Run early so we fail fast before other preprocessors do expensive work.
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var env = DI.Resolve<ReleaseGuardEnvironment>();
            var configuration = env.ResolveConfiguration(report);
            var logger = env.Logger;

            if (!configuration.Enabled)
            {
                logger.LogVerbose(configuration.IsDevelopmentBuild
                    ? "Skipped: development build (skipOnDevelopmentBuilds is enabled)."
                    : "Skipped: disabled in settings or by the active Build Profile override.");
                return;
            }

            var result = env.AuditPipeline.RunForBuild(configuration, report);
            logger.LogReport(result, configuration.FailureThreshold);

            if (!result.ShouldFailBuild(configuration.FailureThreshold)) return;
            var blocking = result.CountAtOrAbove(configuration.FailureThreshold);
            throw new BuildFailedException(
                $"[ReleaseGuard] Build blocked: {blocking} issue(s) at or above {configuration.FailureThreshold}. " +
                "See the Console (or the Release Guard window) for details and fixes.");
        }
    }
}