# Built-in Settings Reference

This page maps the serialized built-in settings objects in the live codebase to their purpose.

For behavior details, prefer the component pages.

## `GeneralSettings`

| Field | Meaning |
|---|---|
| `enabled` | Master switch for all real build stages for the active profile |
| `failureThreshold` | Severity at or above which pre-build findings block a build |
| `verboseLogging` | Enables extra console diagnostics |

## `ComponentSettings`

| Field | Meaning |
|---|---|
| `excludedAssetPaths` | Central asset-path suppression list for pre-build findings with asset paths |
| `autoDiscoverComponents` | Enables TypeCache discovery of custom components |
| `componentToggles` | Per-component settings container storing enabled state and any component-specific fields |

## `componentToggles` entry model

Every component entry derives from `ReleaseGuardComponentSettings` and therefore has:

| Field | Meaning |
|---|---|
| `componentId` | Stable component id for the entry |
| `enabled` | When false, the component is skipped in every phase it participates in |

Components with extra live settings:

| Component id | Settings type | Extra fields |
|---|---|---|
| `managed_stripping` | `ManagedStrippingCheck.Config` | `minLevel` |
| `release_forbidden` | `ReleaseForbiddenCheck.Config` | `excludedAssemblies` |
| `debug_symbol_sweep` | `DebugSymbolSweep.Config` | `delete`, `extraPatterns` |
| `build_manifest` | `BuildManifestWriter.Config` | `outputPath` (string) -- write destination; empty means next to build output; `enabled` defaults to `false` |

All other current built-in components are toggle-only and use only the shared `enabled` field.

## `PluginSettings`

| Field | Meaning |
|---|---|
| `autoDiscoverPlugins` | Enables TypeCache discovery of plugins |
| `disabledPluginIds` | Prevents matching plugins from registering |

## Related but not stored in profile assets

There are three different settings surfaces in the package:

- profile settings: `ReleaseGuardSettings` assets under `Assets/ReleaseGuard/Profiles/`
- component settings: entries inside `components.componentToggles` in the active profile asset
- plugin settings: optional `ReleaseGuardPluginSettings` assets under `Assets/ReleaseGuard/Plugins/`

The Components page edits the second category for the currently selected profile. The Advisories page is different again: it manages project-scoped advisory suppression state, not profile assets.

### Advisory suppressions

Advisory dismissals are not stored in `ReleaseGuardSettings`.

The live implementation uses `AdvisorySuppressionStore`, backed by `EditorPrefs` and keyed to the current project path hash.

### Profile selection

Profile selection is not inside `ReleaseGuardSettings`.

That data lives in:

- `ReleaseGuardRegistry`
- `ReleaseGuardProfile`
- `ProfileActivation`

See [Build profiles](../build-profiles.md).
