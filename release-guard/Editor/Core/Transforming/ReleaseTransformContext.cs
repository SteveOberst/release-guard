using System.Collections.Generic;
using ReleaseGuard.Editor.Config;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace ReleaseGuard.Editor.Core.Transforming
{
    /// <summary>
    /// Everything a transformer needs for one run. Call
    /// <see cref="Info"/>/<see cref="Warning"/>/<see cref="Error"/> to record what was done;
    /// entries are automatically attributed to the running transformer.
    ///
    /// <para><b>Output path:</b> <see cref="OutputPath"/> is the path to the built product
    /// (e.g. <c>Builds/Windows/MyGame.exe</c>). On platforms that output a folder
    /// (Android APK, WebGL) it is the product file/folder itself.</para>
    ///
    /// <para><b>BuildReport availability:</b> <see cref="BuildReport"/> is set during an active
    /// Unity build; it is <c>null</c> when running against an existing output -- always
    /// null-check before reading it.</para>
    /// </summary>
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    // ReSharper disable UnusedMember.Global
    public sealed class ReleaseTransformContext
    {
        private readonly List<ReleaseTransformLog> _log;
        private string _currentTransformerId;

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

        internal static ReleaseTransformContext ForBuild(
            ReleaseGuardSettings settings,
            BuildReport report,
            List<ReleaseTransformLog> log)
            => new(
                settings, report,
                report.summary.platform,
                report.summary.outputPath,
                log);

        internal static ReleaseTransformContext ForOutputPath(ReleaseGuardSettings settings, BuildTarget buildTarget,
            string outputPath, List<ReleaseTransformLog> log) =>
            new(settings, null, buildTarget, outputPath, log);

        // -----------------------------------------------------------------
        // Private constructor
        // -----------------------------------------------------------------

        private ReleaseTransformContext(
            ReleaseGuardSettings settings,
            BuildReport buildReport,
            BuildTarget buildTarget,
            string outputPath,
            List<ReleaseTransformLog> log)
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

        internal void BeginTransformer(ReleaseTransformer transformer) =>
            _currentTransformerId = transformer.Id;

        public void Info(string message) =>
            _log.Add(new ReleaseTransformLog(_currentTransformerId, ReleaseTransformLogLevel.Info, message));

        public void Warning(string message) =>
            _log.Add(new ReleaseTransformLog(_currentTransformerId, ReleaseTransformLogLevel.Warning, message));

        public void Error(string message) =>
            _log.Add(new ReleaseTransformLog(_currentTransformerId, ReleaseTransformLogLevel.Error, message));
    }
    // ReSharper enable UnusedAutoPropertyAccessor.Global
    // ReSharper enable UnusedMember.Global
}