# Plugins

Most teams can ignore plugins at first.

Use `ReleaseGuardPlugin` when you want explicit startup, a stable plugin id, multiple component registrations, or a plugin-specific settings page.

## Minimal plugin

```csharp
using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Plugins;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;

public sealed class MyPlugin : ReleaseGuardPlugin
{
    public override string PluginId => "com.example.release-guard-plugin";
    public override string DisplayName => "Example Release Guard Plugin";

    public override void Register(PluginRegistrationContext context)
    {
        context.ReleaseGuard.Components.Register(new CompanyNameComponent());
    }
}

[InitializeOnLoad]
internal static class MyPluginLoader
{
    static MyPluginLoader()
    {
        ReleaseGuardDI.Resolve<ReleaseGuardEnvironment>().RegisterPlugin(new MyPlugin());
    }
}
```

## What appears in the project

A plugin-based extension usually means:

- your Editor assembly and source files live wherever your project keeps tooling code
- Release Guard still owns the shared package assets under `Assets/ReleaseGuard/`
- if your plugin declares `SettingsType`, Release Guard creates `Assets/ReleaseGuard/Plugins/{pluginId}.asset`

So the normal file inventory becomes:

- `Assets/ReleaseGuard/registry.asset`
- `Assets/ReleaseGuard/Profiles/release.asset`
- `Assets/ReleaseGuard/Profiles/development.asset`
- `Assets/ReleaseGuard/Plugins/{pluginId}.asset` when your plugin has settings

That plugin settings asset appears only after the plugin initializes successfully.

## Why explicit registration is recommended

Auto-discovery exists, but explicit registration is the more predictable production path because:

- assembly dependency order is explicit through your asmdef
- the registration site is obvious in code review
- plugin settings are loaded before `Register(...)` runs
- the plugin can be disabled cleanly by id

## Settings

If your plugin needs settings, override `SettingsType` and derive from `ReleaseGuardPluginSettings`.

Release Guard loads or creates that asset before `Register(...)` runs, so plugin code can read it during registration through `GetSettings()` or `GetSettings<T>()`.

You can also override:

- `Author` when you want the Release Guard window to show plugin authorship metadata in the Plugins foldout

The settings asset is stored at:

`Assets/ReleaseGuard/Plugins/{pluginId}.asset`

![Example plugin settings page in Project Settings](../assets/where_plugin_settings_are_located.png)

The package also generates a matching Project Settings sub-page automatically.

See [Plugin settings](settings.md) for the field model and settings UI behavior.
For the simple end-to-end path, see [Plugin extension workflow](../guides/plugin-extension-workflow.md).
For the deeper settings discussion, see [Advanced plugin settings](../guides/advanced-plugin-settings.md).

## Discovery rules

When `plugins.autoDiscoverPlugins` is enabled, Release Guard discovers plugin types that are:

- concrete
- constructible with a public parameterless constructor
- outside the package assembly
- not marked as test fixtures

Duplicate plugin ids are ignored after the first registration.

## Disable behavior

`disabledPluginIds` disables the whole plugin.

That means:

- the plugin is not registered
- none of its component contributions appear
- none of its settings-backed behavior is active

## Runtime contract

`Register(...)` is called once per environment initialization.

Keep it limited to registration work:

- add components
- read already-loaded plugin settings
- avoid build-time side effects

If `Register(...)` throws, Release Guard logs the exception and continues initializing the rest of the environment.

## Shipping your plugin as a package

If you want to distribute a Release Guard extension as its own UPM package, the practical shape is:

- keep your custom components, plugin type, and plugin settings in an Editor assembly inside your package
- reference `ReleaseGuard.Editor` from that Editor asmdef
- reference `ReleaseGuard.Runtime` too if your settings or shared code use runtime types like `ReleaseIssueSeverity`
- declare `io.researchy.release-guard` as a package dependency in your package manifest

Recommended package split:

- `Runtime/` only if you actually ship runtime code
- `Editor/` for the plugin, component registrations, and settings types
- `Samples~/` only for optional sample content, not for the real plugin implementation

The important point is that the sample is a teaching artifact. Real extension code should live in your package's normal `Editor/` tree, not inside `Samples~/`.
