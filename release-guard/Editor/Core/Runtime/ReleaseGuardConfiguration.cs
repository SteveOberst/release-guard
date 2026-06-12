using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Util;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Core.Runtime
{
    /// <summary>
    /// The effective configuration for a single run, after resolving the development-build
    /// exemption and any Build Profile override against the raw <see cref="ReleaseGuardSettings"/>.
    /// </summary>
    public sealed class ReleaseGuardConfiguration
    {
        /// <summary>Whether Release Guard should run at all for this build/configuration.</summary>
        public bool Enabled { get; }

        public bool IsDevelopmentBuild { get; }

        /// <summary>Active Unity Build Profile name (Unity 6+), or null for classic platform settings.</summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string BuildProfileName { get; }

        public ReleaseIssueSeverity FailureThreshold { get; }

        private ReleaseGuardConfiguration(bool enabled, bool isDevelopmentBuild, string buildProfileName,
            ReleaseIssueSeverity failureThreshold)
        {
            Enabled = enabled;
            IsDevelopmentBuild = isDevelopmentBuild;
            BuildProfileName = buildProfileName;
            FailureThreshold = failureThreshold;
        }

        /// <summary>
        /// Resolve settings for a run. Pass the <paramref name="report"/> during a build, or
        /// <c>null</c> for a manual audit from the editor window (development state then comes
        /// from the Build Settings checkbox).
        /// </summary>
        public static ReleaseGuardConfiguration Resolve(ReleaseGuardSettings settings, BuildReport report)
        {
            var isDevelopment = report is not null
                ? BuildOptionState.IsDevelopmentBuild(report.summary.options)
                : EditorUserBuildSettings.development;

            var profileName = BuildProfileResolver.GetActiveProfileName();
            var enabled = settings.general.enabled;
            var threshold = settings.general.failureThreshold;

            // Per-Build-Profile override (e.g. a "Staging" profile may use a stricter threshold).
            var profileOverride = settings.GetProfileOverride(profileName);
            if (profileOverride != null)
            {
                enabled = profileOverride.enabled;
                threshold = profileOverride.failureThreshold;
            }

            // Development builds are exempt by default - release rules don't apply to debug builds.
            if (isDevelopment && settings.general.skipOnDevelopmentBuilds)
                enabled = false;

            return new ReleaseGuardConfiguration(enabled, isDevelopment, profileName, threshold);
        }
    }
}