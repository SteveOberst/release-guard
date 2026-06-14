using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Core.Components;

namespace ReleaseGuard.Editor.Core.Build
{
    /// <summary>The result of one build event run: every log entry, plus convenience aggregates.</summary>
    public sealed class ReleaseGuardBuildResult
    {
        /// <summary>All log entries produced across all subscribed components, in execution order.</summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public IReadOnlyList<ReleaseGuardBuildLog> Log { get; }

        /// <summary>
        /// Every component with a build subscription for this run (in execution order).
        /// Includes components that produced no log entries or opted out via shouldRun.
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public IReadOnlyList<ReleaseGuardComponent> RegisteredComponents { get; }

        public ReleaseGuardBuildResult(
            IReadOnlyList<ReleaseGuardBuildLog> log,
            IReadOnlyList<ReleaseGuardComponent> registeredComponents = null)
        {
            Log = log ?? new List<ReleaseGuardBuildLog>();
            RegisteredComponents = registeredComponents ?? new List<ReleaseGuardComponent>();
        }

        public int InfoCount => Log.Count(l => l.Level == ReleaseGuardBuildLogLevel.Info);
        public int WarningCount => Log.Count(l => l.Level == ReleaseGuardBuildLogLevel.Warning);
        public int ErrorCount => Log.Count(l => l.Level == ReleaseGuardBuildLogLevel.Error);

        // ReSharper disable once UnusedMember.Global
        public bool HasEntries => Log.Count > 0;
        public bool HasWarnings => WarningCount > 0;
        public bool HasErrors => ErrorCount > 0;
    }
}