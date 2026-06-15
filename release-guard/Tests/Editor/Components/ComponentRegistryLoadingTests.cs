using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ReleaseGuard.Editor.Builtins;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEngine;
using UnityEngine.TestTools;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class ComponentRegistryLoadingTests
    {
        private static ReleaseGuardSettings Settings() =>
            ScriptableObject.CreateInstance<ReleaseGuardSettings>();

        private static ReleaseGuardLogger Logger() => new ReleaseGuardLogger(false);

        private static ReleaseGuardEnvironment Environment(ReleaseGuardSettings settings) =>
            new ReleaseGuardEnvironment().Initialize(settings, Logger());

        private static IReadOnlyList<ReleaseGuardComponent> Components(ReleaseGuardSettings settings) =>
            Environment(settings).Components.Items;

        [Test]
        public void BuiltInRegistry_ContainsExactlySixteenComponents()
        {
            var all = BuiltInComponentRegistry.GetAll();
            Assert.AreEqual(16, all.Count,
                "Built-in count mismatch -- update this test when adding or removing a built-in.");

            var ids = all.Select(c => c.Id).ToList();
            CollectionAssert.AllItemsAreUnique(ids);
            CollectionAssert.Contains(ids, "scripting_backend");
            CollectionAssert.Contains(ids, "build_manifest");
        }

        [Test]
        public void BuiltIns_RegisterWithoutProjectSetup()
        {
            var settings = Settings();
            try
            {
                var ids = Components(settings).Select(c => c.Id).ToList();
                Assert.Contains("scripting_backend", ids);
                Assert.Contains("managed_stripping", ids);
                Assert.Contains("ci_development_build", ids);
                Assert.Contains("debug_symbol_sweep", ids);
                // build_manifest is default-disabled so it is not in Components.Items with default settings.
                // BuiltInRegistry_ContainsExactlySixteenComponents verifies it exists in the raw registry.
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void TestFixtureComponents_AreExcludedFromDiscovery()
        {
            var settings = Settings();
            try
            {
                var ids = Components(settings).Select(c => c.Id).ToList();
                Assert.IsFalse(ids.Contains("test_discovery_low"));
                Assert.IsFalse(ids.Contains("test_discovery_high"));
                Assert.IsFalse(ids.Contains("test_postprocess_low"));
                Assert.IsFalse(ids.Contains("test_transform_low"));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void PreBuildHandlers_SortByPriorityThenId()
        {
            var settings = Settings();
            try
            {
                var environment = Environment(settings);
                var configuration = ReleaseGuardConfiguration.Resolve(environment.Settings, report: null);
                var report = environment.Pipeline.DispatchWithResult(
                    ReleaseGuardPreBuildEvent.ForManualRun(
                        environment.Settings,
                        configuration,
                        environment.Logger,
                        UnityEditor.EditorUserBuildSettings.activeBuildTarget),
                    releaseEvent => releaseEvent.Report);
                Assert.AreEqual("scripting_backend", report.RegisteredComponents[0].Id);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Deduplicates_ById_AcrossPluginComponents()
        {
            var settings = Settings();
            try
            {
                settings.plugins.autoDiscoverPlugins = true;
                settings.components.autoDiscoverComponents = false;
                var found = Components(settings);
                Assert.AreEqual(1, found.Count(c => c.Id == "test_dup_via_plugin"));
                Assert.AreEqual(1, found.Count(c => c.Id == "test_dup_via_post_build_plugin"));
                Assert.AreEqual(1, found.Count(c => c.Id == "test_dup_via_build_plugin"));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Skips_DisabledComponentIds()
        {
            var settings = Settings();
            try
            {
                settings.plugins.autoDiscoverPlugins = true;
                settings.components.componentToggles.SetEnabled("scripting_backend", false);
                settings.components.componentToggles.SetEnabled("test_dup_via_plugin", false);
                var found = Components(settings);
                Assert.IsFalse(found.Any(c => c.Id == "scripting_backend"));
                Assert.IsFalse(found.Any(c => c.Id == "test_dup_via_plugin"));
                Assert.IsTrue(found.Any(c => c.Id == "managed_stripping"));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void AutoDiscover_Off_ExcludesCustomComponents()
        {
            var settings = Settings();
            try
            {
                settings.components.autoDiscoverComponents = false;
                settings.plugins.autoDiscoverPlugins = false;
                var ids = Components(settings).Select(c => c.Id).ToList();

                Assert.IsFalse(ids.Contains("test_discovery_low"));
                Assert.IsFalse(ids.Contains("test_postprocess_low"));
                Assert.IsFalse(ids.Contains("test_transform_low"));
                Assert.IsFalse(ids.Contains("test_dup_via_plugin"));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void Plugins_ContributeComponents()
        {
            var settings = Settings();
            try
            {
                settings.plugins.autoDiscoverPlugins = true;
                var ids = Components(settings).Select(c => c.Id).ToList();
                Assert.Contains("test_dup_via_plugin", ids);
                Assert.Contains("test_dup_via_post_build_plugin", ids);
                Assert.Contains("test_dup_via_build_plugin", ids);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void PluginExceptions_DoNotAbortDiscovery()
        {
            var settings = Settings();
            try
            {
                ThrowingPlugin.Enabled = true;
                global::ReleaseGuard.Editor.Tests.ThrowingPostBuildPlugin.Enabled = true;
                settings.plugins.autoDiscoverPlugins = true;
                LogAssert.ignoreFailingMessages = true;
                Assert.DoesNotThrow(() => Components(settings));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
                ThrowingPlugin.Enabled = false;
                global::ReleaseGuard.Editor.Tests.ThrowingPostBuildPlugin.Enabled = false;
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void DisabledPlugin_ContributesNothing()
        {
            var settings = Settings();
            try
            {
                settings.plugins.autoDiscoverPlugins = true;
                settings.plugins.disabledPluginIds.Add("test.deduplicating-plugin");
                settings.plugins.disabledPluginIds.Add("test.deduplicating-post-build-plugin");
                settings.plugins.disabledPluginIds.Add("test.deduplicating-build-plugin");
                var ids = Components(settings).Select(c => c.Id).ToList();
                Assert.IsFalse(ids.Contains("test_dup_via_plugin"));
                Assert.IsFalse(ids.Contains("test_dup_via_post_build_plugin"));
                Assert.IsFalse(ids.Contains("test_dup_via_build_plugin"));
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }
    }
}
