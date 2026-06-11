using System.Collections.Generic;
using System.Linq;

namespace ReleaseGuard.Editor.Core.PostProcessing
{
    /// <summary>The result of one post-process run: every log entry, plus convenience aggregates.</summary>
    public sealed class ReleasePostProcessResult
    {
        /// <summary>All log entries produced across all post-processors, in execution order.</summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public IReadOnlyList<ReleasePostProcessLog> Log { get; }

        /// <summary>
        /// Every post-processor that was discovered and evaluated in this run (in execution order).
        /// Includes post-processors that produced no log entries.
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public IReadOnlyList<ReleasePostProcessor> DiscoveredPostProcessors { get; }

        public ReleasePostProcessResult(
            IReadOnlyList<ReleasePostProcessLog> log,
            IReadOnlyList<ReleasePostProcessor> discoveredPostProcessors = null)
        {
            Log = log ?? new List<ReleasePostProcessLog>();
            DiscoveredPostProcessors = discoveredPostProcessors ?? new List<ReleasePostProcessor>();
        }

        public int InfoCount => Log.Count(l => l.Level == ReleasePostProcessLogLevel.Info);
        public int WarningCount => Log.Count(l => l.Level == ReleasePostProcessLogLevel.Warning);
        public int ErrorCount => Log.Count(l => l.Level == ReleasePostProcessLogLevel.Error);

        // ReSharper disable once UnusedMember.Global
        public bool HasEntries => Log.Count > 0;
        public bool HasWarnings => WarningCount > 0;
        public bool HasErrors => ErrorCount > 0;
    }
}