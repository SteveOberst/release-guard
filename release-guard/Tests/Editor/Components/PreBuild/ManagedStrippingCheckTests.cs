using System.Linq;
using NUnit.Framework;
using ReleaseGuard.Editor.Builtins.PreBuild;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class ManagedStrippingCheckTests
    {
        [Test]
        public void Reports_WhenBelowConfiguredMinimum()
        {
            var settings = ComponentTestHarness.CreateSettings();
            var componentSettings =
                settings.components.componentToggles.GetOrCreate<ManagedStrippingCheck.Config>("managed_stripping");
            componentSettings.minLevel = ManagedStrippingLevel.High;

            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Standalone);
            var originalLevel = PlayerSettings.GetManagedStrippingLevel(namedTarget);
            try
            {
                PlayerSettings.SetManagedStrippingLevel(namedTarget, ManagedStrippingLevel.Low);

                var report = ComponentTestHarness.RunPreBuild(settings, BuildTarget.StandaloneWindows64);
                var warning = report.Issues.Single(i =>
                    i.ComponentId == "managed_stripping" &&
                    i.Severity == ReleaseIssueSeverity.Warning &&
                    i.SuppressId == null);
                var advisory = report.Issues.Single(i =>
                    i.ComponentId == "managed_stripping" &&
                    i.SuppressId == "managed_stripping.low_deprecated");

                StringAssert.Contains("below the required High", warning.Message);
                StringAssert.Contains("future deprecation", advisory.Message);
            }
            finally
            {
                PlayerSettings.SetManagedStrippingLevel(namedTarget, originalLevel);
                Object.DestroyImmediate(settings);
            }
        }
    }
}
