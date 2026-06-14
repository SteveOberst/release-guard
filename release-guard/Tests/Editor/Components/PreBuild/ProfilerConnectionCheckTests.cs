using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class ProfilerConnectionCheckTests
    {
        [Test]
        public void Reports_WhenProfilerConnectionIsEnabled()
        {
            var settings = ComponentTestHarness.CreateSettings();
            var originalDevelopment = EditorUserBuildSettings.development;
            var originalConnectProfiler = EditorUserBuildSettings.connectProfiler;
            try
            {
                settings.components.componentToggles.SetEnabled("script_debugging", false);
                EditorUserBuildSettings.development = true;
                EditorUserBuildSettings.connectProfiler = true;

                var report = ComponentTestHarness.RunPreBuild(settings);
                var issue = report.Issues.Single(i => i.ComponentId == "profiler_connection");

                StringAssert.Contains("Autoconnect Profiler is enabled", issue.Message);
            }
            finally
            {
                EditorUserBuildSettings.development = originalDevelopment;
                EditorUserBuildSettings.connectProfiler = originalConnectProfiler;
                Object.DestroyImmediate(settings);
            }
        }
    }
}
