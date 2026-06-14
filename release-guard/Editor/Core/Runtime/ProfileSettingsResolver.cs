using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Build;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Core.Runtime
{
    /// <summary>
    /// Resolves which <see cref="ReleaseGuardSettings"/> asset to use for a given build by walking
    /// the profile list top-to-bottom and returning the first profile whose activation condition
    /// matches.
    /// </summary>
    internal static class ProfileSettingsResolver
    {
        /// <summary>
        /// Resolve settings for an actual build. Throws <see cref="BuildFailedException"/> when
        /// no profile matches or the matched profile's asset is missing.
        /// </summary>
        public static ReleaseGuardSettings ResolveForBuild(
            List<ReleaseGuardProfile> profiles,
            BuildReport report,
            DetectedBuildEnvironment env)
        {
            var isDev = BuildOptionState.IsDevelopmentBuild(report.summary.options);
            var unityProfileName = BuildProfileResolver.GetActiveProfileName();

            foreach (var profile in profiles)
            {
                if (!Matches(profile.activation, isDev, env, unityProfileName)) continue;

                var settings = ProfileSettingsRegistry.TryLoad(profile.id);
                if (settings == null)
                    throw new BuildFailedException(
                        $"[ReleaseGuard] Profile '{profile.displayName}' matched this build but its " +
                        $"settings asset is missing: {ProfileSettingsRegistry.AssetPath(profile.id)}. " +
                        "Restore the asset or delete this profile in Project Settings > Release Guard > Profiles.");

                UnityEngine.Debug.Log($"[ReleaseGuard] Applying profile '{profile.displayName}'.");
                return settings;
            }

            throw new BuildFailedException(
                "[ReleaseGuard] No profile matched this build. This should not happen when the built-in " +
                "Release and Development profiles are present. " +
                "Check Project Settings > Release Guard > Profiles.");
        }

        /// <summary>
        /// Lightweight variant: returns the id of the first matching profile without loading assets.
        /// Returns <c>null</c> when no profile matches. Used by the UI mismatch warning.
        /// </summary>
        public static string ResolveProfileId(
            List<ReleaseGuardProfile> profiles,
            bool isDevelopmentBuild,
            DetectedBuildEnvironment env)
        {
            return (from profile in profiles
                where Matches(profile.activation, isDevelopmentBuild, env, unityProfileName: null)
                select profile.id).FirstOrDefault();
        }

        private static bool Matches(
            ProfileActivation activation,
            bool isDev,
            DetectedBuildEnvironment env,
            string unityProfileName)
        {
            return activation.strategy switch
            {
                ActivationStrategy.IsReleaseBuild => !isDev,
                ActivationStrategy.IsDevelopmentBuild => isDev,
                ActivationStrategy.IsCI => env.IsCI,
                ActivationStrategy.IsCIAndDevelopmentBuild => env.IsCI && isDev,
                ActivationStrategy.Always => true,
                ActivationStrategy.UnityBuildProfileNames =>
                    !string.IsNullOrEmpty(unityProfileName) &&
                    activation.unityBuildProfileNames != null &&
                    activation.unityBuildProfileNames.Contains(unityProfileName),
                _ => false
            };
        }
    }
}
