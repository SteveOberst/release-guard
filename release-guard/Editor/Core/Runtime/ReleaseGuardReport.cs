using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Core.Audit;

namespace ReleaseGuard.Editor.Core.Runtime
{
    /// <summary>The result of one audit run: every issue, plus convenience aggregates.</summary>
    public sealed class ReleaseGuardReport
    {
        public IReadOnlyList<ReleaseIssue> Issues { get; }

        /// <summary>
        /// Every auditor that was discovered and evaluated in this run (in execution order).
        /// Includes auditors that found no issues. Useful for verifying a custom auditor is
        /// being picked up, without requiring verbose logging.
        /// </summary>
        public IReadOnlyList<ReleaseAuditor> DiscoveredAuditors { get; }

        public ReleaseGuardReport(IReadOnlyList<ReleaseIssue> issues,
            IReadOnlyList<ReleaseAuditor> discoveredAuditors = null)
        {
            Issues = issues ?? new List<ReleaseIssue>();
            DiscoveredAuditors = discoveredAuditors ?? new List<ReleaseAuditor>();
        }

        public int InfoCount => Issues.Count(i => i.Severity == ReleaseIssueSeverity.Info);
        public int WarningCount => Issues.Count(i => i.Severity == ReleaseIssueSeverity.Warning);
        public int ErrorCount => Issues.Count(i => i.Severity == ReleaseIssueSeverity.Error);

        public bool HasIssues => Issues.Count > 0;
        public bool HasErrors => ErrorCount > 0;

        public ReleaseIssueSeverity HighestSeverity =>
            Issues.Count == 0 ? ReleaseIssueSeverity.Info : Issues.Max(i => i.Severity);

        /// <summary>True if any issue is at or above <paramref name="failureThreshold"/>.</summary>
        public bool ShouldFailBuild(ReleaseIssueSeverity failureThreshold) =>
            Issues.Any(i => i.Severity >= failureThreshold);

        public int CountAtOrAbove(ReleaseIssueSeverity threshold) =>
            Issues.Count(i => i.Severity >= threshold);
    }
}