using ReleaseGuard.Editor.Core.Config.Components;

namespace ReleaseGuard.Editor.Core.Config.Reader
{
    public sealed class ReadContext
    {
        public SettingsComponentReader Reader { get; init; }
        public string ParentPath { get; init; }
        public int ContainerIndex { get; init; }
        public SettingsComponent PrimaryComponent { get; init; }
        public object Instance { get; init; }
    }
}