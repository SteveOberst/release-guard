using System;
using System.Collections.Generic;
using ReleaseGuard.Editor.Core.Config.Renderer;
using ReleaseGuard.Editor.Util;
using UnityEditor;

namespace ReleaseGuard.Editor.Core.Config.Components
{
    public sealed class ExclusionListComponent : SerializedFieldComponent
    {
        private const int PreviewStoreMax = 50;
        private const int PreviewShowMax = 12;

        private bool _expanded;
        private string _key;
        private int _matchCount;
        private List<string> _matches = new();

        protected override void DrawValue(SettingsRenderer renderer)
        {
            var patternsProp = Property.FindPropertyRelative("patterns");
            renderer.LineListField(patternsProp, DisplayName, Tooltip);
            DrawPreview(patternsProp);
        }

        private void DrawPreview(SerializedProperty patternsProp)
        {
            _expanded = EditorGUILayout.Foldout(
                _expanded, "Preview matching assets", true);
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
                if (!path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) continue;
                if (!matcher.IsExcluded(path)) continue;

                _matchCount++;
                if (_matches.Count < PreviewStoreMax)
                    _matches.Add(path);
            }

            _matches.Sort(StringComparer.OrdinalIgnoreCase);
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