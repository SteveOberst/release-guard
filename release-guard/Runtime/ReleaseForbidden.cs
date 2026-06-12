using System;

namespace ReleaseGuard
{
    /// <summary>
    /// Marks a type or member that must NOT ship in a release build - debug hooks, cheat
    /// commands, test scaffolding, dev-only backdoors, etc.
    ///
    /// The built-in <c>ReleaseForbiddenAuditor</c> reports every usage so a release build
    /// fails before such code can be shipped. Prefer also wrapping the implementation in a
    /// debug-only <c>#if</c> so it is physically excluded from the compiled release.
    /// </summary>
    /// <example>
    /// <code>
    /// [ReleaseForbidden(ReleaseIssueSeverity.Error, "Gives infinite money")]
    /// public static void GrantAllCurrency() { ... }
    /// </code>
    /// </example>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum |
        AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property,
        Inherited = false)]
    public sealed class ReleaseForbiddenAttribute : Attribute
    {
        /// <summary>How serious it is to ship this member. Defaults to <see cref="ReleaseIssueSeverity.Error"/>.</summary>
        public ReleaseIssueSeverity Severity { get; }

        /// <summary>Optional human-readable reason, surfaced in the report and Console.</summary>
        public string Reason { get; }

        public ReleaseForbiddenAttribute(
            ReleaseIssueSeverity severity = ReleaseIssueSeverity.Error,
            string reason = null)
        {
            Severity = severity;
            Reason = reason;
        }
    }
}