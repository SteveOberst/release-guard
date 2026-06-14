using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.Plugins;

namespace ReleaseGuard.Editor.Tests
{
    // -- Direct-discovery fixtures
    //
    // These components are marked [TestReleaseGuardComponentFixture] so registry loading skips
    // them during real runs.

    [TestReleaseGuardComponentFixture]
    internal sealed class LowPriorityTestPostBuildComponent : ReleaseGuardComponent
    {
        public override string Id => "test_postprocess_low";
        

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPostBuild(_ => { }, priority: -50);
    }

    [TestReleaseGuardComponentFixture]
    internal sealed class HighPriorityTestPostBuildComponent : ReleaseGuardComponent
    {
        public override string Id => "test_postprocess_high";
        

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPostBuild(_ => { }, priority: 50);
    }

    // -- Plugin fixtures

    [TestReleaseGuardComponentFixture]
    internal sealed class ProvidedTestPostBuildComponent : ReleaseGuardComponent
    {
        public override string Id => "test_provided_post_build_component";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPostBuild(_ => { });
    }

    /// <summary>
    /// A plugin that contributes <see cref="ProvidedTestPostBuildComponent"/> (marked
    /// [TestReleaseGuardComponentFixture], so it is always filtered). Harmless in real builds.
    /// </summary>
    internal sealed class TestPostBuildPlugin : ReleaseGuardPlugin
    {
        public override string PluginId => "test.post-build-plugin";
        public override string DisplayName => "Test Post-Build Plugin";

        public override void Register(PluginRegistrationContext context)
        {
            context.ReleaseGuard.Components.Register(new ProvidedTestPostBuildComponent());
            context.ReleaseGuard.Components.Register(new ProvidedTestPostBuildComponent());
        }
    }

    /// <summary>
    /// A plugin whose Register() throws when <see cref="Enabled"/> is true. Used by
    /// PluginExceptions_DoNotAbortDiscovery for the post-build pipeline.
    /// </summary>
    public sealed class ThrowingPostBuildPlugin : ReleaseGuardPlugin
    {
        public static bool Enabled;

        public override string PluginId => "test.throwing-post-build-plugin";
        public override string DisplayName => "Throwing Post-Build Plugin";

        public override void Register(PluginRegistrationContext context)
        {
            if (!Enabled) return;
            throw new System.InvalidOperationException("boom");
        }
    }

    // -- Deduplication fixture

    internal sealed class DeduplicatingPostBuildPlugin : ReleaseGuardPlugin
    {
        public override string PluginId => "test.deduplicating-post-build-plugin";
        public override string DisplayName => "Deduplicating Post-Build Plugin";

        internal sealed class DupPostBuildComponent : ReleaseGuardComponent
        {
            public override string Id => "test_dup_via_post_build_plugin";

            public override void Register(ReleaseGuardComponentBinder binder) =>
                binder.OnPostBuild(_ => { });
        }

        public override void Register(PluginRegistrationContext context)
        {
            context.ReleaseGuard.Components.Register(new DupPostBuildComponent());
            context.ReleaseGuard.Components.Register(new DupPostBuildComponent()); // intentional dup
        }
    }

    // -- Build-phase component fixtures

    internal sealed class DeduplicatingBuildPhasePlugin : ReleaseGuardPlugin
    {
        public override string PluginId => "test.deduplicating-build-plugin";
        public override string DisplayName => "Deduplicating Build Phase Plugin";

        internal sealed class DupBuildPhaseComponent : ReleaseGuardComponent
        {
            public override string Id => "test_dup_via_build_plugin";

            public override void Register(ReleaseGuardComponentBinder binder) =>
                binder.OnBuild(_ => { });
        }

        public override void Register(PluginRegistrationContext context)
        {
            context.ReleaseGuard.Components.Register(new DupBuildPhaseComponent());
            context.ReleaseGuard.Components.Register(new DupBuildPhaseComponent()); // intentional dup
        }
    }

    [TestReleaseGuardComponentFixture]
    internal sealed class LowPriorityTestBuildComponent : ReleaseGuardComponent
    {
        public override string Id => "test_transform_low";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnBuild(_ => { }, priority: -50);
    }

    [TestReleaseGuardComponentFixture]
    internal sealed class HighPriorityTestBuildComponent : ReleaseGuardComponent
    {
        public override string Id => "test_transform_high";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnBuild(_ => { }, priority: 50);
    }
}
