using System.Collections.Generic;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.PostBuild;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Tests
{
    internal static class ComponentTestHarness
    {
        public static ReleaseGuardSettings CreateSettings() =>
            ScriptableObject.CreateInstance<ReleaseGuardSettings>();

        public static ReleaseGuardPreBuildReport RunPreBuild(
            ReleaseGuardSettings settings,
            BuildTarget? buildTarget = null)
        {
            var environment = new ReleaseGuardEnvironment()
                .Initialize(settings, new ReleaseGuardLogger(false));

            var configuration = ReleaseGuardConfiguration.Resolve(settings, report: null);
            return environment.Pipeline.DispatchWithResult(
                ReleaseGuardPreBuildEvent.ForManualRun(
                    settings,
                    configuration,
                    environment.Logger,
                    buildTarget ?? EditorUserBuildSettings.activeBuildTarget),
                releaseEvent => releaseEvent.Report);
        }

        public static ReleaseGuardPostBuildResult RunPostBuild(
            ReleaseGuardSettings settings,
            BuildTarget buildTarget,
            string outputPath)
        {
            var environment = new ReleaseGuardEnvironment()
                .Initialize(settings, new ReleaseGuardLogger(false));

            ReleaseGuardDI.Configure(container => container.RegisterInstance(environment));
            try
            {
                return environment.Pipeline.DispatchWithResult(
                    new ReleaseGuardPostBuildEvent(
                        ReleaseGuardPostBuildContext.ForOutputPath(
                            settings,
                            buildTarget,
                            outputPath,
                            new List<ReleaseGuardPostBuildLog>())),
                    releaseEvent => releaseEvent.Result);
            }
            finally
            {
                ReleaseGuardDI.Clear();
            }
        }
    }
}
