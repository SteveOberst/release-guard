using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.DI;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Core.Runtime
{
    /// <summary>Unity Editor load hook for Release Guard runtime initialization.</summary>
    [InitializeOnLoad]
    internal static class ReleaseGuardStartup
    {
        static ReleaseGuardStartup()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= ReleaseGuardDI.Clear;
            AssemblyReloadEvents.beforeAssemblyReload += ReleaseGuardDI.Clear;

            // When the user switches editing profile, reinitialize so the environment reflects
            // the newly selected profile's settings.
            ActiveProfileState.Changed += Reload;

            Reload();
        }

        /// <summary>
        /// Reinitialize Release Guard immediately (e.g. from the Settings "Reload" button,
        /// after dismissing an advisory, or after switching the active editing profile).
        /// </summary>
        public static void Reload()
        {
            var isFirstRun = ProfileMigration.Run();

            var registry = ReleaseGuardRegistry.LoadOrCreate();
            var settings = ActiveProfileState.CurrentSettings();

            var environment = new ReleaseGuardEnvironment();
            ReleaseGuardDI.Configure(c => c.RegisterInstance(environment));
            environment.Initialize(settings);
            environment.SetRegistry(registry);

            if (isFirstRun)
                ShowOnboardingPrompt();
        }

        /// <summary>
        /// One-time welcome dialog shown the first time Release Guard initializes in a project.
        /// Gated by an EditorPrefs flag so it never repeats, and skipped entirely in batch mode
        /// so CI/build agents never block on a dialog.
        /// </summary>
        private static void ShowOnboardingPrompt()
        {
            if (Application.isBatchMode) return;

            var shownKey = $"ReleaseGuard.OnboardingShown.{Application.dataPath.GetHashCode():X8}";
            if (EditorPrefs.GetBool(shownKey, false)) return;
            EditorPrefs.SetBool(shownKey, true);

            EditorApplication.delayCall += () =>
            {
                if (EditorUtility.DisplayDialog(
                        "Release Guard",
                        "Release Guard is set up with default Release and Development profiles.\n\n" +
                        "Open the Checks window to review this project's release readiness.",
                        "Open Checks Window",
                        "Later"))
                {
                    EditorApplication.ExecuteMenuItem("Tools/Release Guard/Pre-Build Checks");
                }
            };
        }
    }
}
