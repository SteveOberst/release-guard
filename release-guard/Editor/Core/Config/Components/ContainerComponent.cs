using System.Collections.Generic;

namespace ReleaseGuard.Editor.Core.Config
{
    public abstract class ContainerComponent : SettingsComponent
    {
        public IReadOnlyList<SettingsComponent> Children { get; init; }
    }
}
