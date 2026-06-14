namespace ReleaseGuard.Editor.Core.Build
{
    /// <summary>Severity of a single build-event log entry.</summary>
    public enum ReleaseGuardBuildLogLevel
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    /// <summary>One log entry produced by a component during a build event run.</summary>
    // ReSharper disable NotAccessedPositionalProperty.Global
    public record ReleaseGuardBuildLog(
        string ComponentId,
        ReleaseGuardBuildLogLevel Level,
        string Message);
    // ReSharper enable NotAccessedPositionalProperty.Global
}