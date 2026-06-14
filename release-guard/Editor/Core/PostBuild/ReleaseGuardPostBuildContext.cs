using System.Collections.Generic;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Components;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Core.PostBuild
{
    /// <summary>
    /// Everything a component needs for one post-build event run. Call
    /// <see cref="Info"/>/<see cref="Warning"/>/<see cref="Error"/> to record what was done;
    /// entries are automatically attributed to the running component.
    ///
    /// <para><b>Output path:</b> <see cref="OutputPath"/> is the path to the built product
    /// (e.g. <c>Builds/Windows/MyGame.exe</c>). For file outputs, the build output directory is
    /// <c>System.IO.Path.GetDirectoryName(OutputPath)</c>. On platforms that output a folder
    /// (for example WebGL), <c>OutputPath</c> is the product folder itself.</para>
    ///
    /// <para><b>BuildReport availability:</b> <see cref="BuildReport"/> is set during an active
    /// Unity build via <c>IPostprocessBuildWithReport</c>; it is <c>null</c> when running against
    /// an existing build output -- always null-check before reading it.</para>
    /// </summary>
    public sealed class ReleaseGuardPostBuildContext
    {
        private readonly List<ReleaseGuardPostBuildLog> _log;
        private string _currentComponentId;

        // -----------------------------------------------------------------
        // Public surface
        // -----------------------------------------------------------------

        /// <summary>Project-wide Release Guard settings.</summary>
        public ReleaseGuardSettings Settings { get; }

        /// <summary>
        /// The Unity build report. <c>null</c> when running outside an active Unity build.
        /// Always null-check before use.
        /// </summary>
        public BuildReport BuildReport { get; }

        /// <summary>Target platform. Always set regardless of whether a <see cref="BuildReport"/> is present.</summary>
        public BuildTarget BuildTarget { get; }

        /// <summary>
        /// Path to the built product (e.g. <c>Builds/Windows/MyGame.exe</c>).
        /// Always set. For file outputs, the containing folder is
        /// <c>System.IO.Path.GetDirectoryName(OutputPath)</c>; for folder outputs,
        /// <c>OutputPath</c> is the output folder.
        /// </summary>
        public string OutputPath { get; }

        // -----------------------------------------------------------------
        // Factories
        // -----------------------------------------------------------------

        internal static ReleaseGuardPostBuildContext ForBuild(
            ReleaseGuardSettings settings,
            BuildReport report,
            List<ReleaseGuardPostBuildLog> log)
            => new(
                settings, report,
                report.summary.platform,
                report.summary.outputPath,
                log);

        internal static ReleaseGuardPostBuildContext ForOutputPath(ReleaseGuardSettings settings,
            BuildTarget buildTarget,
            string outputPath, List<ReleaseGuardPostBuildLog> log) =>
            new(settings, null, buildTarget, outputPath, log);

        // -----------------------------------------------------------------
        // Private constructor
        // -----------------------------------------------------------------

        private ReleaseGuardPostBuildContext(
            ReleaseGuardSettings settings,
            BuildReport buildReport,
            BuildTarget buildTarget,
            string outputPath,
            List<ReleaseGuardPostBuildLog> log)
        {
            Settings = settings;
            BuildReport = buildReport;
            BuildTarget = buildTarget;
            OutputPath = outputPath;
            _log = log;
        }

        internal List<ReleaseGuardPostBuildLog> LogEntries => _log;

        // -----------------------------------------------------------------
        // Logging API
        // -----------------------------------------------------------------

        internal void BeginComponent(ReleaseGuardComponent component) =>
            _currentComponentId = component.Id;

        public void Info(string message) =>
            _log.Add(new ReleaseGuardPostBuildLog(_currentComponentId, ReleaseGuardPostBuildLogLevel.Info, message));

        public void Warning(string message) =>
            _log.Add(new ReleaseGuardPostBuildLog(_currentComponentId, ReleaseGuardPostBuildLogLevel.Warning, message));

        public void Error(string message) =>
            _log.Add(new ReleaseGuardPostBuildLog(_currentComponentId, ReleaseGuardPostBuildLogLevel.Error, message));
    }
}