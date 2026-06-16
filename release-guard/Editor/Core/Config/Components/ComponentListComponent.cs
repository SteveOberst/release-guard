using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.Config.Attributes;
using ReleaseGuard.Editor.Core.Config.Renderer;
using ReleaseGuard.Editor.Core.Config.Types;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace ReleaseGuard.Editor.Core.Config.Components
{
    /// <summary>
    /// Renders the <c>componentToggles</c> field as a searchable, scrollable list of every
    /// registered Release Guard component. Each row has an enable/disable toggle and a foldout
    /// arrow; expanding a component reveals its per-component settings inline. Components that
    /// have no settings beyond the enabled toggle show no foldout arrow.
    ///
    /// Advisory management lives on the dedicated Advisories settings page, linked from the
    /// bottom of this component list.
    /// </summary>
    internal sealed class ComponentListComponent : SerializedFieldComponent
    {
        private const float ListViewHeight = 400f;
        private const float RowHeight = 22f;
        private const float ToggleWidth = 18f;
        private const float FoldoutWidth = 14f;
        private const float BadgeWidth = 160f;
        private const float Pad = 4f;
        private const float MinNameWidth = 40f;
        private IReadOnlyList<ComponentCatalogEntry> _catalog;
        private SearchField _searchField;
        private string _search = string.Empty;
        private Vector2 _scroll;
        private GUIStyle _badgeStyle;

        private readonly HashSet<string> _expandedIds = new(StringComparer.OrdinalIgnoreCase);

        protected override void DrawValue(SettingsRenderer renderer)
        {
            if (Property == null) return;

            _catalog ??= ComponentCatalog.GetAll();

            EditorGUILayout.LabelField(
                "Enable or disable individual components. Expand a component to configure its settings.",
                EditorStyles.wordWrappedMiniLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawToolbar();
                DrawList();
            }

            DrawAdvisoriesLink(renderer);
        }

        // -----------------------------------------------------------------
        // Toolbar
        // -----------------------------------------------------------------

        private void DrawToolbar()
        {
            _searchField ??= new SearchField();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _search = _searchField.OnToolbarGUI(_search) ?? string.Empty;
            }
        }

        // -----------------------------------------------------------------
        // Component list
        // -----------------------------------------------------------------

        private void DrawList()
        {
            var entriesProp = Property.FindPropertyRelative("entries");
            var trimmedSearch = _search.Trim();
            var filtering = !string.IsNullOrEmpty(trimmedSearch);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(ListViewHeight));

            var matched = false;
            foreach (var entry in _catalog)
            {
                if (filtering && !Matches(entry, trimmedSearch))
                    continue;

                matched = true;
                DrawEntry(entry, entriesProp);
            }

            if (!matched)
                EditorGUILayout.LabelField(
                    $"No components match \"{trimmedSearch}\".",
                    EditorStyles.miniLabel);

            EditorGUILayout.EndScrollView();
        }

        // -----------------------------------------------------------------
        // Single component entry (row + optional expanded settings)
        // -----------------------------------------------------------------

        private void DrawEntry(ComponentCatalogEntry entry, SerializedProperty entriesProp)
        {
            var isEnabled = GetIsEnabled(entriesProp, entry.Id);
            var isExpanded = _expandedIds.Contains(entry.Id);
            var hasSettings = HasExpandableSettings(entry);

            var rowRect = EditorGUILayout.GetControlRect(false, RowHeight);
            var centerY = rowRect.y + (rowRect.height - EditorGUIUtility.singleLineHeight) * 0.5f;

            // Toggle
            var toggleRect = new Rect(rowRect.x, centerY, ToggleWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            var newEnabled = EditorGUI.Toggle(toggleRect, isEnabled);
            if (EditorGUI.EndChangeCheck())
                SetEnabled(entriesProp, entry.Id, newEnabled, entry.SettingsType);

            // Foldout arrow
            var foldoutX = toggleRect.xMax + Pad;
            var foldoutRect = new Rect(foldoutX, centerY, FoldoutWidth, EditorGUIUtility.singleLineHeight);
            if (hasSettings)
            {
                var newExpanded = EditorGUI.Foldout(foldoutRect, isExpanded, GUIContent.none, true);
                if (newExpanded != isExpanded)
                {
                    if (newExpanded) _expandedIds.Add(entry.Id);
                    else _expandedIds.Remove(entry.Id);
                }
            }

            // Name label
            var nameX = foldoutX + FoldoutWidth + Pad;
            var badgeX = rowRect.xMax - BadgeWidth;
            var nameWidth = Mathf.Max(MinNameWidth, badgeX - nameX - Pad);
            var nameRect = new Rect(nameX, centerY, nameWidth, EditorGUIUtility.singleLineHeight);

            using (new EditorGUI.DisabledScope(!isEnabled))
                EditorGUI.LabelField(nameRect, entry.DisplayName);

            // Phase badge
            if (entry.Phases?.Count > 0)
            {
                _badgeStyle ??= new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
                var badgeRect = new Rect(badgeX, centerY, BadgeWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(badgeRect, string.Join(", ", entry.Phases), _badgeStyle);
            }

            // Expanded settings panel
            if (isExpanded && hasSettings)
                DrawExpandedSettings(entriesProp, entry);
        }

        private static bool HasExpandableSettings(ComponentCatalogEntry entry)
        {
            if (entry.SettingsType == null || entry.SettingsType == typeof(ReleaseGuardComponentSettings))
                return false;
            return entry.SettingsType
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Length > 0;
        }

        private void DrawExpandedSettings(SerializedProperty entriesProp, ComponentCatalogEntry entry)
        {
            var entryProp = FindOrCreateEntry(entriesProp, entry.Id, entry.SettingsType);
            if (entryProp == null) return;

            var fields = entry.SettingsType
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            EditorGUILayout.BeginVertical();
            using (new EditorGUI.IndentLevelScope(1))
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (var field in fields)
                {
                    var subProp = entryProp.FindPropertyRelative(field.Name);
                    if (subProp == null) continue;

                    var labelText = field.GetCustomAttribute<SettingsLabel>()?.Label
                                    ?? ObjectNames.NicifyVariableName(field.Name);
                    var tooltipText = field.GetCustomAttribute<TooltipAttribute>()?.tooltip ?? string.Empty;
                    var content = new GUIContent(labelText, tooltipText);

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(subProp, content, true);
                    if (EditorGUI.EndChangeCheck())
                        subProp.serializedObject.ApplyModifiedProperties();

                    var warning = field.GetCustomAttribute<ConditionalWarning>();
                    if (warning != null
                        && subProp.propertyType == SerializedPropertyType.Boolean
                        && subProp.boolValue)
                    {
                        EditorGUILayout.HelpBox(warning.Message, MessageType.Warning);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        // -----------------------------------------------------------------
        // Advisories page link
        // -----------------------------------------------------------------

        private static void DrawAdvisoriesLink(SettingsRenderer renderer)
        {
            EditorGUILayout.Space();
            renderer.Section("Advisories");
            RenderPrimitives.SectionLink(
                "Advisories",
                ReleaseGuardSettingsProvider.AdvisoriesPath,
                "Manage dismissed advisories");
        }

        // -----------------------------------------------------------------
        // Enable/disable via SerializedProperty
        // -----------------------------------------------------------------

        private static bool GetIsEnabled(SerializedProperty entriesProp, string componentId)
        {
            var entryProp = FindEntryProperty(entriesProp, componentId);
            if (entryProp != null)
                return entryProp.FindPropertyRelative("enabled")?.boolValue ?? true;
            return !ComponentToggleList.DefaultDisabledIds.Contains(componentId);
        }

        private static void SetEnabled(
            SerializedProperty entriesProp, string componentId, bool enabled, Type settingsType)
        {
            var entryProp = FindEntryProperty(entriesProp, componentId);
            if (entryProp != null)
            {
                var enabledProp = entryProp.FindPropertyRelative("enabled");
                if (enabledProp != null)
                {
                    enabledProp.boolValue = enabled;
                    entriesProp.serializedObject.ApplyModifiedProperties();
                }

                return;
            }

            // Only create an entry when disabling; enabling means "use default" which requires no entry.
            if (enabled) return;

            var settings = settingsType != null
                ? (ReleaseGuardComponentSettings)Activator.CreateInstance(settingsType)
                : new ReleaseGuardComponentSettings();
            settings.componentId = componentId;
            settings.enabled = false;

            var idx = entriesProp.arraySize;
            entriesProp.arraySize++;
            entriesProp.GetArrayElementAtIndex(idx).managedReferenceValue = settings;
            entriesProp.serializedObject.ApplyModifiedProperties();
        }

        // -----------------------------------------------------------------
        // Entry lookup / creation
        // -----------------------------------------------------------------

        private static SerializedProperty FindEntryProperty(SerializedProperty entriesProp, string componentId)
        {
            for (var i = 0; i < entriesProp.arraySize; i++)
            {
                var el = entriesProp.GetArrayElementAtIndex(i);
                var cid = el.FindPropertyRelative("componentId");
                if (cid != null && string.Equals(cid.stringValue, componentId, StringComparison.OrdinalIgnoreCase))
                    return el;
            }

            return null;
        }

        private static SerializedProperty FindOrCreateEntry(
            SerializedProperty entriesProp, string componentId, Type settingsType)
        {
            var existing = FindEntryProperty(entriesProp, componentId);
            if (existing != null) return existing;

            var settings = settingsType != null
                ? (ReleaseGuardComponentSettings)Activator.CreateInstance(settingsType)
                : new ReleaseGuardComponentSettings();
            settings.componentId = componentId;

            var idx = entriesProp.arraySize;
            entriesProp.arraySize++;
            entriesProp.GetArrayElementAtIndex(idx).managedReferenceValue = settings;
            entriesProp.serializedObject.ApplyModifiedProperties();

            return entriesProp.GetArrayElementAtIndex(idx);
        }

        // -----------------------------------------------------------------
        // Filtering
        // -----------------------------------------------------------------

        private static bool Matches(ComponentCatalogEntry entry, string query) =>
            (!string.IsNullOrEmpty(entry.DisplayName) &&
             entry.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
            (!string.IsNullOrEmpty(entry.Id) &&
             entry.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}