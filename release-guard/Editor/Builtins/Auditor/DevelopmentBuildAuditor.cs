using ReleaseGuard.Editor.Core.Audit;

namespace ReleaseGuard.Editor.Builtins.Auditor
{
    /// <summary>
    /// Flags a Development Build. Dev builds enable the profiler and script debugging, emit full
    /// stack traces, and ship extra debugging metadata - none of which belongs in a release.
    ///
    /// Note: if <c>skipOnDevelopmentBuilds</c> is on (the default), real dev builds are exempted
    /// before any auditor runs, so this mainly serves as a heads-up in the manual audit window
    /// when the Development Build toggle is currently enabled.
    /// </summary>
    public sealed class DevelopmentBuildAuditor : ReleaseAuditor
    {
        public override string Id => "development_build";
        public override string DisplayName => "Development build disabled";

        public override bool ShouldRun(ReleaseAuditContext context) => context.Settings.auditors.forbidDevelopmentBuild;

        public override void Evaluate(ReleaseAuditContext context)
        {
            if (!context.IsDevelopmentBuild)
                return;

            context.Error(
                "This is a Development Build. The profiler, script debugging and full stack traces " +
                "are enabled and extra debugging metadata is shipped.",
                fixHint: "Disable 'Development Build' in Build Settings (or your Build Profile) before releasing.");
        }
    }
}