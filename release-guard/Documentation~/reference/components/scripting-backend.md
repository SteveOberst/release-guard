# Scripting backend (IL2CPP)

| Field | Value |
|---|---|
| Id | `scripting_backend` |
| Phase | `pre-build` |
| Default posture | blocking check |
| Blocks build | yes |
| Priority | `-10` |

## Overview

This component requires the current scripting backend to be `IL2CPP`. It exists to catch one of the highest-impact release hardening mistakes early: shipping a Mono player with managed assemblies that decompile cleanly back into readable C#.

## How it evaluates

The component resolves the current `NamedBuildTarget` from the build target group and reads the scripting backend through `PlayerSettings.GetScriptingBackend(...)`.

If the active profile requires IL2CPP and the current backend is not `IL2CPP`, it reports an error.

Its priority is `-10`, so it runs earlier than default-priority pre-build checks. That keeps the main backend decision near the front of the run.

## Settings

| Setting | Effect | Typical use |
|---|---|---|
| `enabled` | When off, skips the component entirely for the active profile. | Turn off only for profiles that intentionally allow Mono. |

## Active-profile defaults

This component runs or skips based on the active profile's settings.

In the seeded profile assets:

- `release.asset` leaves this component enabled
- `development.asset` disables this component

## Common pitfalls

- IL2CPP is not a full anti-tamper system. It just raises the reverse-engineering cost substantially compared with Mono.
- Changing the currently edited profile in Project Settings does not force a build to use that profile.
- This component is toggle-only. Its behavior is controlled by whether its component entry is enabled for the active profile.

## Troubleshooting

### "Why did this fail during a manual checks-window run?"

The checks window still resolves the scripting backend from current editor/player settings. It is not a no-op just because there is no `BuildReport`.

### "Why did CI use a different outcome than my local check?"

The usual cause is profile selection, not backend detection. Real builds use the first matching profile. The checks window uses the currently edited profile.

## Related docs

- [Build profiles](../../guides/build-profiles.md)
- [Configuring](../../configuring.md)
- [Built-in components overview](../components.md)
