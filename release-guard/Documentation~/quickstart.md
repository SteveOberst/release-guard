# Quickstart

This walkthrough assumes a clean install and treats the current implementation as the source of truth.

## 1. Install the package

Git URL:

```text
https://github.com/SteveOberst/release-guard.git?path=/release-guard
```

Open `Window > Package Manager`, choose `+ > Add package from git URL`, and paste the URL.

The `?path=/release-guard` suffix is required because the Unity package lives in a subdirectory of the repository.

## 2. Know the import side effects

Release Guard creates its registry and default profile assets during editor startup, not only after you intentionally start configuring it.

In practice, soon after import you should expect these files to appear:

- `Assets/ReleaseGuard/registry.asset`
- `Assets/ReleaseGuard/Profiles/release.asset`
- `Assets/ReleaseGuard/Profiles/development.asset`

Those are the real settings assets now. Release Guard no longer relies on one global package settings asset for all build modes.

Open `Edit > Project Settings > Release Guard` to inspect and edit them.

If those assets do not appear, check this first:

- the project compiles without editor errors
- Unity has finished the domain reload after import
- the Console does not show a Release Guard initialization exception
- `Assets/ReleaseGuard/` is not blocked by a VCS or file-permission issue

Release Guard seeds those assets during editor initialization. If the editor domain never initializes cleanly, the assets will not be created.

## 3. Understand the default profiles

Release Guard seeds two built-in profiles:

- `Release`  
  Matches non-development builds.

- `Development`  
  Matches development builds.

The Development profile deliberately disables the stricter release-only checks so local dev builds stay usable, but it does not collapse to "almost nothing".

These built-in components stay enabled by default in the seeded Development profile:

| Component id | Why it stays enabled |
|---|---|
| `ci_development_build` | A CI job should not accidentally ship a development build. |
| `stack_trace_type` | Development builds still need a deliberate stack trace policy. |
| `strip_engine_code` | Engine-code stripping should still match the intended build shape. |

These built-in components are disabled by default in the seeded Development profile:

| Component id |
|---|
| `scripting_backend` |
| `managed_stripping` |
| `development_build` |
| `script_debugging` |
| `profiler_connection` |
| `broad_preserve` |
| `release_forbidden` |
| `android_debuggable` |
| `webgl_exception_support` |
| `insecure_http` |
| `burst_debug` |

See [Build profiles](build-profiles.md) for the exact selection rules.

## 4. Run the checks window

Open `Tools > Release Guard > Pre-Build Checks` and click `Run Checks`.

This dispatches the `pre-build` event without an active `BuildReport`. That means:

- pre-build component subscriptions run
- build subscriptions do not run
- post-build subscriptions do not run

The window is useful for checking player settings before you start a build, but it is not a full simulation of the entire pipeline.

## 5. Make a real build

Build your project normally.

On a successful build attempt, Release Guard runs in this order:

1. `pre-build` event  
   Blocking. If any issue is at or above the selected failure threshold, the build is aborted with `BuildFailedException`.
2. `build` event  
   Runs only after a successful pre-build phase.
3. `post-build` event  
   Runs last, at `callbackOrder = int.MaxValue`.

Failures in `build` and `post-build` handlers are logged, not rethrown.

## 6. Commit the assets

Commit the registry and profile assets:

- `Assets/ReleaseGuard/registry.asset`
- `Assets/ReleaseGuard/registry.asset.meta`
- `Assets/ReleaseGuard/Profiles/*.asset`
- `Assets/ReleaseGuard/Profiles/*.asset.meta`

Without those files, teammates and CI will not evaluate builds the same way you do locally.

This is not optional cleanup. Those files are part of the package's intended operating model.

## 7. Review the built-ins before disabling anything

Start with:

- [Built-in components overview](reference/components.md)
- [Configuring](configuring.md)

The strict-looking defaults are intentional. Most of the package's value comes from blocking easy-to-miss release mistakes early.

## 8. What a first rollout usually looks like

For a typical team, the first useful loop is:

1. import the package
2. commit the new `Assets/ReleaseGuard/` assets
3. run `Pre-Build Checks`
4. fix obvious player-setting problems
5. make one real release build
6. decide whether to enable `build_manifest`
7. decide whether `debug_symbol_sweep` should stay report-only or become destructive in CI

That sequence gets you from installation to a stable baseline without inventing custom profiles or components too early.


