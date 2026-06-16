using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Config
{
    /// <summary>
    /// Keeps <see cref="ReleaseGuardRegistry"/> consistent at startup.
    ///
    /// Two responsibilities, deliberately separate:
    /// 1. <see cref="EnsureDefaultProfiles"/> is a structural invariant that runs on every load.
    ///    It guarantees the built-in Release and Development profiles (and their settings assets)
    ///    always exist, recovering from any prior partial or broken state.
    /// 2. Version-gated steps (currently just seeding the Release profile from a legacy settings
    ///    asset) run only when <c>schemaVersion &lt; CurrentVersion</c>.
    /// </summary>
    internal static class ProfileMigration
    {
        private const int CurrentVersion = 1;

        /// <summary>Returns true the first time the registry is created or migrated in this project.</summary>
        public static bool Run()
        {
            var registry = ReleaseGuardRegistry.LoadOrCreate();

            // Fresh installs (and projects upgrading from the legacy single-asset layout) seed the
            // Release profile from the old asset exactly once.
            var isFirstRun = registry.schemaVersion < CurrentVersion;

            var changed = EnsureDefaultProfiles(registry, seedReleaseFromLegacy: isFirstRun);

            if (isFirstRun)
            {
                registry.schemaVersion = CurrentVersion;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
            }

            return isFirstRun;
        }

        /// <summary>
        /// Ensures the Release and Development profiles exist in the registry and on disk.
        /// Idempotent: returns true only when it had to add an entry or (re)create an asset.
        /// </summary>
        private static bool EnsureDefaultProfiles(ReleaseGuardRegistry registry, bool seedReleaseFromLegacy)
        {
            ProfileSettingsRegistry.EnsureFolder();

            var changed = false;

            changed |= EnsureProfile(
                registry,
                id: ReleaseGuardRegistry.ReleaseProfileId,
                displayName: "Release",
                strategy: ActivationStrategy.IsReleaseBuild,
                insertAt: 0,
                seedFromLegacy: seedReleaseFromLegacy,
                applyDevelopmentDefaults: false);

            changed |= EnsureProfile(
                registry,
                id: ReleaseGuardRegistry.DevelopmentProfileId,
                displayName: "Development",
                strategy: ActivationStrategy.IsDevelopmentBuild,
                insertAt: 1,
                seedFromLegacy: false,
                applyDevelopmentDefaults: true);

            return changed;
        }

        private static bool EnsureProfile(
            ReleaseGuardRegistry registry,
            string id,
            string displayName,
            ActivationStrategy strategy,
            int insertAt,
            bool seedFromLegacy,
            bool applyDevelopmentDefaults)
        {
            var changed = false;

            if (!registry.profiles.Exists(p => p.id == id))
            {
                var entry = new ReleaseGuardProfile
                {
                    id = id,
                    displayName = displayName,
                    isDefault = true,
                    activation = new ProfileActivation { strategy = strategy }
                };

                var index = Mathf.Clamp(insertAt, 0, registry.profiles.Count);
                registry.profiles.Insert(index, entry);
                changed = true;
            }

            // Ensure the backing settings asset exists (recreates it if the user deleted it).
            var assetPath = ProfileSettingsRegistry.AssetPath(id);
            if (AssetDatabase.LoadAssetAtPath<ReleaseGuardSettings>(assetPath) == null)
            {
                var seeded = false;
                if (seedFromLegacy)
                {
                    const string legacyPath = ReleaseGuardSettings.DefaultAssetPath;
                    if (AssetDatabase.LoadAssetAtPath<ReleaseGuardSettings>(legacyPath) != null)
                        seeded = AssetDatabase.CopyAsset(legacyPath, assetPath);
                }

                if (!seeded)
                {
                    var created = ScriptableObject.CreateInstance<ReleaseGuardSettings>();
                    AssetDatabase.CreateAsset(created, assetPath);
                }

                if (applyDevelopmentDefaults)
                {
                    var settings = AssetDatabase.LoadAssetAtPath<ReleaseGuardSettings>(assetPath);
                    if (settings != null) ApplyDevelopmentDefaults(settings);
                }

                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Disables the strict release-check components for the Development profile, leaving only
        /// <c>ci_development_build</c> active so the gate still catches dev builds running in CI.
        /// </summary>
        private static void ApplyDevelopmentDefaults(ReleaseGuardSettings settings)
        {
            var toggles = settings.components.componentToggles;
            toggles.SetEnabled("scripting_backend", false);
            toggles.SetEnabled("development_build", false);
            toggles.SetEnabled("script_debugging", false);
            toggles.SetEnabled("profiler_connection", false);
            toggles.SetEnabled("managed_stripping", false);
            toggles.SetEnabled("broad_preserve", false);
            toggles.SetEnabled("release_forbidden", false);
            toggles.SetEnabled("burst_debug", false);
            toggles.SetEnabled("webgl_exception_support", false);
            toggles.SetEnabled("android_debuggable", false);
            toggles.SetEnabled("insecure_http", false);
            // ci_development_build stays enabled: it blocks dev builds running in CI
            EditorUtility.SetDirty(settings);
        }
    }
}