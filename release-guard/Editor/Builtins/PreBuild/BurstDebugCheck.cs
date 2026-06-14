using System;
using System.Reflection;
using ReleaseGuard.Editor.Core.PreBuild;
using ReleaseGuard.Editor.Core.Components;

namespace ReleaseGuard.Editor.Builtins.PreBuild
{
    /// <summary>
    /// Advisory: checks the Burst AOT settings for the current build target when the Burst
    /// package is installed.
    ///
    /// <para>Two configurations are flagged, both at advisory level:</para>
    /// <list type="bullet">
    /// <item><b>Optimizations disabled</b> -- Burst compiles native code without optimizations,
    /// shipping slower and easier-to-read machine code.</item>
    /// <item><b>Native debug mode enabled</b> ("Enable debug in all builds") -- Burst emits
    /// debug-friendly, unoptimized native code with debug information in every build,
    /// including releases.</item>
    /// </list>
    ///
    /// <para><b>Soft dependency:</b> this package does not reference Burst. The settings are read
    /// via reflection from <c>Unity.Burst.Editor.BurstPlatformAotSettings</c>. If Burst is not
    /// installed, or a future Burst version renames the internals this component reads, the check
    /// silently skips -- it never reports an issue it cannot verify. Skips are visible in the
    /// Console when verbose logging is enabled in settings.</para>
    /// </summary>
    public sealed class BurstDebugCheck : ReleaseGuardComponent
    {
        public override string Id => "burst_debug";
        public override string DisplayName => "Burst AOT debug settings";

        private const string OptimisationsSuppressId = "burst_debug.optimisations_disabled";
        private const string DebugModeSuppressId = "burst_debug.debug_enabled";

        // Field name candidates across Burst versions. Reads fall back through the list;
        // if none match, the corresponding check is skipped.
        private static readonly string[] OptimisationFieldNames = { "EnableOptimisations", "EnableOptimizations" };
        private static readonly string[] DebugFieldNames = { "EnableDebugInAllBuilds", "EnableBurstDebug" };

        public override void Register(ReleaseGuardComponentBinder binder) =>
            binder.OnPreBuild(releaseEvent => Evaluate(releaseEvent.Context));

        private static void Evaluate(ReleaseGuardPreBuildContext context)
        {
            if (FindAotSettingsType() == null)
            {
                context.Logger.LogVerbose("[BurstDebug] Burst package not installed; skipping.");
                return;
            }

            object aotSettings;
            try
            {
                aotSettings = LoadAotSettings(context);
            }
            catch (Exception e)
            {
                context.Logger.LogVerbose(
                    $"[BurstDebug] Could not read Burst AOT settings via reflection; skipping. ({e.Message})");
                return;
            }

            if (aotSettings == null)
            {
                context.Logger.LogVerbose("[BurstDebug] Burst AOT settings unavailable; skipping.");
                return;
            }

            if (TryReadBool(aotSettings, OptimisationFieldNames, out var optimisationsEnabled)
                && !optimisationsEnabled)
            {
                context.Advisory(
                    OptimisationsSuppressId,
                    ReleaseIssueSeverity.Warning,
                    $"Burst optimizations are disabled for {context.BuildTarget}. Burst-compiled " +
                    "code ships slower and as easier-to-analyze machine code. This setting is " +
                    "usually only disabled temporarily for debugging.",
                    fixHint: "Project Settings > Burst AOT Settings: enable 'Enable Optimisations' " +
                             "for this platform.");
            }

            if (TryReadBool(aotSettings, DebugFieldNames, out var debugInAllBuilds)
                && debugInAllBuilds)
            {
                context.Advisory(
                    DebugModeSuppressId,
                    ReleaseIssueSeverity.Warning,
                    "Burst native debug mode ('Enable debug in all builds') is on for " +
                    $"{context.BuildTarget}. Release builds ship unoptimized Burst code with " +
                    "native debug information.",
                    fixHint: "Project Settings > Burst AOT Settings: disable " +
                             "'Enable debug in all builds' for this platform.");
            }
        }

        // -----------------------------------------------------------------
        // Reflection helpers
        // -----------------------------------------------------------------

        private static Type FindAotSettingsType() =>
            Type.GetType("Unity.Burst.Editor.BurstPlatformAotSettings, Unity.Burst.Editor", throwOnError: false);

        private static object LoadAotSettings(ReleaseGuardPreBuildContext context)
        {
            var type = FindAotSettingsType();

            var method = type?.GetMethod(
                "GetOrCreateSettings",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            return method?.Invoke(null, new object[] { context.BuildTarget });
        }

        private static bool TryReadBool(object instance, string[] fieldNameCandidates, out bool value)
        {
            const BindingFlags binding =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var name in fieldNameCandidates)
            {
                var field = instance.GetType().GetField(name, binding);
                if (field == null || field.FieldType != typeof(bool)) continue;
                value = (bool)field.GetValue(instance);
                return true;
            }

            value = false;
            return false;
        }
    }
}