using System.Collections.Generic;
using ReleaseGuard.Editor.Util;
using UnityEditor;

namespace ReleaseGuard.Editor.Core.Config
{
    /// <summary>
    /// Stateful widget that draws a collapsible "Preview matching assets" foldout beneath an
    /// excluded-asset-patterns list. One instance per renderer keeps expand/collapse state
    /// and caches match results per pattern key to avoid per-frame queries.
    /// </summary>
    public sealed class ExclusionListRenderer
    {
        private const int PreviewStoreMax = 50;
        private const int PreviewShowMax = 12;

        private bool _expanded;
        private string _key;
        private List<string> _matches = new();
        private int _matchCount;

        /// <summary>
        /// Draws the preview foldout for the array-of-strings property at
        /// <paramref name="patternsProp"/>.
        /// </summary>
        public void DrawPreview(SerializedProperty patternsProp)
        {
            _expanded = EditorGUILayout.Foldout(
                _expanded, "Preview matching assets", toggleOnLabelClick: true);
            if (!_expanded)
                return;

            var patterns = ReadPatterns(patternsProp);
            var key = string.Join("\n", patterns);
            if (key != _key)
            {
                _key = key;
                ComputePreview(patterns);
            }

            using (new EditorGUI.IndentLevelScope())
            {
                if (_matchCount == 0)
                {
                    EditorGUILayout.LabelField("No assets match the current patterns.", EditorStyles.miniLabel);
                    return;
                }

                EditorGUILayout.LabelField(
                    $"{_matchCount} asset(s) excluded from release issues:", EditorStyles.miniLabel);
                for (var i = 0; i < _matches.Count && i < PreviewShowMax; i++)
                    EditorGUILayout.LabelField(_matches[i], EditorStyles.miniLabel);
                if (_matchCount > PreviewShowMax)
                    EditorGUILayout.LabelField(
                        $"...and {_matchCount - PreviewShowMax} more.", EditorStyles.miniLabel);
            }
        }

        private void ComputePreview(List<string> patterns)
        {
            _matches = new List<string>();
            _matchCount = 0;

            var matcher = new AssetExclusionMatcher(patterns);
            if (!matcher.HasPatterns) return;

            foreach (var path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (!matcher.IsExcluded(path)) continue;

                _matchCount++;
                if (_matches.Count < PreviewStoreMax)
                    _matches.Add(path);
            }

            _matches.Sort(System.StringComparer.OrdinalIgnoreCase);
        }

        private static List<string> ReadPatterns(SerializedProperty prop)
        {
            var patterns = new List<string>(prop.arraySize);
            for (var i = 0; i < prop.arraySize; i++)
                patterns.Add(prop.GetArrayElementAtIndex(i).stringValue);
            return patterns;
        }
    }
}
