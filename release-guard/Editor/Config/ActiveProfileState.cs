using System;
using UnityEditor;

namespace ReleaseGuard.Editor.Config
{
    /// <summary>
    /// Session-level state tracking which profile is currently being edited in Project Settings.
    /// Persisted via <see cref="EditorPrefs"/>; does NOT affect which profile is used at build
    /// time (builds always use the profile whose activation condition matches).
    /// </summary>
    internal static class ActiveProfileState
    {
        private const string PrefKey = "ReleaseGuard.ActiveProfileId";

        private static ReleaseGuardSettings _cache;

        public static event Action Changed;

        public static string CurrentProfileId
        {
            get => EditorPrefs.GetString(PrefKey, ReleaseGuardRegistry.ReleaseProfileId);
            set
            {
                if (value == CurrentProfileId) return;
                EditorPrefs.SetString(PrefKey, value);
                _cache = null;
                Changed?.Invoke();
            }
        }

        public static ReleaseGuardSettings CurrentSettings()
            => _cache ??= ProfileSettingsRegistry.LoadOrCreate(CurrentProfileId);

        /// <summary>Called when a profile settings asset is known to have changed externally.</summary>
        internal static void InvalidateCache() => _cache = null;
    }
}