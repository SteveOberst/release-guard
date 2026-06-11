using System.Collections.Generic;
using NUnit.Framework;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.Plugins;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class PluginSettingsTests
    {
        private const string RegistryPluginId = "test.plugin-settings-registry";
        private const string DiscoveryPluginId = "test.plugin-settings-discovery";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(PluginSettingsRegistry.AssetPath(RegistryPluginId));
            AssetDatabase.DeleteAsset(PluginSettingsRegistry.AssetPath(DiscoveryPluginId));
            AssetDatabase.Refresh();
        }

        [Test]
        public void PluginSettingsRegistry_LoadOrCreate_CreatesAsset()
        {
            var settings = PluginSettingsRegistry.LoadOrCreate(
                RegistryPluginId, typeof(TestPluginSettings));

            Assert.IsNotNull(settings);
            Assert.IsInstanceOf<TestPluginSettings>(settings);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<TestPluginSettings>(
                PluginSettingsRegistry.AssetPath(RegistryPluginId)));
        }

        [Test]
        public void PluginSettingsRegistry_LoadOrCreate_ReturnsExistingAsset()
        {
            var first = PluginSettingsRegistry.LoadOrCreate(
                RegistryPluginId, typeof(TestPluginSettings));
            var second = PluginSettingsRegistry.LoadOrCreate(
                RegistryPluginId, typeof(TestPluginSettings));

            Assert.AreSame(first, second);
        }

        [Test]
        public void ReleaseGuardPlugin_GetSettings_ReturnsNull_WhenNotWired()
        {
            var plugin = new TestSettingsPlugin();

            Assert.IsNull(plugin.GetSettings());
            Assert.IsNull(plugin.GetSettings<TestPluginSettings>());
        }

        [Test]
        public void ReleaseGuardPlugin_GetSettings_ReturnsInstance_AfterSetSettings()
        {
            var plugin = new TestSettingsPlugin();
            var settings = ScriptableObject.CreateInstance<TestPluginSettings>();
            try
            {
                plugin.SetSettings(settings);

                Assert.AreSame(settings, plugin.GetSettings());
                Assert.AreSame(settings, plugin.GetSettings<TestPluginSettings>());
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void ReleaseGuardPlugin_SettingsAreAvailable_InRegisterAfterSetSettings()
        {
            var plugin = new DiscoverySettingsPlugin();
            var pluginSettings = ScriptableObject.CreateInstance<TestPluginSettings>();
            var releaseGuardSettings = ScriptableObject.CreateInstance<ReleaseGuardSettings>();
            try
            {
                plugin.SetSettings(pluginSettings);
                var releaseGuard = new ReleaseGuardEnvironment().Initialize(
                    releaseGuardSettings,
                    new ReleaseGuardLogger(false));
                var context = new PluginRegistrationContext(releaseGuard);
                plugin.Register(context);

                Assert.AreSame(pluginSettings, plugin.GetSettings());
                Assert.AreSame(pluginSettings, plugin.GetSettings<TestPluginSettings>());
                Assert.AreSame(pluginSettings, plugin.RegisteredSettings);
            }
            finally
            {
                Object.DestroyImmediate(pluginSettings);
                Object.DestroyImmediate(releaseGuardSettings);
            }
        }
    }

    internal sealed class TestPluginSettings : ReleaseGuardPluginSettings
    {
        public bool enabled = true;
    }

    [TestReleaseGuardPlugin]
    internal sealed class TestSettingsPlugin : ReleaseGuardPlugin
    {
        public override string PluginId => "test.plugin-settings-registry";
        public override string DisplayName => "Settings Plugin";
        public override System.Type SettingsType => typeof(TestPluginSettings);
        public override void Register(PluginRegistrationContext context) { }
    }

    [TestReleaseGuardPlugin]
    internal sealed class DiscoverySettingsPlugin : ReleaseGuardPlugin
    {
        public TestPluginSettings RegisteredSettings { get; private set; }

        public override string PluginId => "test.plugin-settings-discovery";
        public override string DisplayName => "Discovery Settings Plugin";
        public override System.Type SettingsType => typeof(TestPluginSettings);
        public override void Register(PluginRegistrationContext context)
        {
            RegisteredSettings = GetSettings<TestPluginSettings>();
        }
    }
}
