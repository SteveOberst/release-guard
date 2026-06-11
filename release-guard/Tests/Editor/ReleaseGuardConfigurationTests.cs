using NUnit.Framework;
using ReleaseGuard;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    /// <summary>
    /// Covers the editor-run resolution path (report == null), where development state comes from
    /// the Build Settings "Development Build" checkbox. Each test saves and restores that global
    /// flag so it never leaks between tests or into the user's editor.
    /// </summary>
    public sealed class ReleaseGuardConfigurationTests
    {
        private static ReleaseGuardSettings Settings() =>
            ScriptableObject.CreateInstance<ReleaseGuardSettings>();

        [Test]
        public void DevelopmentBuild_IsDisabled_WhenSkipOn()
        {
            var s = Settings();
            var original = EditorUserBuildSettings.development;
            try
            {
                s.general.skipOnDevelopmentBuilds = true;
                EditorUserBuildSettings.development = true;

                var cfg = ReleaseGuardConfiguration.Resolve(s, null);

                Assert.IsTrue(cfg.IsDevelopmentBuild);
                Assert.IsFalse(cfg.Enabled, "release rules must not apply to development builds");
            }
            finally
            {
                EditorUserBuildSettings.development = original;
                Object.DestroyImmediate(s);
            }
        }

        [Test]
        public void DevelopmentBuild_StaysEnabled_WhenSkipOff()
        {
            var s = Settings();
            var original = EditorUserBuildSettings.development;
            try
            {
                s.general.enabled = true;
                s.general.skipOnDevelopmentBuilds = false;
                EditorUserBuildSettings.development = true;

                var cfg = ReleaseGuardConfiguration.Resolve(s, null);

                Assert.IsTrue(cfg.IsDevelopmentBuild);
                Assert.IsTrue(cfg.Enabled);
            }
            finally
            {
                EditorUserBuildSettings.development = original;
                Object.DestroyImmediate(s);
            }
        }

        [Test]
        public void ReleaseBuild_PassesThroughEnabledAndThreshold()
        {
            var s = Settings();
            var original = EditorUserBuildSettings.development;
            try
            {
                s.general.enabled = true;
                s.general.failureThreshold = ReleaseIssueSeverity.Warning;
                EditorUserBuildSettings.development = false;

                var cfg = ReleaseGuardConfiguration.Resolve(s, null);

                Assert.IsFalse(cfg.IsDevelopmentBuild);
                Assert.IsTrue(cfg.Enabled);
                Assert.AreEqual(ReleaseIssueSeverity.Warning, cfg.FailureThreshold);
            }
            finally
            {
                EditorUserBuildSettings.development = original;
                Object.DestroyImmediate(s);
            }
        }

        [Test]
        public void MasterDisable_OverridesEverything()
        {
            var s = Settings();
            var original = EditorUserBuildSettings.development;
            try
            {
                s.general.enabled = false;
                EditorUserBuildSettings.development = false;

                var cfg = ReleaseGuardConfiguration.Resolve(s, null);

                Assert.IsFalse(cfg.Enabled);
            }
            finally
            {
                EditorUserBuildSettings.development = original;
                Object.DestroyImmediate(s);
            }
        }
    }
}
