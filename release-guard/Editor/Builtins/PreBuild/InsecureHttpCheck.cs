using ReleaseGuard.Editor.Core.PreBuild;
using ReleaseGuard.Editor.Core.Components;
using UnityEditor;

namespace ReleaseGuard.Editor.Builtins.PreBuild
{
    /// <summary>
    /// Advisory: checks whether the project allows cleartext HTTP requests in release builds.
    ///
    /// <para><c>PlayerSettings.insecureHttpOption</c> has three values:</para>
    /// <list type="bullet">
    /// <item><c>NotAllowed</c> -- cleartext HTTP is blocked everywhere. The safe default; not flagged.</item>
    /// <item><c>DevelopmentOnly</c> -- cleartext HTTP works in development builds but is blocked in
    /// release builds. Fine for releases; not flagged.</item>
    /// <item><c>AlwaysAllowed</c> -- cleartext HTTP works in release builds. Flagged.</item>
    /// </list>
    ///
    /// <para>Shipping with <c>AlwaysAllowed</c> means any HTTP traffic your game produces can be
    /// read and modified in transit (credentials, session tokens, save data, IAP receipts).
    /// It also widens the attack surface for traffic-tampering cheats.</para>
    ///
    /// <para><b>When this may be intentional:</b> games that talk to LAN devices, local
    /// development servers shipped to testers, or legacy backend infrastructure without TLS.
    /// If that applies to your project, dismiss the advisory -- the suppression id is
    /// <c>insecure_http.always_allowed</c>.</para>
    /// </summary>
    public sealed class InsecureHttpCheck : ReleaseGuardComponent
    {
        public override string Id => "insecure_http";
        public override string DisplayName => "Insecure HTTP option";

        private const string SuppressId = "insecure_http.always_allowed";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(releaseEvent => Evaluate(releaseEvent.Context));

        private static void Evaluate(ReleaseGuardPreBuildContext context)
        {
            if (PlayerSettings.insecureHttpOption != InsecureHttpOption.AlwaysAllowed)
                return;

            context.Advisory(
                SuppressId,
                ReleaseIssueSeverity.Warning,
                "'Allow downloads over HTTP' is set to 'Always allowed'. Release builds can make " +
                "cleartext HTTP requests, which can be read and modified in transit. " +
                "If your game only needs HTTP during development, use 'Allowed in development builds' " +
                "instead. If release builds genuinely need cleartext HTTP (e.g. LAN devices), " +
                "dismiss this advisory.",
                fixHint: "Project Settings > Player > Other Settings > Allow downloads over HTTP: " +
                         "set to 'Not allowed' or 'Allowed in development builds'.");
        }
    }
}