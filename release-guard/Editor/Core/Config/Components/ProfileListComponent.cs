using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Config.Renderer;
using ReleaseGuard.Editor.Core.Runtime;
using ReleaseGuard.Editor.Core.Build;
using ReleaseGuard.Editor.Util;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ReleaseGuard.Editor.Core.Config.Components
{
    /// <summary>
    /// Renders the Profiles management page as a master-detail layout:
    /// <list type="bullet">
    /// <item>A reorderable list (master) where drag order is build-time priority, each row showing
    /// the profile name, a readable condition summary, and an "Active" pill on the loaded profile.</item>
    /// <item>A detail card (detail) for the selected profile: rename, edit the activation condition,
    /// and run per-profile actions (Load / Duplicate / Delete).</item>
    /// </list>
    ///
    /// Layout metrics derive from Unity's own editor constants (<see cref="EditorGUIUtility.singleLineHeight"/>,
    /// <see cref="EditorGUIUtility.standardVerticalSpacing"/>) wherever possible. The few remaining
    /// values are structural to this control (column widths, pill size) and are named constants
    /// describing their purpose, not bare literals scattered through the draw code.
    /// </summary>
    internal sealed class ProfileListComponent : SerializedFieldComponent
    {
        // Clears the space the ReorderableList reserves on the left for the drag grip.
        private const float RowContentLeftInset = 18f;

        // Trailing margin so row content does not touch the list's right edge.
        private const float RowRightInset = 4f;

        // Horizontal gap between adjacent row segments.
        private const float ColumnGap = 6f;

        // Fixed width of the name segment so the condition column lines up across every row.
        private const float NameColumnWidth = 170f;

        // The condition summary never collapses below this width.
        private const float MinConditionWidth = 60f;

        // Width of the compact "Active" pill on the loaded profile's row.
        private const float ActivePillWidth = 52f;

        // Action-button widths in the detail card.
        private const float DetailActionWidth = 110f;
        private const float SmallButtonWidth = 80f;

        // Settings root path; "Load profile" focuses this so the other pages rebind to the choice.
        private const string SettingsRootPath = "Project/Release Guard";

        // Activation strategies in the order shown in the "Activate when" dropdown.
        private static readonly ActivationStrategy[] StrategyOrder =
        {
            ActivationStrategy.IsReleaseBuild,
            ActivationStrategy.IsDevelopmentBuild,
            ActivationStrategy.IsCI,
            ActivationStrategy.IsCIAndDevelopmentBuild,
            ActivationStrategy.UnityBuildProfileNames,
            ActivationStrategy.Always,
        };

        private static readonly string[] StrategyLabels =
        {
            "Release builds",
            "Development builds",
            "CI builds",
            "Development builds in CI",
            "Specific Unity Build Profiles",
            "Any build (catch-all)",
        };

        // The "Active" pill color. Muted green that reads on both editor skins.
        private static readonly Color ActivePillColorPro = new(0.27f, 0.52f, 0.34f, 1f);
        private static readonly Color ActivePillColorLight = new(0.36f, 0.62f, 0.42f, 1f);
        private static readonly Color PillTextColor = new(0.94f, 0.97f, 0.94f, 1f);

        private ReorderableList _list;
        private ReleaseGuardRegistry _registry;
        private int _selectedIndex;
        private string _pendingSelectId;
        private GUIStyle _pillStyle;
        private GUIStyle _condStyle;

        private GUIStyle PillStyle => _pillStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = PillTextColor },
        };

        private GUIStyle ConditionStyle => _condStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
        };

        private static float RowHeight =>
            EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 2f;

        protected override void DrawValue(SettingsRenderer renderer)
        {
            if (Property == null) return;
            _registry = Property.serializedObject.targetObject as ReleaseGuardRegistry;
            if (_registry == null) return;

            EditorGUILayout.LabelField(
                "Profiles are evaluated top to bottom. The first profile whose condition matches the " +
                "build is used; drag to reorder priority.",
                EditorStyles.wordWrappedMiniLabel);

            DrawConflictWarning();

            ResolvePendingSelection();
            ClampSelection();

            DrawReorderableList();

            EditorGUILayout.LabelField(
                "Double-click a profile to load it and edit its settings on the other pages.",
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();
            DrawDetailCard();
        }

        // -----------------------------------------------------------------------
        // Master: reorderable list
        // -----------------------------------------------------------------------

        private void DrawReorderableList()
        {
            _list ??= BuildList();
            _list.serializedProperty = Property;
            _list.index = _selectedIndex;

            var height = _list.GetHeight();
            var rect = GUILayoutUtility.GetRect(0f, height, GUILayout.ExpandWidth(true));
            _list.DoList(rect);
        }

        private ReorderableList BuildList()
        {
            var list = new ReorderableList(Property.serializedObject, Property,
                draggable: true, displayHeader: true,
                displayAddButton: true, displayRemoveButton: false)
            {
                elementHeight = RowHeight,
                headerHeight = RowHeight,
                multiSelect = false,
            };

            list.drawHeaderCallback = DrawListHeader;
            list.drawElementCallback = (rect, index, _, _) => DrawRow(rect, index);
            list.onSelectCallback = l => _selectedIndex = l.index;
            list.onAddDropdownCallback = (btnRect, _) => ShowAddMenu(btnRect);
            list.onReorderCallback = l =>
            {
                l.serializedProperty.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(_registry);
                _selectedIndex = l.index;
            };

            return list;
        }

        private void DrawListHeader(Rect rect)
        {
            SplitRow(rect, out var nameRect, out var condRect, out _);
            EditorGUI.LabelField(nameRect, "Profile", EditorStyles.miniBoldLabel);
            EditorGUI.LabelField(condRect, "Condition", EditorStyles.miniBoldLabel);
        }

        private void DrawRow(Rect rect, int index)
        {
            if (index < 0 || index >= _registry.profiles.Count) return;

            var profile = _registry.profiles[index];
            var isActive = string.Equals(profile.id, ActiveProfileState.CurrentProfileId,
                StringComparison.OrdinalIgnoreCase);

            // Double-click loads the profile (non-active rows only).
            if (!isActive
                && Event.current.type == EventType.MouseDown
                && Event.current.clickCount == 2
                && rect.Contains(Event.current.mousePosition))
            {
                ActiveProfileState.CurrentProfileId = profile.id;
                SettingsService.OpenProjectSettings(SettingsRootPath);
                Event.current.Use();
                return;
            }

            SplitRow(rect, out var nameRect, out var condRect, out var pillRect);

            var name = string.IsNullOrEmpty(profile.displayName) ? "(unnamed)" : profile.displayName;
            EditorGUI.LabelField(nameRect, name);
            EditorGUI.LabelField(condRect, Summarize(profile), ConditionStyle);

            if (isActive)
                DrawActivePill(pillRect);
        }

        private void DrawActivePill(Rect pillRect)
        {
            EditorGUI.DrawRect(pillRect, EditorGUIUtility.isProSkin ? ActivePillColorPro : ActivePillColorLight);
            GUI.Label(pillRect, "Active", PillStyle);
        }

        // Single source of truth for the row column rects. The pill column is always reserved
        // so every row (header, active, and non-active) has identical column positions.
        // The caller decides whether to draw the pill contents based on whether the row is active.
        private static void SplitRow(Rect rect,
            out Rect nameRect, out Rect condRect, out Rect pillRect)
        {
            var line = EditorGUIUtility.singleLineHeight;
            var y = rect.y + (rect.height - line) * 0.5f;
            var left = rect.x + RowContentLeftInset;
            var right = rect.xMax - RowRightInset;

            // Pill column is always reserved at the right edge.
            pillRect = new Rect(right - ActivePillWidth, y, ActivePillWidth, line);
            right = pillRect.x - ColumnGap;

            nameRect = new Rect(left, y, NameColumnWidth, line);
            var condLeft = nameRect.xMax + ColumnGap;
            condRect = new Rect(condLeft, y, Mathf.Max(MinConditionWidth, right - condLeft), line);
        }

        // -----------------------------------------------------------------------
        // Detail: selected-profile card
        // -----------------------------------------------------------------------

        private void DrawDetailCard()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _registry.profiles.Count) return;

            var profile = _registry.profiles[_selectedIndex];
            var element = Property.GetArrayElementAtIndex(_selectedIndex);
            var displayNameProp = element.FindPropertyRelative("displayName");
            var activationProp = element.FindPropertyRelative("activation");
            var strategyProp = activationProp.FindPropertyRelative("strategy");
            var namesProp = activationProp.FindPropertyRelative("unityBuildProfileNames");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(displayNameProp, new GUIContent("Name"));
                    if (profile.isDefault)
                        EditorGUILayout.LabelField("Built-in", EditorStyles.miniBoldLabel,
                            GUILayout.Width(SmallButtonWidth));
                }

                EditorGUILayout.LabelField(
                    $"Asset id: {profile.id}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField(
                    ProfileSettingsRegistry.AssetPath(profile.id), EditorStyles.miniLabel);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Activation", EditorStyles.boldLabel);
                DrawActivationEditor(profile, strategyProp, namesProp);

                EditorGUILayout.Space();
                DrawLiveMatchHint(profile);

                EditorGUILayout.Space();
                DrawDetailActions(profile);

                EditorGUILayout.LabelField(
                    "The loaded profile's settings appear on the General, Components, and Plugins pages.",
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawActivationEditor(
            ReleaseGuardProfile profile, SerializedProperty strategyProp, SerializedProperty namesProp)
        {
            var current = (ActivationStrategy)strategyProp.enumValueIndex;
            var currentIndex = Mathf.Max(0, Array.IndexOf(StrategyOrder, current));

            if (profile.isDefault)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.Popup("Activate when", currentIndex, StrategyLabels);
                EditorGUILayout.LabelField(
                    "Built-in profile. Its activation condition is fixed and cannot be changed.",
                    EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField(Describe(current), EditorStyles.wordWrappedMiniLabel);
                return;
            }

            EditorGUI.BeginChangeCheck();
            var nextIndex = EditorGUILayout.Popup("Activate when", currentIndex, StrategyLabels);
            if (EditorGUI.EndChangeCheck())
                strategyProp.enumValueIndex = (int)StrategyOrder[nextIndex];

            EditorGUILayout.LabelField(Describe((ActivationStrategy)strategyProp.enumValueIndex),
                EditorStyles.wordWrappedMiniLabel);

            if ((ActivationStrategy)strategyProp.enumValueIndex == ActivationStrategy.UnityBuildProfileNames)
                DrawBuildProfileNames(namesProp);
        }

        private void DrawBuildProfileNames(SerializedProperty namesProp)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Build profiles", EditorStyles.miniBoldLabel);

            for (var i = 0; i < namesProp.arraySize; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(namesProp.GetArrayElementAtIndex(i), GUIContent.none);
                    if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(SmallButtonWidth)))
                    {
                        namesProp.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Pick build profile", EditorStyles.miniButton,
                        GUILayout.Width(DetailActionWidth)))
                    ShowBuildProfilePicker();

                if (GUILayout.Button("Add manually", EditorStyles.miniButton,
                        GUILayout.Width(DetailActionWidth)))
                {
                    namesProp.arraySize++;
                    namesProp.GetArrayElementAtIndex(namesProp.arraySize - 1).stringValue = string.Empty;
                }

                GUILayout.FlexibleSpace();
            }

            var suggestions = UnityBuildProfileSuggestionProvider.GetAll();
            if (suggestions.Count == 0)
                EditorGUILayout.HelpBox(
                    "No Unity Build Profiles found in this project. " +
                    "Create one in Build Settings > Profiles (Unity 6+), then use 'Pick build profile' " +
                    "to select it, or type the name directly via 'Add manually'.",
                    MessageType.Info);
            else if (namesProp.arraySize == 0)
                EditorGUILayout.LabelField(
                    "No build profiles listed. This profile will never match until you add one.",
                    EditorStyles.wordWrappedMiniLabel);
        }

        private void ShowBuildProfilePicker()
        {
            var profile = _registry.profiles[_selectedIndex];
            var names = profile.activation.unityBuildProfileNames ??= new List<string>();
            var existing = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

            var menu = new GenericMenu();
            var all = UnityBuildProfileSuggestionProvider.GetAll();
            if (all.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Build Profiles found in this project"));
            }
            else
            {
                foreach (var name in all)
                {
                    if (existing.Contains(name))
                    {
                        menu.AddDisabledItem(new GUIContent(name));
                        continue;
                    }

                    var captured = name;
                    menu.AddItem(new GUIContent(name), false, () =>
                    {
                        names.Add(captured);
                        EditorUtility.SetDirty(_registry);
                        Property.serializedObject.Update();
                    });
                }
            }

            menu.ShowAsContext();
        }

        private void DrawLiveMatchHint(ReleaseGuardProfile profile)
        {
            var env = BuildEnvironmentDetector.Detect();
            var isDev = EditorUserBuildSettings.development;
            var matchedId = ProfileSettingsResolver.ResolveProfileId(_registry.profiles, isDev, env);

            if (string.Equals(matchedId, profile.id, StringComparison.OrdinalIgnoreCase))
                EditorGUILayout.HelpBox(
                    "With your current build settings, this profile would be used for the build.",
                    MessageType.Info);
        }

        private void DrawDetailActions(ReleaseGuardProfile profile)
        {
            var isActive = string.Equals(profile.id, ActiveProfileState.CurrentProfileId,
                StringComparison.OrdinalIgnoreCase);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (isActive)
                {
                    using (new EditorGUI.DisabledScope(true))
                        GUILayout.Button("Loaded", GUILayout.Width(DetailActionWidth));
                }
                else if (GUILayout.Button(
                             new GUIContent("Load profile",
                                 "Show this profile's settings on the General, Components, and Plugins pages."),
                             GUILayout.Width(DetailActionWidth)))
                {
                    ActiveProfileState.CurrentProfileId = profile.id;
                    SettingsService.OpenProjectSettings(SettingsRootPath);
                    return;
                }

                if (GUILayout.Button("Duplicate", GUILayout.Width(DetailActionWidth)))
                    DuplicateProfile(_selectedIndex);

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(profile.isDefault))
                {
                    var tip = profile.isDefault
                        ? "Built-in profiles cannot be deleted."
                        : "Delete this profile and its settings asset.";
                    if (GUILayout.Button(new GUIContent("Delete", tip), GUILayout.Width(DetailActionWidth)))
                        ConfirmDeleteProfile(_selectedIndex);
                }
            }
        }

        // -----------------------------------------------------------------------
        // Condition summaries and descriptions
        // -----------------------------------------------------------------------

        private static string Summarize(ReleaseGuardProfile profile) => profile.activation.strategy switch
        {
            ActivationStrategy.IsReleaseBuild => "Release builds",
            ActivationStrategy.IsDevelopmentBuild => "Development builds",
            ActivationStrategy.IsCI => "CI builds",
            ActivationStrategy.IsCIAndDevelopmentBuild => "Development builds in CI",
            ActivationStrategy.UnityBuildProfileNames =>
                profile.activation.unityBuildProfileNames is { Count: > 0 }
                    ? "Build profiles: " + string.Join(", ", profile.activation.unityBuildProfileNames)
                    : "Build profiles: (none set)",
            ActivationStrategy.Always => "Any build",
            _ => profile.activation.strategy.ToString()
        };

        private static string Describe(ActivationStrategy strategy) => strategy switch
        {
            ActivationStrategy.IsReleaseBuild =>
                "Matches any build that does not use the Development Build option.",
            ActivationStrategy.IsDevelopmentBuild =>
                "Matches builds made with the Development Build option enabled.",
            ActivationStrategy.IsCI =>
                "Matches builds running in a continuous integration environment (batch mode or a known CI provider).",
            ActivationStrategy.IsCIAndDevelopmentBuild =>
                "Matches Development Builds running in a CI environment.",
            ActivationStrategy.UnityBuildProfileNames =>
                "Matches when the active Unity Build Profile is one of the names listed below.",
            ActivationStrategy.Always =>
                "Always matches. Place this at the bottom of the list as a final catch-all.",
            _ => string.Empty
        };

        // -----------------------------------------------------------------------
        // Conflict warning
        // -----------------------------------------------------------------------

        private void DrawConflictWarning()
        {
            var conflicts = ProfileConflictAnalyzer.FindConflicts(_registry.profiles);
            if (conflicts.Count == 0) return;

            var pairs = string.Join("\n", conflicts.Select(c => $"  {c.profileA} / {c.profileB}"));
            EditorGUILayout.HelpBox(
                "These profiles can both match the same build. The one higher in the list wins:\n" + pairs,
                MessageType.Warning);
        }

        // -----------------------------------------------------------------------
        // Selection tracking
        // -----------------------------------------------------------------------

        private void ResolvePendingSelection()
        {
            if (string.IsNullOrEmpty(_pendingSelectId)) return;
            var idx = _registry.profiles.FindIndex(p =>
                string.Equals(p.id, _pendingSelectId, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) _selectedIndex = idx;
            _pendingSelectId = null;
        }

        private void ClampSelection()
        {
            var count = _registry.profiles.Count;
            if (count == 0)
            {
                _selectedIndex = 0;
                return;
            }

            if (_selectedIndex < 0 || _selectedIndex >= count)
            {
                // Default to the currently loaded profile so the detail card is never empty.
                var activeIdx = _registry.profiles.FindIndex(p =>
                    string.Equals(p.id, ActiveProfileState.CurrentProfileId, StringComparison.OrdinalIgnoreCase));
                _selectedIndex = activeIdx >= 0 ? activeIdx : Mathf.Clamp(_selectedIndex, 0, count - 1);
            }
        }

        // -----------------------------------------------------------------------
        // CRUD
        // -----------------------------------------------------------------------

        private void ShowAddMenu(Rect rect)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy of Release (strict)"), false,
                () => AddProfile(ReleaseGuardRegistry.ReleaseProfileId));
            menu.AddItem(new GUIContent("Copy of Development (lenient)"), false,
                () => AddProfile(ReleaseGuardRegistry.DevelopmentProfileId));
            menu.AddItem(new GUIContent("Blank"), false, () => AddProfile(null));
            menu.DropDown(rect);
        }

        private void AddProfile(string templateId)
        {
            const string newDisplayName = "New Profile";
            var newId = GenerateUniqueId(newDisplayName, _registry.profiles);
            ProfileSettingsRegistry.CreateFromTemplate(templateId, newId);

            _registry.profiles.Add(new ReleaseGuardProfile
            {
                id = newId,
                displayName = newDisplayName,
                isDefault = false,
                activation = new ProfileActivation { strategy = ActivationStrategy.Always }
            });

            _pendingSelectId = newId;
            ActiveProfileState.CurrentProfileId = newId;
            Persist();
        }

        private void DuplicateProfile(int index)
        {
            var source = _registry.profiles[index];
            var newId = GenerateUniqueId(source.displayName + " copy", _registry.profiles);
            ProfileSettingsRegistry.CreateFromTemplate(source.id, newId);

            _registry.profiles.Insert(index + 1, new ReleaseGuardProfile
            {
                id = newId,
                displayName = source.displayName + " (Copy)",
                isDefault = false,
                activation = new ProfileActivation
                {
                    strategy = source.activation.strategy,
                    unityBuildProfileNames = new List<string>(source.activation.unityBuildProfileNames)
                }
            });

            _pendingSelectId = newId;
            Persist();
        }

        private void ConfirmDeleteProfile(int index)
        {
            var profile = _registry.profiles[index];
            var confirmed = EditorUtility.DisplayDialog(
                "Delete Profile",
                $"Delete the profile \"{profile.displayName}\" and its settings asset?",
                "Delete",
                "Cancel");
            if (!confirmed) return;

            ProfileSettingsRegistry.Delete(profile.id);
            _registry.profiles.RemoveAt(index);

            if (string.Equals(profile.id, ActiveProfileState.CurrentProfileId, StringComparison.OrdinalIgnoreCase) &&
                _registry.profiles.Count > 0)
                ActiveProfileState.CurrentProfileId = _registry.profiles[0].id;

            _selectedIndex = Mathf.Clamp(index, 0, _registry.profiles.Count - 1);
            Persist();
        }

        private void Persist()
        {
            EditorUtility.SetDirty(_registry);
            AssetDatabase.SaveAssets();
            Property.serializedObject.Update();
            _list = null;
            // ExitGUI is only valid inside an IMGUI event (OnGUI). Menu callbacks run
            // outside the event loop, so skipping it there avoids a logged ExitGUIException.
            if (Event.current != null)
                GUIUtility.ExitGUI();
        }

        // -----------------------------------------------------------------------
        // Id generation
        // -----------------------------------------------------------------------

        private static string GenerateUniqueId(string displayName, IEnumerable<ReleaseGuardProfile> existing)
        {
            var slug = Slugify(displayName);
            var ids = new HashSet<string>(existing.Select(p => p.id), StringComparer.OrdinalIgnoreCase);
            if (!ids.Contains(slug)) return slug;

            for (var i = 2; i < 1000; i++)
            {
                var candidate = $"{slug}-{i}";
                if (!ids.Contains(candidate)) return candidate;
            }

            return $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";
        }

        private static string Slugify(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "profile";
            var slug = Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
            return string.IsNullOrEmpty(slug) ? "profile" : slug;
        }
    }
}