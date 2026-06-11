using ReleaseGuard.Editor.Core.Audit;
using UnityEditor;

namespace ReleaseGuard.Editor.Builtins.Auditor
{
    /// <summary>
    /// Advisory: checks whether Unity Engine Code Stripping is enabled in Player Settings.
    ///
    /// <para>Engine code stripping removes unused Unity engine subsystems from the build,
    /// reducing binary size and limiting the engine surface area available to reverse-engineers
    /// and exploit writers. It is disabled by default in Unity and is safe to enable for most
    /// projects.</para>
    ///
    /// <para><b>When this may NOT be safe:</b> if your project uses reflection to access Unity
    /// engine types at runtime (e.g. <c>Type.GetType("UnityEngine.Something")</c>, or accessing
    /// internal Unity classes via reflection), those types may be stripped and cause
    /// <c>NullReferenceException</c> or <c>MissingMethodException</c> at runtime. Third-party
    /// plugins that rely on internal Unity APIs can also break. Always test thoroughly after
    /// enabling this setting.</para>
    ///
    /// <para>This is an advisory (not a blocking error) because the default Unity project does
    /// not enable this. It is surfaced to encourage conscious adoption, not to block every
    /// build that hasn't turned it on yet.</para>
    /// </summary>
    public sealed class StripEngineCodeAuditor : ReleaseAuditor
    {
        public override string Id => "strip_engine_code";
        public override string DisplayName => "Engine code stripping";

        private const string SuppressId = "strip_engine_code.disabled";

        public override void Evaluate(ReleaseAuditContext context)
        {
            if (PlayerSettings.stripEngineCode)
                return;

            context.Advisory(
                SuppressId,
                ReleaseIssueSeverity.Info,
                "Engine code stripping is disabled. Enabling it reduces build size and limits " +
                "the Unity engine surface area available to reverse-engineers. " +
                "Caution: if your project accesses Unity engine types via reflection at runtime, " +
                "enabling stripping may cause NullReferenceException or MissingMethodException. " +
                "Verify thoroughly before enabling in production. " +
                "See: Edit > Project Settings > Player > Other Settings > Strip Engine Code.",
                fixHint: "Enable 'Strip Engine Code' in Player Settings > Other Settings.");
        }
    }
}