using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Config
{
    /// <summary>
    /// Project-level registry for ReleaseGuard profiles. One asset lives at
    /// <see cref="DefaultAssetPath"/>; per-profile settings live at
    /// <c>Assets/ReleaseGuard/Profiles/{id}.asset</c>.
    /// </summary>
    public sealed class ReleaseGuardRegistry : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/ReleaseGuard/registry.asset";

        public const string ReleaseProfileId = "release";
        public const string DevelopmentProfileId = "development";

        [HideInInspector] public int schemaVersion;

        public List<ReleaseGuardProfile> profiles = new();

        public static ReleaseGuardRegistry LoadOrCreate()
        {
            var existing = AssetDatabase.LoadAssetAtPath<ReleaseGuardRegistry>(DefaultAssetPath);
            if (existing != null) return existing;

            ProfileSettingsRegistry.EnsureFolder();

            var registry = CreateInstance<ReleaseGuardRegistry>();
            AssetDatabase.CreateAsset(registry, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            return registry;
        }
    }
}