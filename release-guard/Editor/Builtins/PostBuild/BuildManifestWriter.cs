using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.PostBuild;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ReleaseGuard.Editor.Builtins.PostBuild
{
    /// <summary>
    /// Writes <c>release-guard-manifest.json</c> recording which Release Guard configuration
    /// produced the build: package version, Unity version, build target and GUID, and the
    /// components that were active.
    ///
    /// <para><b>Why opt-in (off by default):</b> the manifest documents the project's hardening
    /// configuration -- information you may not want in players' hands. It is designed as a CI
    /// artifact: enable it only when your pipeline picks up or excludes the file before packaging
    /// the player.</para>
    ///
    /// <para><b>Output path:</b> by default the manifest is written next to the build output.
    /// Set <c>outputPath</c> in settings to redirect it to a dedicated CI artifacts folder so it
    /// never ends up adjacent to shippable binaries.</para>
    ///
    /// <para><b>What is deliberately NOT recorded:</b> absolute paths (which can embed the build
    /// machine's user name) and VCS revision info (reading it would mean spawning external
    /// processes at build time).</para>
    ///
    /// <para>Runs at priority 100 so it executes after the built-in sweep and any
    /// default-priority custom post-build components.</para>
    /// </summary>
    public sealed class BuildManifestWriter : ReleaseGuardComponent<BuildManifestWriter.Config>
    {
        public override string Id => "build_manifest";
        public override string DisplayName => "Build manifest";

        public const string ManifestFileName = "release-guard-manifest.json";

        [Serializable]
        public sealed class Config : ReleaseGuardComponentSettings
        {
            [Tooltip(
                "Where to write the manifest file. Leave empty to write it next to the build " +
                "output (default). Supports absolute paths, project-relative paths " +
                "(e.g. ../ci-artifacts), and environment variables ($VAR or ${VAR} on " +
                "Unix/macOS, %VAR% on Windows). The folder is created automatically if it " +
                "does not exist. Use this to keep the manifest out of the player directory.")]
            public string outputPath = "";
        }

        public override ReleaseGuardComponentSettings CreateDefaultSettings() =>
            new Config { componentId = Id, enabled = false };

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPostBuild(releaseEvent => PostProcess(releaseEvent.Context, Settings), priority: 100);

        internal static void PostProcess(ReleaseGuardPostBuildContext context, Config config = null)
        {
            config ??= new Config();

            var folder = ResolveManifestFolder(config.outputPath, context.OutputPath);
            if (folder == null)
            {
                context.Warning(
                    $"Manifest output folder could not be resolved from '{context.OutputPath}'; manifest not written.");
                return;
            }

            try
            {
                Directory.CreateDirectory(folder);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                context.Error($"Could not create manifest output folder '{folder}': {e.Message}");
                return;
            }

            var manifest = BuildManifest(context);
            var path = Path.Combine(folder, ManifestFileName);

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(manifest, prettyPrint: true));
                context.Info(
                    $"Wrote '{ManifestFileName}' to '{folder}'. " +
                    "Do not ship this file to players -- it documents your hardening configuration.");
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                context.Error($"Could not write '{path}': {e.Message}");
            }
        }

        // -----------------------------------------------------------------
        // Path resolution
        // -----------------------------------------------------------------

        internal static string ResolveManifestFolder(string configuredPath, string buildOutputPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                return ResolveBuildOutputFolder(buildOutputPath);

            var expanded = ExpandEnvVars(configuredPath);

            if (Path.IsPathRooted(expanded))
                return expanded;

            // Relative path: resolve from the Unity project root (parent of Assets).
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return string.IsNullOrEmpty(projectRoot) ? null : Path.GetFullPath(Path.Combine(projectRoot, expanded));
        }

        private static string ResolveBuildOutputFolder(string outputPath)
        {
            if (string.IsNullOrEmpty(outputPath))
                return null;

            return Directory.Exists(outputPath)
                ? outputPath
                : Path.GetDirectoryName(outputPath);
        }

        internal static string ExpandEnvVars(string path)
        {
            // %VAR% -- Windows style (ExpandEnvironmentVariables handles this on all platforms)
            path = Environment.ExpandEnvironmentVariables(path);
            // ${VAR} -- must precede $VAR so braces are consumed first
            path = Regex.Replace(path, @"\$\{([^}]+)\}",
                m => Environment.GetEnvironmentVariable(m.Groups[1].Value) ?? m.Value);
            // $VAR -- bare dollar sign followed by an identifier
            path = Regex.Replace(path, @"\$([A-Za-z_][A-Za-z0-9_]*)",
                m => Environment.GetEnvironmentVariable(m.Groups[1].Value) ?? m.Value);
            return path;
        }

        // -----------------------------------------------------------------
        // Manifest assembly
        // -----------------------------------------------------------------

        private static Manifest BuildManifest(ReleaseGuardPostBuildContext context)
        {
            var releaseGuard = ReleaseGuardDI.Resolve<ReleaseGuardEnvironment>();

            var components = releaseGuard.Components.Items;
            var manifest = new Manifest
            {
                manifestVersion = 1,
                releaseGuardVersion = GetPackageVersion(),
                unityVersion = Application.unityVersion,
                buildTarget = context.BuildTarget.ToString(),
                productName = Application.productName,
                outputFileName = Path.GetFileName(context.OutputPath),
                failureThreshold = context.Settings.general.failureThreshold.ToString(),
                components = components
                    .Select(component => new ComponentManifest
                    {
                        id = component.Id,
                        displayName = component.DisplayName,
                        phases = ResolvePhases(component.Id, releaseGuard).ToList()
                    })
                    .ToList(),
                disabledComponentIds = context.Settings.components.componentToggles.GetDisabledIds(),
                disabledPluginIds = Copy(context.Settings.plugins.disabledPluginIds),
                suppressedAdvisoryIds = AdvisorySuppressionStore.GetAll().ToList()
            };

            if (context.BuildReport == null) return manifest;
            var summary = context.BuildReport.summary;
            manifest.buildGuid = summary.guid.ToString();
            manifest.buildStartedUtc = summary.buildStartedAt.ToUniversalTime().ToString("o");
            manifest.buildEndedUtc = summary.buildEndedAt.ToUniversalTime().ToString("o");
            manifest.totalBuildSizeBytes = (long)summary.totalSize;

            return manifest;
        }

        private static string GetPackageVersion()
        {
            var packageInfo = PackageInfo.FindForAssembly(typeof(BuildManifestWriter).Assembly);
            return packageInfo != null ? packageInfo.version : "unknown";
        }

        private static List<string> Copy(List<string> source) =>
            source != null ? new List<string>(source) : new List<string>();

        private static IEnumerable<string> ResolvePhases(string componentId, ReleaseGuardEnvironment environment)
        {
            return environment.EventBus.GetSubscribedEvents(componentId).Select(releaseEvent => releaseEvent switch
            {
                ReleaseGuardLifecycleEventKind.PreBuild => "pre_build",
                ReleaseGuardLifecycleEventKind.Build => "build",
                ReleaseGuardLifecycleEventKind.PostBuild => "post_build",
                _ => releaseEvent.ToString()
            });
        }

        // -----------------------------------------------------------------
        // Serialized shape (JsonUtility)
        // -----------------------------------------------------------------
        // ReSharper disable NotAccessedField.Local
        [Serializable]
        private sealed class Manifest
        {
            public int manifestVersion;
            public string releaseGuardVersion;
            public string unityVersion;
            public string buildTarget;
            public string productName;
            public string outputFileName;
            public string failureThreshold;

            public string buildGuid;
            public string buildStartedUtc;
            public string buildEndedUtc;
            public long totalBuildSizeBytes;

            public List<ComponentManifest> components;
            public List<string> disabledComponentIds;
            public List<string> disabledPluginIds;
            public List<string> suppressedAdvisoryIds;
        }

        [Serializable]
        private sealed class ComponentManifest
        {
            public string id;
            public string displayName;
            public List<string> phases;
        }
        // ReSharper enable NotAccessedField.Local
    }
}