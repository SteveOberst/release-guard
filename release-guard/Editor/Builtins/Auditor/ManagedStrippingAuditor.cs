using ReleaseGuard.Editor.Core.Audit;
using UnityEditor;
using UnityEditor.Build;

namespace ReleaseGuard.Editor.Builtins.Auditor
{
    /// <summary>
    /// Requires a minimum managed code stripping level. Stripping removes unused code and the
    /// metadata around it, shrinking the build and giving anyone inspecting it less to work with.
    /// </summary>
    public sealed class ManagedStrippingAuditor : ReleaseAuditor
    {
        public override string Id => "managed_stripping";
        public override string DisplayName => "Managed code stripping";

        public override bool ShouldRun(ReleaseAuditContext context) =>
            context.Settings.auditors.minManagedStrippingLevel != ManagedStrippingLevel.Disabled;

        public override void Evaluate(ReleaseAuditContext context)
        {
            var required = context.Settings.auditors.minManagedStrippingLevel;
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(
                BuildPipeline.GetBuildTargetGroup(context.BuildTarget));

            var actual = PlayerSettings.GetManagedStrippingLevel(namedTarget);

            // IMPORTANT: do NOT compare the raw enum values. ManagedStrippingLevel.Minimal was
            // added at value 4 (after High=3), so the enum's integer order does not match how
            // aggressively each level strips. Compare by semantic aggressiveness instead.
            if (Aggressiveness(actual) < Aggressiveness(required))
            {
                context.Warning(
                    $"Managed stripping level is {actual}, below the required {required}.",
                    fixHint:
                    $"Project Settings > Player > Other Settings > Managed Stripping Level: set to {required} or higher.");
            }

            // Advisory: nudge projects that ship below Medium.
            // Medium is the first level that meaningfully reduces the metadata visible to
            // reverse engineers. Disabled, Minimal, and Low all leave substantially more
            // code and reflection metadata in the build.
            if (Aggressiveness(actual) < Aggressiveness(ManagedStrippingLevel.Medium))
            {
                context.Advisory(
                    "managed_stripping.below_medium",
                    ReleaseIssueSeverity.Warning,
                    $"Managed stripping level is '{actual}'. Medium or higher is recommended for " +
                    "release builds -- it reduces build size and leaves significantly less metadata " +
                    "exposed to reverse engineers.",
                    fixHint:
                    "Raise Managed Stripping Level to Medium or High in " +
                    "Project Settings > Player > Other Settings > Managed Stripping Level. " +
                    "To permanently dismiss this advisory, click 'Don't show again' in the " +
                    "Release Guard window, or add 'managed_stripping.below_medium' to " +
                    "Suppressed Advisory Ids in Project Settings > Release Guard.");
            }

            // Advisory: Low is marked for future deprecation in Unity and may be removed.
            if (actual == ManagedStrippingLevel.Low)
            {
                context.Advisory(
                    "managed_stripping.low_deprecated",
                    ReleaseIssueSeverity.Warning,
                    "Managed stripping level 'Low' is marked for future deprecation in Unity " +
                    "and may be removed in a future editor version.",
                    fixHint:
                    "Migrate to Medium or High stripping to future-proof your project. " +
                    "To permanently dismiss this advisory, click 'Don't show again' in the " +
                    "Release Guard window, or add 'managed_stripping.low_deprecated' to " +
                    "Suppressed Advisory Ids in Project Settings > Release Guard.");
            }
        }

        /// <summary>
        /// Maps each level to how much it strips, least to most. The enum's own integer values
        /// can't be used: Minimal=4 sits after High=3 numerically but strips the least of all
        /// the active levels. Order: Disabled &lt; Minimal &lt; Low &lt; Medium &lt; High.
        /// </summary>
        private static int Aggressiveness(ManagedStrippingLevel level) => level switch
        {
            ManagedStrippingLevel.Disabled => 0,
            ManagedStrippingLevel.Minimal => 1,
            ManagedStrippingLevel.Low => 2,
            ManagedStrippingLevel.Medium => 3,
            ManagedStrippingLevel.High => 4,
            _ => 0
        };
    }
}