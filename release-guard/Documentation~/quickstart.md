# Quickstart

This walks you from installing Release Guard to seeing it block a release build.

## 1. Install

Release Guard is a UPM package (`org.researchy.release-guard`, requires Unity `2022.3` or newer). It has no runtime dependencies.

### From a Git URL

Open `Window > Package Manager`, choose `+ > Add package from git URL...`, and enter the repository URL with the `?path` query pointing at the package folder inside the repo:

```
https://github.com/SteveOberst/release-guard.git?path=/release-guard
```

The package lives in the `release-guard` subdirectory of the repository, so the `?path=/release-guard` suffix is required.

### From a local checkout

If you have the repository cloned next to your project, add it as a local `file:` dependency in your project's `Packages/manifest.json`. This is exactly how the bundled dev host references it:

```json
{
  "dependencies": {
    "org.researchy.release-guard": "file:../../release-guard"
  }
}
```

Adjust the relative path so it points at the folder that contains `package.json`.

## 2. Open Project Settings

Go to `Edit > Project Settings > Release Guard`. Opening the settings creates the configuration asset at `Assets/ReleaseGuard/ReleaseGuardSettings.asset` if it does not exist yet. The settings are split into five pages: General, Auditors, Post-Processors, Transformers, and Plugins.

## 3. Review the defaults

On the General page:

- `enabled` is on. This is the master switch.
- `skipOnDevelopmentBuilds` is on. Development builds are exempt - only non-development (release) builds are gated.
- `failureThreshold` is `Error`. A build is blocked when any finding is at severity `Error` or above.

On the Auditors page the built-in rules are on by default, including require IL2CPP, forbid development build, forbid script debugging, forbid profiler connection, a minimum managed stripping level of `Medium`, forbid broad `[Preserve]`, and the `[ReleaseForbidden]` scan. Several advisory checks (engine code stripping, stack trace type, insecure HTTP, Burst debug) are dismissible and do not block under the default threshold.

Leave the defaults as a starting point and tighten later.

## 4. Run a manual audit

Open the audit window from `Tools > Release Guard > Audit` (or click `Open Audit Window` on the settings overview page). Click `Run Audit`. The window lists every discovered auditor and every finding grouped by severity, each with a fix hint and an asset ping where applicable. A manual audit never blocks anything - it just reports. See the [audit window guide](guides/audit-window.md) for filtering and advisory dismissal.

## 5. Trigger the build gate

The same checks run automatically before every non-development build. To see the gate in action, make a release build (`File > Build Settings...`, ensure `Development Build` is off, then `Build`). If any finding is at or above the failure threshold, the build aborts with a `BuildFailedException` and a message like:

```
[ReleaseGuard] Build blocked: N issue(s) at or above Error. See the Console (or the Release Guard window) for details and fixes.
```

Fix the reported issues (or adjust settings) and rebuild. A development build is skipped entirely while `skipOnDevelopmentBuilds` is on.

## 6. Commit the settings asset

Commit `Assets/ReleaseGuard/ReleaseGuardSettings.asset` (and its `.meta`) to version control. The asset is the single source of truth for the gate, so committing it gives every teammate and your CI pipeline the same rules and the same failure threshold.

## Next steps

- [Asset exclusions](guides/asset-exclusions.md) to silence findings for third-party or generated assets.
- [Build profiles](guides/build-profiles.md) to use a looser threshold for a staging profile.
- [Release-forbidden code](guides/release-forbidden.md) to mark debug-only code that must never ship.
