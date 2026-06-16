using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class CIDevelopmentBuildCheckTests
    {
        [Test]
        public void DoesNotReport_WhenNotDevelopmentBuild()
        {
            var settings = ComponentTestHarness.CreateSettings();
            var original = EditorUserBuildSettings.development;
            try
            {
                EditorUserBuildSettings.development = false;
                var report = ComponentTestHarness.RunPreBuild(settings);
                Assert.IsFalse(report.Issues.Any(i => i.ComponentId == "ci_development_build"));
            }
            finally
            {
                EditorUserBuildSettings.development = original;
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void DoesNotReport_WhenDevelopmentBuild_ButNotInCI()
        {
            // BuildEnvironmentDetector gates on Application.isBatchMode first.
            // EditMode tests are never in batchmode, so IsCI is always false here.
            // The positive case (CI + dev build) requires a batchmode run.
            var settings = ComponentTestHarness.CreateSettings();
            var original = EditorUserBuildSettings.development;
            try
            {
                EditorUserBuildSettings.development = true;
                var report = ComponentTestHarness.RunPreBuild(settings);
                Assert.IsFalse(report.Issues.Any(i => i.ComponentId == "ci_development_build"),
                    "Should not report in editor (non-CI) even with development build enabled.");
            }
            finally
            {
                EditorUserBuildSettings.development = original;
                Object.DestroyImmediate(settings);
            }
        }
    }
}
