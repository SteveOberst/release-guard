# Quickstart

This walks you from installing Release Guard to seeing it block a release build.

## 1. Install

Release Guard is a UPM package (`org.researchy.release-guard`, requires Unity `2022.3` or newer). It has no runtime dependencies.

> **Two assemblies.** The package ships two assemblies. `ReleaseGuard.Runtime` is
> `autoReferenced: true` — it contains only `[ReleaseForbidden]` and `ReleaseIssueSeverity`,
> which gameplay code can apply without pulling in Editor-only types. `ReleaseGuard.Editor`
> contains the pipeline, settings, and extension API. You never reference it directly from
> gameplay assemblies. For a plugin's `[InitializeOnLoad]` loader, add an explicit asmdef
> reference to `ReleaseGuard.Editor` so Unity's assembly-initialization order is deterministic.
> For just writing auditors in a plain Editor assembly, auto-reference is sufficient.

### From a Git URL

Open `Window > Package Manager`, choose `+ > Add package from git URL...`, and enter the repository URL with the `?path` query pointing at the package folder inside the repo:

```
https://github.com/SteveOberst/release-guard.git?path=/release-guard
```

The package lives in the `release-guard` subdirectory of the repository, so the `?path=/release-guard` suffix is required.

### From OpenUPM

If you use OpenUPM, install the package with:

```bash
openupm add org.researchy.release-guard
```

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

After Unity reimports the package, confirm it compiled correctly: open `Window > Package Manager`, find **Release Guard** in the list, and ensure there are no errors in the Console. If `Edit > Project Settings > Release Guard` does not appear, check the Console for compilation errors in the `ReleaseGuard.Editor` assembly.

Go to `Edit > Project Settings > Release Guard`. Opening the settings creates the configuration asset at `Assets/ReleaseGuard/ReleaseGuardSettings.asset` if it does not exist yet. The settings are split into five pages: General, Auditors, Post-Processors, Transformers, and Plugins.

## 3. Review the defaults

On the General page:

- `enabled` is on. This is the master switch.
- `skipOnDevelopmentBuilds` is on. Development builds are exempt - only non-development (release) builds are gated.
- `failureThreshold` is `Error`. A build is blocked when any finding is at severity `Error` or above.

On the Auditors page the built-in rules are on by default, including require IL2CPP, forbid development build, forbid script debugging, forbid profiler connection, a minimum managed stripping level of `Medium`, forbid broad `[Preserve]`, and the `[ReleaseForbidden]` scan. Several advisory checks (engine code stripping, stack trace type, insecure HTTP, Burst debug) are dismissible and do not block under the default threshold.

Leave the defaults as a starting point and tighten later.

## 4. Understand first audit output

A fresh Unity project commonly reports a mix of hard errors, warnings, and advisories:

- `Error` findings block release builds with the default threshold. Common examples are Mono
  scripting backend, Development Build, Script Debugging, Autoconnect Profiler, broad preserve
  rules, or `[ReleaseForbidden]` code.
- `Warning` findings are visible but do not block while `failureThreshold` is `Error`. Managed
  stripping below the configured minimum is a warning by default.
- Advisory findings are dismissible best-practice prompts. They are useful review items, but
  they do not block at the default threshold unless you lower the threshold to their severity.

## 5. Run a manual audit

Open the audit window from `Tools > Release Guard > Audit` (or click `Open Audit Window` on the settings overview page). Click `Run Audit`. The window lists every registered auditor and every finding grouped by severity, each with a fix hint and an asset ping where applicable. A manual audit never blocks anything - it just reports. See the [audit window guide](guides/audit-window.md) for filtering and advisory dismissal.

## 6. Trigger the build gate

The same checks run automatically before every non-development build. To see the gate in action, make a release build (`File > Build Settings...`, ensure `Development Build` is off, then `Build`). If any finding is at or above the failure threshold, the build aborts with a `BuildFailedException` and a message like:

```
[ReleaseGuard] Build blocked: N issue(s) at or above Error. See the Console (or the Release Guard window) for details and fixes.
```

The per-finding output in the Console looks like:

```
[ReleaseGuard] [Error] scripting_backend — Scripting backend is Mono. IL2CPP is required for release builds.
  Fix: Switch to IL2CPP in Edit > Project Settings > Player > Other Settings > Scripting Backend.
[ReleaseGuard] [Error] development_build — Development Build flag is set.
  Fix: Uncheck Development Build in File > Build Settings.
[ReleaseGuard] Build blocked: 2 issue(s) at or above Error. See the Console (or the Release Guard window) for details and fixes.
```

Each line names the auditor id, the finding message, and a fix hint. Fix the reported issues (or adjust settings) and rebuild. A development build is skipped entirely while `skipOnDevelopmentBuilds` is on.

## 7. Commit the settings asset

Commit `Assets/ReleaseGuard/ReleaseGuardSettings.asset` (and its `.meta`) to version control. The asset is the single source of truth for the gate, so committing it gives every teammate and your CI pipeline the same rules and the same failure threshold.

## Next steps

- [Asset exclusions](guides/asset-exclusions.md) to silence findings for third-party or generated assets.
- [Build profiles](guides/build-profiles.md) to tune thresholds or disable the gate per Build Profile.
- [Release-forbidden code](guides/release-forbidden.md) to mark debug-only code that must never ship.
- [First custom auditor plugin](guides/custom-auditor-plugin.md) to add a project-specific rule.
- [CI integration](guides/ci-integration.md) to run the gate from your build pipeline.
