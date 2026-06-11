using System.Collections.Generic;
using System.Linq;

namespace ReleaseGuard.Editor.Core.Transforming
{
    /// <summary>The result of one transformer run: every log entry, plus convenience aggregates.</summary>
    public sealed class ReleaseTransformResult
    {
        /// <summary>All log entries produced across all transformers, in execution order.</summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public IReadOnlyList<ReleaseTransformLog> Log { get; }

        /// <summary>
        /// Every transformer that was discovered and evaluated in this run (in execution order).
        /// Includes transformers that produced no log entries.
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public IReadOnlyList<ReleaseTransformer> DiscoveredTransformers { get; }

        public ReleaseTransformResult(
            IReadOnlyList<ReleaseTransformLog> log,
            IReadOnlyList<ReleaseTransformer> discoveredTransformers = null)
        {
            Log = log ?? new List<ReleaseTransformLog>();
            DiscoveredTransformers = discoveredTransformers ?? new List<ReleaseTransformer>();
        }

        public int InfoCount => Log.Count(l => l.Level == ReleaseTransformLogLevel.Info);
        public int WarningCount => Log.Count(l => l.Level == ReleaseTransformLogLevel.Warning);
        public int ErrorCount => Log.Count(l => l.Level == ReleaseTransformLogLevel.Error);

        // ReSharper disable once UnusedMember.Global
        public bool HasEntries => Log.Count > 0;
        public bool HasWarnings => WarningCount > 0;
        public bool HasErrors => ErrorCount > 0;
    }
}