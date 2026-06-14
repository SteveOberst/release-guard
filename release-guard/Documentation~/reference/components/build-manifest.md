# Build manifest

| Field | Value |
|---|---|
| Id | `build_manifest` |
| Phase | `post-build` |
| Default posture | opt-in artifact writer |
| Blocks build | no |
| Priority | `100` |

## Overview

This component writes `release-guard-manifest.json` into the resolved build output folder after a successful build. It exists as a CI and traceability artifact so downstream tooling can validate or archive the exact Release Guard configuration that produced a build.

## How it evaluates

After a successful build, the component writes a manifest containing:

- manifest version
- Release Guard package version
- Unity version
- build target
- product name
- output file name
- failure threshold
- registered components and their subscribed phases
- disabled component ids
- disabled plugin ids
- suppressed advisory ids
- build GUID, timestamps, and total build size when `BuildReport` data is available

It deliberately does not record:

- absolute file system paths
- source-control metadata

Its priority is `100`, and lower priority values run earlier while higher values run later. That places the built-in writer after default-priority post-build work so it describes the output folder state after normal built-in post-build processing has already happened.

## Settings

| Setting | Effect | Typical use |
|---|---|---|
| `enabled` | Enables manifest writing for the active profile. | Turn on when CI or artifact handling wants provenance data. |

## Active-profile defaults

Behavior depends on the active profile's settings.

In the seeded profile assets:

- `release.asset` leaves manifest writing off by default
- `development.asset` also leaves manifest writing off by default

## Common pitfalls

- This is not a player-facing file. It belongs in CI artifacts or internal archives.
- The component does not record commit metadata by itself.
- This is post-build behavior, so the checks window will never show it.

## Troubleshooting

### "Why isn't the manifest there?"

The usual causes are:

- the build did not succeed, so post-build handlers never ran
- the component's `enabled` toggle is off in the active profile
- the active profile disabled the component

### "Can we add our own metadata?"

Yes. Register a custom post-build component with:

- a lower priority than `100` if you want it to run before the built-in writer
- a higher priority than `100` if you want it to run after the built-in writer and amend the output afterward

## Related docs

- [CI integration](../../guides/ci-integration.md)
- [Debug symbol sweep](debug-symbol-sweep.md)
- [Build profiles](../../guides/build-profiles.md)
