# Release-forbidden members

| Field | Value |
|---|---|
| Id | `release_forbidden` |
| Phase | `pre-build` |
| Default posture | severity comes from each attribute |
| Blocks build | if any discovered attribute reports at or above the active profile's failure threshold |

## Overview

This component scans player-shipping assemblies for types and members annotated with `[ReleaseForbidden]`. It is the package's explicit "this code must never ship" mechanism for things like cheat hooks, admin commands, debug menus, test scaffolding, and dev-only backdoors.

## How it evaluates

The component asks Unity's compilation pipeline for player assemblies and inspects only loaded assemblies whose names are in that shipping set.

It scans:

- type declarations
- methods
- fields
- properties

It skips:

- editor-only assemblies
- assemblies listed in the component's `excludedAssemblies` settings

Each finding uses the exact severity and optional reason carried by the attribute instance.

## Settings

| Setting | Effect | Typical use |
|---|---|---|
| `enabled` | When off, skips the component entirely for the active profile. | Use rarely; this removes the explicit ship-gate mechanism. |
| `excludedAssemblies` | Excludes named assemblies from the scan. | Use when an assembly is intentionally outside the policy. |

## Active-profile defaults

Behavior depends on the active profile's settings.

In the seeded profile assets:

- `release.asset` leaves this component enabled
- `development.asset` disables this component

## Common pitfalls

- The attribute does not remove code from the build on its own.
- This component is build-time enforcement, not compile-time exclusion.
- The severity is attribute-driven. The component does not hardcode every finding as `Error`.

## Troubleshooting

### "Why is the code still compiling even though it is forbidden?"

Because `[ReleaseForbidden]` is a build gate, not a compiler feature. If you also want compile-time exclusion, wrap the code in a preprocessor guard such as:

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
...
#endif
```

### "Why didn't a marked member trigger?"

The most common causes are that the containing assembly is editor-only, excluded by name, or not part of the player-shipping assembly set for that build.

## Related docs

- [Release-forbidden code](../../guides/release-forbidden.md)
- [Asset exclusions](../../guides/asset-exclusions.md)
- [Build profiles](../../build-profiles.md)
