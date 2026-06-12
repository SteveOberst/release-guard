using System.Collections.Generic;
using ReleaseGuard.Editor.Config;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Core.PostProcessing
{
    /// <summary>
    /// Everything a post-processor needs for one run. Call
    /// <see cref="Info"/>/<see cref="Warning"/>/<see cref="Error"/> to record what was done;
    /// entries are automatically attributed to the running post-processor.
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
    public sealed class ReleasePostProcessContext
    {
        private readonly List<ReleasePostProcessLog> _log;
        private string _currentPostProcessorId;

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
        /// Always set. The containing folder is <c>System.IO.Path.GetDirectoryName(OutputPath)</c>.
        /// </summary>
        public string OutputPath { get; }

        // -----------------------------------------------------------------
        // Factories
        // -----------------------------------------------------------------

        internal static ReleasePostProcessContext ForBuild(
            ReleaseGuardSettings settings,
            BuildReport report,
            List<ReleasePostProcessLog> log)
            => new(
                settings, report,
                report.summary.platform,
                report.summary.outputPath,
                log);

        internal static ReleasePostProcessContext ForOutputPath(ReleaseGuardSettings settings, BuildTarget buildTarget,
            string outputPath, List<ReleasePostProcessLog> log) =>
            new(settings, null, buildTarget, outputPath, log);

        // -----------------------------------------------------------------
        // Private constructor
        // -----------------------------------------------------------------

        private ReleasePostProcessContext(
            ReleaseGuardSettings settings,
            BuildReport buildReport,
            BuildTarget buildTarget,
            string outputPath,
            List<ReleasePostProcessLog> log)
        {
            Settings = settings;
            BuildReport = buildReport;
            BuildTarget = buildTarget;
            OutputPath = outputPath;
            _log = log;
        }

        // -----------------------------------------------------------------
        // Logging API
        // -----------------------------------------------------------------

        internal void BeginPostProcessor(ReleasePostProcessor postProcessor) =>
            _currentPostProcessorId = postProcessor.Id;

        public void Info(string message) =>
            _log.Add(new ReleasePostProcessLog(_currentPostProcessorId, ReleasePostProcessLogLevel.Info, message));

        public void Warning(string message) =>
            _log.Add(new ReleasePostProcessLog(_currentPostProcessorId, ReleasePostProcessLogLevel.Warning, message));

        public void Error(string message) =>
            _log.Add(new ReleasePostProcessLog(_currentPostProcessorId, ReleasePostProcessLogLevel.Error, message));
    }
}