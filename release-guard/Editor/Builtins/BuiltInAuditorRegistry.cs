using System.Collections.Generic;
using ReleaseGuard.Editor.Builtins.Auditor;
using ReleaseGuard.Editor.Core.Audit;

namespace ReleaseGuard.Editor.Builtins
{
    /// <summary>
    /// Canonical list of every auditor that ships with this package.
    ///
    /// Registry loading adds these directly rather than relying on <c>TypeCache</c> to scan
    /// the <c>ReleaseGuard.Editor</c> assembly. This keeps the
    /// built-in set explicit, auditable at a glance, and immune to test-fixture pollution
    /// (TypeCache includes test assemblies in the Editor domain when Test Framework is
    /// installed, which would otherwise cause fixture auditors to appear in real audit runs).
    ///
    /// Adding a new built-in:
    ///   1. Create the auditor class under <c>Editor/Builtins/Auditor/</c>.
    ///   2. Add a new instantiation line to <see cref="GetAll"/>.
    ///
    /// Removing a built-in: delete the class and remove the line from <see cref="GetAll"/>.
    /// </summary>
    internal static class BuiltInAuditorRegistry
    {
        /// <summary>
        /// Returns a fresh snapshot of every built-in auditor in its canonical priority order.
        /// Callers must not cache this across settings changes.
        /// </summary>
        public static IReadOnlyList<ReleaseAuditor> GetAll() => new ReleaseAuditor[]
        {
            // Priority -10: runs first. Most fundamental security check -- wrong backend
            // means all other hardening is irrelevant.
            new ScriptingBackendAuditor(),

            // Priority 0 (default): remaining checks have equal priority; their relative
            // order within this group is stable but unspecified.
            new ManagedStrippingAuditor(),
            new DevelopmentBuildAuditor(),
            new ScriptDebuggingAuditor(),
            new ProfilerConnectionAuditor(),
            new BroadPreserveAuditor(),
            new ReleaseForbiddenAuditor(),

            // Platform-specific checks. Each gates itself via ShouldRun, so it costs
            // nothing on other targets.
            new AndroidDebuggableAuditor(),
            new WebGLExceptionSupportAuditor(),

            // Advisory checks (dismissible, never blocking under the default threshold).
            // These inform users of best-practice improvements without failing builds.
            new StripEngineCodeAuditor(),
            new StackTraceTypeAuditor(),
            new InsecureHttpAuditor(),
            new BurstDebugAuditor()
        };
    }
}