using ReleaseGuard.Editor.Core.PreBuild;
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.Build;

namespace ReleaseGuard.Editor.Builtins.PreBuild
{
    /// <summary>
    /// Flags release builds that allow the Unity profiler to auto-connect. This keeps
    /// instrumentation hooks available in a build that is meant to be shipped.
    /// </summary>
    public sealed class ProfilerConnectionCheck : ReleaseGuardComponent
    {
        public override string Id => "profiler_connection";
        public override string DisplayName => "Profiler connection disabled";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(releaseEvent => Evaluate(releaseEvent.Context));

        private static void Evaluate(ReleaseGuardPreBuildContext context)
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