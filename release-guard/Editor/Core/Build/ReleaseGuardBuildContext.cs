using System.Collections.Generic;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Components;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Core.Build
{
    /// <summary>
    /// Everything a component needs for one build event run. Call
    /// <see cref="Info"/>/<see cref="Warning"/>/<see cref="Error"/> to record what was done;
    /// entries are automatically attributed to the running component.
    ///
    /// <para><b>Output path:</b> <see cref="OutputPath"/> is the path to the built product
    /// (e.g. <c>Builds/Windows/MyGame.exe</c>). On platforms that output a folder
    /// (for example WebGL) it is the product folder itself.</para>
    ///
    /// <para><b>BuildReport availability:</b> <see cref="BuildReport"/> is set during an active
    /// Unity build; it is <c>null</c> when running against an existing output -- always
    /// null-check before reading it.</para>
    /// </summary>
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    // ReSharper disable UnusedMember.Global
    public sealed class ReleaseGuardBuildContext
    {
        private readonly List<ReleaseGuardBuildLog> _log;
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

        /// <summary>Path to the built product (e.g. <c>Builds/Windows/MyGame.exe</c>). Always set.</summary>
        public string OutputPath { get; }

        // -----------------------------------------------------------------
        // Factories
        // -----------------------------------------------------------------

        internal static ReleaseGuardBuildContext ForBuild(
            ReleaseGuardSettings settings,
            BuildReport report,
            List<ReleaseGuardBuildLog> log)
            => new(
                settings, report,
                report.summary.platform,
                report.summary.outputPath,
                log);

        internal static ReleaseGuardBuildContext ForOutputPath(ReleaseGuardSettings settings, BuildTarget buildTarget,
            string outputPath, List<ReleaseGuardBuildLog> log) =>
            new(settings, null, buildTarget, outputPath, log);

        // -----------------------------------------------------------------
        // Private constructor
        // -----------------------------------------------------------------

        private ReleaseGuardBuildContext(
            ReleaseGuardSettings settings,
            BuildReport buildReport,
            BuildTarget buildTarget,
            string outputPath,
            List<ReleaseGuardBuildLog> log)
        {
            Settings = settings;
            BuildReport = buildReport;
            BuildTarget = buildTarget;
            OutputPath = outputPath;
            _log = log;
        }

        internal List<ReleaseGuardBuildLog> LogEntries => _log;

        // -----------------------------------------------------------------
        // Logging API
        // -----------------------------------------------------------------

        internal void BeginComponent(ReleaseGuardComponent component) =>
            _currentComponentId = component.Id;

        public void Info(string message) =>
            _log.Add(new ReleaseGuardBuildLog(_currentComponentId, ReleaseGuardBuildLogLevel.Info, message));

        public void Warning(string message) =>
            _log.Add(new ReleaseGuardBuildLog(_currentComponentId, ReleaseGuardBuildLogLevel.Warning, message));

        public void Error(string message) =>
            _log.Add(new ReleaseGuardBuildLog(_currentComponentId, ReleaseGuardBuildLogLevel.Error, message));
    }
    // ReSharper enable UnusedAutoPropertyAccessor.Global
    // ReSharper enable UnusedMember.Global
}