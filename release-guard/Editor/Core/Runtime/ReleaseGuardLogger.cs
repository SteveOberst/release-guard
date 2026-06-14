using System;
using UnityEngine;

namespace ReleaseGuard.Editor.Core.Runtime
{
    /// <summary>
    /// Thin wrapper over <see cref="UnityEngine.Debug"/> that prefixes messages and routes them
    /// to the right Console channel by severity. Verbose diagnostics are gated by settings.
    /// </summary>
    // ReSharper disable MemberCanBePrivate.Global
    // ReSharper disable MemberCanBeMadeStatic.Global
    public sealed class ReleaseGuardLogger
    {
        private const string Prefix = "[ReleaseGuard]";

        private readonly bool _verbose;

        public ReleaseGuardLogger(bool verbose) => _verbose = verbose;

        public void LogReport(ReleaseGuardPreBuildReport report, ReleaseIssueSeverity failureThreshold)
        {
            if (!report.HasIssues)
            {
                Log("No issues found - release checks passed.");
                return;
            }

            foreach (var issue in report.Issues)
                LogIssue(issue);

            Log($"{report.ErrorCount} error(s), {report.WarningCount} warning(s), {report.InfoCount} info.");

            if (report.ShouldFailBuild(failureThreshold))
            {
                LogError(
                    $"{report.CountAtOrAbove(failureThreshold)} issue(s) at or above the failure " +
                    $"threshold ({failureThreshold}); the build will be blocked.");
            }
        }

        public void LogIssue(ReleaseIssue issue)
        {
            var message = $"{Prefix} [{issue.Severity}] {issue.Message}";
            if (!string.IsNullOrEmpty(issue.AssetPath))
                message += $"\n  Asset: {issue.AssetPath}";
            if (!string.IsNullOrEmpty(issue.FixHint))
                message += $"\n  Fix: {issue.FixHint}";

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (issue.Severity)
            {
                case ReleaseIssueSeverity.Error: Debug.LogError(message); break;
                case ReleaseIssueSeverity.Warning: Debug.LogWarning(message); break;
                default: Debug.Log(message); break;
            }
        }

        public void Log(string message) => Debug.Log($"{Prefix} {message}");
        public void LogWarning(string message) => Debug.LogWarning($"{Prefix} {message}");
        public void LogError(string message) => Debug.LogError($"{Prefix} {message}");

        public void LogVerbose(string message)
        {
            if (_verbose) Debug.Log($"{Prefix} {message}");
        }

        public void LogException(string message, Exception exception)
        {
            Debug.LogError($"{Prefix} {message}");
            Debug.LogException(exception);
        }
    }
    // ReSharper enable MemberCanBePrivate.Global
    // ReSharper enable MemberCanBeMadeStatic.Global
}