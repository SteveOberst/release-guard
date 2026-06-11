using System.Collections.Generic;
using ReleaseGuard.Editor.Builtins.PostProcessor;
using ReleaseGuard.Editor.Core.PostProcessing;

namespace ReleaseGuard.Editor.Builtins
{
    /// <summary>
    /// Explicit list of every built-in post-processor shipped with the package.
    ///
    /// Built-ins are loaded directly here rather than via TypeCache so that test fixtures
    /// never appear in the runtime post-processor list.
    /// </summary>
    internal static class BuiltInPostProcessorRegistry
    {
        public static IReadOnlyList<ReleasePostProcessor> GetAll() => new ReleasePostProcessor[]
        {
            // Priority 0: report-only by default; deletion is a settings opt-in.
            new DebugSymbolSweepPostProcessor(),

            // Priority 100: off by default (writeBuildManifest). Runs last so the manifest
            // records the output folder's final, post-sweep state.
            new BuildManifestPostProcessor()
        };
    }
}