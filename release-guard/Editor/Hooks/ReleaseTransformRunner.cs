using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Runtime;
using ReleaseGuard.Editor.Core.Transforming;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Hooks
{
    /// <summary>
    /// Post-build hook that runs the transformer pipeline after a successful build.
    ///
    /// Runs at <c>callbackOrder = 0</c>, before the post-processor pipeline
    /// (<see cref="ReleasePostProcessRunner"/> at <c>int.MaxValue</c>). Transformers
    /// therefore see the raw build output, and post-processors see the transformed result.
    ///
    /// A transformer failure is logged but never throws <see cref="BuildFailedException"/> --
    /// the build has already succeeded and transformer errors are surfaced in the Console.
    ///
    /// Respects the same enabled/skipOnDevelopmentBuilds gate as the audit preprocessor.
    /// </summary>
    internal sealed class ReleaseTransformRunner : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            var env = DI.Resolve<ReleaseGuardEnvironment>();
            var configuration = env.ResolveConfiguration(report);
            var logger = env.Logger;

            if (!configuration.Enabled)
            {
                logger.LogVerbose(configuration.IsDevelopmentBuild
                    ? "Transformer pipeline skipped: development build."
                    : "Transformer pipeline skipped: disabled in settings or by the active Build Profile override.");
                return;
            }

            env.TransformPipeline.RunForBuild(report);
        }
    }
}