using NUnit.Framework;
using ReleaseGuard;
using ReleaseGuard.Editor.Config;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class ReleaseGuardSettingsTests
    {
        private static ReleaseGuardSettings New() =>
            ScriptableObject.CreateInstance<ReleaseGuardSettings>();

        [Test]
        public void BuiltInReleaseChecks_DefaultToEnabled()
        {
            var s = New();
            try
            {
                Assert.IsTrue(s.auditors.requireIl2Cpp);
                Assert.IsTrue(s.auditors.forbidDevelopmentBuild);
                Assert.IsTrue(s.auditors.forbidScriptDebugging);
                Assert.IsTrue(s.auditors.forbidProfilerConnection);
                Assert.IsTrue(s.auditors.forbidBroadPreserve);
            }
            finally { Object.DestroyImmediate(s); }
        }

        [Test]
        public void IsAuditorDisabled_MatchesById()
        {
            var s = New();
            try
            {
                s.auditors.disabledAuditorIds.Add("scripting_backend");
                Assert.IsTrue(s.IsAuditorDisabled("scripting_backend"));
                Assert.IsFalse(s.IsAuditorDisabled("managed_stripping"));
            }
            finally { Object.DestroyImmediate(s); }
        }

        [Test]
        public void GetProfileOverride_FindsByExactName()
        {
            var s = New();
            try
            {
                s.general.profileOverrides.Add(new BuildProfileOverride
                {
                    buildProfileName = "Staging",
                    enabled = false,
                    failureThreshold = ReleaseIssueSeverity.Warning,
                });

                var found = s.GetProfileOverride("Staging");
                Assert.IsNotNull(found);
                Assert.IsFalse(found.enabled);
                Assert.AreEqual(ReleaseIssueSeverity.Warning, found.failureThreshold);
            }
            finally { Object.DestroyImmediate(s); }
        }

        [Test]
        public void GetProfileOverride_ReturnsNull_ForUnknownOrEmpty()
        {
            var s = New();
            try
            {
                s.general.profileOverrides.Add(new BuildProfileOverride { buildProfileName = "Production" });
                Assert.IsNull(s.GetProfileOverride("Staging"));
                Assert.IsNull(s.GetProfileOverride(null));
                Assert.IsNull(s.GetProfileOverride(""));
            }
            finally { Object.DestroyImmediate(s); }
        }

        // -- Advisory suppression

        [Test]
        public void SuppressAdvisory_AddsId()
        {
            var s = New();
            try
            {
                Assert.IsFalse(s.IsAdvisorySuppressed("advisory.foo"));
                s.auditors.suppressedAdvisoryIds.Add("advisory.foo");
                Assert.IsTrue(s.IsAdvisorySuppressed("advisory.foo"));
            }
            finally { Object.DestroyImmediate(s); }
        }

        [Test]
        public void IsAdvisorySuppressed_ReturnsFalse_ForUnsuppressedId()
        {
            var s = New();
            try
            {
                s.auditors.suppressedAdvisoryIds.Add("advisory.bar");
                Assert.IsFalse(s.IsAdvisorySuppressed("advisory.baz"),
                    "An unsuppressed id must not be reported as suppressed.");
            }
            finally { Object.DestroyImmediate(s); }
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
            finally { Object.DestroyImmediate(s); }
        }

        // -- Post-processor disabled

        [Test]
        public void IsPostProcessorDisabled_MatchesById()
        {
            var s = New();
            try
            {
                s.postProcessors.disabledPostProcessorIds.Add("debug_symbol_sweep");
                Assert.IsTrue(s.IsPostProcessorDisabled("debug_symbol_sweep"));
                Assert.IsFalse(s.IsPostProcessorDisabled("build_manifest"));
            }
            finally { Object.DestroyImmediate(s); }
        }

        [Test]
        public void IdLists_AreComparedTrimmed_AndIgnoreBlankAndCommentLines()
        {
            // The settings UI edits id lists as free text, one entry per line, so the
            // stored entries may carry whitespace, blank lines, and '#' comment lines.
            var s = New();
            try
            {
                s.auditors.disabledAuditorIds.Add("  scripting_backend  ");
                s.auditors.disabledAuditorIds.Add("");
                s.auditors.disabledAuditorIds.Add("# a comment line");

                Assert.IsTrue(s.IsAuditorDisabled("scripting_backend"),
                    "Entries must match after trimming.");
                Assert.IsFalse(s.IsAuditorDisabled(""),
                    "An empty id must never match, even with blank entries present.");
                Assert.IsFalse(s.IsAuditorDisabled("managed_stripping"),
                    "Blank and comment lines must not disable unrelated ids.");
            }
            finally { Object.DestroyImmediate(s); }
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
            finally { Object.DestroyImmediate(s); }
        }

        // -- ReleaseForbidden assembly exclusions

        [Test]
        public void IsAssemblyExcludedFromReleaseForbidden_CaseInsensitive()
        {
            var s = New();
            try
            {
                s.auditors.releaseForbiddenExcludedAssemblies.Add("MyPlugin.Runtime");
                Assert.IsTrue(s.IsAssemblyExcludedFromReleaseForbidden("myplugin.runtime"),
                    "Assembly exclusion check must be case-insensitive.");
                Assert.IsTrue(s.IsAssemblyExcludedFromReleaseForbidden("MYPLUGIN.RUNTIME"),
                    "Assembly exclusion check must be case-insensitive.");
                Assert.IsFalse(s.IsAssemblyExcludedFromReleaseForbidden("SomeOther.Assembly"),
                    "Non-excluded assembly must return false.");
            }
            finally { Object.DestroyImmediate(s); }
        }
    }
}
