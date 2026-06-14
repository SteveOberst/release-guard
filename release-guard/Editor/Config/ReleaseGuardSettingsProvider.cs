using System;
using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Core.Config.Components;
using ReleaseGuard.Editor.Core.Config.Renderer;
using ReleaseGuard.Editor.UI;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Config
{
    /// <summary>
    /// Registers the Release Guard pages in Project Settings.
    ///
    /// Page tree:
    ///   Project/Release Guard      overview
    ///   - General                  active profile's general settings
    ///   - Profiles                 manage the profile list (add, reorder, rename, conditions)
    ///   - Components               active profile's component settings
    ///   - Advisories               global dismissed advisory management
    ///   - Plugins
    ///
    /// Which profile the General/Components/etc. pages edit is chosen by the profile dropdown in the
    /// page header (see <see cref="ReleaseGuardPageHeader"/>). That selection is an editor-only
    /// preference and never affects builds; at build time the active profile is resolved from the
    /// build's options and environment.
    ///
    /// The Profiles page is the single place profiles are managed. There are no per-profile
    /// sub-pages: to edit a specific profile's settings, select it in the header dropdown (or click
    /// "Edit" on its row) and use the regular General/Components/etc. pages.
    /// </summary>
    internal static class ReleaseGuardSettingsProvider
    {
        private const string RootPath = "Project/Release Guard";

        // The numeric prefix orders this page directly after "1 General" and before "2 Components".
        // The reader assigns "1 General", "2 Components", ... to the profile-bound pages; siblings are
        // sorted by path segment, so "1 General" < "1 Profiles" < "2 Components".
        internal const string ProfilesPath = "Project/Release Guard/1 Profiles";
        internal const string AdvisoriesPath = "Project/Release Guard/3 Advisories";

        private static readonly SettingsRenderer Renderer = new ReleaseGuardSettingsRenderer();

        [SettingsProviderGroup]
        public static SettingsProvider[] CreateAll()
        {
            // Guarantee the built-in profiles exist before the UI binds to them. Migration also
            // runs at editor startup; calling it here makes the page robust to startup ordering so
            // the Profiles list (and the header dropdown) can never render empty.
            ProfileMigration.Run();

            var registry = ReleaseGuardRegistry.LoadOrCreate();

            // Profile-bound pages (General, Components, Plugins).
            // The getter is called on every draw so switching the active profile rebinds them.
            Func<ScriptableObject> profileGetter = ActiveProfileState.CurrentSettings;
            var providers = Renderer.CreateProviders(RootPath, profileGetter).ToList();

            // Profiles management page (registry-bound). Created via the getter overload so the
            // SerializedObject is cached across frames, which the ReorderableList depends on.
            var registryScreen = Renderer.ReadRootScreen(registry, ProfilesPath);
            var profilesProvider = registryScreen.CreateProvider(() => registry, Renderer);
            profilesProvider.label = "Profiles";
            providers.Add(profilesProvider);

            providers.Add(CreateAdvisoriesProvider());

            foreach (var provider in providers)
                InjectPageHeader(provider, registry);

            return providers.ToArray();
        }

        private static void InjectPageHeader(SettingsProvider provider, ReleaseGuardRegistry registry)
        {
            provider.titleBarGuiHandler = () => ReleaseGuardPageHeader.DrawTitleBar(registry);

            var original = provider.guiHandler;
            provider.guiHandler = search =>
            {
                ReleaseGuardPageHeader.DrawBodyWarning(registry);
                original?.Invoke(search);
            };
        }

        private static SettingsProvider CreateAdvisoriesProvider()
        {
            return new SettingsProvider(AdvisoriesPath, SettingsScope.Project)
            {
                label = "Advisories",
                guiHandler = _ =>
                {
                    Renderer.DrawPaddedContent(() =>
                    {
                        RenderPrimitives.Intro(
                            "Dismissed advisories are project-scoped and profile-independent. " +
                            "Restore one here to make it show up in checks again.");

                        Renderer.Section("Dismissed advisories");
                        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                        {
                            var records = AdvisorySuppressionStore.GetAllRecords();
                            if (records.Count == 0)
                            {
                                RenderPrimitives.HelpBox(
                                    "No dismissed advisories.\nUse \"Don't show again\" in the checks window to suppress an advisory finding.",
                                    MessageType.None);
                                RenderPrimitives.Row(() =>
                                    Renderer.ActionButton("Open Checks Window", ReleaseGuardWindow.ShowWindow));
                                return;
                            }

                            foreach (var record in records)
                            {
                                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        EditorGUILayout.LabelField(
                                            string.IsNullOrWhiteSpace(record.componentDisplayName)
                                                ? record.componentId
                                                : record.componentDisplayName,
                                            EditorStyles.boldLabel);
                                        GUILayout.FlexibleSpace();
                                        if (GUILayout.Button("Restore", EditorStyles.miniButton, GUILayout.Width(72f)))
                                        {
                                            AdvisorySuppressionStore.Unsuppress(record.suppressId);
                                            GUIUtility.ExitGUI();
                                        }
                                    }

                                    EditorGUILayout.LabelField(
                                        string.IsNullOrWhiteSpace(record.message)
                                            ? "(No description recorded)"
                                            : record.message,
                                        EditorStyles.wordWrappedMiniLabel);
                                }
                            }
                        }
                    });
                }
            };
        }

        private sealed class ReleaseGuardSettingsRenderer : SettingsRenderer
        {
            protected override HashSet<string> GetRootKeywords() =>
                Keywords("release", "guard", "overview", "status", "checks", "build", "profile");
        }
    }
}
