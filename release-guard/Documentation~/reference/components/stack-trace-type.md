# Stack trace log types

| Field | Value |
|---|---|
| Id | `stack_trace_type` |
| Phase | `pre-build` |
| Default posture | advisory |
| Blocks build | no by itself |

## Overview

This component reports when any Unity log channel is configured for full stack trace collection. It exists because full stack traces in shipped builds add logging overhead and expose more runtime symbol information than many teams intend.

## How it evaluates

The component checks the configured stack trace mode for:

- `Log`
- `Warning`
- `Error`
- `Assert`
- `Exception`

It reports when any channel is set to full stack trace collection.

Advisory id:

- `stack_trace_type.full`

The component's own guidance is usually:

- `Error` and `Exception` -> `ScriptOnly` or `None`
- `Log` and `Warning` -> `None`

Severity:

- `stack_trace_type.full` is emitted at `Info` severity

That means it is advisory-only by default and will not block a build at the seeded `Error` failure threshold.

## Settings

| Setting | Effect | Typical use |
|---|---|---|
| `enabled` | When off, skips the component entirely for the active profile. | Use when stack trace posture is intentionally unmanaged for that profile. |
| advisory suppressions | Can suppress `stack_trace_type.full` through the checks window. | Use when fuller traces are intentional for this project. |

This component has no dedicated settings object.

## Active-profile defaults

Behavior depends on whether the active profile disables the component or suppresses its advisory.

In the seeded profile assets:

- `release.asset` leaves this component enabled
- `development.asset` leaves this component enabled

## Common pitfalls

- This component is advisory by design. Some teams intentionally keep fuller traces in candidate or staging-style profiles.
- It is not limited to one log channel. Any full-stack-trace channel can trigger it.

## Troubleshooting

### "Why is this still enabled in the seeded development profile?"

Because the seeded profile assets are starting defaults, not hardcoded runtime modes. This advisory stays visible there unless you decide to relax it.

### "Why didn't suppressing one noisy advisory fix the next run?"

If multiple channels are configured for full traces, you may still be seeing the same advisory from the same underlying posture. The component is not per-channel suppressible.

## Related docs

- [Pre-Build Checks window](../../guides/checks-window.md)
- [Build profiles](../../build-profiles.md)
- [WebGL exception support](webgl-exception-support.md)
