namespace ReleaseGuard.Editor.Core.Config
{
    public sealed class ReadContext
    {
        public SettingsComponentReader Reader           { get; init; }
        public string                  ParentPath       { get; init; }
        public int                     ContainerIndex   { get; init; }
        public SettingsComponent       PrimaryComponent { get; init; }
        public object                  Instance         { get; init; }
    }
}
