using ReleaseGuard.Editor.Core.PreBuild;
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.Build;

namespace ReleaseGuard.Editor.Builtins.PreBuild
{
    /// <summary>
    /// Flags script debugging on release builds. Managed debugger attachment makes runtime
    /// inspection and method patching substantially easier in shipped builds.
    /// </summary>
    public sealed class ScriptDebuggingCheck : ReleaseGuardComponent
    {
        public override string Id => "script_debugging";
        public override string DisplayName => "Script debugging disabled";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(releaseEvent => Evaluate(releaseEvent.Context));

        private static void Evaluate(ReleaseGuardPreBuildContext context)
        {
            var enabled = context.BuildReport is not null
                ? BuildOptionState.IsScriptDebuggingEnabled(context.BuildReport.summary.options)
                : BuildOptionState.IsScriptDebuggingEnabledInEditor();

            if (!enabled)
                return;

            context.Error(
                "Script debugging is enabled. This allows managed debuggers to attach to the player.",
                fixHint: "Disable 'Script Debugging' in Build Settings (or your Build Profile) before releasing.");
        }
    }
}
