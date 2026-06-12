using System.Collections.Generic;

namespace ReleaseGuard.Editor.Core.Config
{
    public enum ComponentReadOrder { Before, Primary, After }

    public interface IComponentReader
    {
        ComponentReadOrder Order    { get; }
        int                Priority { get; }
        bool               CanRead(object source);
        IEnumerable<SettingsComponent> Read(object source, ReadContext context);
    }
}
