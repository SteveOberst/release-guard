using ReleaseGuard.Editor.Core.PreBuild;
using ReleaseGuard.Editor.Core.Components;
using UnityEditor;
using UnityEditor.Build;

namespace ReleaseGuard.Editor.Builtins.PreBuild
{
    /// <summary>
    /// Requires the IL2CPP scripting backend. This is the single biggest anti-reverse-engineering
    /// lever: Mono builds ship your game logic as standard .NET assemblies that can be decompiled
    /// back to readable C# with free tools (dnSpy, ILSpy). IL2CPP compiles to native C++ first.
    /// </summary>
    public sealed class ScriptingBackendCheck : ReleaseGuardComponent
    {
        public override string Id => "scripting_backend";
        public override string DisplayName => "Scripting backend (IL2CPP)";

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(releaseEvent => Evaluate(releaseEvent.Context), priority: -10);

        private static void Evaluate(ReleaseGuardPreBuildContext context)
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