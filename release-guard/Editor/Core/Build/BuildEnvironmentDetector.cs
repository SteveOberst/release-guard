using System;
using UnityEngine;

namespace ReleaseGuard.Editor.Core.Build
{
    // ReSharper disable InconsistentNaming
    public enum BuildEnvironment
    {
        UnityEditor,
        CI_Unknown,
        CI_GitHub,
        CI_GitLab,
        CI_Jenkins,
        CI_CircleCI,
        CI_AzureDevOps,
        CI_TeamCity,
        CI_UnityCloudBuild,
    }
    // ReSharper enable InconsistentNaming

    public readonly struct DetectedBuildEnvironment
    {
        public BuildEnvironment Environment { get; }
        public bool IsCI => Environment != BuildEnvironment.UnityEditor;
        public bool IsBatchMode => Application.isBatchMode;

        internal DetectedBuildEnvironment(BuildEnvironment environment)
        {
            Environment = environment;
        }
    }

    public static class BuildEnvironmentDetector
    {
        public static DetectedBuildEnvironment Detect()
        {
            if (!Application.isBatchMode)
                return new DetectedBuildEnvironment(BuildEnvironment.UnityEditor);

#if UNITY_CLOUD_BUILD
            return new DetectedBuildEnvironment(BuildEnvironment.CI_UnityCloudBuild);
#else
            if (IsSet("GITHUB_ACTIONS")) return new DetectedBuildEnvironment(BuildEnvironment.CI_GitHub);
            if (IsSet("GITLAB_CI")) return new DetectedBuildEnvironment(BuildEnvironment.CI_GitLab);
            if (IsSet("JENKINS_URL")) return new DetectedBuildEnvironment(BuildEnvironment.CI_Jenkins);
            if (IsSet("CIRCLECI")) return new DetectedBuildEnvironment(BuildEnvironment.CI_CircleCI);
            if (IsSet("TF_BUILD")) return new DetectedBuildEnvironment(BuildEnvironment.CI_AzureDevOps);
            if (IsSet("TEAMCITY_VERSION")) return new DetectedBuildEnvironment(BuildEnvironment.CI_TeamCity);
            return new DetectedBuildEnvironment(BuildEnvironment.CI_Unknown);
#endif
        }

        private static bool IsSet(string varName)
        {
            var v = Environment.GetEnvironmentVariable(varName);
            return !string.IsNullOrEmpty(v);
        }
    }
}
