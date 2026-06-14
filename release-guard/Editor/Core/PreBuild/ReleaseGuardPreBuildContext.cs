using System.Collections.Generic;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.Runtime;
using ReleaseGuard.Editor.Util;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Core.PreBuild
{
    /// <summary>
    /// Everything a component needs for one pre-build run, plus the API to report findings.
    /// Components call <see cref="Error"/>/<see cref="Warning"/>/<see cref="Info"/> rather than
    /// touching lists directly; the issue is automatically attributed to the running component.
    /// </summary>
    public sealed class ReleaseGuardPreBuildContext
    {
        private readonly List<ReleaseIssue> _issues;
        private readonly AssetExclusionMatcher _exclusions;
        private string _currentComponentId;

        public ReleaseGuardSettings Settings { get; }

        // ReSharper disable once MemberCanBePrivate.Global
        public ReleaseGuardConfiguration Configuration { get; }
        public ReleaseGuardLogger Logger { get; }

        /// <summary>The build being produced. <c>null</c> for a manual run from the window.</summary>
        public BuildReport BuildReport { get; }

        /// <summary>Target platform of this run (from the build, or the active editor target).</summary>
        public BuildTarget BuildTarget { get; }

        public bool IsDevelopmentBuild => Configuration.IsDevelopmentBuild;

        /// <summary>
        /// True if this pre-build run is targeting <paramref name="target"/>. Convenience helper
        /// for platform-specific early returns inside component <c>Evaluate</c> methods.
        /// </summary>
        public bool IsForPlatform(BuildTarget target) => BuildTarget == target;

        public ReleaseGuardPreBuildContext(
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
            _exclusions = new AssetExclusionMatcher(settings.components.excludedAssetPaths.patterns);
        }

        internal IReadOnlyList<ReleaseIssue> Issues => _issues;

        /// <summary>Called by the executor before each component so reports are attributed correctly.</summary>
        internal void BeginComponent(ReleaseGuardComponent component) => _currentComponentId = component.Id;

        /// <summary>
        /// Record a finding. The asset path is normalized to a canonical Unity asset path. If it
        /// matches the settings' asset-exclusion list, the issue is dropped here -- this is the
        /// single canonical enforcement point, so exclusion applies uniformly to every component
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
                    $"Issue from '{_currentComponentId}' for '{normalized}' suppressed by the asset-exclusion list.");
                return;
            }

            _issues.Add(new ReleaseIssue(_currentComponentId, severity, message, normalized, fixHint));
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
        /// the global <c>AdvisorySuppressionStore</c> (profile-independent).
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

            _issues.Add(new ReleaseIssue(_currentComponentId, severity, message, null, fixHint, suppressId));
        }
    }
}