using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ReleaseGuard.Editor.Builtins.PostBuild;
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.PostBuild;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class BuildManifestWriterTests
    {
        [Test]
        public void WritesManifest_WithRegisteredComponentsAndPhases()
        {
            var settings = ComponentTestHarness.CreateSettings();
            settings.components.componentToggles
                .GetOrCreate<BuildManifestWriter.Config>("build_manifest")
                .enabled = true;
            settings.components.componentToggles.SetEnabled("managed_stripping", false);

            var outputFolder = Path.Combine(Path.GetTempPath(), $"ReleaseGuardManifest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(outputFolder);
            var outputPath = Path.Combine(outputFolder, "Game.exe");

            try
            {
                var result = ComponentTestHarness.RunPostBuild(
                    settings, BuildTarget.StandaloneWindows64, outputPath);
                var manifestPath = Path.Combine(outputFolder, BuildManifestWriter.ManifestFileName);

                Assert.IsTrue(File.Exists(manifestPath), "Expected the build manifest to be written.");
                Assert.IsTrue(result.Log.Any(entry =>
                    entry.ComponentId == "build_manifest" &&
                    entry.Level == ReleaseGuardPostBuildLogLevel.Info));

                var json = File.ReadAllText(manifestPath);
                var manifest = JsonUtility.FromJson<ManifestProbe>(json);

                Assert.AreEqual("StandaloneWindows64", manifest.buildTarget);
                Assert.AreEqual("Game.exe", manifest.outputFileName);
                CollectionAssert.Contains(manifest.disabledComponentIds, "managed_stripping");

                var preBuildComponent = manifest.components.Single(c => c.id == "scripting_backend");
                CollectionAssert.Contains(preBuildComponent.phases, "pre_build");

                var postBuildComponent = manifest.components.Single(c => c.id == "build_manifest");
                CollectionAssert.Contains(postBuildComponent.phases, "post_build");
            }
            finally
            {
                if (Directory.Exists(outputFolder))
                    Directory.Delete(outputFolder, recursive: true);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void WritesManifest_ToCustomOutputPath()
        {
            var settings = ComponentTestHarness.CreateSettings();
            var config = settings.components.componentToggles
                .GetOrCreate<BuildManifestWriter.Config>("build_manifest");
            config.enabled = true;

            var buildFolder = Path.Combine(Path.GetTempPath(), $"ReleaseGuardBuild_{Guid.NewGuid():N}");
            var artifactsFolder = Path.Combine(Path.GetTempPath(), $"ReleaseGuardArtifacts_{Guid.NewGuid():N}");
            Directory.CreateDirectory(buildFolder);
            var outputPath = Path.Combine(buildFolder, "Game.exe");

            config.outputPath = artifactsFolder;

            try
            {
                ComponentTestHarness.RunPostBuild(
                    settings, BuildTarget.StandaloneWindows64, outputPath);

                var manifestPath = Path.Combine(artifactsFolder, BuildManifestWriter.ManifestFileName);
                Assert.IsTrue(File.Exists(manifestPath),
                    $"Expected manifest at custom path '{manifestPath}'.");
                Assert.IsFalse(
                    File.Exists(Path.Combine(buildFolder, BuildManifestWriter.ManifestFileName)),
                    "Manifest should not be written next to the build output when outputPath is set.");
            }
            finally
            {
                if (Directory.Exists(buildFolder))
                    Directory.Delete(buildFolder, recursive: true);
                if (Directory.Exists(artifactsFolder))
                    Directory.Delete(artifactsFolder, recursive: true);
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void ExpandEnvVars_ExpandsPercentStyle()
        {
            var original = Environment.GetEnvironmentVariable("TEMP") ?? Environment.GetEnvironmentVariable("TMP");
            if (original == null) Assert.Ignore("No TEMP/TMP env var available on this machine.");

            var result = BuildManifestWriter.ExpandEnvVars("%TEMP%/artifacts");
            StringAssert.StartsWith(original, result);
        }

        [Test]
        public void ResolveManifestFolder_EmptyPath_ReturnsBuildOutputFolder()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"ReleaseGuardResolve_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                var outputExe = Path.Combine(tempDir, "Game.exe");
                var resolved = BuildManifestWriter.ResolveManifestFolder("", outputExe);
                Assert.AreEqual(tempDir, resolved);
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        [Test]
        public void ResolveManifestFolder_AbsolutePath_ReturnsAbsolutePath()
        {
            var absPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
            var resolved = BuildManifestWriter.ResolveManifestFolder(absPath, "/some/build/Game.exe");
            Assert.AreEqual(absPath, resolved);
        }

        [Serializable]
        private sealed class ManifestProbe
        {
            public string buildTarget;
            public string outputFileName;
            public List<string> disabledComponentIds;
            public List<ComponentProbe> components;
        }

        [Serializable]
        private sealed class ComponentProbe
        {
            public string id;
            public List<string> phases;
        }
    }
}
