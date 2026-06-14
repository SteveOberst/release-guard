# Development build disabled

| Field | Value |
|---|---|
| Id | `development_build` |
| Phase | `pre-build` |
| Default posture | blocking check |
| Blocks build | yes |

## Overview

This component reports when the current build is a Development Build. It exists to stop shipping a player with extra debugging and diagnostics surface that is appropriate during development and usually wrong for a release artifact.

## How it evaluates

The component checks `context.IsDevelopmentBuild`.

That value comes from:

- `BuildReport.summary.options` during a real build
- `EditorUserBuildSettings.development` during a checks-window run

If the active profile forbids development builds and the flag is on, the component reports an error.

## Settings

| Setting | Effect | Typical use |
|---|---|---|
| `enabled` | When off, skips the component entirely for the active profile. | Turn off for profiles that intentionally allow development builds. |

## Active-profile defaults

The component's behavior comes from the active profile.

In the seeded profile assets:

- `release.asset` leaves this component enabled
- `development.asset` disables this component

## Common pitfalls

- This component is separate from `ci_development_build`. One asks whether development builds are allowed for the active profile; the other asks whether CI is producing one at all.
- A Development Build can influence other checks, but this component only answers the narrow question "is the development flag on?"
- Editing the seeded `development.asset` profile in Project Settings does not make all builds use it.

## Troubleshooting

### "Why did this fire from the checks window?"

Because the checks window uses the current Build Settings development flag when there is no `BuildReport`.

### "Why didn't this fire in a build I expected to block?"

The active profile may have disabled the component.

## Related docs

- [Build profiles](../../guides/build-profiles.md)
- [CI integration](../../guides/ci-integration.md)
- [Development build in CI](ci-development-build.md)
