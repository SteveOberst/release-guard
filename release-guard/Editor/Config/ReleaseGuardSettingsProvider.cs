using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Core.Config;
using ReleaseGuard.Editor.Core.Config.Attributes;
using ReleaseGuard.Editor.Core.Config.Reader;
using ReleaseGuard.Editor.Core.Config.Renderer;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;

namespace ReleaseGuard.Editor.Config
{
    /// <summary>
    /// Registers the Release Guard pages in Project Settings.
    ///
    /// The full page tree is driven by the component reader: class-level
    /// <see cref="SettingsPage"/> controls which sub-objects become pages,
    /// <see cref="SettingsHeader"/> adds section headings,
    /// <see cref="ConditionalWarning"/> adds warning boxes, and
    /// <see cref="IDynamicSettingsPage"/> implementations contribute runtime-discovered children
    /// (e.g. plugin sub-pages).
    /// </summary>
    internal static class ReleaseGuardSettingsProvider
    {
        private const string RootPath = "Project/Release Guard";

        private static readonly SettingsRenderer Renderer = new ReleaseGuardSettingsRenderer();

        [SettingsProviderGroup]
        public static SettingsProvider[] CreateAll()
        {
            var settings = ReleaseGuardSettings.LoadOrCreate();
            return Renderer.CreateProviders(RootPath, settings).ToArray();
        }

        private sealed class ReleaseGuardSettingsRenderer : SettingsRenderer
        {
            protected override HashSet<string> GetRootKeywords() =>
                Keywords("release", "guard", "overview", "status", "audit", "build");
        }
    }
}