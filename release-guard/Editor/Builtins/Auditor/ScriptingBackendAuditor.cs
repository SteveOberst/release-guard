using ReleaseGuard.Editor.Core.Audit;
using UnityEditor;
using UnityEditor.Build;

namespace ReleaseGuard.Editor.Builtins.Auditor
{
    /// <summary>
    /// Requires the IL2CPP scripting backend. This is the single biggest anti-reverse-engineering
    /// lever: Mono builds ship your game logic as standard .NET assemblies that can be decompiled
    /// back to readable C# with free tools (dnSpy, ILSpy). IL2CPP compiles to native C++ first.
    /// </summary>
    public sealed class ScriptingBackendAuditor : ReleaseAuditor
    {
        public override string Id => "scripting_backend";
        public override string DisplayName => "Scripting backend (IL2CPP)";

        // Runs before all other built-ins (priority 0). Wrong backend means IL2CPP-based
        // hardening is absent entirely, so this failure should surface first.
        public override int Priority => -10;

        public override bool ShouldRun(ReleaseAuditContext context) => context.Settings.auditors.requireIl2Cpp;

        public override void Evaluate(ReleaseAuditContext context)
        {
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(
                BuildPipeline.GetBuildTargetGroup(context.BuildTarget));

            var backend = PlayerSettings.GetScriptingBackend(namedTarget);
            if (backend != ScriptingImplementation.IL2CPP)
            {
                context.Error(
                    $"Scripting backend for {namedTarget.TargetName} is {backend}, not IL2CPP. " +
                    "Mono builds ship as .NET assemblies that decompile straight back to your C# source.",
                    fixHint: "Project Settings > Player > Other Settings > Scripting Backend: set to IL2CPP.");
            }
        }
    }
}