using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Config
{
    /// <summary>Asset-path helpers for per-profile settings assets.</summary>
    internal static class ProfileSettingsRegistry
    {
        private const string RootFolder = "Assets/ReleaseGuard";
        private const string ProfilesFolder = "Assets/ReleaseGuard/Profiles";

        public static string AssetPath(string profileId)
            => $"{ProfilesFolder}/{profileId}.asset";

        public static ReleaseGuardSettings TryLoad(string profileId)
            => AssetDatabase.LoadAssetAtPath<ReleaseGuardSettings>(AssetPath(profileId));

        public static ReleaseGuardSettings LoadOrCreate(string profileId)
        {
            var path = AssetPath(profileId);
            var existing = AssetDatabase.LoadAssetAtPath<ReleaseGuardSettings>(path);
            if (existing != null) return existing;

            EnsureFolder();
            var settings = ScriptableObject.CreateInstance<ReleaseGuardSettings>();
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            return settings;
        }

        public static ReleaseGuardSettings CreateFromTemplate(string templateId, string newId)
        {
            EnsureFolder();
            var newPath = AssetPath(newId);

            if (!string.IsNullOrEmpty(templateId))
            {
                var templatePath = AssetPath(templateId);
                if (AssetDatabase.LoadAssetAtPath<ReleaseGuardSettings>(templatePath) != null)
                {
                    AssetDatabase.CopyAsset(templatePath, newPath);
                    AssetDatabase.SaveAssets();
                    return AssetDatabase.LoadAssetAtPath<ReleaseGuardSettings>(newPath);
                }
            }

            var settings = ScriptableObject.CreateInstance<ReleaseGuardSettings>();
            AssetDatabase.CreateAsset(settings, newPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        public static void Delete(string profileId)
        {
            var path = AssetPath(profileId);
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }

        /// <summary>
        /// Ensures the ReleaseGuard asset folders exist in the AssetDatabase. Uses
        /// <see cref="AssetDatabase.CreateFolder"/> rather than <c>Directory.CreateDirectory</c>
        /// so the folders are registered with Unity immediately and asset creation never fails.
        /// </summary>
        public static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(RootFolder))
                AssetDatabase.CreateFolder("Assets", "ReleaseGuard");
            if (!AssetDatabase.IsValidFolder(ProfilesFolder))
                AssetDatabase.CreateFolder(RootFolder, "Profiles");
        }
    }
}