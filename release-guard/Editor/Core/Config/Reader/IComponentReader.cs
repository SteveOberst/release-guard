using System.Collections.Generic;
using ReleaseGuard.Editor.Core.Config.Components;

namespace ReleaseGuard.Editor.Core.Config.Reader
{
    public enum ComponentReadOrder
    {
        Before,
        Primary,
        After
    }

    public interface IComponentReader
    {
        ComponentReadOrder Order { get; }
        int Priority { get; }
        bool CanRead(object source);
        IEnumerable<SettingsComponent> Read(object source, ReadContext context);
    }
}