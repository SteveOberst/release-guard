using ReleaseGuard.Editor.Core.PreBuild;
using ReleaseGuard.Editor.Core.Components;
using UnityEditor;

namespace ReleaseGuard.Editor.Builtins.PreBuild
{
    /// <summary>
    /// Advisory, WebGL only: checks the exception support level in Player Settings.
    ///
    /// <para>The two "Full" modes instrument all compiled code with exception handling:</para>
    /// <list type="bullet">
    /// <item><c>FullWithStacktrace</c> -- largest size and slowest execution; also embeds managed
    /// stack trace information (class and method names) into the shipped WebAssembly.</item>
    /// <item><c>FullWithoutStacktrace</c> -- smaller than the above but still carries a
    /// significant size and performance cost over the default.</item>
    /// </list>
    ///
    /// <para>Unity's default, <c>ExplicitlyThrownExceptionsOnly</c>, is the recommended release
    /// setting for most projects: explicit <c>throw</c> statements still work, without
    /// instrumenting every operation.</para>
    ///
    /// <para><b>When Full modes are intentional:</b> projects that need to catch hardware
    /// exceptions (null references, out-of-bounds access) at runtime to keep the player session
    /// alive, or release-candidate builds kept verbose for triage. Dismiss the advisory in that
    /// case -- the suppression id is <c>webgl_exception_support.full</c>.</para>
    /// </summary>
    public sealed class WebGLExceptionSupportCheck : ReleaseGuardComponent
    {
        public override string Id => "webgl_exception_support";
        public override string DisplayName => "WebGL exception support";

        private const string SuppressId = "webgl_exception_support.full";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(releaseEvent => Evaluate(releaseEvent.Context));

        private static void Evaluate(ReleaseGuardPreBuildContext context)
        {
            if (!context.IsForPlatform(BuildTarget.WebGL)) return;

            var support = PlayerSettings.WebGL.exceptionSupport;

            if (support != WebGLExceptionSupport.FullWithStacktrace &&
                support != WebGLExceptionSupport.FullWithoutStacktrace)
                return;

            // FullWithStacktrace additionally leaks managed symbol names, so it gets the
            // stronger severity; both share one suppression id since the underlying decision
            // ("do we need full exception support?") is the same.
            var severity = support == WebGLExceptionSupport.FullWithStacktrace
                ? ReleaseIssueSeverity.Warning
                : ReleaseIssueSeverity.Info;

            var stackTraceNote = support == WebGLExceptionSupport.FullWithStacktrace
                ? " It also embeds managed class and method names into the shipped build."
                : "";

            context.Advisory(
                SuppressId,
                severity,
                $"WebGL exception support is '{support}'. Full exception support instruments all " +
                $"compiled code, increasing build size and reducing runtime performance.{stackTraceNote} " +
                "Use 'Explicitly Thrown Exceptions Only' for release unless your game must survive " +
                "hardware exceptions at runtime.",
                fixHint: "Project Settings > Player > Publishing Settings > Enable Exceptions: " +
                         "set to 'Explicitly Thrown Exceptions Only'.");
        }
    }
}