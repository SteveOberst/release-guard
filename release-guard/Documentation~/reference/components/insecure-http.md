# Insecure HTTP option

| Field | Value |
|---|---|
| Id | `insecure_http` |
| Phase | `pre-build` |
| Default posture | advisory |
| Blocks build | no by itself |

## Overview

This component reports when `PlayerSettings.insecureHttpOption` is `AlwaysAllowed`. It exists because allowing cleartext HTTP in shipped builds means traffic can be read or modified in transit.

## How it evaluates

The component checks `PlayerSettings.insecureHttpOption` and reports only when the value is `AlwaysAllowed`.

It does not report:

- `NotAllowed`
- `DevelopmentOnly`

Advisory id:

- `insecure_http.always_allowed`

## Settings

| Setting | Effect | Typical use |
|---|---|---|
| `enabled` | When off, skips the component entirely for the active profile. | Use only when HTTP transport policy is intentionally outside Release Guard's scope. |
| advisory suppressions | Can suppress `insecure_http.always_allowed` through the checks window. | Use when cleartext HTTP is intentionally required for this project. |

This component has no dedicated settings object.

## Active-profile defaults

Behavior depends on the active profile's disable and advisory-suppression settings.

In the seeded profile assets:

- `release.asset` leaves this component enabled
- `development.asset` disables this component

## Common pitfalls

- This is advisory-only because real exceptions do exist, such as LAN devices, local companion apps, or legacy infrastructure without TLS.
- `DevelopmentOnly` is not reported. The component is intentionally narrower than "any non-TLS capability exists."

## Troubleshooting

### "Why is this not blocking the build?"

Because Release Guard treats insecure HTTP as a case that often needs a conscious exception rather than a universal hard stop.

### "Why is this quiet in one profile and noisy in another?"

Because the active profile may disable the component or suppress its advisory id. The transport setting itself may be unchanged.

## Related docs

- [Pre-Build Checks window](../../guides/checks-window.md)
- [Build profiles](../../build-profiles.md)
- [CI integration](../../guides/ci-integration.md)
