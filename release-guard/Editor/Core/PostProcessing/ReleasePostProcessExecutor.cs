using System;
using System.Collections.Generic;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Core.PostProcessing
{
    /// <summary>
    /// Runs every registered post-processor after a successful build.
    /// Mirrors <see cref="ReleaseGuardExecutor"/> for the post-processor pipeline.
    ///
    /// One post-processor throwing never aborts the run -- the exception is caught, logged
    /// as an error in the result, and execution continues with the next post-processor.
    /// </summary>
    public sealed class ReleasePostProcessExecutor
    {
        private readonly ReleaseGuardEnvironment _releaseGuard;

        internal ReleasePostProcessExecutor(ReleaseGuardEnvironment releaseGuard)
        {
            _releaseGuard = releaseGuard;
        }

        public ReleasePostProcessResult RunForBuild(BuildReport report)
        {
            var settings = _releaseGuard.Settings;
            var logger = _releaseGuard.Logger;
            var log = new List<ReleasePostProcessLog>();
            var context = ReleasePostProcessContext.ForBuild(settings, report, log);
            var postProcessors = _releaseGuard.Registries.PostProcessors.Items;

            logger.LogVerbose(
                $"Registered {postProcessors.Count} post-processor(s) for target {report.summary.platform}.");

            foreach (var pp in postProcessors)
            {
                try
                {
                    if (!pp.ShouldRun(context))
                    {
                        logger.LogVerbose($"Post-processor '{pp.Id}' opted out (ShouldRun returned false).");
                        continue;
                    }

                    context.BeginPostProcessor(pp);
                    pp.PostProcess(context);
                }
                catch (Exception e)
                {
                    logger.LogException($"Post-processor '{pp.Id}' threw and was skipped.", e);
                    log.Add(new ReleasePostProcessLog(
                        pp.Id,
                        ReleasePostProcessLogLevel.Error,
                        $"Post-processor '{pp.Id}' failed: {e.Message}"));
                }
            }

            var result = new ReleasePostProcessResult(log, postProcessors);

            if (result.HasErrors)
                logger.LogWarning($"Post-process pipeline completed with {result.ErrorCount} error(s).");
            else if (result.HasWarnings)
                logger.LogWarning($"Post-process pipeline completed with {result.WarningCount} warning(s).");
            else
                logger.LogVerbose($"Post-process pipeline completed: {result.InfoCount} info entry/entries.");

            return result;
        }
    }
}