# Plugin Extension Workflow

This guide is the simple end-to-end path for a normal Release Guard extension:

1. write a `ReleaseGuardComponent`
2. register it through a `ReleaseGuardPlugin`
3. optionally declare plugin settings
4. let Release Guard render that plugin settings page in Project Settings

If you want the full settings deep dive, including when to use plugin settings versus per-profile component settings, continue with [Advanced plugin settings](advanced-plugin-settings.md).

## The minimal shape

Most explicit plugin-based extensions have three pieces:

- one or more `ReleaseGuardComponent`s
- one `ReleaseGuardPlugin`
- one `[InitializeOnLoad]` loader that calls `RegisterPlugin(...)`

Plugin settings are optional.

## 1. Write a component

```csharp
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.PreBuild;
using UnityEditor;

public sealed class CompanyNameComponent : ReleaseGuardComponent
{
    public override string Id => "com.example.company_name";
    public override string DisplayName => "Company name configured";

    public override void Register(ReleaseGuardComponentBinder binder)
    {
        binder.OnPreBuild(OnPreBuild);
    }

    private static void OnPreBuild(ReleaseGuardPreBuildEvent releaseEvent)
    {
        var context = releaseEvent.Context;

        if (string.IsNullOrWhiteSpace(PlayerSettings.companyName) ||
            PlayerSettings.companyName == "DefaultCompany")
        {
            context.Error(
                "Company name is unset or still 'DefaultCompany'.",
                fixHint: "Set Project Settings > Player > Company Name.");
        }
    }
}
```

## 2. Wrap it in a plugin

```csharp
using ReleaseGuard.Editor.Core.Plugins;

public sealed class CompanyPolicyPlugin : ReleaseGuardPlugin
{
    public override string PluginId => "com.example.company-policy";
    public override string DisplayName => "Company Policy";

    public override void Register(PluginRegistrationContext context)
    {
        context.ReleaseGuard.Components.Register(new CompanyNameComponent());
    }
}
```

## 3. Register the plugin at editor startup

```csharp
using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;

[InitializeOnLoad]
internal static class CompanyPolicyPluginLoader
{
    static CompanyPolicyPluginLoader()
    {
        ReleaseGuardDI.Resolve<ReleaseGuardEnvironment>()
            .RegisterPlugin(new CompanyPolicyPlugin());
    }
}
```

That is enough to get the component into Release Guard without relying on auto-discovery.

## 4. Add plugin settings if you need them

If your plugin needs its own Project Settings page, add a `ReleaseGuardPluginSettings` type and expose it through `SettingsType`.

```csharp
using ReleaseGuard.Editor.Core.Config.Attributes;
using ReleaseGuard.Editor.Core.Plugins;

[SettingsPage("Company Policy", intro: "Project-wide settings for the company policy plugin.")]
public sealed class CompanyPolicyPluginSettings : ReleaseGuardPluginSettings
{
    [SettingsHeader("Behavior")]
    public bool strictMode = true;
}
```

Then add it to the plugin:

```csharp
public override System.Type SettingsType => typeof(CompanyPolicyPluginSettings);
```

Release Guard will create the settings asset and render a plugin page for it automatically.

## What you get in Project Settings

Once the plugin initializes successfully, its settings page appears under `Edit > Project Settings > Release Guard > Plugins`.

![Example plugin settings page in Project Settings](../assets/where_plugin_settings_are_located.png)

## What to read next

- [Plugins](../api/plugins.md)
- [Plugin settings and settings UI](../api/settings.md)
- [Advanced plugin settings](advanced-plugin-settings.md)
