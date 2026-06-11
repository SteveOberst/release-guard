using System.Collections.Generic;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Runtime;
using ReleaseGuard.Editor.Util;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Core.Audit
{
    /// <summary>
    /// Everything an auditor needs for one run, plus the API to report findings. Auditors call
    /// <see cref="Error"/>/<see cref="Warning"/>/<see cref="Info"/> rather than touching lists
    /// directly; the issue is automatically attributed to the running auditor.
    /// </summary>
    public sealed class ReleaseAuditContext
    {
        private readonly List<ReleaseIssue> _issues;
        private readonly AssetExclusionMatcher _exclusions;
        private string _currentAuditorId;

        public ReleaseGuardSettings Settings { get; }

        // ReSharper disable once MemberCanBePrivate.Global
        public ReleaseGuardConfiguration Configuration { get; }
        public ReleaseGuardLogger Logger { get; }

        /// <summary>The build being produced. <c>null</c> for a manual audit run from the window.</summary>
        public BuildReport BuildReport { get; }

        /// <summary>Target platform of this run (from the build, or the active editor target).</summary>
        public BuildTarget BuildTarget { get; }

        public bool IsDevelopmentBuild => Configuration.IsDevelopmentBuild;

        /// <summary>
        /// True if this audit run is targeting <paramref name="target"/>. Convenience helper for
        /// use inside <see cref="ReleaseAuditor.ShouldRun"/> to restrict a check to one platform.
        /// <code>
        /// public override bool ShouldRun(ReleaseAuditContext ctx) => ctx.IsForPlatform(BuildTarget.Android);
        /// </code>
        /// </summary>
        public bool IsForPlatform(BuildTarget target) => BuildTarget == target;

        public ReleaseAuditContext(
            ReleaseGuardSettings settings,
            ReleaseGuardConfiguration configuration,
            ReleaseGuardLogger logger,
            BuildReport buildReport,
            BuildTarget buildTarget,
            List<ReleaseIssue> issues)
        {
            Settings = settings;
            Configuration = configuration;
            Logger = logger;
            BuildReport = buildReport;
            BuildTarget = buildTarget;
            _issues = issues;
            _exclusions = new AssetExclusionMatcher(settings.auditors.excludedAssetPaths.patterns);
        }

        /// <summary>Called by the executor before each auditor so reports are attributed correctly.</summary>
        internal void BeginAuditor(ReleaseAuditor auditor) => _currentAuditorId = auditor.Id;

        /// <summary>
        /// Record a finding. The asset path is normalized to a canonical Unity asset path. If it
        /// matches the settings' asset-exclusion list, the issue is dropped here -- this is the
        /// single canonical enforcement point, so exclusion applies uniformly to every auditor
        /// (built-in or custom) for both builds and manual runs. Issues with no asset path are
        /// never excluded by asset patterns.
        /// </summary>
        public void Report(ReleaseIssueSeverity severity, string message, string assetPath = null,
            string fixHint = null)
        {
            var normalized = AssetExclusionMatcher.NormalizePath(assetPath);

            if (normalized != null && _exclusions.IsExcluded(normalized))
            {
                Logger.LogVerbose(
                    $"Issue from '{_currentAuditorId}' for '{normalized}' suppressed by the asset-exclusion list.");
                return;
            }

            _issues.Add(new ReleaseIssue(_currentAuditorId, severity, message, normalized, fixHint));
        }

        // ReSharper disable once UnusedMember.Global
        public void Info(string message, string assetPath = null, string fixHint = null)
            => Report(ReleaseIssueSeverity.Info, message, assetPath, fixHint);

        public void Warning(string message, string assetPath = null, string fixHint = null)
            => Report(ReleaseIssueSeverity.Warning, message, assetPath, fixHint);

        public void Error(string message, string assetPath = null, string fixHint = null)
            => Report(ReleaseIssueSeverity.Error, message, assetPath, fixHint);

        /// <summary>
        /// Record a dismissible advisory. The issue is shown in the Release Guard window with a
        /// "Don't show again" button that writes <paramref name="suppressId"/> to
        /// <see cref="Config.AuditorSettings.suppressedAdvisoryIds"/>.
        ///
        /// If the user has already suppressed this id, the call is a no-op (the advisory is
        /// silently dropped, no issue is recorded).
        /// </summary>
        public void Advisory(
            string suppressId,
            ReleaseIssueSeverity severity,
            string message,
            string fixHint = null)
        {
            if (Settings.IsAdvisorySuppressed(suppressId))
            {
                Logger.LogVerbose($"Advisory '{suppressId}' is suppressed; skipping.");
                return;
            }

            _issues.Add(new ReleaseIssue(_currentAuditorId, severity, message, null, fixHint, suppressId));
        }
    }
}