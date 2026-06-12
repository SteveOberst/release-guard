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
    /// <summary>Per-Build-Profile override of the global settings (Unity 6+ Build Profiles).</summary>
    [Serializable]
    public sealed class BuildProfileOverride
    {
        [Tooltip("Exact name of the Unity Build Profile this override applies to.")]
        public string buildProfileName;

        [Tooltip("Whether Release Guard runs at all for this profile.")]
        public bool enabled = true;

        [Tooltip("Builds using this profile fail when an issue is at or above this severity.")]
        public ReleaseIssueSeverity failureThreshold = ReleaseIssueSeverity.Error;
    }

    [SettingsPage("General",
        intro: "Global behavior shared by every pipeline.",
        description: "Build gate, logging, build-profile overrides.")]
    [Serializable]
    public sealed class GeneralSettings : ISettingsPage
    {
        [SettingsHeader("Build Gate")] [Tooltip("Master switch. When off, Release Guard never runs.")]
        public bool enabled = true;

        [Tooltip(
            "Skip all checks for Development builds. Release rules only apply to non-development (release) builds.")]
        public bool skipOnDevelopmentBuilds = true;

        [Tooltip("A build is blocked when any issue is at or above this severity.")]
        public ReleaseIssueSeverity failureThreshold = ReleaseIssueSeverity.Error;

        [SettingsHeader("Logging")]
        [Tooltip("Log extra diagnostic detail (registered auditors, skips, etc.) to the Console.")]
        public bool verboseLogging = false;

        [SettingsHeader("Build Profile Overrides (Unity 6+)")]
        [Tooltip("Optional per-profile overrides, matched against the active Build Profile name.")]
        public List<BuildProfileOverride> profileOverrides = new();
    }

    [SettingsPage("Auditors",
        intro: "Auditors inspect project and player settings before a release build " +
               "and report issues. The build fails at or above the failure threshold.",
        description: "Pre-build rules, exclusions, advisory suppressions.")]
    [Serializable]
    public sealed class AuditorSettings : ISettingsPage
    {
        [SettingsHeader("Built-in Rules")]
        [SettingsLabel("Require IL2CPP")]
        [Tooltip(
            "Require the IL2CPP scripting backend. Mono ships your C# as .NET assemblies that decompile trivially; IL2CPP compiles to native code and is far harder to reverse-engineer.")]
        public bool requireIl2Cpp = true;

        [Tooltip("Treat shipping a Development Build as a release issue (profiler/debugger/stack traces exposed).")]
        public bool forbidDevelopmentBuild = true;

        [Tooltip("Treat allowing a managed script debugger to attach as a release issue.")]
        public bool forbidScriptDebugging = true;

        [Tooltip("Treat shipping with the Unity profiler connection enabled as a release issue.")]
        public bool forbidProfilerConnection = true;

        [Tooltip(
            "Minimum managed code stripping level required (Disabled = don't check). Higher = less metadata/code shipped.")]
        public ManagedStrippingLevel minManagedStrippingLevel = ManagedStrippingLevel.Medium;

        [Tooltip("Treat broad [Preserve] usage and broad link.xml preservation rules as a release issue.")]
        public bool forbidBroadPreserve = true;

        [SettingsHeader("Discovery")]
        [Tooltip(
            "Automatically run every ReleaseAuditor subclass found in the project (excluding test fixtures and built-in types).")]
        public bool autoDiscoverAuditors = false;

        [Tooltip("Auditor ids to skip, e.g. \"scripting_backend\".")]
        public List<string> disabledAuditorIds = new();

        [SettingsHeader("Exclusions")]
        [Tooltip("gitignore-style glob patterns for asset paths to exclude from release issues. " +
                 "Any issue tied to a matching asset is dropped. Examples: 'Assets/ThirdParty/**', " +
                 "'*.generated.cs', 'Assets/Samples/'. Use '!' to re-include. See package docs.")]
        public ExclusionList excludedAssetPaths = new();

        [Tooltip("Assembly names to exclude from the [ReleaseForbidden] scan. Use this for third-party " +
                 "assemblies you cannot modify that legitimately contain [ReleaseForbidden]-decorated " +
                 "members. Enter assembly names without the .dll extension; matching is case-insensitive, " +
                 "e.g. \"MyPlugin.Runtime\".")]
        public List<string> releaseForbiddenExcludedAssemblies = new();

        [SettingsHeader("Advisory Suppressions")]
        [Tooltip(
            "Advisory ids that have been dismissed via 'Don't show again'. Remove an id from this list to re-enable the advisory.")]
        public List<string> suppressedAdvisoryIds = new();
    }

    [SettingsPage("Post-Processors",
        intro:
        "Post-processors run last after a release build and operate on the final build output: cleanup, manifests, metadata.",
        description: "Debug symbol sweep, build manifest.")]
    [Serializable]
    public sealed class PostProcessorSettings : ISettingsPage
    {
        [SettingsHeader("Debug Symbol Sweep")]
        [Tooltip("Scan the build output folder after a release build for debug artifacts Unity leaves " +
                 "next to the player (*_BackUpThisFolder_ButDontShipItWithYourGame, " +
                 "*_BurstDebugInformation_DoNotShip, loose .pdb files). " +
                 "Report-only: nothing is deleted unless deletion is explicitly enabled below.")]
        public bool debugSymbolSweepEnabled = true;

        [ConditionalWarning(
            "Deletion is enabled: found artifacts are removed from the output folder. " +
            "Deleted symbol folders cannot be regenerated without rebuilding -- " +
            "archive them for crash symbolication first.")]
        [Tooltip("DESTRUCTIVE when enabled: delete found debug artifacts from the output folder instead " +
                 "of only reporting them. Off by default. Only enable this once you have verified the " +
                 "report-only output matches your expectations -- deleted symbol folders cannot be " +
                 "regenerated without rebuilding, and you may want to archive them for crash symbolication " +
                 "before deleting.")]
        public bool debugSymbolSweepDelete = false;

        [Tooltip("Additional file or folder names to treat as debug artifacts. Matched against entries " +
                 "directly inside the output folder (not recursive); '*' wildcards are allowed, " +
                 "e.g. \"*.map\" or \"DebugData\". Subject to the same report/delete behavior as the " +
                 "built-in patterns.")]
        public List<string> debugSymbolSweepExtraPatterns = new();

        [SettingsHeader("Build Manifest")]
        [Tooltip("Write a release-guard-manifest.json next to the build output after every release build, " +
                 "recording the Release Guard version, Unity version, build target, build GUID, and the " +
                 "auditors, post-processors, and transformers that were active. Off by default: the manifest documents your " +
                 "hardening configuration and is intended as a CI artifact, not as a file to ship to " +
                 "players. Only enable it if your packaging step excludes it from the shipped build.")]
        public bool writeBuildManifest = false;

        [SettingsHeader("Discovery")]
        [Tooltip("Automatically run every ReleasePostProcessor subclass found in the project after a release build.")]
        public bool autoDiscoverPostProcessors = false;

        [Tooltip("Post-processor ids to skip, e.g. \"debug_symbol_sweep\".")]
        public List<string> disabledPostProcessorIds = new();
    }

    [SettingsPage("Transformers",
        intro: "Transformers operate on build artifacts at a low level (IL manipulation, " +
               "binary patching, obfuscation) and run before post-processors. " +
               "No built-in transformers ship with this package -- derive from " +
               "ReleaseTransformer in any Editor assembly to add one.",
        description: "Artifact-level transforms (IL, obfuscation).")]
    [Serializable]
    public sealed class TransformerSettings : ISettingsPage
    {
        [SettingsHeader("Discovery")]
        [Tooltip("Automatically run every ReleaseTransformer subclass found in the project after a release build, " +
                 "before the post-processor pipeline. Transformers are for low-level artifact operations " +
                 "such as IL manipulation, binary patching, or obfuscation.")]
        public bool autoDiscoverTransformers = false;

        [Tooltip("Transformer ids to skip.")] public List<string> disabledTransformerIds = new();
    }

    [SettingsPage("Plugins",
        intro: "Plugins contribute auditors, post-processors, and transformers from a single identifiable entry point.",
        description: "Plugin discovery and disabling.")]
    [Serializable]
    public sealed class PluginSettings : IDynamicSettingsPage
    {
        [SettingsHeader("Discovery")]
        [Tooltip("Automatically discover every ReleaseGuardPlugin subclass via TypeCache and invoke it. " +
                 "Off by default: prefer explicit registration via [InitializeOnLoad] so your plugin loads " +
                 "predictably without any scanning overhead. Enable for zero-configuration use cases.")]
        public bool autoDiscoverPlugins = false;

        [Tooltip("Plugin ids to skip entirely (all of a plugin's contributions are excluded).")]
        public List<string> disabledPluginIds = new();

        IEnumerable<SettingsProvider> IDynamicSettingsPage.ResolveChildren(
            SettingsComponentReader reader, string parentPath, SettingsRenderer renderer)
        {
            return from plugin in DI.Resolve<ReleaseGuardEnvironment>().Plugins
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
    /// All code outside the UI layer should access fields through those sub-objects directly,
    /// e.g. <c>settings.auditors.requireIl2Cpp</c>.
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
        public AuditorSettings auditors = new();
        public PostProcessorSettings postProcessors = new();
        public TransformerSettings transformers = new();
        public PluginSettings plugins = new();
        [NonSerialized] public InlineComponent actionsSection;

        // -----------------------------------------------------------------
        // Query methods -- convenience wrappers over sub-object lists.
        // -----------------------------------------------------------------

        public bool IsAuditorDisabled(string id) =>
            ContainsId(auditors.disabledAuditorIds, id);

        public bool IsPostProcessorDisabled(string id) =>
            ContainsId(postProcessors.disabledPostProcessorIds, id);

        public bool IsTransformerDisabled(string id) =>
            ContainsId(transformers.disabledTransformerIds, id);

        public bool IsPluginDisabled(string id) =>
            ContainsId(plugins.disabledPluginIds, id);

        public bool IsAdvisorySuppressed(string suppressId) =>
            ContainsId(auditors.suppressedAdvisoryIds, suppressId);

        public bool IsAssemblyExcludedFromReleaseForbidden(string assemblyName) =>
            auditors.releaseForbiddenExcludedAssemblies != null &&
            auditors.releaseForbiddenExcludedAssemblies.Exists(e =>
                e != null && string.Equals(e.Trim(), assemblyName, StringComparison.OrdinalIgnoreCase));

        public BuildProfileOverride GetProfileOverride(string profileName)
        {
            if (string.IsNullOrEmpty(profileName) || general.profileOverrides == null)
                return null;
            return general.profileOverrides.Find(o => o != null && o.buildProfileName == profileName);
        }

        /// <summary>
        /// Permanently suppress an advisory and persist the settings asset.
        /// Call this from the Release Guard window's "Don't show again" button.
        /// </summary>
        public void SuppressAdvisory(string suppressId)
        {
            if (string.IsNullOrEmpty(suppressId)) return;
            auditors.suppressedAdvisoryIds ??= new List<string>();
            if (auditors.suppressedAdvisoryIds.Contains(suppressId)) return;
            auditors.suppressedAdvisoryIds.Add(suppressId);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private void OnEnable()
        {
            EnsureSubObjects();

            statusSection = new InlineComponent("Status", renderer =>
            {
                renderer.Section("Status");
                RenderPrimitives.Label(general.enabled
                    ? $"Active -- builds fail when an issue is at severity {general.failureThreshold} or above."
                    : "Disabled -- no checks run. Enable Release Guard under General.");
            });

            sectionsHeader = new InlineComponent("Sections", renderer =>
                renderer.Section("Sections"));

            actionsSection = new InlineComponent("Actions", renderer =>
            {
                renderer.Section("Actions");
                RenderPrimitives.Row(() =>
                {
                    renderer.ActionButton("Open Audit Window", () => ReleaseGuardWindow.ShowWindow());
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
            auditors ??= new AuditorSettings();
            postProcessors ??= new PostProcessorSettings();
            transformers ??= new TransformerSettings();
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