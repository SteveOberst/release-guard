# Script debugging disabled

| Field | Value |
|---|---|
| Id | `script_debugging` |
| Phase | `pre-build` |
| Default posture | blocking check |
| Blocks build | yes |

## Overview

This component reports when managed script debugging is enabled. It exists because debugger attachment makes runtime inspection and method patching much easier in a shipped player.

## How it evaluates

During a real build, the component checks `BuildOptions.AllowDebugging`.

During a checks-window run, it checks `EditorUserBuildSettings.allowDebugging`.

If the active profile forbids script debugging and debugging is enabled, the component reports an error.

## Settings

| Setting | Effect | Typical use |
|---|---|---|
| `enabled` | When off, skips the component entirely for the active profile. | Turn off only when a profile intentionally permits debugging. |

## Active-profile defaults

Behavior is driven by the active profile's settings.

In the seeded profile assets:

- `release.asset` leaves this component enabled
- `development.asset` disables this component

## Common pitfalls

- This component is distinct from `development_build`. Release Guard keeps the direct debugging decision separately visible and separately controllable.
- A checks-window run can still surface this because the editor stores the relevant build flag even without a `BuildReport`.

## Troubleshooting

### "Why is this firing when I only meant to test locally?"

Your current active profile for the build likely still forbids debugging, or you are running the checks window while editing a stricter profile.

### "Why didn't disabling Development Build fix this?"

Because this component looks at the debugging flag directly. The development-build check and debugging check are separate.

## Related docs

- [Development build disabled](development-build.md)
- [Build profiles](../../guides/build-profiles.md)
- [Pre-Build Checks window](../../guides/checks-window.md)
