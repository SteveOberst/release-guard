using ReleaseGuard.Editor.Core.PreBuild;
using ReleaseGuard.Editor.Core.Components;

namespace ReleaseGuard.Editor.Builtins.PreBuild
{
    /// <summary>
    /// Flags broad preserve rules that keep large amounts of code and metadata in the player.
    /// Whole-assembly preserve rules defeat stripping and make shipped builds larger and easier
    /// to inspect than they need to be.
    /// </summary>
    public sealed class BroadPreserveCheck : ReleaseGuardComponent
    {
        public override string Id => "broad_preserve";
        public override string DisplayName => "Broad preserve rules";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(releaseEvent => Evaluate(releaseEvent.Context));

        private static void Evaluate(ReleaseGuardPreBuildContext context)
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