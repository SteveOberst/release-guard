using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.Plugins;

namespace ReleaseGuard.Editor.Tests
{
    // -- Direct-discovery fixtures
    //
    // These components are marked [TestReleaseGuardComponentFixture] so registry loading skips
    // them during real runs even when the Test Framework is installed (and the assembly is
    // compiled). They exist solely to exercise priority sorting and the fixture filter.

    [TestReleaseGuardComponentFixture]
    internal sealed class LowPriorityTestComponent : ReleaseGuardComponent
    {
        public override string Id => "test_discovery_low";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(_ => { }, priority: -50);
    }

    [TestReleaseGuardComponentFixture]
    internal sealed class HighPriorityTestComponent : ReleaseGuardComponent
    {
        public override string Id => "test_discovery_high";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(_ => { }, priority: 50);
    }

    // -- Plugin fixtures
    //
    // Plugin classes below are NOT marked [TestReleaseGuardPlugin] -- they must be
    // discoverable when autoDiscoverPlugins = true so that the plugin discovery tests work.
    // Their contributed components are either marked [TestReleaseGuardComponentFixture]
    // (filtered before they reach the result list) or are harmless test components.

    [TestReleaseGuardComponentFixture]
    internal sealed class ProvidedTestComponent : ReleaseGuardComponent
    {
        public override string Id => "test_provided";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(_ => { });
    }

    /// <summary>
    /// A plugin that contributes <see cref="ProvidedTestComponent"/> (marked
    /// [TestReleaseGuardComponentFixture],
    /// so it is always filtered). Its presence in the Editor domain is harmless during real
    /// builds. Used by Plugins_ContributeComponents and Deduplicates_ById tests.
    /// </summary>
    internal sealed class TestComponentPlugin : ReleaseGuardPlugin
    {
        public override string PluginId => "test.component-plugin";
        public override string DisplayName => "Test Component Plugin";

        public override void Register(PluginRegistrationContext context)
        {
            // ProvidedTestComponent is [TestReleaseGuardComponentFixture] -- the registry filters it.
            context.ReleaseGuard.Components.Register(new ProvidedTestComponent());
            context.ReleaseGuard.Components.Register(new ProvidedTestComponent());
        }
    }

    /// <summary>
    /// A plugin whose Register() throws when <see cref="Enabled"/> is true. Used by
    /// PluginExceptions_DoNotAbortDiscovery. Not marked [TestReleaseGuardPlugin] so it is
    /// discovered automatically; harmless when Enabled = false (the default).
    /// </summary>
    public sealed class ThrowingPlugin : ReleaseGuardPlugin
    {
        public static bool Enabled;

        public override string PluginId => "test.throwing-plugin";
        public override string DisplayName => "Throwing Plugin";

        public override void Register(PluginRegistrationContext context)
        {
            if (!Enabled) return;
            throw new System.InvalidOperationException("boom");
        }
    }

    // -- Deduplication fixture
    //
    // DeduplicatingPlugin returns two non-fixture components with the same id so that
    // Deduplicates_ById can verify only one survives. DupComponent is not marked
    // [TestReleaseGuardComponentFixture] so it passes the fixture filter and reaches the
    // dedup check.

    internal sealed class DeduplicatingPlugin : ReleaseGuardPlugin
    {
        public override string PluginId => "test.deduplicating-plugin";
        public override string DisplayName => "Deduplicating Plugin";

        internal sealed class DupComponent : ReleaseGuardComponent
        {
            public override string Id => "test_dup_via_plugin";

            public override void Register(ReleaseGuardComponentBinder binder) =>
                binder.OnPreBuild(_ => { });
        }

        public override void Register(PluginRegistrationContext context)
        {
            context.ReleaseGuard.Components.Register(new DupComponent());
            context.ReleaseGuard.Components.Register(new DupComponent()); // intentional dup
        }
    }
}
