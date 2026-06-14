using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ReleaseGuard.Editor.Core.Config;
using ReleaseGuard.Editor.Core.Config.Attributes;
using ReleaseGuard.Editor.Core.Config.Components;
using ReleaseGuard.Editor.Core.Config.Reader;
using ReleaseGuard.Editor.Core.Config.Renderer;
using ReleaseGuard.Editor.Core.Config.Types;
using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Plugins;
using ReleaseGuard.Editor.Core.Runtime;
using ReleaseGuard.Editor.UI;
using UnityEditor;
using UnityEngine;

// ReSharper disable RedundantDefaultMemberInitializer
// ReSharper disable UnusedMember.Global
namespace ReleaseGuard.Editor.Config
{
    [SettingsPage("General",
        intro: "Behavior for this profile's pipeline.",
        description: "Build gate, logging.")]
    [Serializable]
    public sealed class GeneralSettings : ISettingsPage
    {
        [SettingsHeader("Build Gate")]
        [Tooltip(
            "Master switch for real build stages. When off, Release Guard does not gate builds or run post-build pipelines; manual checks can still run.")]
        public bool enabled = true;

        [Tooltip("A build is blocked when any issue is at or above this severity.")]
        public ReleaseIssueSeverity failureThreshold = ReleaseIssueSeverity.Error;

        [SettingsHeader("Logging")]
        [Tooltip("Log extra diagnostic detail (registered components, skips, etc.) to the Console.")]
        public bool verboseLogging = false;
    }

    [SettingsPage("Components",
        intro: "Components are the units of build logic in Release Guard. A component can " +
               "contribute checks, build-time behavior, post-build output handling, or any " +
               "combination of those responsibilities.",
        description: "Enable or disable components, configure their settings, discovery, exclusions.")]
    [Serializable]
    public sealed class ComponentSettings : ISettingsPage
    {
        [SettingsHeader("Exclusions")]
        [Tooltip("gitignore-style glob patterns for asset paths to exclude from release issues. " +
                 "Any issue tied to a matching asset is dropped. Examples: 'Assets/ThirdParty/**', " +
                 "'*.generated.cs'. Use '!' to re-include. See package docs.")]
        public ExclusionList excludedAssetPaths = new ExclusionList();

        [SettingsHeader("Discovery")]
        [Tooltip(
            "Automatically run concrete custom ReleaseGuardComponent subclasses found outside the Release Guard package assembly. Requires a public parameterless constructor; excludes test fixtures. Off by default.")]
        public bool autoDiscoverComponents = false;

        [SettingsHeader("Components")]
        [Tooltip(
            "Enable or disable individual components. Disabling a component skips it in every phase it participates in. " +
            "Expand a component row to configure its settings.")]
        public ComponentToggleList componentToggles = new ComponentToggleList();
    }

    [SettingsPage("Plugins",
        intro: "Plugins contribute components from a single identifiable entry point.",
        description: "Plugin discovery and disabling.")]
    [Serializable]
    public sealed class PluginSettings : IDynamicSettingsPage
    {
        [SettingsHeader("Discovery")]
        [Tooltip(
            "Automatically discover concrete custom ReleaseGuardPlugin subclasses outside the Release Guard package assembly via TypeCache and invoke them. Requires a public parameterless constructor; excludes test fixtures. " +
            "Off by default: prefer explicit registration via [InitializeOnLoad] so your plugin loads " +
            "predictably without any scanning overhead. Enable for zero-configuration use cases.")]
        public bool autoDiscoverPlugins = false;

        [Tooltip("Plugin ids to skip entirely (all of a plugin's contributions are excluded).")]
        public List<string> disabledPluginIds = new List<string>();

        IEnumerable<SettingsProvider> IDynamicSettingsPage.ResolveChildren(
            SettingsComponentReader reader, string parentPath, SettingsRenderer renderer)
        {
            return from plugin in ReleaseGuardDI.Resolve<ReleaseGuardEnvironment>()
                    .Plugins
                where plugin.SettingsType != null
                select MakePluginProvider(plugin, parentPath, renderer);
        }

        private static SettingsProvider MakePluginProvider(
            ReleaseGuardPlugin plugin, string parentPath, SettingsRenderer renderer)
        {
            var path = $"{parentPath}/{plugin.DisplayName}";
            SettingsComponentReader pluginReader = null;
            ScreenComponent pluginScreen = null;

            return new SettingsProvider(path, SettingsScope.Project)
            {
                label = plugin.DisplayName,
                guiHandler = _ =>
                {
                    var ps = PluginSettingsRegistry.LoadOrCreate(plugin.PluginId, plugin.SettingsType);
                    if (ps == null) return;

                    if (pluginReader == null)
                    {
                        pluginReader = new SettingsComponentReader();
                        BuiltinComponents.RegisterAll(pluginReader);
                        ps.ConfigureReader(pluginReader);
                        pluginScreen = pluginReader.Read(ps, path, plugin.DisplayName);
                    }

                    var so = new SerializedObject(ps);
                    so.Update();
                    renderer.DrawPaddedContent(() =>
                    {
                        if (!string.IsNullOrEmpty(pluginScreen.Intro))
                            RenderPrimitives.Intro(pluginScreen.Intro);
                        SettingsComponentReader.BindProperties(pluginScreen.Children, name => so.FindProperty(name));
                        foreach (var child in pluginScreen.Children)
                            child.Render(renderer);
                    });
                    if (so.ApplyModifiedProperties())
                        renderer.OnSettingsChanged();
                }
            };
        }
    }

    /// <summary>
    /// Project-wide Release Guard configuration. A single asset lives at
    /// <see cref="DefaultAssetPath"/> and is edited via Edit > Project Settings > Release Guard.
    ///
    /// Settings are grouped into typed sub-objects that map one-to-one to Project Settings pages.
    /// Components with configurable settings access them via <c>this.Settings.field</c> inside
    /// the component's own methods, populated during <c>Initialize</c> by the environment.
    /// </summary>
    public sealed class ReleaseGuardSettings : ScriptableObject
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public const string DefaultAssetPath = "Assets/ReleaseGuard/ReleaseGuardSettings.asset";

        // Overview page -- rendered in declaration order.
        // NonSerialized fields are only processed by Read(instance, ...), not ReadChildren.
        [NonSerialized] public InlineComponent statusSection;
        [NonSerialized] public InlineComponent sectionsHeader;
        public GeneralSettings general = new();
        public ComponentSettings components = new();
        public PluginSettings plugins = new();
        [NonSerialized] public InlineComponent additionalSections;
        [NonSerialized] public InlineComponent actionsSection;

        // -----------------------------------------------------------------
        // Query methods -- convenience wrappers over sub-object lists.
        // -----------------------------------------------------------------

        public bool IsComponentDisabled(string id) =>
            !components.componentToggles.IsEnabled(id);

        public bool IsPluginDisabled(string id) =>
            ContainsId(plugins.disabledPluginIds, id);

        public bool IsAdvisorySuppressed(string suppressId) =>
            AdvisorySuppressionStore.IsSuppressed(suppressId);

        /// <summary>
        /// Suppress an advisory globally (profile-independent).
        /// Delegates to <see cref="AdvisorySuppressionStore"/> so the suppression
        /// persists across profiles and is not tied to any settings asset.
        /// </summary>
        public void SuppressAdvisory(string suppressId) =>
            AdvisorySuppressionStore.Suppress(suppressId);

        public void SuppressAdvisory(
            string suppressId,
            string message,
            string componentId,
            string componentDisplayName) =>
            AdvisorySuppressionStore.Suppress(suppressId, message, componentId, componentDisplayName);

        private void OnEnable()
        {
            EnsureSubObjects();

            statusSection = new InlineComponent("Status", renderer =>
            {
                renderer.Section("Status");
                RenderPrimitives.Label(general.enabled
                    ? $"Active: builds fail when an issue is at severity {general.failureThreshold} or above."
                    : "Disabled for build stages; manual checks can still run. Enable Release Guard under General.");
            });

            sectionsHeader = new InlineComponent("Sections", renderer =>
                renderer.Section("Sections"));

            additionalSections = new InlineComponent("AdditionalSections", _ =>
            {
                RenderPrimitives.SectionLink(
                    "Profiles",
                    ReleaseGuardSettingsProvider.ProfilesPath,
                    "Create, reorder, and activate profiles");
                RenderPrimitives.SectionLink(
                    "Advisories",
                    ReleaseGuardSettingsProvider.AdvisoriesPath,
                    "Review and restore dismissed advisories");
            });

            actionsSection = new InlineComponent("Actions", renderer =>
            {
                renderer.Section("Actions");
                RenderPrimitives.Row(() =>
                {
                    renderer.ActionButton("Open Checks Window", () => ReleaseGuardWindow.ShowWindow());
                    renderer.ActionButton("Ping Settings Asset", () =>
                    {
                        Selection.activeObject = this;
                        EditorGUIUtility.PingObject(this);
                    });
                    renderer.ActionButton("Reload Release Guard", () => ReleaseGuardStartup.Reload());
                });
            });
        }

        private void EnsureSubObjects()
        {
            general ??= new GeneralSettings();
            components ??= new ComponentSettings();
            plugins ??= new PluginSettings();
        }

        private static bool ContainsId(List<string> list, string id) =>
            !string.IsNullOrEmpty(id) && list != null &&
            list.Exists(e => e != null && e.Trim() == id);

        /// <summary>Load the settings asset, creating it (and its folder) on first use.</summary>
        public static ReleaseGuardSettings LoadOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ReleaseGuardSettings>(DefaultAssetPath);
            if (settings is not null)
                return settings;

            var directory = Path.GetDirectoryName(DefaultAssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            settings = CreateInstance<ReleaseGuardSettings>();
            settings.EnsureSubObjects();
            AssetDatabase.CreateAsset(settings, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }
    }
}
// ReSharper enable RedundantDefaultMemberInitializer
// ReSharper enable UnusedMember.Global
