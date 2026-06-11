# Build profiles

Release Guard resolves an effective configuration for each run before any auditor, transformer, or post-processor executes. Resolution combines the raw settings asset, the active Unity Build Profile, and the development-build exemption into a single immutable configuration object (`ReleaseGuardConfiguration`). This guide explains that resolution and how to use per-Build-Profile overrides.

## What gets resolved

For a single run the resolver computes:

- `Enabled` - whether Release Guard runs at all for this run.
- `IsDevelopmentBuild` - whether this is a development build.
- `BuildProfileName` - the active Unity Build Profile name (Unity 6 and newer), or `null` for classic platform settings.
- `FailureThreshold` - the severity at or above which a build is blocked.

## Resolution order

The resolver applies these steps in order:

1. Determine development state. During a build it reads the `BuildOptions.Development` flag from the build report. For a manual audit (no build report) it reads `EditorUserBuildSettings.development` from the Build Settings checkbox.
2. Read the active Build Profile name (see below).
3. Start from the global General settings: `enabled` and `failureThreshold`.
4. Apply a per-profile override if one matches the active profile name. The override replaces both `enabled` and `failureThreshold` for this run.
5. Apply the development-build exemption last: if this is a development build and `skipOnDevelopmentBuilds` is on, `enabled` is forced to `false`. This step wins over a profile override.

Because the development exemption is applied after the profile override, a profile that sets `enabled = true` will still be skipped on a development build while `skipOnDevelopmentBuilds` is on.

## How the active Build Profile is resolved

The active profile name is read through reflection on `UnityEditor.Build.Profile.BuildProfile.GetActiveBuildProfile()`. Reflection is used deliberately so the package still compiles on Editor versions that predate Build Profiles. On an Editor without Build Profiles, or when no profile is active, the name resolves to `null`, which means "classic platform settings" and no override is matched.

## Per-profile overrides

Build Profile overrides live on the General settings page in the `profileOverrides` list. Each entry has:

- `buildProfileName` - the exact name of the Build Profile this override applies to. Matching is by exact string equality against the active profile's name.
- `enabled` - whether Release Guard runs for that profile (default `true`).
- `failureThreshold` - the severity threshold for that profile (default `Error`).

An override is matched only when its `buildProfileName` exactly equals the active profile name. If no override matches (or there is no active profile), the global General settings apply unchanged.

### Example: a looser staging profile

Suppose you have two Unity Build Profiles, `Production` and `Staging`. You want production builds to fail on any `Error`, but staging builds to keep building and only fail on nothing (effectively report-only at the highest severity). Add one override:

- `buildProfileName`: `Staging`
- `enabled`: `true`
- `failureThreshold`: `Error`

Leave the global `failureThreshold` at `Error` for `Production`. Builds made with the `Production` profile (or with no profile) use the global settings; builds made with the `Staging` profile use the override.

### Example: disable the gate for one profile

To turn Release Guard off entirely for an internal `QA` profile while keeping it on everywhere else, add an override with `buildProfileName` = `QA` and `enabled` = `false`. Every Release Guard stage will skip and log "disabled in settings or by the active Build Profile override" for builds made with that profile.

## Interaction with the development-build exemption

Remember the order: the development exemption is applied after profile resolution. If you want a profile to run even for development builds, you must also turn off the global `skipOnDevelopmentBuilds` - a profile override cannot re-enable a run that the development exemption has disabled.

See also [the audit window](audit-window.md) for how a manual audit resolves the same configuration (using the Build Settings development checkbox in place of a build report).
