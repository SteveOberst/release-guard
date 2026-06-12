using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ReleaseGuard.Editor.Builtins;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class AuditorRegistryLoadingTests
    {
        private static ReleaseGuardSettings Settings() =>
            ScriptableObject.CreateInstance<ReleaseGuardSettings>();

        private static ReleaseGuardLogger Logger() => new ReleaseGuardLogger(false);

        private static IReadOnlyList<ReleaseAuditor> Auditors(ReleaseGuardSettings settings) =>
            new ReleaseGuardEnvironment()
                .Initialize(settings, Logger())
                .Registries
                .Auditors
                .Items;

        // -- Built-in registry

        [Test]
        public void BuiltInRegistry_ContainsExactlyThirteenAuditors()
        {
            var all = BuiltInAuditorRegistry.GetAll();
            Assert.AreEqual(13, all.Count,
                "Built-in count mismatch -- update this test when adding or removing a built-in.");

            var ids = all.Select(a => a.Id).ToList();
            CollectionAssert.AllItemsAreUnique(ids, "Built-in auditors must have unique ids.");
            CollectionAssert.AllItemsAreNotNull(ids, "Every built-in auditor must have a non-null id.");
            foreach (var id in ids)
                Assert.IsFalse(string.IsNullOrEmpty(id), $"Empty id found in built-in registry.");
        }

        [Test]
        public void BuiltInAuditors_RequireNoManualRegistration()
        {
            var s = Settings();
            try
            {
                var found = Auditors(s);
                var ids = found.Select(a => a.Id).ToList();

                // All 13 built-ins must be present without any project-side setup.
                Assert.Contains("scripting_backend", ids);
                Assert.Contains("managed_stripping", ids);
                Assert.Contains("development_build", ids);
                Assert.Contains("script_debugging", ids);
                Assert.Contains("profiler_connection", ids);
                Assert.Contains("broad_preserve", ids);
                Assert.Contains("release_forbidden", ids);
                Assert.Contains("android_debuggable", ids);
                Assert.Contains("webgl_exception_support", ids);
                Assert.Contains("strip_engine_code", ids);
                Assert.Contains("stack_trace_type", ids);
                Assert.Contains("insecure_http", ids);
                Assert.Contains("burst_debug", ids);
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }

        [Test]
        public void TestFixtureAuditors_AreExcludedFromDiscovery()
        {
            // Fixture auditors marked [TestAuditorFixture] must never appear in real audit runs.
            var s = Settings();
            try
            {
                var found = Auditors(s);
                var ids = found.Select(a => a.Id).ToList();

                Assert.IsFalse(ids.Contains("test_discovery_low"),
                    "[TestAuditorFixture] auditor leaked into discovery.");
                Assert.IsFalse(ids.Contains("test_discovery_high"),
                    "[TestAuditorFixture] auditor leaked into discovery.");
                Assert.IsFalse(ids.Contains("test_provided"),
                    "[TestAuditorFixture] auditor contributed by a plugin leaked into discovery.");
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }

        // -- Sorting

        [Test]
        public void Sorts_ByPriorityAscending()
        {
            var s = Settings();
            try
            {
                var found = Auditors(s);

                // scripting_backend has Priority = -10; all other built-ins default to 0.
                Assert.AreEqual("scripting_backend", found[0].Id,
                    "scripting_backend (priority -10) must be the first auditor in the sorted list.");

                // Verify the full list is monotonically non-decreasing by priority.
                for (var i = 0; i < found.Count - 1; i++)
                    Assert.LessOrEqual(found[i].Priority, found[i + 1].Priority,
                        $"'{found[i].Id}' (p={found[i].Priority}) must not come after " +
                        $"'{found[i + 1].Id}' (p={found[i + 1].Priority}).");
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }

        // -- Deduplication

        [Test]
        public void Deduplicates_ById()
        {
            var s = Settings();
            try
            {
                // DeduplicatingPlugin returns two auditors with id "test_dup_via_plugin".
                // Only one should survive.
                s.auditors.autoDiscoverAuditors = false;
                s.plugins.autoDiscoverPlugins = true;
                var found = Auditors(s);
                Assert.AreEqual(1, found.Count(a => a.Id == "test_dup_via_plugin"),
                    "Duplicate auditor ids from a plugin must be deduplicated to one entry.");
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }

        // -- Disabled ids

        [Test]
        public void Skips_DisabledIds()
        {
            var s = Settings();
            try
            {
                s.auditors.disabledAuditorIds.Add("scripting_backend");
                var found = Auditors(s);
                Assert.IsFalse(found.Any(a => a.Id == "scripting_backend"),
                    "Disabled auditor id must not appear in the result.");
                Assert.IsTrue(found.Any(a => a.Id == "managed_stripping"),
                    "Non-disabled auditors must still be present.");
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }

        [Test]
        public void Skips_DisabledIds_FromPlugin()
        {
            var s = Settings();
            try
            {
                s.auditors.autoDiscoverAuditors = false;
                s.plugins.autoDiscoverPlugins = true;
                s.auditors.disabledAuditorIds.Add("test_dup_via_plugin");
                var found = Auditors(s);
                Assert.IsFalse(found.Any(a => a.Id == "test_dup_via_plugin"),
                    "Disabled auditor id contributed by a plugin must not appear in the result.");
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }

        // -- Auto-discover flags

        [Test]
        public void AutoDiscover_Off_ExcludesCustomAuditors()
        {
            var s = Settings();
            try
            {
                s.auditors.autoDiscoverAuditors = false;
                s.plugins.autoDiscoverPlugins = false;
                var found = Auditors(s);
                var ids = found.Select(a => a.Id).ToList();

                // Built-in auditors always register; custom and plugin-contributed auditors must not.
                Assert.IsFalse(ids.Contains("test_discovery_low"),
                    "Custom test auditors must not appear when autoDiscoverAuditors is off.");
                Assert.IsFalse(ids.Contains("test_discovery_high"),
                    "Custom test auditors must not appear when autoDiscoverAuditors is off.");
                Assert.IsFalse(ids.Contains("test_dup_via_plugin"),
                    "Plugin auditors must not appear when autoDiscoverPlugins is off.");
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }

        // -- Plugins

        [Test]
        public void Plugins_ContributeAuditors()
        {
            var s = Settings();
            try
            {
                // DeduplicatingPlugin contributes "test_dup_via_plugin".
                s.auditors.autoDiscoverAuditors = false;
                s.plugins.autoDiscoverPlugins = true;
                var found = Auditors(s);
                Assert.IsTrue(found.Any(a => a.Id == "test_dup_via_plugin"),
                    "Plugins must contribute their auditors to the discovery result.");
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }

        [Test]
        public void PluginExceptions_DoNotAbortDiscovery()
        {
            var s = Settings();
            try
            {
                ThrowingPlugin.Enabled = true;
                LogAssert.ignoreFailingMessages = true;
                Assert.DoesNotThrow(() => Auditors(s),
                    "A throwing plugin must not abort the entire discovery run.");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                ThrowingPlugin.Enabled = false;
                Object.DestroyImmediate(s);
            }
        }

        [Test]
        public void DisabledPlugin_ContributesNothing()
        {
            var s = Settings();
            try
            {
                s.auditors.autoDiscoverAuditors = false;
                s.plugins.autoDiscoverPlugins = true;
                s.plugins.disabledPluginIds.Add("test.deduplicating-plugin");
                var found = Auditors(s);
                Assert.IsFalse(found.Any(a => a.Id == "test_dup_via_plugin"),
                    "A disabled plugin must not contribute any auditors.");
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }
    }
}