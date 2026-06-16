using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class BurstDebugCheckTests
    {
        [Test]
        public void DoesNotReport_WhenBurstNotInstalled()
        {
            var settings = ComponentTestHarness.CreateSettings();
            try
            {
                var report = ComponentTestHarness.RunPreBuild(settings);
                Assert.IsFalse(report.Issues.Any(i => i.ComponentId == "burst_debug"),
                    "BurstDebugCheck should produce no issues when the Burst package is not installed.");
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }
    }
}
