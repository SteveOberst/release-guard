using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ReleaseGuard.Editor.Builtins.PostProcessor;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.PostProcessing;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    public sealed class DebugSymbolSweepPostProcessorTests
    {
        private string _outputFolder;

        [SetUp]
        public void SetUp()
        {
            _outputFolder = Path.Combine(
                Path.GetTempPath(), $"ReleaseGuardSweepTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_outputFolder);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_outputFolder))
                Directory.Delete(_outputFolder, recursive: true);
        }

        // -- Fixture helpers

        private string CreateFile(string name)
        {
            var path = Path.Combine(_outputFolder, name);
            File.WriteAllText(path, "x");
            return path;
        }

        private string CreateFolder(string name)
        {
            var path = Path.Combine(_outputFolder, name);
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "content.txt"), "x");
            return path;
        }

        private static ReleaseGuardSettings Settings() =>
            ScriptableObject.CreateInstance<ReleaseGuardSettings>();

        private ReleasePostProcessContext Context(
            ReleaseGuardSettings settings, List<ReleasePostProcessLog> log, string productFileName = "Game.exe")
            => ReleasePostProcessContext.ForOutputPath(
                settings,
                BuildTarget.StandaloneWindows64,
                Path.Combine(_outputFolder, productFileName),
                log);

        // -- FindArtifacts

        [Test]
        public void FindArtifacts_MatchesKnownUnityArtifacts()
        {
            var backup = CreateFolder("Game_BackUpThisFolder_ButDontShipItWithYourGame");
            var burst  = CreateFolder("Game_BurstDebugInformation_DoNotShip");
            var pdb    = CreateFile("UnityPlayer_Win64_il2cpp_x64.pdb");
            CreateFile("Game.exe");
            CreateFolder("Game_Data");

            var found = DebugSymbolSweepPostProcessor.FindArtifacts(_outputFolder, null);

            Assert.AreEqual(3, found.Count);
            CollectionAssert.Contains(found, Path.GetFullPath(backup));
            CollectionAssert.Contains(found, Path.GetFullPath(burst));
            CollectionAssert.Contains(found, Path.GetFullPath(pdb));
        }

        [Test]
        public void FindArtifacts_MatchesExtraPatterns()
        {
            var map = CreateFile("game.map");
            CreateFile("Game.exe");

            var found = DebugSymbolSweepPostProcessor.FindArtifacts(
                _outputFolder, new[] { "*.map" });

            Assert.AreEqual(1, found.Count);
            CollectionAssert.Contains(found, Path.GetFullPath(map));
        }

        [Test]
        public void FindArtifacts_IgnoresBlankAndInvalidPatterns()
        {
            CreateFile("Game.exe");

            Assert.DoesNotThrow(() =>
            {
                var found = DebugSymbolSweepPostProcessor.FindArtifacts(
                    _outputFolder, new[] { "", "   ", null, "../escape" });
                Assert.IsEmpty(found);
            });
        }

        [Test]
        public void FindArtifacts_IsTopLevelOnly()
        {
            var nested = Path.Combine(CreateFolder("Game_Data"), "nested.pdb");
            File.WriteAllText(nested, "x");

            var found = DebugSymbolSweepPostProcessor.FindArtifacts(_outputFolder, null);

            Assert.IsEmpty(found);
        }

        // -- PostProcess: report-only (default)

        [Test]
        public void PostProcess_ReportOnly_WarnsAndDeletesNothing()
        {
            var backup = CreateFolder("Game_BackUpThisFolder_ButDontShipItWithYourGame");
            var s   = Settings();
            var log = new List<ReleasePostProcessLog>();
            try
            {
                var context = Context(s, log);
                context.BeginPostProcessor(new DebugSymbolSweepPostProcessor());

                new DebugSymbolSweepPostProcessor().PostProcess(context);

                Assert.IsTrue(Directory.Exists(backup), "Report-only mode must not delete anything.");
                Assert.AreEqual(1, log.Count(e => e.Level == ReleasePostProcessLogLevel.Warning),
                    "Each artifact must produce one warning in report-only mode.");
            }
            finally { UnityEngine.Object.DestroyImmediate(s); }
        }

        // -- PostProcess: deletion (opt-in)

        [Test]
        public void PostProcess_Delete_RemovesArtifacts_AndKeepsProduct()
        {
            var backup = CreateFolder("Game_BackUpThisFolder_ButDontShipItWithYourGame");
            var pdb    = CreateFile("Game.pdb");
            var exe    = CreateFile("Game.exe");
            var s   = Settings();
            var log = new List<ReleasePostProcessLog>();
            try
            {
                s.postProcessors.debugSymbolSweepDelete = true;
                var context = Context(s, log);
                context.BeginPostProcessor(new DebugSymbolSweepPostProcessor());

                new DebugSymbolSweepPostProcessor().PostProcess(context);

                Assert.IsFalse(Directory.Exists(backup), "Backup folder must be deleted.");
                Assert.IsFalse(File.Exists(pdb),          "Loose .pdb must be deleted.");
                Assert.IsTrue(File.Exists(exe),           "The build product must never be touched.");
                Assert.AreEqual(2, log.Count(e => e.Level == ReleasePostProcessLogLevel.Info),
                    "Each deletion must be individually logged.");
            }
            finally { UnityEngine.Object.DestroyImmediate(s); }
        }

        // -- ShouldRun gate

        [Test]
        public void ShouldRun_FollowsSettingsFlag()
        {
            var s   = Settings();
            var log = new List<ReleasePostProcessLog>();
            try
            {
                var pp      = new DebugSymbolSweepPostProcessor();
                var context = Context(s, log);

                s.postProcessors.debugSymbolSweepEnabled = true;
                Assert.IsTrue(pp.ShouldRun(context));

                s.postProcessors.debugSymbolSweepEnabled = false;
                Assert.IsFalse(pp.ShouldRun(context));
            }
            finally { UnityEngine.Object.DestroyImmediate(s); }
        }
    }
}
