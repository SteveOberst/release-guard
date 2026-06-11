namespace ReleaseGuard.Editor.Core.Transforming
{
    /// <summary>Severity of a single transformer log entry.</summary>
    public enum ReleaseTransformLogLevel
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }

    /// <summary>One log entry produced by a transformer during a transform run.</summary>
    // ReSharper disable NotAccessedPositionalProperty.Global
    public record ReleaseTransformLog(
        string TransformerId,
        ReleaseTransformLogLevel Level,
        string Message);
    // ReSharper enable NotAccessedPositionalProperty.Global
}