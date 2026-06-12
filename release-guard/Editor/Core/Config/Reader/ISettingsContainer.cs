using System.Collections.Generic;
using ReleaseGuard.Editor.Core.Config.Renderer;
using UnityEditor;

namespace ReleaseGuard.Editor.Core.Config.Reader
{
    public interface ISettingsContainer
    {
    }

    public interface ISettingsPage : ISettingsContainer
    {
    }

    public interface ISettingsSection : ISettingsContainer
    {
    }

    public interface IDynamicSettingsPage : ISettingsPage
    {
        IEnumerable<SettingsProvider> ResolveChildren(
            SettingsComponentReader reader, string parentPath, SettingsRenderer renderer);
    }
}