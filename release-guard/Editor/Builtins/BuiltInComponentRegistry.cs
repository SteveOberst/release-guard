using System.Collections.Generic;
using ReleaseGuard.Editor.Builtins.PreBuild;
using ReleaseGuard.Editor.Builtins.PostBuild;
using ReleaseGuard.Editor.Core.Components;

namespace ReleaseGuard.Editor.Builtins
{
    /// <summary>
    /// Canonical list of every built-in component that ships with this package.
    /// </summary>
    internal static class BuiltInComponentRegistry
    {
        public static IReadOnlyList<ReleaseGuardComponent> GetAll() => new ReleaseGuardComponent[]
        {
            new ScriptingBackendCheck(),
            new ManagedStrippingCheck(),
            new DevelopmentBuildCheck(),
            new CIDevelopmentBuildCheck(),
            new ScriptDebuggingCheck(),
            new ProfilerConnectionCheck(),
            new BroadPreserveCheck(),
            new ReleaseForbiddenCheck(),
            new AndroidDebuggableCheck(),
            new WebGLExceptionSupportCheck(),
            new StripEngineCodeCheck(),
            new StackTraceTypeCheck(),
            new InsecureHttpCheck(),
            new BurstDebugCheck(),
            new DebugSymbolSweep(),
            new BuildManifestWriter()
        };
    }
}