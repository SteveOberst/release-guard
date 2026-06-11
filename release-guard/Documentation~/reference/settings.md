# Settings Reference

Release Guard is configured by a single `ReleaseGuardSettings` ScriptableObject,
edited via `Edit > Project Settings > Release Guard`. Settings are grouped into
typed sub-objects, each mapping one-to-one to a Project Settings page. Code outside
the UI layer reads fields through these sub-objects, e.g.
`settings.auditors.requireIl2Cpp`.

## Asset path and loading

- Asset path constant: `ReleaseGuardSettings.DefaultAssetPath` =
  `Assets/ReleaseGuard/ReleaseGuardSettings.asset`
- `ReleaseGuardSettings.LoadOrCreate()` loads the asset at the default path; on
  first use it creates the containing folder (if missing) and the asset, then
  saves. The instance ensures all sub-objects are non-null on `OnEnable` and on
  creation.

## Settings pages

Pages are ordered by the `[SettingsPage(order, ...)]` attribute on each sub-object
field:

| Order | Field | Type | Page label |
| --- | --- | --- | --- |
| 1 | `general` | `GeneralSettings` | General |
| 2 | `auditors` | `AuditorSettings` | Auditors |
| 3 | `postProcessors` | `PostProcessorSettings` | Post-Processors |
| 4 | `transformers` | `TransformerSettings` | Transformers |
| 5 | `plugins` | `PluginSettings` | Plugins |

## General (`settings.general`, `GeneralSettings`)

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `enabled` | `bool` | `true` | Master switch. When off, Release Guard never runs. |
| `skipOnDevelopmentBuilds` | `bool` | `true` | Skip all checks for Development builds; release rules only apply to non-development builds. |
| `failureThreshold` | `ReleaseIssueSeverity` | `Error` | A build is blocked when any issue is at or above this severity. |
| `verboseLogging` | `bool` | `false` | Log extra diagnostic detail (discovered auditors, skips, etc.) to the Console. |
| `profileOverrides` | `List<BuildProfileOverride>` | empty list | Optional per-profile overrides, matched against the active Build Profile name. |

### BuildProfileOverride

A per-Build-Profile override of the global settings (Unity 6+ Build Profiles).

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `buildProfileName` | `string` | null | Exact name of the Unity Build Profile this override applies to. |
| `enabled` | `bool` | `true` | Whether Release Guard runs at all for this profile. |
| `failureThreshold` | `ReleaseIssueSeverity` | `Error` | Builds using this profile fail when an issue is at or above this severity. |

See [guides/build-profiles](../guides/build-profiles.md).

## Auditors (`settings.auditors`, `AuditorSettings`)

Built-in Rules:

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `requireIl2Cpp` | `bool` | `true` | Require the IL2CPP scripting backend (Mono ships C# as decompilable .NET assemblies). |
| `forbidDevelopmentBuild` | `bool` | `true` | Treat shipping a Development Build as a release issue. |
| `forbidScriptDebugging` | `bool` | `true` | Treat allowing a managed script debugger to attach as a release issue. |
| `forbidProfilerConnection` | `bool` | `true` | Treat shipping with the Unity profiler connection enabled as a release issue. |
| `minManagedStrippingLevel` | `ManagedStrippingLevel` | `Medium` | Minimum managed code stripping level required (`Disabled` = don't check). |
| `forbidBroadPreserve` | `bool` | `true` | Treat broad `[Preserve]` usage and broad link.xml preservation rules as a release issue. |

Discovery:

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `autoDiscoverAuditors` | `bool` | `false` | Automatically run every `ReleaseAuditor` subclass found in the project (excluding test fixtures and built-in types). |
| `disabledAuditorIds` | `List<string>` | empty list | Auditor ids to skip, e.g. `scripting_backend`. |

Exclusions:

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `excludedAssetPaths` | `ExclusionList` | empty | gitignore-style glob patterns for asset paths to exclude from release issues. Use `!` to re-include. |
| `releaseForbiddenExcludedAssemblies` | `List<string>` | empty list | Assembly names to exclude from the `[ReleaseForbidden]` scan (case-insensitive, no .dll extension). |

Advisory Suppressions:

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `suppressedAdvisoryIds` | `List<string>` | empty list | Advisory ids dismissed via "Don't show again". Remove an id to re-enable the advisory. |

See [guides/asset-exclusions](../guides/asset-exclusions.md) and
[guides/release-forbidden](../guides/release-forbidden.md).

## Post-Processors (`settings.postProcessors`, `PostProcessorSettings`)

Debug Symbol Sweep:

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `debugSymbolSweepEnabled` | `bool` | `true` | Scan the build output folder after a release build for debug artifacts. Report-only unless deletion is enabled below. |
| `debugSymbolSweepDelete` | `bool` | `false` | DESTRUCTIVE when enabled: delete found debug artifacts instead of only reporting them. (Carries a conditional warning in the UI when on.) |
| `debugSymbolSweepExtraPatterns` | `List<string>` | empty list | Additional file or folder names to treat as debug artifacts. Matched against entries directly inside the output folder; `*` wildcards allowed. |

Build Manifest:

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `writeBuildManifest` | `bool` | `false` | Write a `release-guard-manifest.json` next to the build output after every release build. Off by default; intended as a CI artifact, not for shipping. |

Discovery:

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `autoDiscoverPostProcessors` | `bool` | `false` | Automatically run every `ReleasePostProcessor` subclass found in the project after a release build. |
| `disabledPostProcessorIds` | `List<string>` | empty list | Post-processor ids to skip, e.g. `debug_symbol_sweep`. |

See [built-in-post-processors](built-in-post-processors.md).

## Transformers (`settings.transformers`, `TransformerSettings`)

Discovery:

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `autoDiscoverTransformers` | `bool` | `false` | Automatically run every `ReleaseTransformer` subclass found in the project after a release build, before the post-processor pipeline. |
| `disabledTransformerIds` | `List<string>` | empty list | Transformer ids to skip. |

No built-in transformers ship - see [built-in-transformers](built-in-transformers.md).

## Plugins (`settings.plugins`, `PluginSettings`)

Discovery:

| Field | Type | Default | Description |
| --- | --- | --- | --- |
| `autoDiscoverPlugins` | `bool` | `false` | Automatically discover every `ReleaseGuardPlugin` subclass via TypeCache and invoke it. Off by default; prefer explicit registration. |
| `disabledPluginIds` | `List<string>` | empty list | Plugin ids to skip entirely (all of a plugin's contributions are excluded). |

See [api/plugins](../api/plugins.md).

## Overview page members

The root overview page is generated from attributes on `ReleaseGuardSettings`:

- `StatusText` (`[SettingsStatus]`) - a status line that reflects whether Release
  Guard is active and the current `failureThreshold`.
- `[SettingsAction]` buttons: "Open Audit Window" (order 0), "Ping Settings Asset"
  (order 1), "Reload Release Guard" (order 2).

## Query helper methods

`ReleaseGuardSettings` exposes convenience lookups. Id lists are compared trimmed,
so a stray space never silently breaks a lookup.

| Method | Returns |
| --- | --- |
| `IsAuditorDisabled(string id)` | `bool` - id is in `auditors.disabledAuditorIds`. |
| `IsPostProcessorDisabled(string id)` | `bool` - id is in `postProcessors.disabledPostProcessorIds`. |
| `IsTransformerDisabled(string id)` | `bool` - id is in `transformers.disabledTransformerIds`. |
| `IsPluginDisabled(string id)` | `bool` - id is in `plugins.disabledPluginIds`. |
| `IsAdvisorySuppressed(string suppressId)` | `bool` - id is in `auditors.suppressedAdvisoryIds`. |
| `IsAssemblyExcludedFromReleaseForbidden(string assemblyName)` | `bool` - case-insensitive match in `auditors.releaseForbiddenExcludedAssemblies`. |
| `GetProfileOverride(string profileName)` | `BuildProfileOverride` or null - the override whose `buildProfileName` matches. |
| `SuppressAdvisory(string suppressId)` | void - adds the id to `suppressedAdvisoryIds` and persists the asset. |

## See also

- [Built-in auditors](built-in-auditors.md)
- [Built-in post-processors](built-in-post-processors.md)
- [Built-in transformers](built-in-transformers.md)
- [Attributes reference](attributes.md)
- [api/settings](../api/settings.md)
