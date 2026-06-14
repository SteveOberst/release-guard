using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Core.Components;

namespace ReleaseGuard.Editor.Core.PostBuild
{
    /// <summary>The result of one post-build run: every log entry, plus convenience aggregates.</summary>
    public sealed class ReleaseGuardPostBuildResult
    {
        /// <summary>All log entries produced across all subscribed components, in execution order.</summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public IReadOnlyList<ReleaseGuardPostBuildLog> Log { get; }

        /// <summary>
        /// Every component with a post-build subscription for this run (in execution order).
        /// Includes components that produced no log entries or opted out via shouldRun.
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public IReadOnlyList<ReleaseGuardComponent> RegisteredComponents { get; }

        public ReleaseGuardPostBuildResult(
            IReadOnlyList<ReleaseGuardPostBuildLog> log,
            IReadOnlyList<ReleaseGuardComponent> registeredComponents = null)
        {
            Log = log ?? new List<ReleaseGuardPostBuildLog>();
            RegisteredComponents = registeredComponents ?? new List<ReleaseGuardComponent>();
        }

        public int InfoCount => Log.Count(l => l.Level == ReleaseGuardPostBuildLogLevel.Info);
        public int WarningCount => Log.Count(l => l.Level == ReleaseGuardPostBuildLogLevel.Warning);
        public int ErrorCount => Log.Count(l => l.Level == ReleaseGuardPostBuildLogLevel.Error);

        // ReSharper disable once UnusedMember.Global
        public bool HasEntries => Log.Count > 0;
        public bool HasWarnings => WarningCount > 0;
        public bool HasErrors => ErrorCount > 0;
    }
}