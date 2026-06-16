using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.Config.Attributes;
using ReleaseGuard.Editor.Core.PostBuild;
using UnityEngine;

namespace ReleaseGuard.Editor.Builtins.PostBuild
{
    /// <summary>
    /// Scans the build output folder for debug artifacts Unity leaves next to the player and
    /// reports them (or, strictly opt-in, deletes them).
    ///
    /// <para><b>What is matched</b> -- entries directly inside the output folder (not recursive):</para>
    /// <list type="bullet">
    /// <item><c>*_BackUpThisFolder_ButDontShipItWithYourGame</c> -- IL2CPP symbols and generated
    /// C++; shipping it hands reverse-engineers a symbol map of your entire game.</item>
    /// <item><c>*_BurstDebugInformation_DoNotShip</c> -- Burst native debug data.</item>
    /// <item><c>*.pdb</c> -- loose managed/native debug symbol files.</item>
    /// <item>Any extra patterns from <c>debugSymbolSweep.extraPatterns</c> in settings.</item>
    /// </list>
    ///
    /// <para><b>Behavior</b> -- controlled by the component's settings:</para>
    /// <list type="bullet">
    /// <item><c>delete = false</c> (default): findings are reported as warnings in the
    /// post-build log. Nothing is touched.</item>
    /// <item><c>delete = true</c>: found artifacts are deleted, each deletion individually logged.
    /// Deletion refuses to touch anything that does not resolve to a path inside the output
    /// folder.</item>
    /// </list>
    ///
    /// <para><b>Before enabling deletion:</b> symbol folders are required to symbolicate crash
    /// dumps from players and cannot be regenerated without rebuilding. Archive them somewhere
    /// outside the shipped folder first; deletion here is for pipelines where the output folder
    /// is uploaded as-is.</para>
    /// </summary>
    public sealed class DebugSymbolSweep : ReleaseGuardComponent<DebugSymbolSweep.Config>
    {
        public override string Id => "debug_symbol_sweep";
        public override string DisplayName => "Debug symbol sweep";

        [Serializable]
        public sealed class Config : ReleaseGuardComponentSettings
        {
            [ConditionalWarning(
                "Deletion is enabled: found artifacts are removed from the output folder. " +
                "Deleted symbol folders cannot be regenerated without rebuilding; " +
                "archive them for crash symbolication first.")]
            [Tooltip("DESTRUCTIVE when enabled: delete found debug artifacts from the output folder instead " +
                     "of only reporting them. Off by default.")]
            public bool delete = false;

            [Tooltip("Additional file or folder names to treat as debug artifacts. Matched against entries " +
                     "directly inside the output folder; '*' wildcards are allowed, e.g. \"*.map\" or \"DebugData\".")]
            public List<string> extraPatterns = new();
        }

        private static readonly string[] BuiltInPatterns =
        {
            "*_BackUpThisFolder_ButDontShipItWithYourGame",
            "*_BurstDebugInformation_DoNotShip",
            "*.pdb"
        };

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPostBuild(releaseEvent => Execute(releaseEvent.Context, Settings));

        internal static void Execute(ReleaseGuardPostBuildContext context, Config settings = null)
        {
            settings ??= new Config();

            var outputFolder = ResolveOutputFolder(context.OutputPath);
            if (outputFolder == null || !Directory.Exists(outputFolder))
            {
                context.Warning($"Output folder could not be resolved from '{context.OutputPath}'; sweep skipped.");
                return;
            }

            var artifacts = FindArtifacts(outputFolder, settings.extraPatterns);

            if (artifacts.Count == 0)
            {
                context.Info("No debug artifacts found in the build output folder.");
                return;
            }

            if (!settings.delete)
            {
                foreach (var artifact in artifacts)
                    context.Warning(
                        $"Debug artifact in build output: '{Path.GetFileName(artifact)}'. " +
                        "Do not ship this with your game. Delete it before distribution, or enable " +
                        "deletion in Project Settings > Release Guard > Components > Debug symbol sweep " +
                        "(archive symbols for crash symbolication first).");
                return;
            }

            foreach (var artifact in artifacts)
                DeleteArtifact(artifact, outputFolder, context);
        }

        // -----------------------------------------------------------------
        // Core scanning -- static and IO-only so tests can run it against temp folders
        // -----------------------------------------------------------------

        /// <summary>
        /// Returns the full paths of all matching entries (files and folders) directly inside
        /// <paramref name="outputFolder"/>. Built-in patterns plus <paramref name="extraPatterns"/>;
        /// null/blank extra patterns and patterns containing path separators or ".." are ignored
        /// (patterns match names, not paths). Results are distinct and sorted for stable output.
        /// </summary>
        internal static List<string> FindArtifacts(string outputFolder, IEnumerable<string> extraPatterns)
        {
            var patterns = BuiltInPatterns.Concat(
                (extraPatterns ?? Enumerable.Empty<string>()).Where(IsValidPattern));

            var found = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pattern in patterns)
            {
                try
                {
                    foreach (var entry in Directory.EnumerateFileSystemEntries(
                                 outputFolder, pattern, SearchOption.TopDirectoryOnly))
                        found.Add(Path.GetFullPath(entry));
                }
                catch (ArgumentException)
                {
                    // Pattern rejected by the runtime: skip it.
                }
            }

            return found.ToList();
        }

        private static bool IsValidPattern(string pattern) =>
            !string.IsNullOrWhiteSpace(pattern) &&
            pattern.IndexOf('/') < 0 &&
            pattern.IndexOf('\\') < 0 &&
            !pattern.Contains("..");

        // -----------------------------------------------------------------
        // Deletion
        // -----------------------------------------------------------------

        public static void DeleteArtifact(string artifact, string outputFolder, ReleaseGuardPostBuildContext context)
        {
            // Refuse to delete anything that escapes the output folder, however it got matched.
            var fullArtifact = Path.GetFullPath(artifact);
            var fullOutput = Path.GetFullPath(outputFolder).TrimEnd(Path.DirectorySeparatorChar)
                             + Path.DirectorySeparatorChar;

            if (!fullArtifact.StartsWith(fullOutput, StringComparison.OrdinalIgnoreCase))
            {
                context.Error($"Refusing to delete '{fullArtifact}': outside the build output folder.");
                return;
            }

            try
            {
                if (Directory.Exists(fullArtifact))
                    Directory.Delete(fullArtifact, recursive: true);
                else if (File.Exists(fullArtifact))
                    File.Delete(fullArtifact);
                else
                    return;

                context.Info($"Deleted debug artifact: '{Path.GetFileName(fullArtifact)}'.");
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                context.Error($"Could not delete '{fullArtifact}': {e.Message}");
            }
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private static string ResolveOutputFolder(string outputPath)
        {
            if (string.IsNullOrEmpty(outputPath))
                return null;

            return Directory.Exists(outputPath)
                ? outputPath
                : Path.GetDirectoryName(outputPath);
        }
    }
}