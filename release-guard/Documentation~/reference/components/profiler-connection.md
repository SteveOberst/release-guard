# Profiler connection disabled

| Field | Value |
|---|---|
| Id | `profiler_connection` |
| Phase | `pre-build` |
| Default posture | blocking check |
| Blocks build | yes |

## Overview

This component reports when `Autoconnect Profiler` is enabled. It exists because profiler connection support is useful during development and unnecessary surface in a shipped player.

## How it evaluates

During a real build, the component checks `BuildOptions.ConnectWithProfiler`.

During a checks-window run, it checks `EditorUserBuildSettings.connectProfiler`.

If the active profile forbids profiler connection and the flag is enabled, the component reports an error.

## Settings

| Setting | Effect | Typical use |
|---|---|---|
| `enabled` | When off, skips the component entirely for the active profile. | Turn off only when a profile intentionally allows profiler attachment. |

## Active-profile defaults

Behavior depends on the active profile's settings.

In the seeded profile assets:

- `release.asset` leaves this component enabled
- `development.asset` disables this component

## Common pitfalls

- This is separate from `development_build` on purpose.
- Turning off the broader Development Build flag is not the same thing as explicitly verifying profiler autoconnect is off.

## Troubleshooting

### "Why did this still fire even though I was focused on another build flag?"

Because the profiler flag is checked directly. It is not inferred from the broader development-build status.

### "Why do manual checks show this?"

The checks window reads the current editor-side build option when no `BuildReport` is present.

## Related docs

- [Development build disabled](development-build.md)
- [Pre-Build Checks window](../../guides/checks-window.md)
- [Build profiles](../../build-profiles.md)
