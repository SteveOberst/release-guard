namespace ReleaseGuard.Editor.Core.PostProcessing
{
    /// <summary>Severity of a single post-processor log entry.</summary>
    public enum ReleasePostProcessLogLevel
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    /// <summary>One log entry produced by a post-processor during a post-process run.</summary>
    // ReSharper disable NotAccessedPositionalProperty.Global
    public record ReleasePostProcessLog(
        string PostProcessorId,
        ReleasePostProcessLogLevel Level,
        string Message);
    // ReSharper enable NotAccessedPositionalProperty.Global
}