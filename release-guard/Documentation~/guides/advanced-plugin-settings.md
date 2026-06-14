# Advanced Plugin Settings

This guide is the deep dive into Release Guard's settings capabilities for extension authors.

The central design question is not "how do I render a field?" It is "which settings scope should own this value?"

Release Guard gives you two distinct extension settings surfaces:

- plugin settings: one project-scoped asset per plugin under `Assets/ReleaseGuard/Plugins/{pluginId}.asset`
- component settings: per-profile entries stored inside `components.componentToggles`

If you get that split wrong, the extension becomes hard to reason about.

## Two settings scopes

### Plugin settings

Plugin settings are project-scoped.

They are loaded once for the plugin and do not change when the active Release Guard profile changes.

Good uses:

- organization-wide defaults
- feature switches for the plugin itself
- external service configuration
- metadata that affects registration
- shared policy values that should not vary by profile

Bad uses:

- thresholds that should differ between `Release` and `Development`
- destructive post-build toggles that should be stricter in one profile than another
- allowlists or exclusions that are intentionally profile-specific

### Component settings

Component settings are profile-scoped.

They live in the active `ReleaseGuardSettings` asset under `components.componentToggles`, one entry per component id.

Good uses:

- enabling or disabling a component per profile
- per-profile thresholds
- per-profile exclusions
- different post-build behavior in `Release` versus `Development`

## Mental model

Use plugin settings when you want one value for the whole project.

Use component settings when you want the value to follow the active Release Guard profile.

That is the most important rule in this document.

## The common mixed setup

Real extensions often need both:

- plugin settings for project-wide defaults
- component settings for profile-specific behavior

That combination is valid, but only if the responsibilities are clear.

## Example: project-wide default plus per-profile override

Imagine a component that validates the company name:

- the studio wants one default company name for the whole project
- some profiles should be excluded entirely
- some profiles may want different strictness

That means:

- the default company name belongs in plugin settings
- per-profile exclusions or severity behavior belong in component settings

### Plugin settings

```csharp
using ReleaseGuard.Editor.Core.Config.Attributes;
using ReleaseGuard.Editor.Core.Plugins;

[SettingsPage("Company Policy", intro: "Project-wide settings for the company policy plugin.")]
public sealed class CompanyPolicyPluginSettings : ReleaseGuardPluginSettings
{
    [SettingsHeader("Defaults")]
    public string defaultCompanyName = "My Studio";

    [SettingsHeader("Registration")]
    public bool registerCompanyNameComponent = true;
}
```

### Component settings

```csharp
using System;
using ReleaseGuard.Editor.Core.Components;
using UnityEditor.Build;

[Serializable]
public sealed class CompanyNameComponentSettings : ReleaseGuardComponentSettings
{
    public string overrideCompanyName = string.Empty;
    public BuildTarget[] excludedTargets = Array.Empty<BuildTarget>();
}
```

### Component implementation

```csharp
using System.Linq;
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.PreBuild;
using UnityEditor;

public sealed class CompanyNameComponent : ReleaseGuardComponent<CompanyNameComponentSettings>
{
    private readonly CompanyPolicyPluginSettings _pluginSettings;

    public CompanyNameComponent(CompanyPolicyPluginSettings pluginSettings)
    {
        _pluginSettings = pluginSettings;
    }

    public override string Id => "com.example.company_name";
    public override string DisplayName => "Company name configured";

    public override void Register(ReleaseGuardComponentBinder binder)
    {
        binder.OnPreBuild(OnPreBuild);
    }

    private void OnPreBuild(ReleaseGuardPreBuildEvent releaseEvent)
    {
        var context = releaseEvent.Context;

        if (Settings.excludedTargets.Contains(context.BuildTarget))
            return;

        var expected = string.IsNullOrWhiteSpace(Settings.overrideCompanyName)
            ? _pluginSettings.defaultCompanyName
            : Settings.overrideCompanyName;

        if (PlayerSettings.companyName != expected)
        {
            context.Error(
                $"Company name is '{PlayerSettings.companyName}', expected '{expected}'.",
                fixHint: $"Set Project Settings > Player > Company Name to '{expected}'.");
        }
    }
}
```

This is a good split:

- `defaultCompanyName` is stable project policy
- `overrideCompanyName` and `excludedTargets` are profile-specific

## Example: registration-time feature flag

Some settings should influence whether a component gets registered at all.

That is plugin-settings territory because registration happens before the component participates in any profile-driven behavior.

```csharp
public override void Register(PluginRegistrationContext context)
{
    var settings = GetSettings<CompanyPolicyPluginSettings>();
    if (settings == null || !settings.registerCompanyNameComponent)
        return;

    context.ReleaseGuard.Components.Register(new CompanyNameComponent(settings));
}
```

That kind of switch is project-scoped by nature.

## Example: destructive post-build behavior

This is the kind of thing that should usually stay in component settings, not plugin settings.

The built-in `debug_symbol_sweep` is a good reference shape:

- the component itself is project functionality
- the destructive toggle (`delete`) is profile-scoped

That makes sense because teams often want:

- report-only behavior in `Development`
- destructive cleanup in `Release` or a dedicated CI profile

If you moved that toggle into plugin settings, you would lose the ability to vary it by active profile.

## What the plugin settings UI can render

By default, `ReleaseGuardPluginSettings` pages use the same settings UI system as the built-in pages.

Useful built-in behavior:

- primitive serialized fields render as standard Unity controls
- `List<string>` renders as a multiline list editor
- `ExclusionList` gets a specialized asset-preview UI
- `SettingsHeader` creates section headings
- `SettingsLabel` overrides display labels
- `Tooltip` surfaces normal Unity tooltips

That means many plugins do not need custom UI code at all.

## Advanced settings UI

Override `ConfigureReader(SettingsComponentReader reader)` when stock serialized rendering is not enough.

That is the path for:

- custom readers
- custom-drawn sections
- more specialized settings-page behavior

Use that only when the built-in field rendering stops being expressive enough. Most plugin settings pages should stay relatively plain.

## Where the plugin settings page appears

When a plugin declares `SettingsType`, Release Guard creates a Project Settings sub-page for it under `Plugins`.

![Example plugin settings page in Project Settings](../assets/where_plugin_settings_are_located.png)

## Common mistakes

- putting profile-dependent values into plugin settings
- putting registration-time switches into component settings
- treating plugin settings and component settings as interchangeable
- overusing custom UI when normal serialized fields would be clearer

## Internal-style use case patterns

These are the kinds of shapes the system is good at:

- project-wide plugin defaults with per-profile component overrides
- plugin-level opt-in registration of optional components
- shared settings used by multiple components registered from one plugin
- per-profile destructive or strict behavior layered on top of project-wide plugin policy

## Recommended reading order

1. [Plugin extension workflow](plugin-extension-workflow.md)
2. [Plugin settings and settings UI](../api/settings.md)
3. [Plugins](../api/plugins.md)
4. [Custom components](custom-components.md)
