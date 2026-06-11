using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.Plugins;
using ReleaseGuard.Editor.Core.PostProcessing;
using ReleaseGuard.Editor.Core.Transforming;

namespace ReleaseGuard.Editor.Tests
{
    // -- Direct-discovery fixtures
    //
    // These post-processors are marked [TestPostProcessorFixture] so registry loading skips
    // them during real post-process runs.

    [TestPostProcessorFixture]
    internal sealed class LowPriorityTestPostProcessor : ReleasePostProcessor
    {
        public override string Id       => "test_postprocess_low";
        public override int    Priority => -50;
        public override void PostProcess(ReleasePostProcessContext context) { }
    }

    [TestPostProcessorFixture]
    internal sealed class HighPriorityTestPostProcessor : ReleasePostProcessor
    {
        public override string Id       => "test_postprocess_high";
        public override int    Priority => 50;
        public override void PostProcess(ReleasePostProcessContext context) { }
    }

    // -- Plugin fixtures

    [TestPostProcessorFixture]
    internal sealed class ProvidedTestPostProcessor : ReleasePostProcessor
    {
        public override string Id => "test_provided_postprocessor";
        public override void PostProcess(ReleasePostProcessContext context) { }
    }

    /// <summary>
    /// A plugin that contributes <see cref="ProvidedTestPostProcessor"/> (marked
    /// [TestPostProcessorFixture], so it is always filtered). Harmless in real builds.
    /// </summary>
    internal sealed class TestPostProcessorPlugin : ReleaseGuardPlugin
    {
        public override string PluginId    => "test.postprocessor-plugin";
        public override string DisplayName => "Test Post-Processor Plugin";

        public override void Register(PluginRegistrationContext context)
        {
            context.ReleaseGuard.Registries.PostProcessors.Register(new ProvidedTestPostProcessor());
            context.ReleaseGuard.Registries.PostProcessors.Register(new ProvidedTestPostProcessor());
        }
    }

    /// <summary>
    /// A plugin whose Register() throws when <see cref="Enabled"/> is true. Used by
    /// PluginExceptions_DoNotAbortDiscovery for the post-processor pipeline.
    /// </summary>
    public sealed class ThrowingPostProcessorPlugin : ReleaseGuardPlugin
    {
        public static bool Enabled;

        public override string PluginId    => "test.throwing-postprocessor-plugin";
        public override string DisplayName => "Throwing Post-Processor Plugin";

        public override void Register(PluginRegistrationContext context)
        {
            if (!Enabled) return;
            throw new System.InvalidOperationException("boom");
        }
    }

    // -- Deduplication fixture

    internal sealed class DeduplicatingPostProcessorPlugin : ReleaseGuardPlugin
    {
        public override string PluginId    => "test.deduplicating-postprocessor-plugin";
        public override string DisplayName => "Deduplicating Post-Processor Plugin";

        internal sealed class DupPostProcessor : ReleasePostProcessor
        {
            public override string Id => "test_dup_via_postprocessor_plugin";
            public override void PostProcess(ReleasePostProcessContext context) { }
        }

        public override void Register(PluginRegistrationContext context)
        {
            context.ReleaseGuard.Registries.PostProcessors.Register(new DupPostProcessor());
            context.ReleaseGuard.Registries.PostProcessors.Register(new DupPostProcessor()); // intentional dup
        }
    }

    // -- Transformer pipeline fixtures (separate from post-processors)

    [TestTransformerFixture]
    internal sealed class LowPriorityTestTransformer : ReleaseTransformer
    {
        public override string Id       => "test_transform_low";
        public override int    Priority => -50;
        public override void Transform(ReleaseTransformContext context) { }
    }

    [TestTransformerFixture]
    internal sealed class HighPriorityTestTransformer : ReleaseTransformer
    {
        public override string Id       => "test_transform_high";
        public override int    Priority => 50;
        public override void Transform(ReleaseTransformContext context) { }
    }
}
