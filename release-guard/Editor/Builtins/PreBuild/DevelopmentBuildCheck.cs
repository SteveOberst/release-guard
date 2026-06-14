using ReleaseGuard.Editor.Core.PreBuild;
using ReleaseGuard.Editor.Core.Components;

namespace ReleaseGuard.Editor.Builtins.PreBuild
{
    /// <summary>
    /// Flags a Development Build. Dev builds enable the profiler and script debugging, emit full
    /// stack traces, and ship extra debugging metadata - none of which belongs in a release.
    /// This component is disabled by default in the Development profile; it is primarily meaningful
    /// in the Release profile to catch accidental dev-flag leakage.
    /// </summary>
    public sealed class DevelopmentBuildCheck : ReleaseGuardComponent
    {
        public override string Id => "development_build";
        public override string DisplayName => "Development build disabled";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(releaseEvent => Evaluate(releaseEvent.Context));

        private static void Evaluate(ReleaseGuardPreBuildContext context)
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