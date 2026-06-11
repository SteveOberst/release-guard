namespace ReleaseGuard.Editor.Core.Runtime
{
    /// <summary>A single finding produced by an auditor.</summary>
    public record ReleaseIssue(
        string AuditorId,
        ReleaseIssueSeverity Severity,
        string Message,
        string AssetPath = null,
        string FixHint = null,
        // When non-null, this issue is a dismissible advisory. The id is used as the key
        // stored in <c>AuditorSettings.suppressedAdvisoryIds</c> when the user clicks
        // "Don't show again" in the Release Guard window.
        string SuppressId = null);
}