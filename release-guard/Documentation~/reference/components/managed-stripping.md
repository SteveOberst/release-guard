# Managed code stripping

| Field | Value |
|---|---|
| Id | `managed_stripping` |
| Phase | `pre-build` |
| Default posture | warning threshold check plus advisories |
| Blocks build | only if the active profile's failure threshold treats its warning as blocking |

## Overview

This component checks whether the current managed stripping level is strong enough for the active profile and emits additional advisories for especially weak settings. It exists because stripping is not only a size optimization; it also reduces how much managed code and metadata ship in the player.

## How it evaluates

The component reads the current stripping level for the current named build target.

It performs three separate evaluations:

1. a threshold check against the configured minimum
2. an advisory when the actual level is below `Medium`
3. an advisory when the actual level is exactly `Low`

It compares stripping levels by semantic aggressiveness rather than raw enum values:

`Disabled < Minimal < Low < Medium < High`

That matters because Unity added `Minimal` later with an enum value that does not match stripping aggressiveness order.

Advisory ids:

- `managed_stripping.below_medium`
- `managed_stripping.low_deprecated`

## Settings

| Setting | Effect | Typical use |
|---|---|---|
| `enabled` | When off, skips the component entirely for the active profile. | Use rarely; this removes both threshold checking and advisories. |
| `minLevel` | Minimum acceptable stripping level. `Disabled` turns off only the threshold check. | `Medium` is the seeded default. |
| advisory suppressions | Can suppress `managed_stripping.below_medium` and `managed_stripping.low_deprecated` through the checks window. | Use when a weaker stripping posture is intentional and documented. |

## Active-profile defaults

Behavior is driven by the active profile's settings.

In the seeded profile assets:

- `release.asset` keeps the component enabled with a minimum of `Medium`
- `development.asset` disables the component

## Common pitfalls

- Setting the minimum to `Disabled` does not mean weak stripping is now recommended. It only turns off the threshold check.
- This component is not purely blocking. It can still emit advisories even when the threshold check passes or is disabled.
- `Low` is treated separately because Unity marks it for future deprecation concerns, not just because it is weaker than `Medium`.

## Troubleshooting

### "Why am I seeing warnings even though I changed the minimum?"

You likely changed the threshold check but not the advisories. The below-`Medium` and `Low` advisories are separate outputs.

### "Why did the checks window and build disagree?"

The stripping-level logic is the same. A mismatch usually means a different profile was active, not that the component evaluated differently.

## Related docs

- [Pre-Build Checks window](../../guides/checks-window.md)
- [Build profiles](../../guides/build-profiles.md)
- [Built-in settings reference](../settings.md)
