using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.PostProcessing;
using ReleaseGuard.Editor.Core.Runtime;
using ReleaseGuard.Editor.Core.Transforming;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ReleaseGuard.Editor.Builtins.PostProcessor
{
    /// <summary>
    /// Writes <c>release-guard-manifest.json</c> next to the build output, recording which
    /// Release Guard configuration produced the build: package version, Unity version, build
    /// target and GUID, and the auditors, post-processors, and transformers that were active.
    ///
    /// <para><b>Why opt-in (off by default):</b> this post-processor adds a file to the build
    /// output folder. Anything next to the shipped binaries risks being packaged and shipped by
    /// accident, and this particular file intentionally documents the project's hardening
    /// configuration -- information you may not want in players' hands. It is designed as a CI
    /// artifact: enable <c>writeBuildManifest</c> in settings only when your packaging step picks
    /// up the manifest separately (or excludes it from the shipped archive).</para>
    ///
    /// <para><b>What is deliberately NOT recorded:</b> absolute paths (which can embed the build
    /// machine's user name) and VCS revision info (reading it would mean spawning external
    /// processes at build time). If you need a commit hash in the manifest, write your own
    /// post-processor with a priority greater than 100 and amend the file, or stamp the hash
    /// elsewhere in your CI pipeline.</para>
    ///
    /// <para>Runs at priority 100 so it executes after the built-in sweep and any default-priority
    /// custom post-processors, and therefore records the state the output folder actually shipped in.</para>
    /// </summary>
    public sealed class BuildManifestPostProcessor : ReleasePostProcessor
    {
        public override string Id => "build_manifest";
        public override string DisplayName => "Build manifest";
        public override int Priority => 100;

        // ReSharper disable once MemberCanBePrivate.Global
        public const string ManifestFileName = "release-guard-manifest.json";

        public override bool ShouldRun(ReleasePostProcessContext context) =>
            context.Settings.postProcessors.writeBuildManifest;

        public override void PostProcess(ReleasePostProcessContext context)
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

        private static Manifest BuildManifest(ReleasePostProcessContext context)
        {
            var releaseGuard = DI.Resolve<ReleaseGuardEnvironment>();

            var manifest = new Manifest
            {
                manifestVersion = 1,
                releaseGuardVersion = GetPackageVersion(),
                unityVersion = Application.unityVersion,
                buildTarget = context.BuildTarget.ToString(),
                productName = Application.productName,
                outputFileName = Path.GetFileName(context.OutputPath),
                failureThreshold = context.Settings.general.failureThreshold.ToString(),

                auditorIds = releaseGuard.Registries.Auditors.Items.Select(a => a.Id).ToList(),
                postProcessorIds = releaseGuard.Registries.PostProcessors.Items.Select(p => p.Id).ToList(),
                transformerIds = releaseGuard.Registries.Transformers.Items.Select(t => t.Id).ToList(),

                disabledAuditorIds = Copy(context.Settings.auditors.disabledAuditorIds),
                disabledPostProcessorIds = Copy(context.Settings.postProcessors.disabledPostProcessorIds),
                disabledTransformerIds = Copy(context.Settings.transformers.disabledTransformerIds),
                disabledPluginIds = Copy(context.Settings.plugins.disabledPluginIds),
                suppressedAdvisoryIds = Copy(context.Settings.auditors.suppressedAdvisoryIds)
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
            var packageInfo = PackageInfo.FindForAssembly(typeof(BuildManifestPostProcessor).Assembly);
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

            public List<string> auditorIds;
            public List<string> postProcessorIds;
            public List<string> transformerIds;
            public List<string> disabledAuditorIds;
            public List<string> disabledPostProcessorIds;
            public List<string> disabledTransformerIds;
            public List<string> disabledPluginIds;
            public List<string> suppressedAdvisoryIds;
        }
        // ReSharper enable NotAccessedField.Local
    }
}