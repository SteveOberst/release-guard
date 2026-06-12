using System.Reflection;

namespace ReleaseGuard.Editor.Core.Config
{
    public sealed class SectionGroupComponent : ContainerComponent
    {
        public FieldInfo FieldInfo { get; init; }

        public override void Render(SettingsRenderer renderer)
        {
            renderer.Section(DisplayName);
            foreach (var child in Children)
                child.Render(renderer);
        }
    }
}
