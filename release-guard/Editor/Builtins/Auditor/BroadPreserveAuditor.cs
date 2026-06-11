using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Util;

namespace ReleaseGuard.Editor.Builtins.Auditor
{
    /// <summary>
    /// Flags broad preserve rules that keep large amounts of code and metadata in the player.
    /// Whole-assembly preserve rules defeat stripping and make shipped builds larger and easier
    /// to inspect than they need to be.
    /// </summary>
    public sealed class BroadPreserveAuditor : ReleaseAuditor
    {
        public override string Id => "broad_preserve";
        public override string DisplayName => "Broad preserve rules";

        public override bool ShouldRun(ReleaseAuditContext context) => context.Settings.auditors.forbidBroadPreserve;

        public override void Evaluate(ReleaseAuditContext context)
        {
            foreach (var finding in BroadPreserveAnalyzer.AnalyzeProject())
            {
                context.Error(
                    finding.Message,
                    finding.AssetPath,
                    "Prefer targeted [Preserve] usage or explicit link.xml entries for the exact members reflection needs.");
            }
        }
    }
}