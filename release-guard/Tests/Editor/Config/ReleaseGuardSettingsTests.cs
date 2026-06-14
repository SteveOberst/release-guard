using System;
using System.Linq;
using NUnit.Framework;
using ReleaseGuard;
using ReleaseGuard.Editor.Builtins.PreBuild;
using ReleaseGuard.Editor.Config;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class ReleaseGuardSettingsTests
    {
        private static ReleaseGuardSettings New() =>
            ScriptableObject.CreateInstance<ReleaseGuardSettings>();

        [Test]
        public void IsComponentDisabled_MatchesById()
        {
            var s = New();
            try
            {
                s.components.componentToggles.SetEnabled("scripting_backend", false);
                Assert.IsTrue(s.IsComponentDisabled("scripting_backend"));
                Assert.IsFalse(s.IsComponentDisabled("managed_stripping"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(s);
            }
        }

        [Test]
        public void IsComponentDisabled_CustomComponent_DisabledViaSetEnabled()
        {
            var s = New();
            try
            {
                s.components.componentToggles.SetEnabled("com.example.custom", false);
                Assert.IsTrue(s.IsComponentDisabled("com.example.custom"));
                Assert.IsFalse(s.IsComponentDisabled("com.example.other"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(s);
            }
        }

        // -- Advisory suppression

        [Test]
        public void SuppressAdvisory_AddsId()
        {
            AdvisorySuppressionStore.Unsuppress("advisory.foo"); // ensure clean state
            var s = New();
            try
            {
                Assert.IsFalse(s.IsAdvisorySuppressed("advisory.foo"));
                s.SuppressAdvisory("advisory.foo");
                Assert.IsTrue(s.IsAdvisorySuppressed("advisory.foo"));
            }
            finally
            {
                AdvisorySuppressionStore.Unsuppress("advisory.foo");
                UnityEngine.Object.DestroyImmediate(s);
            }
        }

        [Test]
        public void SuppressAdvisory_WithContext_PersistsRecord()
        {
            AdvisorySuppressionStore.Unsuppress("advisory.recorded");
            var s = New();
            try
            {
                s.SuppressAdvisory(
                    "advisory.recorded",
                    "Managed stripping is low.",
                    "managed_stripping",
                    "Managed code stripping");

                var record = AdvisorySuppressionStore.GetAllRecords()
                    .FirstOrDefault(r => r.suppressId == "advisory.recorded");

                Assert.IsNotNull(record);
                Assert.AreEqual("Managed stripping is low.", record.message);
                Assert.AreEqual("managed_stripping", record.componentId);
                Assert.AreEqual("Managed code stripping", record.componentDisplayName);
            }
            finally
            {
                AdvisorySuppressionStore.Unsuppress("advisory.recorded");
                UnityEngine.Object.DestroyImmediate(s);
            }
        }

        [Test]
        public void GetAllRecords_FillsLegacySuppressions_WithPlaceholderDescription()
        {
            AdvisorySuppressionStore.Unsuppress("advisory.legacy");
            try
            {
                AdvisorySuppressionStore.Suppress("advisory.legacy");

                var record = AdvisorySuppressionStore.GetAllRecords()
                    .FirstOrDefault(r => r.suppressId == "advisory.legacy");

                Assert.IsNotNull(record);
                Assert.AreEqual("(No description recorded)", record.message);
                Assert.AreEqual("advisory.legacy", record.componentId);
                Assert.AreEqual("advisory.legacy", record.componentDisplayName);
            }
            finally
            {
                AdvisorySuppressionStore.Unsuppress("advisory.legacy");
            }
        }

        [Test]
        public void Unsuppress_RemovesStoredRecord()
        {
            AdvisorySuppressionStore.Unsuppress("advisory.remove");
            try
            {
                AdvisorySuppressionStore.Suppress(
                    "advisory.remove",
                    "Some advisory",
                    "component.id",
                    "Component Name");

                AdvisorySuppressionStore.Unsuppress("advisory.remove");

                Assert.IsFalse(AdvisorySuppressionStore.IsSuppressed("advisory.remove"));
                Assert.IsFalse(AdvisorySuppressionStore.GetAllRecords()
                    .Any(r => r.suppressId == "advisory.remove"));
            }
            finally
            {
                AdvisorySuppressionStore.Unsuppress("advisory.remove");
            }
        }

        [Test]
        public void IsAdvisorySuppressed_ReturnsFalse_ForUnsuppressedId()
        {
            AdvisorySuppressionStore.Unsuppress("advisory.bar");
            var s = New();
            try
            {
                AdvisorySuppressionStore.Suppress("advisory.bar");
                Assert.IsFalse(s.IsAdvisorySuppressed("advisory.baz"),
                    "An unsuppressed id must not be reported as suppressed.");
            }
            finally
            {
                AdvisorySuppressionStore.Unsuppress("advisory.bar");
                UnityEngine.Object.DestroyImmediate(s);
            }
        }

        [Test]
        public void IsAdvisorySuppressed_ReturnsFalse_ForNullOrEmpty()
        {
            var s = New();
            try
            {
                Assert.IsFalse(s.IsAdvisorySuppressed(null));
                Assert.IsFalse(s.IsAdvisorySuppressed(""));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(s);
            }
        }

        // -- Plugin disabled

        [Test]
        public void IsPluginDisabled_MatchesById()
        {
            var s = New();
            try
            {
                s.plugins.disabledPluginIds.Add("com.example.my-plugin");
                Assert.IsTrue(s.IsPluginDisabled("com.example.my-plugin"));
                Assert.IsFalse(s.IsPluginDisabled("com.example.other-plugin"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(s);
            }
        }

        // -- ReleaseForbidden assembly exclusions

        [Test]
        public void ReleaseForbiddenSettings_ExcludedAssemblies_CaseInsensitive()
        {
            var s = New();
            try
            {
                s.components.componentToggles
                    .GetOrCreate<ReleaseForbiddenCheck.Config>("release_forbidden")
                    .excludedAssemblies.Add("MyPlugin.Runtime");

                var loaded = s.components.componentToggles
                    .GetSettings<ReleaseForbiddenCheck.Config>("release_forbidden");

                Assert.IsTrue(
                    loaded.excludedAssemblies.Exists(e =>
                        e != null && string.Equals(e.Trim(), "myplugin.runtime", StringComparison.OrdinalIgnoreCase)),
                    "Assembly exclusion lookup must be case-insensitive.");
                Assert.IsTrue(
                    loaded.excludedAssemblies.Exists(e =>
                        e != null && string.Equals(e.Trim(), "MYPLUGIN.RUNTIME", StringComparison.OrdinalIgnoreCase)),
                    "Assembly exclusion lookup must be case-insensitive.");
                Assert.IsFalse(
                    loaded.excludedAssemblies.Exists(e =>
                        e != null && string.Equals(e.Trim(), "SomeOther.Assembly", StringComparison.OrdinalIgnoreCase)),
                    "Non-excluded assembly must not appear in the list.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(s);
            }
        }
    }
}
