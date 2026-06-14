using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class StripEngineCodeCheckTests
    {
        [Test]
        public void Reports_WhenDisabled()
        {
            var settings = ComponentTestHarness.CreateSettings();
            var original = PlayerSettings.stripEngineCode;
            try
            {
                PlayerSettings.stripEngineCode = false;

                var report = ComponentTestHarness.RunPreBuild(settings);
                var issue = report.Issues.Single(i => i.SuppressId == "strip_engine_code.disabled");

                StringAssert.Contains("Engine code stripping is disabled", issue.Message);
            }
            finally
            {
                PlayerSettings.stripEngineCode = original;
                Object.DestroyImmediate(settings);
            }
        }
    }
}
