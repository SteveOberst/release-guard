using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// Writes <c>release-guard-manifest.json</c> into the resolved build output folder, recording which
    /// Release Guard configuration produced the build: package version, Unity version, build
    /// target and GUID, and the components that were active.
    ///
    /// <para><b>Why opt-in (off by default):</b> this component adds a file to the build
    /// output folder. Anything next to the shipped binaries risks being packaged and shipped by
    /// accident, and this particular file intentionally documents the project's hardening
    /// configuration -- information you may not want in players' hands. It is designed as a CI
    /// artifact: enable <c>writeBuildManifest</c> in settings only when your packaging step picks
    /// up the manifest separately (or excludes it from the shipped archive).</para>
    ///
    /// <para><b>What is deliberately NOT recorded:</b> absolute paths (which can embed the build
    /// machine's user name) and VCS revision info (reading it would mean spawning external
    /// processes at build time). If you need a commit hash in the manifest, write your own
    /// post-build component with a priority greater than 100 and amend the file, or stamp the hash
    /// elsewhere in your CI pipeline.</para>
    ///
    /// <para>Runs at priority 100 so it executes after the built-in sweep and any default-priority
    /// custom post-build components, and therefore records the state the output folder actually shipped in.</para>
    /// </summary>
    public sealed class BuildManifestWriter : ReleaseGuardComponent
    {
        public override string Id => "build_manifest";
        public override string DisplayName => "Build manifest";

        // ReSharper disable once MemberCanBePrivate.Global
        public const string ManifestFileName = "release-guard-manifest.json";

        public override ReleaseGuardComponentSettings CreateDefaultSettings() =>
            new ReleaseGuardComponentSettings { componentId = Id, enabled = false };

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPostBuild(releaseEvent => PostProcess(releaseEvent.Context), priority: 100);

        private static void PostProcess(ReleaseGuardPostBuildContext context)
        {
            var folder = ResolveOutputFolder(context.OutputPath);
            if (folder == null || !Directory.Exists(folder))
            {
                context.Warning(
                    $"Output folder could not be resolved from '{context.OutputPath}'; manifest not written.");
                return;
            }

            var manifest = BuildManifest(context);
            var path = Path.Combine(folder, ManifestFileName);

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(manifest, prettyPrint: true));
                context.Info($"Wrote '{ManifestFileName}' to the build output folder. " +
                             "Do not ship this file to players -- it documents your hardening configuration.");
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                context.Error($"Could not write '{path}': {e.Message}");
            }
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

        private static string ResolveOutputFolder(string outputPath)
        {
            if (string.IsNullOrEmpty(outputPath))
                return null;

            return Directory.Exists(outputPath)
                ? outputPath
                : Path.GetDirectoryName(outputPath);
        }

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