using System;
using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Core.Components;

namespace ReleaseGuard.Editor.Core.Config.Types
{
    /// <summary>
    /// Dynamic container for per-component settings. Each component entry is stored polymorphically
    /// via <see cref="entries"/>: components with configurable fields use their typed inner
    /// <c>Settings</c> class (which extends <see cref="ReleaseGuardComponentSettings"/>); toggle-only
    /// components use the base type directly.
    ///
    /// Entries are created lazily - either by the component itself during
    /// <see cref="ReleaseGuardComponent.Initialize"/> (for reading) or by the settings UI when the
    /// user interacts with a component row (for persisting). Unknown components (not yet in
    /// <see cref="entries"/>) are treated as enabled by default, except those listed in
    /// <see cref="DefaultDisabledIds"/>.
    /// </summary>
    [Serializable]
    public sealed class ComponentToggleList
    {
        [UnityEngine.SerializeReference]
        public List<ReleaseGuardComponentSettings> entries = new();

        /// <summary>
        /// Component ids that are disabled when no explicit entry exists (i.e., their default state
        /// is disabled). Build manifest is opt-in since it writes a file to the build output folder.
        /// </summary>
        internal static readonly HashSet<string> DefaultDisabledIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "build_manifest" };

        public bool IsEnabled(string componentId)
        {
            var entry = FindEntry(componentId);
            if (entry != null) return entry.enabled;
            return !DefaultDisabledIds.Contains(componentId);
        }

        /// <summary>
        /// Returns the typed settings for a component if an entry exists, otherwise returns a
        /// transient default. Does not modify <see cref="entries"/> - safe to call during builds.
        /// </summary>
        public TSettings GetSettings<TSettings>(string componentId)
            where TSettings : ReleaseGuardComponentSettings, new()
        {
            if (FindEntry(componentId) is TSettings typed) return typed;
            return new TSettings { componentId = componentId };
        }

        /// <summary>
        /// Returns the typed settings for a component, creating and persisting an entry if one does
        /// not already exist. For use in tests and editor setup code only.
        /// </summary>
        public TSettings GetOrCreate<TSettings>(string componentId)
            where TSettings : ReleaseGuardComponentSettings, new()
        {
            if (FindEntry(componentId) is TSettings typed) return typed;
            var existing = FindEntry(componentId);
            if (existing != null) entries.Remove(existing);
            var created = new TSettings { componentId = componentId };
            entries.Add(created);
            return created;
        }

        /// <summary>Sets the enabled state for a component, creating a base entry if needed.</summary>
        public void SetEnabled(string componentId, bool enabled)
        {
            var entry = FindEntry(componentId);
            if (entry != null)
            {
                entry.enabled = enabled;
                return;
            }
            if (!enabled)
                entries.Add(new ReleaseGuardComponentSettings { componentId = componentId, enabled = false });
        }

        /// <summary>Returns a flat list of all explicitly disabled component ids.</summary>
        public List<string> GetDisabledIds()
        {
            return (from entry in entries
                where entry is { enabled: false } && !string.IsNullOrEmpty(entry.componentId)
                select entry.componentId).ToList();
        }

        public ReleaseGuardComponentSettings FindEntry(string componentId) =>
            entries.Find(e => e != null && string.Equals(
                e.componentId, componentId, StringComparison.OrdinalIgnoreCase));
    }
}
