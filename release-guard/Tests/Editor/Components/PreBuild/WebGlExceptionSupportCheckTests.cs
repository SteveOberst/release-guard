using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class WebGlExceptionSupportCheckTests
    {
        [Test]
        public void Reports_WhenFullWithStacktraceIsEnabled()
        {
            var settings = ComponentTestHarness.CreateSettings();
            var original = PlayerSettings.WebGL.exceptionSupport;
            try
            {
                PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithStacktrace;

                var report = ComponentTestHarness.RunPreBuild(settings, BuildTarget.WebGL);
                var issue = report.Issues.Single(i => i.SuppressId == "webgl_exception_support.full");

                Assert.AreEqual(ReleaseIssueSeverity.Warning, issue.Severity);
                StringAssert.Contains("FullWithStacktrace", issue.Message);
            }
            finally
            {
                PlayerSettings.WebGL.exceptionSupport = original;
                Object.DestroyImmediate(settings);
            }
        }
    }
}
