using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class DevelopmentBuildCheckTests
    {
        [Test]
        public void Reports_WhenDevelopmentBuildIsEnabled()
        {
            var settings = ComponentTestHarness.CreateSettings();
            var originalDevelopment = EditorUserBuildSettings.development;
            try
            {
                EditorUserBuildSettings.development = true;

                var report = ComponentTestHarness.RunPreBuild(settings);
                var issue = report.Issues.Single(i => i.ComponentId == "development_build");

                StringAssert.Contains("This is a Development Build", issue.Message);
            }
            finally
            {
                EditorUserBuildSettings.development = originalDevelopment;
                Object.DestroyImmediate(settings);
            }
        }
    }
}
