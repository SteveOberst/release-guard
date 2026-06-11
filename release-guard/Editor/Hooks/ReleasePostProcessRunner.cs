using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.PostProcessing;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Hooks
{
    /// <summary>
    /// Post-build hook that runs the post-processor pipeline after a successful build.
    ///
    /// Runs at <c>callbackOrder = int.MaxValue</c> so it executes last, after all Unity
    /// post-processing and after the transformer pipeline (which runs at 0). Post-processors
    /// therefore always see the fully written and transformed build output.
    ///
    /// A post-processor failure is logged but never throws <see cref="BuildFailedException"/> --
    /// the build has already succeeded and post-processor errors are surfaced in the Console.
    ///
    /// Respects the same enabled/skipOnDevelopmentBuilds gate as the audit preprocessor.
    /// </summary>
    internal sealed class ReleasePostProcessRunner : IPostprocessBuildWithReport
    {
        public int callbackOrder => int.MaxValue;

        public void OnPostprocessBuild(BuildReport report)
        {
            var env = DI.Resolve<ReleaseGuardEnvironment>();
            var configuration = env.ResolveConfiguration(report);
            var logger = env.Logger;

            if (!configuration.Enabled)
            {
                logger.LogVerbose(configuration.IsDevelopmentBuild
                    ? "Post-processor pipeline skipped: development build."
                    : "Post-processor pipeline skipped: disabled in settings or by the active Build Profile override.");
                return;
            }

            // ReSharper disable once UnusedVariable
            var result = env.PostProcessPipeline.RunForBuild(report);
        }
    }
}