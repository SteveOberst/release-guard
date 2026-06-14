using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class ScriptDebuggingCheckTests
    {
        [Test]
        public void Reports_WhenScriptDebuggingIsEnabled()
        {
            var settings = ComponentTestHarness.CreateSettings();
            var originalDevelopment = EditorUserBuildSettings.development;
            var originalAllowDebugging = EditorUserBuildSettings.allowDebugging;
            try
            {
                settings.components.componentToggles.SetEnabled("profiler_connection", false);
                EditorUserBuildSettings.development = true;
                EditorUserBuildSettings.allowDebugging = true;

                var report = ComponentTestHarness.RunPreBuild(settings);
                var issue = report.Issues.Single(i => i.ComponentId == "script_debugging");

                StringAssert.Contains("Script debugging is enabled", issue.Message);
            }
            finally
            {
                EditorUserBuildSettings.development = originalDevelopment;
                EditorUserBuildSettings.allowDebugging = originalAllowDebugging;
                Object.DestroyImmediate(settings);
            }
        }
    }
}
