using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace ReleaseGuard.Editor.Util
{
    /// <summary>
    /// Lists the names of Unity Build Profiles defined in the project, used to populate the
    /// "Pick build profile" dropdown on the Profiles page.
    ///
    /// Returns an empty list on editors that predate Build Profiles: the <c>"t:BuildProfile"</c>
    /// search simply matches nothing there, so no reflection or version checks are needed.
    /// </summary>
    internal static class UnityBuildProfileSuggestionProvider
    {
        public static IReadOnlyList<string> GetAll()
        {
            return AssetDatabase.FindAssets("t:BuildProfile")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .OrderBy(name => name, System.StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}