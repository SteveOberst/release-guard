# Built-in Post-Processors

Post-processors run last, after a successful release build, and operate on the
final build output folder: cleanup, manifests, and metadata. Each derives from
`ReleasePostProcessor` and reports Info, Warning, or Error entries to the
post-process log. They run after any transformers, so they always see the final
state of the output folder.

The canonical list lives in `BuiltInPostProcessorRegistry.GetAll()` - built-ins
are added explicitly there (not discovered via TypeCache), so this is the
complete shipped set. Two post-processors ship.

To write your own, see
[api/custom-post-processors](../api/custom-post-processors.md).

## Summary table

| Id | DisplayName | Priority | Output | Gated by (ShouldRun) | Destructive |
| --- | --- | --- | --- | --- | --- |
| `debug_symbol_sweep` | Debug symbol sweep | 0 | Log warnings (report-only) | `postProcessors.debugSymbolSweepEnabled` (default on) | Deletes files only when `postProcessors.debugSymbolSweepDelete` is on (default off) |
| `build_manifest` | Build manifest | 100 | `release-guard-manifest.json` in the output folder | `postProcessors.writeBuildManifest` (default off) | No |

Both default the `BuildReport` to null when run against an existing build output
rather than during a live build; both null-check before using it.

## Post-processor details

### debug_symbol_sweep - Debug symbol sweep

- Id: `debug_symbol_sweep`
- DisplayName: `Debug symbol sweep`
- Priority: `0`
- ShouldRun: `context.Settings.postProcessors.debugSymbolSweepEnabled` (default on)

Scans the top level of the build output folder (not recursive) for debug
artifacts Unity leaves next to the player. Built-in patterns:

- `*_BackUpThisFolder_ButDontShipItWithYourGame` (IL2CPP symbols and generated C++)
- `*_BurstDebugInformation_DoNotShip` (Burst native debug data)
- `*.pdb` (loose managed/native debug symbol files)

Plus any extra patterns from `postProcessors.debugSymbolSweepExtraPatterns`.
Invalid extra patterns (blank, or containing a path separator or `..`) are
ignored - patterns match names, not paths.

Behavior is controlled by two settings flags:

- Report-only (default): each found artifact is logged as a Warning. Nothing is
  touched. When no artifacts are found, a single Info entry is logged.
- Delete (`postProcessors.debugSymbolSweepDelete`, default off): found artifacts
  are deleted and each deletion is logged as Info. Deletion refuses to touch
  anything that does not resolve to a path inside the output folder (logs an Error
  instead). IO or access errors during deletion are logged as Error.

Before enabling deletion: symbol folders are required to symbolicate crash dumps
and cannot be regenerated without rebuilding. Archive them outside the shipped
folder first.

If the output folder cannot be resolved from the output path, a Warning is logged
and the sweep is skipped.

### build_manifest - Build manifest

- Id: `build_manifest`
- DisplayName: `Build manifest`
- Priority: `100` (runs after the sweep and any default-priority custom
  post-processors, so it records the output folder's final, post-sweep state)
- ShouldRun: `context.Settings.postProcessors.writeBuildManifest` (default off)
- Output file name constant: `ManifestFileName` = `release-guard-manifest.json`

Writes `release-guard-manifest.json` into the build output folder, recording which
Release Guard configuration produced the build. The manifest is serialized with
`JsonUtility` (pretty-printed) and contains:

- `manifestVersion` (always `1`)
- `releaseGuardVersion` (package version, or `unknown` if not resolvable)
- `unityVersion`
- `buildTarget`
- `productName`
- `outputFileName`
- `failureThreshold` (from `general.failureThreshold`)
- `auditorIds`, `postProcessorIds`, `transformerIds` (the active registered ids)
- `disabledAuditorIds`, `disabledPostProcessorIds`, `disabledTransformerIds`,
  `disabledPluginIds`, `suppressedAdvisoryIds` (copied from settings)

When a `BuildReport` is available it additionally records `buildGuid`,
`buildStartedUtc`, `buildEndedUtc` (round-trip "o" format, UTC), and
`totalBuildSizeBytes`.

Deliberately NOT recorded: absolute paths and VCS revision info. To add a commit
hash, write your own higher-priority post-processor that amends the file, or stamp
it elsewhere in CI.

Off by default because the file documents the project's hardening configuration
and should not ship to players - it is intended as a CI artifact. Enable it only
if your packaging step excludes it from the shipped archive.

On success an Info entry is logged. If the output folder cannot be resolved, a
Warning is logged and no manifest is written. IO or access errors while writing
are logged as Error.

## See also

- [Settings reference](settings.md) - the post-processor settings page.
- [Built-in auditors](built-in-auditors.md)
- [Built-in transformers](built-in-transformers.md)
- [api/custom-post-processors](../api/custom-post-processors.md)
