# Engine code stripping

| Field | Value |
|---|---|
| Id | `strip_engine_code` |
| Phase | `pre-build` |
| Default posture | advisory |
| Blocks build | no by itself |

## Overview

This component reports when `PlayerSettings.stripEngineCode` is disabled. It exists to surface a setting that can reduce build size and shrink the amount of Unity engine surface shipped with the player.

## How it evaluates

The component checks the current `PlayerSettings.stripEngineCode` value.

If engine code stripping is off, it emits the advisory:

- `strip_engine_code.disabled`

## Settings

| Setting | Effect | Typical use |
|---|---|---|
| `enabled` | When off, skips the component entirely for the active profile. | Use when the setting is irrelevant to that profile. |
| advisory suppressions | Can suppress `strip_engine_code.disabled` through the checks window. | Use when disabling engine stripping is intentional and documented. |

This component has no dedicated settings object.

## Active-profile defaults

The component's behavior depends on the active profile's disable and advisory-suppression settings.

In the seeded profile assets:

- `release.asset` leaves this component enabled
- `development.asset` leaves this component enabled

## Common pitfalls

- This is advisory-only on purpose. Enabling engine stripping is not universally safe for every project.
- Passing this check does not mean overall code stripping is configured well. That is a separate concern covered by `managed_stripping`.

## Troubleshooting

### "Why isn't this blocking the build?"

Because the component is meant to force an explicit decision, not to assume every project can enable engine stripping without testing.

### "Why did the seeded development profile still keep this on?"

Because seeded profiles are just starting assets. This advisory stays enabled there by default so the stripping posture is still visible unless you choose otherwise.

## Related docs

- [Managed code stripping](managed-stripping.md)
- [Pre-Build Checks window](../../guides/checks-window.md)
- [Build profiles](../../guides/build-profiles.md)
