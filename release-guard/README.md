# Release Guard

[![Latest release](https://img.shields.io/github/v/release/SteveOberst/release-guard?include_prereleases&label=release&color=blue)](https://github.com/SteveOberst/release-guard/releases/latest)
[![Unity 2022.3+](https://img.shields.io/badge/unity-2022.3%2B-black)](https://unity.com/releases/editor/archive)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE.md)
[![CI](https://github.com/SteveOberst/release-guard/actions/workflows/validate.yml/badge.svg)](https://github.com/SteveOberst/release-guard/actions/workflows/validate.yml)
[![Release Please](https://github.com/SteveOberst/release-guard/actions/workflows/release-please.yml/badge.svg)](https://github.com/SteveOberst/release-guard/actions/workflows/release-please.yml)

Release Guard is a Unity package for release-build hygiene. It hooks into Unity's build pipeline,
runs a focused audit before the player is built, and stops the build when findings meet the
configured failure threshold.

The defaults are intentionally strict around settings that are usually mistakes in a shipped
build: development toggles, debugger and profiler hooks, broad preserve rules, and code you
explicitly mark as forbidden for release.

## Built-in checks

| Auditor | What it enforces |
|---|---|
| Scripting backend | IL2CPP required for release builds |
| Managed stripping | Minimum managed stripping level |
| Development build flag | No development builds shipped by accident |
| Script debugging | No managed debugger attachment in release builds |
| Profiler connection | No Autoconnect Profiler in release builds |
| Broad preserve rules | No assembly-wide `[Preserve]` or broad `link.xml` rules |
| `[ReleaseForbidden]` | Annotated code must not reach a release build |
| Android debuggable | No explicit `debuggable=true` in custom Android templates |
| WebGL exceptions | Advisory: full exception support modes in release builds (dismissible) |
| Engine code stripping | Advisory: suggests enabling Strip Engine Code (dismissible) |
| Stack trace log types | Advisory: flags full stack trace collection in release builds (dismissible) |
| Insecure HTTP | Advisory: cleartext HTTP allowed in release builds (dismissible) |
| Burst debug settings | Advisory, when Burst is installed: disabled optimizations or native debug mode (dismissible) |

Post-build, the transformer and post-processor pipelines run against the build output:

| Post-build pipeline item | What it does |
|---|---|
| Debug symbol sweep post-processor | Reports (or, opt-in, deletes) debug artifact folders Unity writes next to the player output (`*_BackUpThisFolder_ButDontShipItWithYourGame`, `*_BurstDebugInformation_DoNotShip`) and loose `.pdb` files |
| Build manifest post-processor | Opt-in: writes `release-guard-manifest.json` recording the configuration that produced the build |

All built-in auditors, post-processors, and transformers are individually toggleable. Custom
auditors, post-processors, transformers, and plugins are auto-discovered with `TypeCache`.
At runtime, `ReleaseGuardStartup` initializes a `ReleaseGuardContext` once per Editor-domain
load. The context owns settings, logging, loaded plugins, and typed registries.
Relevant Project Settings changes rebuild that runtime-only state.

The runtime code is split by responsibility: `Core/Runtime` owns startup/context/container
composition, `Core/Registries` owns typed auditor/post-processor/transformer registries,
`Core/Plugins` owns plugin loading/settings, and `Hooks` contains the thin Unity build callbacks.

## Install

### Git URL

Add the package to `Packages/manifest.json`:

```json
"org.researchy.release-guard": "https://github.com/SteveOberst/release-guard.git?path=/release-guard#<release-tag>"
```

The `?path=/release-guard` query is required -- the package lives in the `release-guard/`
subfolder of the repository.

### Release tarball

Each tagged release publishes `org.researchy.release-guard-<version>.tgz`. Download it from the
[releases page](https://github.com/SteveOberst/release-guard/releases) and install it with
**Window > Package Manager > + > Add package from tarball**.

## Quick start

1. Open **Edit > Project Settings > Release Guard**.
2. Keep the default hardening checks on unless your project has a specific documented reason not to.
3. Under **General**, set the failure threshold that matches your build policy.
4. Under **Auditors**, add asset exclusion globs only for paths you deliberately want to suppress.
5. Start a non-development build. Release Guard runs automatically before Unity continues.

Settings are organized into sub-pages that mirror the build pipeline stages:
**General -> Auditors -> Post-Processors -> Transformers -> Plugins**

You can also run the same audit manually from **Tools > Release Guard > Audit**.

## Configuring

Release Guard ships with strict defaults that are the right choice for most release builds.
They are not the right choice for every project. Read the
[Configuring guide](Documentation~/configuring.md) to understand what each setting does,
why it exists, the tradeoffs involved, and when you should deviate from the default.

At a minimum, review `General > Failure Threshold` and the individual auditor toggles
under `Auditors` before your first production build.

## Extending

The primary way to extend Release Guard is through a **plugin**. A plugin derives from
`ReleaseGuardPlugin` (`ReleaseGuard.Editor.Core.Plugins`) and contributes auditors,
post-processors, and transformers from a single named entry point. Plugins are visible in
the Release Guard window, can carry author metadata, and can expose their own settings page.

Register a plugin with `[InitializeOnLoad]` - this is the recommended pattern:

```csharp
using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Plugins;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;

[InitializeOnLoad]
internal static class MyPluginLoader
{
    static MyPluginLoader()
    {
        DI.Resolve<ReleaseGuardEnvironment>().RegisterPlugin(new MyPlugin());
    }
}

public sealed class MyPlugin : ReleaseGuardPlugin
{
    public override string PluginId    => "com.myteam.my-plugin";
    public override string DisplayName => "My Plugin";

    public override void Register(PluginRegistrationContext context)
    {
        context.ReleaseGuard.Registries.Auditors.Register(new MyAuditor());
    }
}
```

The three extensible base types are:

| Base class | Namespace | Override | When it runs |
|---|---|---|---|
| `ReleaseAuditor` | `ReleaseGuard.Editor.Core.Audit` | `Evaluate` | Pre-build, reports findings |
| `ReleaseTransformer` | `ReleaseGuard.Editor.Core.Transforming` | `Transform` | Post-build, before post-processors |
| `ReleasePostProcessor` | `ReleaseGuard.Editor.Core.PostProcessing` | `PostProcess` | Post-build, after transformers |

`ReleaseGuard.Editor` is `autoReferenced`, so no extra asmdef entry is needed to see the
base types from any Editor assembly in your project.

See the API guides for full details:
[plugins](Documentation~/api/plugins.md),
[custom auditors](Documentation~/api/custom-auditors.md),
[custom transformers](Documentation~/api/custom-transformers.md),
[custom post-processors](Documentation~/api/custom-post-processors.md), and
[plugin settings and custom renderers](Documentation~/api/settings.md).

## Documentation

Full documentation lives in [`Documentation~/index.md`](Documentation~/index.md).

- [Quick start](Documentation~/quickstart.md)
- [Configuring](Documentation~/configuring.md)
- [Development](Documentation~/development.md)

Guides:

- [Asset exclusions](Documentation~/guides/asset-exclusions.md)
- [Build profiles](Documentation~/guides/build-profiles.md)
- [`[ReleaseForbidden]`](Documentation~/guides/release-forbidden.md)
- [Audit window](Documentation~/guides/audit-window.md)

API:

- [Plugins](Documentation~/api/plugins.md)
- [Custom auditors](Documentation~/api/custom-auditors.md)
- [Custom post-processors](Documentation~/api/custom-post-processors.md)
- [Custom transformers](Documentation~/api/custom-transformers.md)
- [Plugin settings and custom renderers](Documentation~/api/settings.md)

Reference:

- [Built-in auditors](Documentation~/reference/built-in-auditors.md)
- [Built-in post-processors](Documentation~/reference/built-in-post-processors.md)
- [Built-in transformers](Documentation~/reference/built-in-transformers.md)
- [Settings](Documentation~/reference/settings.md)
- [Attributes](Documentation~/reference/attributes.md)

## Requirements

Unity 2022.3 or newer. No package dependencies.

## License

MIT. See [LICENSE.md](LICENSE.md).

