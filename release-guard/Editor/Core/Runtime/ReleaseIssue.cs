namespace ReleaseGuard.Editor.Core.Runtime
{
    /// <summary>A single finding produced by a component pre-build handler.</summary>
    public record ReleaseIssue(
        string ComponentId,
        ReleaseIssueSeverity Severity,
        string Message,
        string AssetPath = null,
        string FixHint = null,
        // When non-null, this issue is a dismissible advisory. The id is stored globally
        // in AdvisorySuppressionStore (profile-independent) when the user clicks "Don't show again".
        string SuppressId = null);
}