using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Util;

namespace ReleaseGuard.Editor.Builtins.Auditor
{
    /// <summary>
    /// Flags script debugging on release builds. Managed debugger attachment makes runtime
    /// inspection and method patching substantially easier in shipped builds.
    /// </summary>
    public sealed class ScriptDebuggingAuditor : ReleaseAuditor
    {
        public override string Id => "script_debugging";
        public override string DisplayName => "Script debugging disabled";

        public override bool ShouldRun(ReleaseAuditContext context) => context.Settings.auditors.forbidScriptDebugging;

        public override void Evaluate(ReleaseAuditContext context)
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