# Development build in CI

| Field | Value |
|---|---|
| Id | `ci_development_build` |
| Phase | `pre-build` |
| Default posture | blocking check |
| Blocks build | yes |

## Overview

This component blocks a Development Build that is running in CI. It exists as a separate guardrail because a profile may intentionally relax local development checks while still wanting CI to fail if a pipeline is producing a development build artifact.

## How it evaluates

The component only reports when both of these are true:

- the current build is a Development Build
- the environment is classified as CI

The current environment model is:

- not in batchmode -> `UnityEditor`
- in batchmode -> CI
- known provider variables only refine the CI label

So if Unity is in batchmode and no known provider matches, Release Guard still treats the run as CI and labels it `CI_Unknown`.

## Settings

| Setting | Effect | Typical use |
|---|---|---|
| `enabled` | Disables the component entirely for the active profile when turned off in that component's `componentToggles` entry. | Use only if a CI pipeline intentionally produces development builds. |

This component has no dedicated per-component settings object beyond profile-level disable control.

## Active-profile defaults

This component depends on the active profile only through whether that profile disables it.

In the seeded profile assets:

- `release.asset` leaves this component enabled
- `development.asset` also leaves this component enabled

That seeded setup is intentional. The profile can stay lenient locally while CI still catches a misrouted development build.

## Common pitfalls

- This is not redundant with `development_build`.
- Local batchmode runs count as CI, even when no vendor-specific environment variable is present.
- The currently edited profile in Project Settings does not control build-time selection in CI.

## Troubleshooting

### "Why did my local command-line build trigger this?"

Because local batchmode is treated as CI. That is expected behavior, not a bug in provider detection.

### "Why does the seeded `development.asset` still fail in CI?"

Because this component remains enabled there by default. The seeded profile is lenient for local workflows, not for CI release hygiene.

## Related docs

- [CI integration](../../guides/ci-integration.md)
- [Build profiles](../../guides/build-profiles.md)
- [Development build disabled](development-build.md)
