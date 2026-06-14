using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Core.Components;

namespace ReleaseGuard.Editor.Core.Runtime
{
    /// <summary>The result of one pre-build run: every issue, plus convenience aggregates.</summary>
    public sealed class ReleaseGuardPreBuildReport
    {
        public IReadOnlyList<ReleaseIssue> Issues { get; }

        /// <summary>
        /// Every component with a matching pre-build subscription for this run (in execution order).
        /// Includes components that found no issues or opted out via shouldRun.
        /// </summary>
        public IReadOnlyList<ReleaseGuardComponent> RegisteredComponents { get; }

        public ReleaseGuardPreBuildReport(IReadOnlyList<ReleaseIssue> issues,
            IReadOnlyList<ReleaseGuardComponent> registeredComponents = null)
        {
            Issues = issues ?? new List<ReleaseIssue>();
            RegisteredComponents = registeredComponents ?? new List<ReleaseGuardComponent>();
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