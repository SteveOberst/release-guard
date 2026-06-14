using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ReleaseGuard.Editor.Core.PreBuild;
using ReleaseGuard.Editor.Core.Components;
using UnityEditor.Compilation;
using UnityEngine;

namespace ReleaseGuard.Editor.Builtins.PreBuild
{
    /// <summary>
    /// Flags any type or member marked with <see cref="ReleaseForbidden"/> that would
    /// ship in the player build. This catches debug hooks, cheat commands, test scaffolding, and
    /// dev-only backdoors left in the codebase before they reach production.
    ///
    /// "Shipping" is determined by the set of assemblies the Unity player would include.
    /// In editor manual-run mode this is approximated via
    /// <see cref="CompilationPipeline.GetAssemblies(AssembliesType)"/>; during an actual build
    /// the same heuristic applies.
    ///
    /// Internal -- for unit tests only: set <see cref="ShippingAssemblyNamesOverride"/> to inject
    /// a custom set of assembly names so tests can verify detection without a full player build.
    /// </summary>
    public sealed class ReleaseForbiddenCheck : ReleaseGuardComponent<ReleaseForbiddenCheck.Config>
    {
        public override string Id => "release_forbidden";
        public override string DisplayName => "Release-forbidden members";

        [Serializable]
        public sealed class Config : ReleaseGuardComponentSettings
        {
            [Tooltip("Assembly names to exclude from the [ReleaseForbidden] scan. Use this for third-party assemblies you cannot modify. Enter names without the .dll extension; matching is case-insensitive.")]
            public List<string> excludedAssemblies = new List<string>();
        }

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(releaseEvent => Evaluate(releaseEvent.Context));

        /// <summary>
        /// Override the set of shipping assembly names for unit-test isolation.
        /// When non-null this is used instead of the CompilationPipeline query.
        /// </summary>
        internal static HashSet<string> ShippingAssemblyNamesOverride { get; set; }

        private void Evaluate(ReleaseGuardPreBuildContext context)
        {
            var shippingNames = GetShippingAssemblyNames();

            // Scan all currently loaded assemblies whose name is in the shipping set.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var assemblyName = assembly.GetName().Name;

                if (!shippingNames.Contains(assemblyName))
                    continue;

                // Honor per-assembly exclusions from settings.
                if (Settings.excludedAssemblies.Exists(e =>
                        e != null && string.Equals(e.Trim(), assemblyName, StringComparison.OrdinalIgnoreCase)))
                {
                    context.Logger.LogVerbose(
                        $"[ReleaseForbidden] Assembly '{assemblyName}' is excluded via settings; skipping.");
                    continue;
                }

                foreach (var type in GetLoadableTypes(assembly))
                    ScanType(type, context);
            }
        }

        // -----------------------------------------------------------------
        // Scanning
        // -----------------------------------------------------------------

        private static void ScanType(Type type, ReleaseGuardPreBuildContext context)
        {
            // Type-level attribute.
            var typeAttr = type.GetCustomAttribute<ReleaseForbidden>(inherit: false);
            if (typeAttr != null)
                context.Report(typeAttr.Severity, FormatMessage(type.FullName, typeAttr));

            const BindingFlags binding =
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.DeclaredOnly;

            // Methods.
            foreach (var method in type.GetMethods(binding))
            {
                var attr = method.GetCustomAttribute<ReleaseForbidden>(inherit: false);
                if (attr != null)
                    context.Report(attr.Severity, FormatMessage($"{type.FullName}.{method.Name}", attr));
            }

            // Fields.
            foreach (var field in type.GetFields(binding))
            {
                var attr = field.GetCustomAttribute<ReleaseForbidden>(inherit: false);
                if (attr != null)
                    context.Report(attr.Severity, FormatMessage($"{type.FullName}.{field.Name}", attr));
            }

            // Properties.
            foreach (var prop in type.GetProperties(binding))
            {
                var attr = prop.GetCustomAttribute<ReleaseForbidden>(inherit: false);
                if (attr != null)
                    context.Report(attr.Severity, FormatMessage($"{type.FullName}.{prop.Name}", attr));
            }
        }

        private static string FormatMessage(string memberName, ReleaseForbidden attr)
        {
            var msg = $"[ReleaseForbidden] '{memberName}' must not ship in a release build.";
            if (!string.IsNullOrEmpty(attr.Reason))
                msg += $" Reason: {attr.Reason}";
            return msg;
        }

        // -----------------------------------------------------------------
        // Assembly helpers
        // -----------------------------------------------------------------

        private static HashSet<string> GetShippingAssemblyNames()
        {
            if (ShippingAssemblyNamesOverride != null)
                return ShippingAssemblyNamesOverride;

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var asm in CompilationPipeline.GetAssemblies(AssembliesType.Player))
                    names.Add(asm.name);
            }
            catch (Exception)
            {
                // CompilationPipeline may fail in some editor states; fall through with empty set.
            }

            return names;
        }

        private static IEnumerable<Type> GetLoadableTypes(System.Reflection.Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }
    }
}
