using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ReleaseGuard.Editor.Core.Plugins
{
    internal static class PluginSettingsRegistry
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public const string PluginSettingsFolderPath = "Assets/ReleaseGuard/Plugins";

        public static string AssetPath(string pluginId) =>
            $"{PluginSettingsFolderPath}/{pluginId}.asset";

        public static ReleaseGuardPluginSettings LoadOrCreate(string pluginId, Type settingsType)
        {
            Validate(pluginId, settingsType);

            var assetPath = AssetPath(pluginId);
            var existing = TryLoad(pluginId, settingsType);
            if (existing is not null)
                return existing;

            var mismatched = AssetDatabase.LoadAssetAtPath<ReleaseGuardPluginSettings>(assetPath);
            if (mismatched is not null)
                throw new InvalidOperationException(
                    $"Plugin settings asset at '{assetPath}' is '{mismatched.GetType().FullName}', " +
                    $"not '{settingsType.FullName}'.");

            var directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var settings = (ReleaseGuardPluginSettings)ScriptableObject.CreateInstance(settingsType);
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            return settings;
        }

        public static ReleaseGuardPluginSettings TryLoad(string pluginId, Type settingsType)
        {
            Validate(pluginId, settingsType);

            var settings = AssetDatabase.LoadAssetAtPath<ReleaseGuardPluginSettings>(
                AssetPath(pluginId));
            return settingsType.IsInstanceOfType(settings) ? settings : null;
        }

        private static void Validate(string pluginId, Type settingsType)
        {
            if (string.IsNullOrEmpty(pluginId))
                throw new ArgumentException("Plugin id must be non-empty.", nameof(pluginId));
            if (settingsType is null)
                throw new ArgumentNullException(nameof(settingsType));
            if (!typeof(ReleaseGuardPluginSettings).IsAssignableFrom(settingsType))
                throw new ArgumentException(
                    $"Settings type must derive from {nameof(ReleaseGuardPluginSettings)}.",
                    nameof(settingsType));
            if (settingsType.IsAbstract)
                throw new ArgumentException("Settings type must be concrete.", nameof(settingsType));
        }
    }
}