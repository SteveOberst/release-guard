using System.Collections.Generic;
using UnityEditor;

namespace ReleaseGuard.Editor.Core.Config
{
    public interface ISettingsContainer { }
    public interface ISettingsPage    : ISettingsContainer { }
    public interface ISettingsSection : ISettingsContainer { }

    public interface IDynamicSettingsPage : ISettingsPage
    {
        IEnumerable<SettingsProvider> ResolveChildren(
            SettingsComponentReader reader, string parentPath, SettingsRenderer renderer);
    }
}
