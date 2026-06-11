using System;
using System.Collections.Generic;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Core.Transforming
{
    /// <summary>
    /// Discovers and runs every transformer after a successful build, before post-processors.
    /// Mirrors <see cref="ReleaseGuardExecutor"/> for the transformer pipeline.
    ///
    /// One transformer throwing never aborts the run -- the exception is caught, logged as an
    /// error in the result, and execution continues with the next transformer.
    /// </summary>
    public sealed class ReleaseTransformExecutor
    {
        private readonly ReleaseGuardEnvironment _releaseGuard;

        internal ReleaseTransformExecutor(ReleaseGuardEnvironment releaseGuard)
        {
            _releaseGuard = releaseGuard;
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        public ReleaseTransformResult RunForBuild(BuildReport report)
        {
            var settings = _releaseGuard.Settings;
            var logger = _releaseGuard.Logger;
            var log = new List<ReleaseTransformLog>();
            var context = ReleaseTransformContext.ForBuild(settings, report, log);
            var transformers = _releaseGuard.Registries.Transformers.Items;

            logger.LogVerbose($"Discovered {transformers.Count} transformer(s) for target {report.summary.platform}.");

            foreach (var transformer in transformers)
            {
                try
                {
                    if (!transformer.ShouldRun(context))
                    {
                        logger.LogVerbose($"Transformer '{transformer.Id}' opted out (ShouldRun returned false).");
                        continue;
                    }

                    context.BeginTransformer(transformer);
                    transformer.Transform(context);
                }
                catch (Exception e)
                {
                    logger.LogException($"Transformer '{transformer.Id}' threw and was skipped.", e);
                    log.Add(new ReleaseTransformLog(
                        transformer.Id,
                        ReleaseTransformLogLevel.Error,
                        $"Transformer '{transformer.Id}' failed: {e.Message}"));
                }
            }

            var result = new ReleaseTransformResult(log, transformers);

            if (result.HasErrors)
                logger.LogWarning($"Transform pipeline completed with {result.ErrorCount} error(s).");
            else if (result.HasWarnings)
                logger.LogWarning($"Transform pipeline completed with {result.WarningCount} warning(s).");
            else
                logger.LogVerbose($"Transform pipeline completed: {result.InfoCount} info entry/entries.");

            return result;
        }
    }
}