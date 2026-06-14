using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class InsecureHttpCheckTests
    {
        [Test]
        public void Reports_WhenAlwaysAllowed()
        {
            var settings = ComponentTestHarness.CreateSettings();
            var original = PlayerSettings.insecureHttpOption;
            try
            {
                PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;

                var report = ComponentTestHarness.RunPreBuild(settings);
                var issue = report.Issues.Single(i => i.SuppressId == "insecure_http.always_allowed");

                StringAssert.Contains("cleartext HTTP requests", issue.Message);
            }
            finally
            {
                PlayerSettings.insecureHttpOption = original;
                Object.DestroyImmediate(settings);
            }
        }
    }
}
