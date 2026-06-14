# WebGL exception support

| Field | Value |
|---|---|
| Id | `webgl_exception_support` |
| Phase | `pre-build` |
| Default posture | advisory |
| Blocks build | no by itself |
| Scope | WebGL only |

## Overview

This component reports when WebGL exception support is configured for either full mode. It exists because full exception handling increases code size and runtime cost, and the stacktrace variant also ships more managed symbol information.

## How it evaluates

The component reports only when WebGL exception support is:

- `FullWithStacktrace`
- `FullWithoutStacktrace`

Severity differs by mode:

- `FullWithStacktrace` -> `Warning`
- `FullWithoutStacktrace` -> `Info`

Advisory id:

- `webgl_exception_support.full`

## Settings

| Setting | Effect | Typical use |
|---|---|---|
| `enabled` | When off, skips the component entirely for the active profile. | Use if WebGL exception policy is intentionally out of scope for that profile. |
| advisory suppressions | Can suppress `webgl_exception_support.full` through the checks window. | Use when fuller exception handling is intentional for this project. |

This component has no dedicated settings object.

## Active-profile defaults

The component's advisory behavior depends on whether the active profile disables it or suppresses its advisory id.

In the seeded profile assets:

- `release.asset` leaves this component enabled
- `development.asset` disables this component

## Common pitfalls

- This is advisory on purpose. It exists to force a conscious choice, not to assume full exception support is always unacceptable.
- Only the full modes are reported. `ScriptOnly` and weaker modes are not.

## Troubleshooting

### "Why is this only an advisory?"

Because some teams intentionally keep fuller exception behavior in staging or release-candidate style profiles for better triage.

### "Why didn't my checks window suppression carry into builds?"

It should. Suppression is stored project-wide, so a mismatch usually means you are looking at a different project or the advisory was never suppressed successfully.

## Related docs

- [Pre-Build Checks window](../../guides/checks-window.md)
- [Build profiles](../../build-profiles.md)
- [Stack trace log types](stack-trace-type.md)
