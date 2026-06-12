using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.Plugins;

namespace ReleaseGuard.Editor.Tests
{
    // -- Direct-discovery fixtures
    //
    // These auditors are marked [TestAuditorFixture] so registry loading skips them during
    // real audit runs even when the Test Framework is installed (and the assembly is compiled).
    // They exist solely to exercise priority sorting and the [TestAuditorFixture] filter.

    [TestAuditorFixture]
    internal sealed class LowPriorityTestAuditor : ReleaseAuditor
    {
        public override string Id => "test_discovery_low";
        public override int Priority => -50;

        public override void Evaluate(ReleaseAuditContext context)
        {
        }
    }

    [TestAuditorFixture]
    internal sealed class HighPriorityTestAuditor : ReleaseAuditor
    {
        public override string Id => "test_discovery_high";
        public override int Priority => 50;

        public override void Evaluate(ReleaseAuditContext context)
        {
        }
    }

    // -- Plugin fixtures
    //
    // Plugin classes below are NOT marked [TestReleaseGuardPlugin] -- they must be
    // discoverable when autoDiscoverPlugins = true so that the plugin discovery tests work.
    // Their contributed auditors are either marked [TestAuditorFixture] (filtered before
    // they reach the result list) or are harmless test auditors.

    [TestAuditorFixture]
    internal sealed class ProvidedTestAuditor : ReleaseAuditor
    {
        public override string Id => "test_provided";

        public override void Evaluate(ReleaseAuditContext context)
        {
        }
    }

    /// <summary>
    /// A plugin that contributes <see cref="ProvidedTestAuditor"/> (marked [TestAuditorFixture],
    /// so it is always filtered). Its presence in the Editor domain is harmless during real
    /// builds. Used by Plugins_ContributeAuditors and Deduplicates_ById tests.
    /// </summary>
    internal sealed class TestAuditorPlugin : ReleaseGuardPlugin
    {
        public override string PluginId => "test.auditor-plugin";
        public override string DisplayName => "Test Auditor Plugin";

        public override void Register(PluginRegistrationContext context)
        {
            // ProvidedTestAuditor is [TestAuditorFixture] -- the registry filters it.
            context.ReleaseGuard.Registries.Auditors.Register(new ProvidedTestAuditor());
            context.ReleaseGuard.Registries.Auditors.Register(new ProvidedTestAuditor());
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
    // DeduplicatingPlugin returns two non-fixture auditors with the same id so that
    // Deduplicates_ById can verify only one survives. DupAuditor is not marked
    // [TestAuditorFixture] so it passes the fixture filter and reaches the dedup check.

    internal sealed class DeduplicatingPlugin : ReleaseGuardPlugin
    {
        public override string PluginId => "test.deduplicating-plugin";
        public override string DisplayName => "Deduplicating Plugin";

        internal sealed class DupAuditor : ReleaseAuditor
        {
            public override string Id => "test_dup_via_plugin";

            public override void Evaluate(ReleaseAuditContext context)
            {
            }
        }

        public override void Register(PluginRegistrationContext context)
        {
            context.ReleaseGuard.Registries.Auditors.Register(new DupAuditor());
            context.ReleaseGuard.Registries.Auditors.Register(new DupAuditor()); // intentional dup
        }
    }
}