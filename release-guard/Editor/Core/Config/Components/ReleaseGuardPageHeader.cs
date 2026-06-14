using System;
using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Runtime;
using ReleaseGuard.Editor.Core.Build;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Core.Config.Components
{
    /// <summary>
    /// Static IMGUI helper injected at the top of every Release Guard settings page via
    /// <c>guiHandler</c> wrapping in <c>ReleaseGuardSettingsProvider.CreateAll()</c>.
    /// Draws the environment badge, profile dropdown, and optional mismatch warning.
    /// </summary>
    internal static class ReleaseGuardPageHeader
    {
        // Width of the environment label (e.g. "CI_GitHub", "UnityEditor") in the header strip
        private const float EnvironmentBadgeWidth = 120f;

        // Width of the "Profile:" prefix label
        private const float ProfileLabelWidth = 46f;

        // Width of the profile selector dropdown
        private const float ProfileDropdownWidth = 160f;

        public static void DrawTitleBar(ReleaseGuardRegistry registry)
        {
            if (registry == null || registry.profiles == null || registry.profiles.Count == 0)
                return;

            var profiles = registry.profiles;
            var names = profiles.Select(p => p.displayName).ToArray();
            var ids = profiles.Select(p => p.id).ToArray();
            var currentId = ActiveProfileState.CurrentProfileId;
            var currentIndex = Math.Max(Array.IndexOf(ids, currentId), 0);

            var env = BuildEnvironmentDetector.Detect();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(
                    env.Environment.ToString(),
                    EditorStyles.miniLabel,
                    GUILayout.Width(EnvironmentBadgeWidth));
                EditorGUILayout.LabelField(
                    "Profile:",
                    EditorStyles.miniLabel,
                    GUILayout.Width(ProfileLabelWidth));

                EditorGUI.BeginChangeCheck();
                var nextIndex = EditorGUILayout.Popup(
                    currentIndex, names, GUILayout.Width(ProfileDropdownWidth));
                if (EditorGUI.EndChangeCheck() && nextIndex != currentIndex)
                    ActiveProfileState.CurrentProfileId = ids[nextIndex];
            }
        }

        public static void DrawBodyWarning(ReleaseGuardRegistry registry)
        {
            if (registry == null || registry.profiles == null || registry.profiles.Count == 0)
                return;

            DrawMismatchWarning(registry.profiles, BuildEnvironmentDetector.Detect());
        }

        private static void DrawMismatchWarning(List<ReleaseGuardProfile> profiles, DetectedBuildEnvironment env)
        {
            var wouldUseId = ProfileSettingsResolver.ResolveProfileId(
                profiles, EditorUserBuildSettings.development, env);
            if (wouldUseId == null) return;

            var editingId = ActiveProfileState.CurrentProfileId;
            if (string.Equals(wouldUseId, editingId, StringComparison.OrdinalIgnoreCase))
                return;

            var editing = profiles.FirstOrDefault(p => p.id == editingId);
            var actual = profiles.FirstOrDefault(p => p.id == wouldUseId);
            if (editing == null || actual == null) return;

            EditorGUILayout.HelpBox(
                $"Your current build settings would activate '{actual.displayName}', " +
                $"but you are editing '{editing.displayName}'.",
                MessageType.Warning);
        }
    }
}
