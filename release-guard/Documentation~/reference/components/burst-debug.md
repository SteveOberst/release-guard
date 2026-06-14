# Burst AOT debug settings

| Field | Value |
|---|---|
| Id | `burst_debug` |
| Phase | `pre-build` |
| Default posture | advisory |
| Blocks build | no by itself |
| Scope | only when Burst editor internals can be resolved |

## Overview

This component checks Burst AOT settings and reports when optimizations are disabled or debug mode is enabled for all builds. It exists because both settings are reasonable during debugging and bad defaults for shipped native code.

## How it evaluates

The component reflects into Burst editor internals rather than taking a direct Burst package dependency.

It looks for settings that indicate:

- optimizations disabled
- native debug mode enabled for all builds

If the reflection path cannot be resolved for the installed Burst version, the component safely skips instead of guessing. In that case it emits only verbose logs when verbose logging is enabled.

Advisory ids:

- `burst_debug.optimisations_disabled`
- `burst_debug.debug_enabled`

## Settings

| Setting | Effect | Typical use |
|---|---|---|
| `enabled` | When off, skips the component entirely for the active profile. | Use when Burst posture is intentionally unmanaged for that profile. |
| advisory suppressions | Can suppress the Burst advisory ids above through the checks window. | Use when the project intentionally keeps debug-friendly Burst output. |
| `general.verboseLogging` | Shows skip diagnostics when reflection fails. | Useful when validating behavior across Burst versions. |

This component has no dedicated settings object.

## Active-profile defaults

Behavior depends on the active profile's disable and advisory-suppression settings.

In the seeded profile assets:

- `release.asset` leaves this component enabled
- `development.asset` disables this component

## Common pitfalls

- A silent skip can be intentional. The component chooses safe non-detection over fragile guessing across Burst versions.
- This does not mean Burst is broken; it may only mean the current editor-side internals are not discoverable by the supported reflection path.

## Troubleshooting

### "Why didn't this run at all?"

The most likely reason is that Burst editor internals could not be found through the reflection path. Turn on verbose logging to confirm.

### "Why is this advisory-only?"

Because many teams intentionally relax Burst settings in certain profiles during debugging or late-stage investigation builds.

## Related docs

- [Pre-Build Checks window](../../guides/checks-window.md)
- [Build profiles](../../build-profiles.md)
- [Development notes](../../development.md)
