using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Config;

namespace ReleaseGuard.Editor.Core.Runtime
{
    /// <summary>
    /// UI-only: detects pairs of profiles whose activation conditions can simultaneously match
    /// the same build. Used by the Profiles management page to show a conflict warning.
    /// At build time, list ordering resolves ambiguity (first match wins); no error is thrown.
    /// </summary>
    internal static class ProfileConflictAnalyzer
    {
        public readonly struct Conflict
        {
            public readonly string profileA;
            public readonly string profileB;

            public Conflict(string a, string b)
            {
                profileA = a;
                profileB = b;
            }
        }

        public static List<Conflict> FindConflicts(List<ReleaseGuardProfile> profiles)
        {
            var result = new List<Conflict>();
            if (profiles == null) return result;

            for (var i = 0; i < profiles.Count; i++)
            for (var j = i + 1; j < profiles.Count; j++)
            {
                if (CanSimultaneouslyMatch(profiles[i].activation, profiles[j].activation))
                    result.Add(new Conflict(profiles[i].displayName, profiles[j].displayName));
            }

            return result;
        }

        private static bool CanSimultaneouslyMatch(ProfileActivation a, ProfileActivation b)
        {
            // Always matches everything; order handles it, so don't flag as a conflict.
            if (a.strategy == ActivationStrategy.Always || b.strategy == ActivationStrategy.Always)
                return false;

            // Two profiles with the same base condition.
            if (a.strategy == b.strategy)
            {
                if (a.strategy == ActivationStrategy.UnityBuildProfileNames)
                    return ListsIntersect(a.unityBuildProfileNames, b.unityBuildProfileNames);
                return true;
            }

            // Release + CI: not a conflict (release builds in CI are valid; order handles it).
            if (IsReleaseBased(a.strategy) && b.strategy == ActivationStrategy.IsCI) return false;
            if (IsReleaseBased(b.strategy) && a.strategy == ActivationStrategy.IsCI) return false;

            // Dev + CI: dev builds in CI can match both IsDevelopmentBuild and IsCI.
            if (a.strategy == ActivationStrategy.IsDevelopmentBuild && b.strategy == ActivationStrategy.IsCI)
                return true;
            if (b.strategy == ActivationStrategy.IsDevelopmentBuild && a.strategy == ActivationStrategy.IsCI)
                return true;

            // IsCIAndDevelopmentBuild vs IsDevelopmentBuild: conflict on dev builds in CI.
            if (a.strategy == ActivationStrategy.IsCIAndDevelopmentBuild &&
                b.strategy == ActivationStrategy.IsDevelopmentBuild) return true;
            if (b.strategy == ActivationStrategy.IsCIAndDevelopmentBuild &&
                a.strategy == ActivationStrategy.IsDevelopmentBuild) return true;

            // IsCIAndDevelopmentBuild vs IsCI: conflict on dev builds in CI.
            if (a.strategy == ActivationStrategy.IsCIAndDevelopmentBuild &&
                b.strategy == ActivationStrategy.IsCI) return true;
            if (b.strategy == ActivationStrategy.IsCIAndDevelopmentBuild &&
                a.strategy == ActivationStrategy.IsCI) return true;

            return false;
        }

        private static bool IsReleaseBased(ActivationStrategy s)
            => s == ActivationStrategy.IsReleaseBuild;

        private static bool ListsIntersect(List<string> a, List<string> b)
        {
            if (a == null || b == null) return false;
            return a.Any(b.Contains);
        }
    }
}