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
                .GetOrCreate<ReleaseGuardComponentSettings>("build_manifest")
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
