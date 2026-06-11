# Release Guard

[![Latest release](https://img.shields.io/github/v/release/SteveOberst/release-guard?label=release&color=blue)](https://github.com/SteveOberst/release-guard/releases/latest)
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

Post-build, the transformer pipeline runs against the build output:

| Transformer | What it does |
|---|---|
| Debug symbol sweep | Reports (or, opt-in, deletes) `DoNotShip` folders and loose `.pdb` files in the output folder |
| Build manifest | Opt-in: writes `release-guard-manifest.json` recording the configuration that produced the build |

All built-in auditors and transformers are individually toggleable, and custom auditors,
transformers, and plugins are auto-discovered with `TypeCache`.

## Install

### Git URL

Add the package to `Packages/manifest.json`:

```json
"org.researchy.release-guard": "https://github.com/SteveOberst/release-guard.git?path=/release-guard#<release-tag>"
```

The `?path=/release-guard` query is required -- the package lives in the `release-guard/`
subfolder of this repository.

### Release tarball

Each tagged release publishes `org.researchy.release-guard-<version>.tgz`. Download it from the
[releases page](https://github.com/SteveOberst/release-guard/releases) and install it with
**Window > Package Manager > + > Add package from tarball**.

## Quick start

1. Open **Edit > Project Settings > Release Guard**.
2. Keep the default hardening checks on unless your project has a specific documented reason not to.
3. Set the failure threshold that matches your build policy.
4. Add asset exclusion globs only for paths you deliberately want to suppress.
5. Start a non-development build. Release Guard runs automatically before Unity continues.

You can also run the same audit manually from **Tools > Release Guard > Audit**.

## Extending

### Single auditor / post-processor / transformer

Derive from `ReleaseAuditor`, `ReleasePostProcessor`, or `ReleaseTransformer` in any Editor
assembly and enable the corresponding **autoDiscover** setting — or register via a plugin (see below).

```csharp
public sealed class MyAuditor : ReleaseAuditor
{
    public override string Id => "myteam.my_auditor";
    public override int Priority => 10;

    public override void Evaluate(ReleaseAuditContext context)
    {
        if (SomeConditionFails())
            context.Report(ReleaseIssueSeverity.Error, "Reason", "Assets/OffendingFile.cs");
    }
}
```

### Plugin (multiple contributions from one entry point)

Register from an `[InitializeOnLoad]` static constructor. Assembly dependency ordering guarantees
Release Guard initializes before your code runs.

```csharp
using ReleaseGuard.Editor.Core.DI;
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
```

`RegisterPlugin` returns `false` (without throwing) if the plugin is already registered — safe to
call even when **autoDiscoverPlugins** is also enabled.

### Custom settings field renderer

Plugins can register `ITypeRenderer` implementations on their `SettingsRenderer` to control how
custom field types are drawn in Project Settings — without overriding the full renderer:

```csharp
public sealed class MyTypeRenderer : ITypeRenderer
{
    public void Render(SettingsField field, SettingsRenderer renderer)
    {
        // draw using EditorGUILayout or renderer helpers
    }
}

public sealed class MyPluginSettingsRenderer : SettingsRenderer
{
    public MyPluginSettingsRenderer()
    {
        ComponentRenderer.TypeRenderers.Register(typeof(MyCustomType), new MyTypeRenderer());
    }
}

// In MyPluginSettings:
public override ISettingsRenderer Renderer { get; } = new MyPluginSettingsRenderer();
```

See [`release-guard/Documentation~/index.md`](release-guard/Documentation~/index.md) for the full
ComponentRenderer / TypeRenderer API.

## Documentation

The full package guide lives in [`release-guard/Documentation~/index.md`](release-guard/Documentation~/index.md).
It covers settings, build profile overrides, asset exclusions, `[ReleaseForbidden]`, custom
auditors, the post-build transformer pipeline, plugins, and the audit window.

## Repository layout

| Path | Purpose |
|---|---|
| [`release-guard/`](release-guard/) | The UPM package (this is what you install) |
| [`UnityDevHost/`](UnityDevHost/) | Development Unity project consuming the package via `file:` for compiling and running its tests |
