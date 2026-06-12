using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ReleaseGuard.Editor.Builtins;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.PostProcessing;
using ReleaseGuard.Editor.Core.Runtime;
using ReleaseGuard.Editor.Core.Transforming;
using UnityEngine;
using UnityEngine.TestTools;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class PostProcessorRegistryLoadingTests
    {
        private static ReleaseGuardSettings Settings() =>
            ScriptableObject.CreateInstance<ReleaseGuardSettings>();

        private static ReleaseGuardLogger Logger() => new ReleaseGuardLogger(false);

        private static IReadOnlyList<ReleasePostProcessor> PostProcessors(ReleaseGuardSettings settings) =>
            new ReleaseGuardEnvironment()
                .Initialize(settings, Logger())
                .Registries
                .PostProcessors
                .Items;

        // -- Built-in registry

        [Test]
        public void BuiltInRegistry_ContainsExactlyTwoPostProcessors()
        {
            var all = BuiltInPostProcessorRegistry.GetAll();
            Assert.AreEqual(2, all.Count,
                "Built-in post-processor count mismatch -- update this test when adding or removing a built-in.");

            var ids = all.Select(p => p.Id).ToList();
            Assert.Contains("debug_symbol_sweep", ids);
            Assert.Contains("build_manifest", ids);
            CollectionAssert.AllItemsAreUnique(ids, "Built-in post-processors must have unique ids.");
        }

        [Test]
        public void TransformerRegistry_IsEmpty()
        {
            var all = BuiltInTransformerRegistry.GetAll();
            Assert.AreEqual(0, all.Count,
                "No built-in transformers are shipped in the initial release; update this test when adding one.");
        }

        // -- Fixture exclusion

        [Test]
        public void TestFixturePostProcessors_AreExcludedFromDiscovery()
        {
            var s = Settings();
            try
            {
                var found = PostProcessors(s);
                var ids = found.Select(p => p.Id).ToList();

                Assert.IsFalse(ids.Contains("test_postprocess_low"),
                    "[TestPostProcessorFixture] post-processor leaked into discovery.");
                Assert.IsFalse(ids.Contains("test_postprocess_high"),
                    "[TestPostProcessorFixture] post-processor leaked into discovery.");
                Assert.IsFalse(ids.Contains("test_provided_postprocessor"),
                    "[TestPostProcessorFixture] post-processor contributed by a plugin leaked into discovery.");
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
            s.plugins.autoDiscoverPlugins = true;
            try
            {
                var found = PostProcessors(s);

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
                s.postProcessors.autoDiscoverPostProcessors = false;
                s.plugins.autoDiscoverPlugins = true;
                var found = PostProcessors(s);
                Assert.AreEqual(1, found.Count(p => p.Id == "test_dup_via_postprocessor_plugin"),
                    "Duplicate post-processor ids from a plugin must be deduplicated to one entry.");
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }

        // -- Disabled ids

        [Test]
        public void Skips_DisabledPostProcessorIds()
        {
            var s = Settings();
            try
            {
                s.postProcessors.autoDiscoverPostProcessors = false;
                s.plugins.autoDiscoverPlugins = true;
                s.postProcessors.disabledPostProcessorIds.Add("test_dup_via_postprocessor_plugin");
                var found = PostProcessors(s);
                Assert.IsFalse(found.Any(p => p.Id == "test_dup_via_postprocessor_plugin"),
                    "Disabled post-processor id must not appear in the result.");
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }

        // -- Auto-discover flags

        [Test]
        public void AutoDiscover_Off_ExcludesCustomPostProcessors()
        {
            var s = Settings();
            try
            {
                s.postProcessors.autoDiscoverPostProcessors = false;
                s.plugins.autoDiscoverPlugins = false;
                var found = PostProcessors(s);
                var ids = found.Select(p => p.Id).ToList();

                // Built-in post-processors always register; custom and plugin-contributed ones must not.
                Assert.IsFalse(ids.Contains("test_postprocess_low"),
                    "Custom test post-processors must not appear when autoDiscoverPostProcessors is off.");
                Assert.IsFalse(ids.Contains("test_postprocess_high"),
                    "Custom test post-processors must not appear when autoDiscoverPostProcessors is off.");
                Assert.IsFalse(ids.Contains("test_provided_postprocessor"),
                    "Plugin post-processors must not appear when autoDiscoverPlugins is off.");
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }

        // -- Plugins

        [Test]
        public void Plugins_ContributePostProcessors()
        {
            var s = Settings();
            try
            {
                s.postProcessors.autoDiscoverPostProcessors = false;
                s.plugins.autoDiscoverPlugins = true;
                var found = PostProcessors(s);
                Assert.IsTrue(found.Any(p => p.Id == "test_dup_via_postprocessor_plugin"),
                    "Plugins must contribute their post-processors to the discovery result.");
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
                ThrowingPostProcessorPlugin.Enabled = true;
                LogAssert.ignoreFailingMessages = true;
                Assert.DoesNotThrow(() => PostProcessors(s),
                    "A throwing plugin must not abort the entire post-processor discovery run.");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                ThrowingPostProcessorPlugin.Enabled = false;
                Object.DestroyImmediate(s);
            }
        }

        [Test]
        public void DisabledPlugin_ContributesNothing()
        {
            var s = Settings();
            try
            {
                s.postProcessors.autoDiscoverPostProcessors = false;
                s.plugins.autoDiscoverPlugins = true;
                s.plugins.disabledPluginIds.Add("test.deduplicating-postprocessor-plugin");
                var found = PostProcessors(s);
                Assert.IsFalse(found.Any(p => p.Id == "test_dup_via_postprocessor_plugin"),
                    "A disabled plugin must not contribute any post-processors.");
            }
            finally
            {
                Object.DestroyImmediate(s);
            }
        }
    }
}