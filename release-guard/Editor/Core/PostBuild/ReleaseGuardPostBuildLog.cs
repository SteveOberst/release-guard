namespace ReleaseGuard.Editor.Core.PostBuild
{
    /// <summary>Severity of a single post-build-event log entry.</summary>
    public enum ReleaseGuardPostBuildLogLevel
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    /// <summary>One log entry produced by a component during a post-build run.</summary>
    // ReSharper disable NotAccessedPositionalProperty.Global
    public record ReleaseGuardPostBuildLog(
        string ComponentId,
        ReleaseGuardPostBuildLogLevel Level,
        string Message);
    // ReSharper enable NotAccessedPositionalProperty.Global
}