using ReleaseGuard.Editor.Core.PreBuild;
using ReleaseGuard.Editor.Core.Components;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Builtins.PreBuild
{
    /// <summary>
    /// Advisory: checks whether any log channel has full stack trace collection enabled in
    /// Player Settings.
    ///
    /// <para>Full stack traces are captured for every <c>Debug.Log</c>, <c>Debug.LogWarning</c>,
    /// <c>Debug.LogError</c>, and exception in the player runtime. In release builds this has two
    /// downsides: it adds measurable per-log CPU overhead, and it exposes your class hierarchy,
    /// method names, and namespace layout to anyone who captures a log dump.</para>
    ///
    /// <para><b>Recommended release configuration:</b> set Error and Exception to
    /// <c>ScriptOnly</c> (symbol resolution without the overhead of full managed traces) or
    /// <c>None</c>. Set Log and Warning to <c>None</c>.</para>
    ///
    /// <para><b>When full traces may be intentional:</b> some studios keep full traces in their
    /// "release candidate" builds for crash triage, shipping only after a final stripping pass.
    /// Suppress this advisory once you have made a conscious choice. The suppression id is
    /// <c>stack_trace_type.full</c>.</para>
    /// </summary>
    public sealed class StackTraceTypeCheck : ReleaseGuardComponent
    {
        public override string Id => "stack_trace_type";
        public override string DisplayName => "Stack trace log types";

        private const string SuppressId = "stack_trace_type.full";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(releaseEvent => Evaluate(releaseEvent.Context));

        private static void Evaluate(ReleaseGuardPreBuildContext context)
        {
            // Check all five log channels.
            var hasFullTrace =
                PlayerSettings.GetStackTraceLogType(LogType.Log) == StackTraceLogType.Full ||
                PlayerSettings.GetStackTraceLogType(LogType.Warning) == StackTraceLogType.Full ||
                PlayerSettings.GetStackTraceLogType(LogType.Error) == StackTraceLogType.Full ||
                PlayerSettings.GetStackTraceLogType(LogType.Assert) == StackTraceLogType.Full ||
                PlayerSettings.GetStackTraceLogType(LogType.Exception) == StackTraceLogType.Full;

            if (!hasFullTrace)
                return;

            context.Advisory(
                SuppressId,
                ReleaseIssueSeverity.Info,
                "One or more log channels use full stack trace collection (Full). " +
                "In release builds this adds CPU overhead per log call and exposes your " +
                "class hierarchy and method names to anyone who captures a log dump. " +
                "Consider setting Error and Exception to 'ScriptOnly' or 'None', and " +
                "Log and Warning to 'None'. " +
                "See: Edit > Project Settings > Player > Other Settings > Stack Trace.",
                fixHint: "Set stack trace log types to 'None' or 'ScriptOnly' in Player Settings > Other Settings.");
        }
    }
}