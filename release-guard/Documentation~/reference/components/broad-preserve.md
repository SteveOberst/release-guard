# Broad preserve rules

| Field | Value |
|---|---|
| Id | `broad_preserve` |
| Phase | `pre-build` |
| Default posture | blocking check |
| Blocks build | yes |

## Overview

This component flags stripping rules that preserve too much code or metadata. It exists because broad preserve rules quietly undo Unity's stripping work and often remain in a project long after the original reflection or debugging problem is gone.

## How it evaluates

The component checks two sources:

- assembly-level `[Preserve]`
- `link.xml` rules under `Assets/`

It reports findings for patterns that preserve too broadly, including:

- assembly-level `[Preserve]`
- `link.xml` assembly rules with `preserve="all"`
- empty assembly rules in `link.xml` that preserve whole assemblies
- wildcard type patterns in `link.xml`

Important limits:

- only player-shipping assemblies are scanned
- editor-only assemblies are skipped
- malformed `link.xml` files are ignored rather than guessed at

## Settings

| Setting | Effect | Typical use |
|---|---|---|
| `enabled` | When off, skips the component entirely for the active profile. | Use rarely. |
| `excludedAssetPaths` | Can suppress `link.xml` findings by asset path. | Use when a third-party file is intentionally broad and cannot be changed. |

## Active-profile defaults

Behavior depends on the active profile's settings.

In the seeded profile assets:

- `release.asset` leaves this component enabled
- `development.asset` disables this component

## Common pitfalls

- This is not a blanket ban on `Preserve`. The component targets preserve rules that are broader than they need to be.
- Asset exclusions help only for path-backed findings such as `link.xml`, not for assembly-level metadata with no asset path.
- Disabling the component is much broader than documenting one intentional preserve rule.

## Troubleshooting

### "What should replace a broad preserve rule?"

Prefer targeted `[Preserve]` on the exact members reflection needs or explicit `link.xml` entries for specific types or members.

### "Why didn't my exclusion silence everything?"

Because assembly-level findings do not come from an asset path and cannot be filtered through asset exclusions.

## Related docs

- [Asset exclusions](../../guides/asset-exclusions.md)
- [Managed code stripping](managed-stripping.md)
- [Release-forbidden members](release-forbidden.md)
