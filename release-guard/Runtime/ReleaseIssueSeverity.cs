namespace ReleaseGuard
{
    /// <summary>
    /// Severity of a Release Guard finding, ordered least to most serious.
    /// A build fails when any issue is at or above the configured failure threshold.
    /// </summary>
    public enum ReleaseIssueSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }
}