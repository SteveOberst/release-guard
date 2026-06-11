using System;
using System.Collections.Generic;
using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Util;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Core.Runtime
{
    /// <summary>
    /// Discovers and runs every auditor, collecting their findings into a single report.
    /// One auditor throwing never aborts the run - it is logged and turned into a Warning.
    /// </summary>
    public sealed class ReleaseGuardExecutor
    {
        private readonly ReleaseGuardEnvironment _releaseGuard;

        internal ReleaseGuardExecutor(ReleaseGuardEnvironment releaseGuard)
        {
            _releaseGuard = releaseGuard;
        }

        /// <summary>Run during a build (has a <see cref="BuildReport"/>).</summary>
        public ReleaseGuardReport RunForBuild(
            ReleaseGuardConfiguration configuration,
            BuildReport report)
            => Run(configuration, report, report.summary.platform);

        /// <summary>Run manually from the editor window (no build in progress).</summary>
        public ReleaseGuardReport RunInEditor()
        {
            var configuration = _releaseGuard.ResolveConfiguration(report: null);
            return Run(configuration, report: null, EditorUserBuildSettings.activeBuildTarget);
        }

        private ReleaseGuardReport Run(
            ReleaseGuardConfiguration configuration,
            BuildReport report,
            BuildTarget buildTarget)
        {
            var settings = _releaseGuard.Settings;
            var logger = _releaseGuard.Logger;
            var issues = new List<ReleaseIssue>();
            var context = new ReleaseAuditContext(settings, configuration, logger, report, buildTarget, issues);

            var auditors = _releaseGuard.Registries.Auditors.Items;
            logger.LogVerbose($"Discovered {auditors.Count} auditor(s) for target {buildTarget}.");

            MemberInfoUnityPathResolver.BeginAudit();
            try
            {
                foreach (var auditor in auditors)
                {
                    try
                    {
                        if (!auditor.ShouldRun(context))
                        {
                            logger.LogVerbose($"Auditor '{auditor.Id}' opted out (ShouldRun returned false).");
                            continue;
                        }

                        context.BeginAuditor(auditor);
                        auditor.Evaluate(context);
                    }
                    catch (Exception e)
                    {
                        logger.LogException($"Auditor '{auditor.Id}' threw and was skipped.", e);
                        issues.Add(new ReleaseIssue(
                            auditor.Id,
                            ReleaseIssueSeverity.Warning,
                            $"Auditor '{auditor.Id}' failed to run: {e.Message}",
                            null,
                            "This is a bug in the auditor itself. See the Console for the full stack trace."));
                    }
                }
            }
            finally
            {
                MemberInfoUnityPathResolver.EndAudit();
            }

            return new ReleaseGuardReport(issues, auditors);
        }
    }
}