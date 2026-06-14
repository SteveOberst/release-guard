using System;
using System.Collections.Generic;
using System.Linq;
using ReleaseGuard.Editor.Builtins;
using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Runtime;

namespace ReleaseGuard.Editor.Core.Components
{
    /// <summary>
    /// A single component as presented in the Components settings list: its id, display name, and
    /// the lifecycle phases it participates in (for the phase badges).
    /// </summary>
    internal readonly struct ComponentCatalogEntry
    {
        public ComponentCatalogEntry(
            string id,
            string displayName,
            IReadOnlyList<string> phases,
            Type settingsType)
        {
            Id = id;
            DisplayName = displayName;
            Phases = phases;
            SettingsType = settingsType;
        }

        public string Id { get; }
        public string DisplayName { get; }

        /// <summary>Human-readable phase labels, e.g. "Pre-Build", "Build", "Post-Build".</summary>
        public IReadOnlyList<string> Phases { get; }

        /// <summary>
        /// The concrete settings type for this component (a subclass of
        /// <see cref="ReleaseGuardComponentSettings"/>), or null for toggle-only components.
        /// </summary>
        public Type SettingsType { get; }
    }

    /// <summary>
    /// Enumerates every component that should appear in the Components settings list, independent of
    /// whether it is currently enabled. Built-in components always appear; plugin and auto-discovered
    /// components appear when they are registered in the active environment.
    ///
    /// Phases are resolved by replaying each component's <see cref="ReleaseGuardComponent.Register"/>
    /// into a throwaway binder, so they are available even for components that are currently disabled
    /// (and therefore absent from the live event bus).
    /// </summary>
    internal static class ComponentCatalog
    {
        public static IReadOnlyList<ComponentCatalogEntry> GetAll()
        {
            var byId = new Dictionary<string, ReleaseGuardComponent>(StringComparer.OrdinalIgnoreCase);

            foreach (var component in BuiltInComponentRegistry.GetAll())
                if (component != null && !string.IsNullOrEmpty(component.Id))
                    byId.TryAdd(component.Id, component);

            foreach (var component in RegisteredComponents())
                if (component != null && !string.IsNullOrEmpty(component.Id))
                    byId.TryAdd(component.Id, component);

            var entries = new List<ComponentCatalogEntry>(byId.Count);
            entries.AddRange(byId.Values.Select(component => new ComponentCatalogEntry(component.Id,
                component.DisplayName, ResolvePhases(component), component.SettingsType)));

            entries.Sort((a, b) =>
                string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            return entries;
        }

        private static IEnumerable<ReleaseGuardComponent> RegisteredComponents()
        {
            ReleaseGuardEnvironment environment;
            try
            {
                environment = ReleaseGuardDI.Resolve<ReleaseGuardEnvironment>();
            }
            catch
            {
                return Array.Empty<ReleaseGuardComponent>();
            }

            return environment?.Components?.Items ?? Array.Empty<ReleaseGuardComponent>();
        }

        private static IReadOnlyList<string> ResolvePhases(ReleaseGuardComponent component)
        {
            var binder = new ReleaseGuardComponentBinder(component);
            try
            {
                component.Register(binder);
            }
            catch
            {
                return Array.Empty<string>();
            }

            var kinds = new SortedSet<ReleaseGuardLifecycleEventKind>();
            foreach (var listener in binder.Build())
                kinds.Add(listener.EventKind);

            var labels = new List<string>(kinds.Count);
            labels.AddRange(kinds.Select(PhaseLabel));
            return labels;
        }

        private static string PhaseLabel(ReleaseGuardLifecycleEventKind kind) => kind switch
        {
            ReleaseGuardLifecycleEventKind.PreBuild => "Pre-Build",
            ReleaseGuardLifecycleEventKind.Build => "Build",
            ReleaseGuardLifecycleEventKind.PostBuild => "Post-Build",
            _ => kind.ToString()
        };
    }
}