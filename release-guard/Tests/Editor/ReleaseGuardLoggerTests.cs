using NUnit.Framework;
using ReleaseGuard;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class ReleaseGuardLoggerTests
    {
        [Test]
        public void LogIssue_Formats_Asset_And_FixHint()
        {
            var issue = new ReleaseIssue(
                "auditor",
                ReleaseIssueSeverity.Warning,
                "Something went wrong.",
                "Assets/Test.cs",
                "Fix it.");

            LogAssert.Expect(LogType.Warning, "[ReleaseGuard] [Warning] Something went wrong.\n  Asset: Assets/Test.cs\n  Fix: Fix it.");

            new ReleaseGuardLogger(false).LogIssue(issue);
        }

        [Test]
        public void LogReport_Logs_Summary_And_BuildBlocking_Message()
        {
            var report = new ReleaseGuardReport(new[]
            {
                new ReleaseIssue("auditor", ReleaseIssueSeverity.Warning, "Warning message"),
                new ReleaseIssue("auditor", ReleaseIssueSeverity.Error, "Error message"),
            });

            LogAssert.Expect(LogType.Warning, "[ReleaseGuard] [Warning] Warning message");
            LogAssert.Expect(LogType.Error, "[ReleaseGuard] [Error] Error message");
            LogAssert.Expect(LogType.Log, "[ReleaseGuard] 1 error(s), 1 warning(s), 0 info.");
            LogAssert.Expect(LogType.Error, "[ReleaseGuard] 1 issue(s) at or above the failure threshold (Error); the build will be blocked.");

            new ReleaseGuardLogger(false).LogReport(report, ReleaseIssueSeverity.Error);
        }

        [Test]
        public void LogVerbose_Respects_Verbose_Flag()
        {
            LogAssert.Expect(LogType.Log, "[ReleaseGuard] Verbose message");
            new ReleaseGuardLogger(true).LogVerbose("Verbose message");

            new ReleaseGuardLogger(false).LogVerbose("Hidden message");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
