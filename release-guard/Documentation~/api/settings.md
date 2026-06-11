# Plugin Settings and Custom Renderers

A [plugin](plugins.md) can expose its own settings page in `Edit > Project Settings > Release Guard`.
Release Guard stores one `ScriptableObject` asset per plugin, generates the Project Settings sub-page
automatically, and renders each field with the same widgets as the built-in pages. This guide covers
the full workflow from declaring a settings class through adding a custom field type and teaching the
renderer how to display it.

See also the [attributes reference](../reference/attributes.md) and the
[settings reference](../reference/settings.md).

## Declaring a settings class

Subclass `ReleaseGuardPluginSettings` (namespace `ReleaseGuard.Editor.Core.Plugins`). It derives from
`ScriptableObject`. Declare public serialized fields - each becomes a row on the generated page.

```csharp
using ReleaseGuard;                              // ReleaseIssueSeverity
using ReleaseGuard.Editor.Core.Config;           // SettingsSection, SettingsConditionalWarning
using ReleaseGuard.Editor.Core.Config.Types;     // ExclusionList
using ReleaseGuard.Editor.Core.Plugins;          // ReleaseGuardPluginSettings
using UnityEngine;                               // Tooltip

namespace ExamplePlugin
{
    public sealed class ExamplePluginSettings : ReleaseGuardPluginSettings
    {
        [SettingsSection("Behavior")]
        [Tooltip("When enabled, the example auditor fires on every release build.")]
        public bool strictMode = false;

        [SettingsConditionalWarning("Strict mode is on - the example auditor fires on every release build.")]
        public bool strictModeAcknowledged = false;

        [SettingsSection("Exclusions")]
        [Tooltip("Asset paths this plugin should ignore. One gitignore-style glob per line.")]
        public ExclusionList ignoredAssets = new();

        [SettingsSection("Reporting")]
        [Tooltip("Severity of findings when strict mode is on.")]
        public ReleaseIssueSeverity findingSeverity = ReleaseIssueSeverity.Warning;
    }
}
```

Wire it to the plugin by returning its type from `SettingsType`, then read it with `GetSettings<T>()`:

```csharp
public override System.Type SettingsType => typeof(ExamplePluginSettings);

public override void Register(PluginRegistrationContext context)
{
    var settings = GetSettings<ExamplePluginSettings>();
    context.ReleaseGuard.Registries.Auditors.Register(new ExampleAuditor(settings));
}
```

## Where the asset lives

When `SettingsType` is non-null the framework loads or creates the asset at:

```
Assets/ReleaseGuard/Plugins/{PluginId}.asset
```

The folder is created on first use. If an asset already exists at that path but is a different settings
type the framework throws an `InvalidOperationException` naming both types - rename or remove the stale
asset. The Project Settings sub-page is placed automatically under the Release Guard tree.

## Field types the default renderer handles

The default renderer walks every visible serialized field. Three cases get special widgets; everything
else falls back to a standard `PropertyField`.

| Field type | Widget |
| --- | --- |
| `ExclusionList` (`ReleaseGuard.Editor.Core.Config.Types.ExclusionList`) | Multiline pattern text area (one glob per line) followed by a collapsible "Preview matching assets" foldout. |
| `List<string>` | Multiline text area, one entry per line. |
| Any other serialized type | Standard `EditorGUILayout.PropertyField` with children. |

`ExclusionList` is a serializable wrapper around `public List<string> patterns`. Use it instead of a
bare `List<string>` when the field represents asset-path exclusions and you want the live preview.

## Settings attributes

These attributes control layout and warnings. All live in `ReleaseGuard.Editor.Core.Config` unless noted.

| Attribute | Target | Effect |
| --- | --- | --- |
| `[SettingsSection("Heading")]` | field | Draws a bold section heading above the field. Unlike Unity's `[Header]`, works consistently for both scalar and list fields. |
| `[SettingsConditionalWarning("message")]` | `bool` field | While the field value is `true`, draws a warning help box beneath the toggle. |
| `[Tooltip("...")]` | field | Standard Unity tooltip / hint text, shown as the field's hint line. |

The following attributes build the auto-generated overview/leaf page tree. For a simple flat plugin
settings object you will not need these - they apply to multi-page settings trees built on
`ReleaseGuardSettings` itself:

| Attribute | Target | Effect |
| --- | --- | --- |
| `[SettingsPage(order, label, intro, description)]` | sub-object field | Promotes a sub-object field to its own leaf page. |
| `[SettingsIntro("text")]` | class | Intro text shown at the top of the root overview page. |
| `[SettingsStatus]` | string property | Value shown in the overview "Status" section. |
| `[SettingsAction("Label", order)]` | parameterless method | Renders a button in the overview "Actions" row. |

## Custom rendering

### Level 1 - subclass SettingsRenderer

For a custom section order, intro text, or per-field tweaks, subclass `SettingsRenderer`
(namespace `ReleaseGuard.Editor.Core.Config`) and override `DrawSerializedObject`:

```csharp
using ReleaseGuard.Editor.Core.Config;
using UnityEditor;

public sealed class MyPluginRenderer : SettingsRenderer
{
    public override void DrawSerializedObject(SerializedObject so)
    {
        // Custom layout here. Call base for the default field walk.
        base.DrawSerializedObject(so);
    }
}
```

Return it from the settings object:

```csharp
private readonly MyPluginRenderer _renderer = new();
public override ISettingsRenderer Renderer => _renderer;
```

`SettingsRenderer` inherits layout helpers from `SettingsRenderPrimitives`, including `Section(string)`,
`HelpBox(string, MessageType)`, `LineListField(SerializedProperty, string)`,
`ExclusionListField(SettingsField)`, and `ExclusionPreview(SerializedProperty)`.

### Level 2 - add a custom field type with a custom renderer

The more interesting scenario: you have a custom C# type in your settings and want the settings page
to display it in a specific way, rather than falling back to Unity's default `PropertyField` foldout.
The workflow has three steps.

**Step 1 - define the custom type.**

Any `[Serializable]` struct or class works. Example: a severity range (min + max):

```csharp
using System;
using ReleaseGuard;

namespace ExamplePlugin
{
    [Serializable]
    public sealed class SeverityRange
    {
        public ReleaseIssueSeverity min = ReleaseIssueSeverity.Warning;
        public ReleaseIssueSeverity max = ReleaseIssueSeverity.Error;
    }
}
```

Add a field of that type to the settings class:

```csharp
[SettingsSection("Severity")]
[Tooltip("Minimum and maximum severity the example auditor will report.")]
public SeverityRange severityRange = new();
```

Without a custom renderer this renders as a foldout with two enum dropdowns (the default
`PropertyField` with children). The next steps replace that with something purpose-built.

**Step 2 - implement ITypeRenderer.**

`ITypeRenderer` (namespace `ReleaseGuard.Editor.Core.Config`) has one method:

```csharp
void Render(SettingsField field, SettingsRenderer renderer);
```

`SettingsField` carries `SerializedProperty Property`, `FieldInfo FieldInfo`, `string Name`,
`string Tooltip`, `bool IsStringList`, and `bool IsExclusionList`. Use the `renderer` helpers or
Unity's `EditorGUILayout` APIs directly:

```csharp
using ReleaseGuard.Editor.Core.Config;
using UnityEditor;
using UnityEngine;

namespace ExamplePlugin
{
    public sealed class SeverityRangeRenderer : ITypeRenderer
    {
        public void Render(SettingsField field, SettingsRenderer renderer)
        {
            var minProp = field.Property.FindPropertyRelative("min");
            var maxProp = field.Property.FindPropertyRelative("max");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(field.Property.displayName,
                GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUILayout.PropertyField(minProp, GUIContent.none, GUILayout.Width(100));
            EditorGUILayout.LabelField("to", GUILayout.Width(20));
            EditorGUILayout.PropertyField(maxProp, GUIContent.none, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(field.Tooltip))
                renderer.HelpBox(field.Tooltip, MessageType.None);
        }
    }
}
```

**Step 3 - register the renderer.**

Register from a `SettingsRenderer` subclass constructor. The registry is
`ComponentRenderer.TypeRenderers`, typed as `IRegistry<System.Type, ITypeRenderer>`:

```csharp
using ReleaseGuard.Editor.Core.Config;

namespace ExamplePlugin
{
    public sealed class ExamplePluginRenderer : SettingsRenderer
    {
        public ExamplePluginRenderer()
        {
            ComponentRenderer.TypeRenderers.Register(typeof(SeverityRange), new SeverityRangeRenderer());
        }
    }
}
```

Return the renderer from the settings class:

```csharp
private readonly ExamplePluginRenderer _renderer = new();
public override ISettingsRenderer Renderer => _renderer;
```

The settings page now draws `SeverityRange` fields as a compact inline min/max row. All other fields
continue to use their default renderers.

**Registration rules:**

- Built-in entries for `ExclusionList` and `List<string>` are registered as defaults and can be overridden.
- A second `Register(...)` call for the same type is a silent no-op that returns `false`.
- When no renderer is registered for a type, the fallback is `EditorGUILayout.PropertyField(field.Property, includeChildren: true)`.

## Full example

Putting it together - plugin, settings with a custom type, custom renderer, and loader:

```csharp
// SeverityRange.cs
using System;
using ReleaseGuard;

namespace ExamplePlugin
{
    [Serializable]
    public sealed class SeverityRange
    {
        public ReleaseIssueSeverity min = ReleaseIssueSeverity.Warning;
        public ReleaseIssueSeverity max = ReleaseIssueSeverity.Error;
    }
}

// SeverityRangeRenderer.cs
using ReleaseGuard.Editor.Core.Config;
using UnityEditor;
using UnityEngine;

namespace ExamplePlugin
{
    public sealed class SeverityRangeRenderer : ITypeRenderer
    {
        public void Render(SettingsField field, SettingsRenderer renderer)
        {
            var minProp = field.Property.FindPropertyRelative("min");
            var maxProp = field.Property.FindPropertyRelative("max");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(field.Property.displayName,
                GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUILayout.PropertyField(minProp, GUIContent.none, GUILayout.Width(100));
            EditorGUILayout.LabelField("to", GUILayout.Width(20));
            EditorGUILayout.PropertyField(maxProp, GUIContent.none, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }
    }
}

// ExamplePluginSettings.cs
using ReleaseGuard.Editor.Core.Config;
using ReleaseGuard.Editor.Core.Plugins;
using UnityEngine;

namespace ExamplePlugin
{
    public sealed class ExamplePluginSettings : ReleaseGuardPluginSettings
    {
        [SettingsSection("Severity")]
        [Tooltip("Severity range the example auditor will report within.")]
        public SeverityRange severityRange = new();

        private readonly ExamplePluginRenderer _renderer = new();
        public override ISettingsRenderer Renderer => _renderer;
    }

    public sealed class ExamplePluginRenderer : SettingsRenderer
    {
        public ExamplePluginRenderer()
        {
            ComponentRenderer.TypeRenderers.Register(typeof(SeverityRange), new SeverityRangeRenderer());
        }
    }
}

// ExamplePlugin.cs
using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Plugins;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;

namespace ExamplePlugin
{
    public sealed class ExamplePlugin : ReleaseGuardPlugin
    {
        public override string PluginId    => "com.example.example-plugin";
        public override string DisplayName => "Example Plugin";
        public override System.Type SettingsType => typeof(ExamplePluginSettings);

        public override void Register(PluginRegistrationContext context)
        {
            var settings = GetSettings<ExamplePluginSettings>();
            context.ReleaseGuard.Registries.Auditors.Register(new ExampleAuditor(settings));
        }
    }

    [InitializeOnLoad]
    internal static class ExamplePluginLoader
    {
        static ExamplePluginLoader()
        {
            DI.Resolve<ReleaseGuardEnvironment>().RegisterPlugin(new ExamplePlugin());
        }
    }
}
```
