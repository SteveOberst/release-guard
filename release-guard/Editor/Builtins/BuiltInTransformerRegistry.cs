using System.Collections.Generic;
using ReleaseGuard.Editor.Core.Transforming;

namespace ReleaseGuard.Editor.Builtins
{
    /// <summary>
    /// Explicit list of every built-in transformer shipped with the package.
    ///
    /// Built-ins are loaded directly here rather than via TypeCache so that test fixtures
    /// never appear in the runtime transformer list. No built-in transformers are included
    /// in the initial release -- this registry is the extension point for future additions.
    /// </summary>
    internal static class BuiltInTransformerRegistry
    {
        public static IReadOnlyList<ReleaseTransformer> GetAll() => new ReleaseTransformer[]
        {
            // No built-in transformers. Add entries here when built-in artifact transformations
            // are introduced in a future release.
        };
    }
}