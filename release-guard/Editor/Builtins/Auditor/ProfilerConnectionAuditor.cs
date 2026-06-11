using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Util;

namespace ReleaseGuard.Editor.Builtins.Auditor
{
    /// <summary>
    /// Flags release builds that allow the Unity profiler to auto-connect. This keeps
    /// instrumentation hooks available in a build that is meant to be shipped.
    /// </summary>
    public sealed class ProfilerConnectionAuditor : ReleaseAuditor
    {
        public override string Id => "profiler_connection";
        public override string DisplayName => "Profiler connection disabled";

        public override bool ShouldRun(ReleaseAuditContext context) =>
            context.Settings.auditors.forbidProfilerConnection;

        public override void Evaluate(ReleaseAuditContext context)
        {
            var enabled = context.BuildReport is not null
                ? BuildOptionState.IsProfilerConnectionEnabled(context.BuildReport.summary.options)
                : BuildOptionState.IsProfilerConnectionEnabledInEditor();

            if (!enabled)
                return;

            context.Error(
                "Autoconnect Profiler is enabled. This ships extra profiling hooks in the player.",
                fixHint: "Disable 'Autoconnect Profiler' in Build Settings (or your Build Profile) before releasing.");
        }
    }
}