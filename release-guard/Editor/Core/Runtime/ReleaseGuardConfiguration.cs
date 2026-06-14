using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Build;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Core.Runtime
{
    /// <summary>
    /// The effective configuration for a single run, resolved from a specific
    /// <see cref="ReleaseGuardSettings"/> asset (the active profile for this build or manual run).
    /// </summary>
    public sealed class ReleaseGuardConfiguration
    {
        /// <summary>Whether Release Guard should run at all for this build/configuration.</summary>
        public bool Enabled { get; }

        public bool IsDevelopmentBuild { get; }

        public ReleaseIssueSeverity FailureThreshold { get; }

        /// <summary>The profile-specific settings used for this run.</summary>
        public ReleaseGuardSettings EffectiveSettings { get; }

        private ReleaseGuardConfiguration(
            bool enabled,
            bool isDevelopmentBuild,
            ReleaseIssueSeverity failureThreshold,
            ReleaseGuardSettings effectiveSettings)
        {
            Enabled = enabled;
            IsDevelopmentBuild = isDevelopmentBuild;
            FailureThreshold = failureThreshold;
            EffectiveSettings = effectiveSettings;
        }

        /// <summary>
        /// Resolve configuration from the given profile-specific settings. The caller is
        /// responsible for selecting the correct profile settings for this build.
        /// Pass <paramref name="report"/> during a build, or <c>null</c> for a manual pre-build
        /// from the editor window (development state then comes from Build Settings).
        /// </summary>
        public static ReleaseGuardConfiguration Resolve(
            ReleaseGuardSettings settings,
            BuildReport report)
        {
            var isDevelopment = report is not null
                ? BuildOptionState.IsDevelopmentBuild(report.summary.options)
                : EditorUserBuildSettings.development;

            return new ReleaseGuardConfiguration(
                enabled: settings.general.enabled,
                isDevelopmentBuild: isDevelopment,
                failureThreshold: settings.general.failureThreshold,
                effectiveSettings: settings);
        }
    }
}
