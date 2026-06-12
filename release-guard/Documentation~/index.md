# Release Guard

Release Guard is a build-time security and hardening auditor for the Unity Editor. It inspects your project and player settings before a release build, blocks the build when a finding is at or above a configurable severity threshold, and runs optional artifact transformers and output post-processors after a successful build. The auditor, transformer, and post-processor pipelines are all extensible, and the entire built-in rule set ships as ordinary auditor classes you can disable individually.

The package id is `org.researchy.release-guard` (`org.researchy` is the author's UPM namespace) and it targets Unity `2022.3` and newer. It has no runtime package dependencies.

## How it works

Release Guard hooks into the standard Unity build callbacks. A single Editor environment is created once per domain load by a `[InitializeOnLoad]` startup type (`ReleaseGuardStartup`), which builds the registries of auditors, post-processors, transformers, and plugins from the current settings asset and registers the environment in the package's dependency container. The same environment is reused by the build hooks and the audit window until the next domain reload (script recompile, settings reload, or entering play mode). The container is disposed on `beforeAssemblyReload` so the lifecycle is exactly once per domain load.

For every build the pipeline runs in this order:

1. Pre-build audit (the gate). `ReleaseBuildPreprocessor` implements `IPreprocessBuildWithReport` at `callbackOrder = 0`. It resolves the effective configuration for the build, runs every registered auditor, logs the report, and - if any finding is at or above the failure threshold - throws `BuildFailedException` to stop the build before output is written. If a single auditor throws, it is caught, logged, and turned into a Warning so it never aborts the run by itself.
2. Post-build transformers. After a successful build, `ReleaseTransformRunner` implements `IPostprocessBuildWithReport` at `callbackOrder = 0`. Transformers operate on the raw build artifacts (IL manipulation, binary patching, obfuscation). No transformers ship built in, so this stage does no artifact work until you register one. A transformer failure is logged but never fails the build, which has already succeeded.
3. Post-build post-processors. `ReleasePostProcessRunner` implements `IPostprocessBuildWithReport` at `callbackOrder = int.MaxValue`, so it runs last - after Unity's own post-processing and after the transformers. Post-processors therefore see the final, transformed output folder. A post-processor failure is logged but never fails the build.

Before each stage the effective configuration is resolved (see [build profiles](guides/build-profiles.md)). When Release Guard is disabled - by the master switch, by a Build Profile override, or by the development-build exemption - every real build stage skips and logs the reason.

A manual audit from the [audit window](guides/audit-window.md) runs only the auditor stage (step 1) without a build, so it never gates anything and never runs transformers or post-processors. It is informational and can still be used while a real build would be skipped by the effective configuration.

## Install

Release Guard requires Unity `2022.3` or newer and has no runtime package dependencies.

**From a git URL** - open `Window > Package Manager`, choose `+ > Add package from git URL`, and enter:

```
https://github.com/SteveOberst/release-guard.git?path=/release-guard
```

The `?path=/release-guard` suffix is required; the package lives in a subdirectory of the repository. Pin to a specific release by appending `#<tag>`, e.g. `...release-guard#v1.0.0`.

**From a release tarball** - download `org.researchy.release-guard-<version>.tgz` from the [releases page](https://github.com/SteveOberst/release-guard/releases) and install via `+ > Add package from tarball`.

**From OpenUPM** - the repository includes `openupm-package.yml`. If you use OpenUPM, install with:

```bash
openupm add org.researchy.release-guard
```

**From a local checkout** - add a `file:` entry to `Packages/manifest.json` pointing at the folder that contains `package.json`:

```json
"org.researchy.release-guard": "file:../../release-guard"
```

## Quick start

1. Open `Edit > Project Settings > Release Guard`. The settings asset is created at `Assets/ReleaseGuard/ReleaseGuardSettings.asset` on first use.
2. Review the defaults on the **General** and **Auditors** pages. The defaults are strict; read the [Configuring guide](configuring.md) before disabling anything.
3. Run a manual audit from `Tools > Release Guard > Audit` to see which checks fire against your current project.
4. Make a non-development release build. Release Guard gates it automatically. If anything is at or above the failure threshold the build aborts with `[ReleaseGuard] Build blocked: N issue(s)...`.
5. Commit `Assets/ReleaseGuard/ReleaseGuardSettings.asset` and its `.meta` file so the whole team and CI share the same gate.

See [Quickstart](quickstart.md) for the full walkthrough.

## Settings location

All configuration lives in a single `ReleaseGuardSettings` asset at `Assets/ReleaseGuard/ReleaseGuardSettings.asset`. The asset is created on first use. Edit it through `Edit > Project Settings > Release Guard`, which exposes the General, Auditors, Post-Processors, Transformers, and Plugins pages. Commit the asset to version control so the whole team and your CI share the same gate.

The defaults are strict and appropriate for most release builds, but they are not one-size-fits-all. See the [Configuring guide](configuring.md) for a guided tour of every setting, including why each default is what it is and when to deviate from it.

## Compatibility and API surface

| Area | Support level |
| --- | --- |
| Unity Editor | Package minimum is Unity `2022.3`. |
| Unity 6 Build Profiles | Supported opportunistically through reflection. On older Editors, Build Profile resolution returns `null` and global settings apply. |
| CI coverage in this repo | EditMode tests run against the `UnityDevHost` project on Unity `6000.4.8f1` in GitHub Actions. The package still declares `2022.3` as its minimum supported Editor. |
| Runtime API | `ReleaseGuard.ReleaseForbidden` and `ReleaseGuard.ReleaseIssueSeverity` are the runtime-facing API. |
| Editor extension API | `ReleaseAuditor`, `ReleasePostProcessor`, `ReleaseTransformer`, `ReleaseGuardPlugin`, plugin settings, registries, and documented settings components/readers are intended extension points. Types under `Core/Runtime`, `Core/DI`, and low-level renderer internals may change more freely unless documented in the API pages. |

## Documentation

| Document | What it covers |
| --- | --- |
| [Quickstart](quickstart.md) | Install, review defaults, run a manual audit, trigger the gate, commit the asset. |
| [Configuring](configuring.md) | Guided tour of every setting: what it does, why it exists, tradeoffs, and recommended choices. |
| [Development](development.md) | Local package layout, dev host setup, tests, and adding built-in auditors and post-processors. |
| [Changelog](../../CHANGELOG.md) | Version history, breaking changes, and migration notes. |
| [Guide: Asset exclusions](guides/asset-exclusions.md) | The gitignore-style pattern engine for excluding asset paths from findings. |
| [Guide: Build profiles](guides/build-profiles.md) | Configuration resolution and per-Build-Profile overrides. |
| [Guide: Release-forbidden code](guides/release-forbidden.md) | The `[ReleaseForbidden]` attribute and the auditor that scans for it. |
| [Guide: Audit window](guides/audit-window.md) | Running the on-demand audit, filtering, and dismissing advisories. |
| [Guide: First custom auditor plugin](guides/custom-auditor-plugin.md) | End-to-end plugin registration, settings, custom auditor, and verification. |
| [Guide: First custom post-processor plugin](guides/custom-post-processor-plugin.md) | End-to-end plugin registration, settings, post-processor writing, and verification. |
| [Guide: CI integration](guides/ci-integration.md) | Batchmode build shape, failure thresholds, artifacts, and troubleshooting. |
| [API: Plugins](api/plugins.md) | Bundling contributions behind a `ReleaseGuardPlugin` - the primary extension point. |
| [API: Custom auditors](api/custom-auditors.md) | Writing your own `ReleaseAuditor`. |
| [API: Custom post-processors](api/custom-post-processors.md) | Writing your own `ReleasePostProcessor`. |
| [API: Custom transformers](api/custom-transformers.md) | Writing your own `ReleaseTransformer`. |
| [API: Plugin settings and custom readers](api/settings.md) | Declaring plugin settings, custom types, and custom field readers. |
| [Reference: Built-in auditors](reference/built-in-auditors.md) | Every auditor that ships with the package. |
| [Reference: Built-in post-processors](reference/built-in-post-processors.md) | Every post-processor that ships with the package. |
| [Reference: Built-in transformers](reference/built-in-transformers.md) | Transformer base type and shipped transformers. |
| [Reference: Settings](reference/settings.md) | Every settings field. |
| [Reference: Attributes](reference/attributes.md) | Public attributes such as `[ReleaseForbidden]`. |
