# Plugin Settings and Custom Readers

A [plugin](plugins.md) can expose its own settings page in `Edit > Project Settings > Release Guard`.
Release Guard stores one `ScriptableObject` asset per plugin and generates the sub-page
automatically from its fields. This guide covers the full workflow: declaring a settings class,
controlling how fields render, adding custom sections, and extending the pipeline with
custom readers.

See also the [attributes reference](../reference/attributes.md) and the
[settings reference](../reference/settings.md).

## Declaring a settings class

Subclass `ReleaseGuardPluginSettings` (namespace `ReleaseGuard.Editor.Core.Plugins`). It derives
from `ScriptableObject`. Declare serialized instance fields -- public fields or non-public
fields marked `[SerializeField]` -- and each becomes a row on the generated page.

```csharp
using ReleaseGuard;
using ReleaseGuard.Editor.Core.Config.Attributes;
using ReleaseGuard.Editor.Core.Config.Types;
using ReleaseGuard.Editor.Core.Plugins;
using UnityEngine;

namespace ExamplePlugin
{
    [SettingsPage("Example Plugin", intro: "Controls for the example auditor.")]
    public sealed class ExamplePluginSettings : ReleaseGuardPluginSettings
    {
        [SettingsHeader("Behavior")]
        [Tooltip("When enabled, the example auditor reports a finding on every release build.")]
        public bool strictMode = false;

        // ConditionalWarning draws a warning box beneath the field while the value is true.
        [ConditionalWarning("Strict mode is on - ensure this is intentional.")]
        public bool strictModeAcknowledged = false;

        [SettingsHeader("Reporting")]
        [Tooltip("Severity of findings reported by the example auditor when strict mode is on.")]
        public ReleaseIssueSeverity findingSeverity = ReleaseIssueSeverity.Warning;

        // ExclusionList gives the field a "Preview matching assets" foldout in Project Settings.
        // Use it instead of List<string> whenever the field represents asset-path exclusions.
        [SettingsHeader("Exclusions")]
        public ExclusionList ignoredAssets = new();
    }
}
```

`ExclusionList` (namespace `ReleaseGuard.Editor.Core.Config.Types`) is the right field type for
any setting that accepts asset-path exclusion patterns. It serializes as a `List<string>` but
renders with a live "Preview matching assets" foldout in the settings UI. Use `List<string>`
only for lists that are not asset-path exclusions - the built-in renderer treats them differently.

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

The folder is created on first use. If an asset already exists at that path but is a different
settings type the framework throws an `InvalidOperationException` naming both types -- rename or
remove the stale asset.

## How fields are rendered

The framework builds a component tree from the settings class using `SettingsComponentReader`.
For every visible field it runs a three-pass pipeline:

1. **Before pass** -- attribute-based readers that emit components above the field, keyed by
   attribute type. `[SettingsHeader]` is handled here: it produces a `SectionHeaderComponent`
   drawn above the field.
2. **Primary pass** -- the first registered reader whose `CanRead` returns `true` for the
   field's `FieldInfo`. This produces the main component: a toggle, enum dropdown, text area,
   exclusion list widget, or a generic `PropertyField` fallback.
3. **After pass** -- attribute-based readers that emit components below the field.
   `[ConditionalWarning]` is handled here: it produces a warning box drawn below
   the toggle it is attached to.

After the primary pass the reader applies every `InjectProperty` annotation on the
field. This mutates the primary component -- for example `[SettingsLabel]` sets its
`DisplayName`. Injection runs for all primary readers (builtin and custom) so injection
attributes work regardless of what produced the component.

Fields are discovered in declaration order across the class hierarchy (base class fields first,
within each class ordered by `MetadataToken`). Public fields and non-public `[SerializeField]`
fields are included unless they are static, readonly, `[NonSerialized]`, or `[HideInInspector]`.
On the root settings object, `[NonSerialized]` `SettingsComponent` fields such as
`InlineComponent` are also included so code can insert computed sections.

## Attribute summary

All settings attributes are in namespace `ReleaseGuard.Editor.Core.Config.Attributes`.
Full signatures are in the [attributes reference](../reference/attributes.md).

| Attribute | Target | Effect |
| --- | --- | --- |
| `[SettingsPage(label, intro, description?)]` | Class | Page title, intro text, and description shown in the overview link. |
| `[SettingsHeader("Section")]` | Field | Bold section heading drawn above the field. |
| `[Tooltip("...")]` | Field | Unity tooltip forwarded to the component. |
| `[SettingsLabel("Override")]` | Field | Replaces the auto-generated field label. |
| `[ConditionalWarning("msg")]` | `bool` field | Warning box drawn below the field while the value is `true`. |

## Custom sections with InlineComponent

`InlineComponent` lets you draw a custom section on the page without serializing a field.
Declare a `[NonSerialized]` public field of type `InlineComponent` on the root settings class
and initialize it in `OnEnable`. The root reader includes `InlineComponent` fields even though
they are not serialized.

```csharp
using System;
using ReleaseGuard.Editor.Core.Config.Components;
using ReleaseGuard.Editor.Core.Plugins;
using UnityEditor;

public sealed class ExamplePluginSettings : ReleaseGuardPluginSettings
{
    [NonSerialized]
    public InlineComponent statusSection;

    private void OnEnable()
    {
        statusSection = new InlineComponent("Status", renderer =>
        {
            renderer.HelpBox("All systems nominal.", MessageType.Info);
        });
    }
}
```

`InlineComponent` takes a display name and an `Action<SettingsRenderer>` callback. The
`SettingsRenderer` parameter provides helpers: `HelpBox`, `Label`, `LineListField`,
`Section`, `Row`, and `DrawPaddedContent`. The component is positioned relative to other
fields by declaration order.

## Custom field types with IComponentReader

To control how a specific type or attribute renders, implement `IComponentReader` (namespace
`ReleaseGuard.Editor.Core.Config.Reader`) and register it via `ConfigureReader`.

```csharp
public interface IComponentReader
{
    ComponentReadOrder Order { get; }  // Before, Primary, or After
    int Priority { get; }              // lower value runs first within the same Order
    bool CanRead(object source);       // FieldInfo (Primary) or Attribute (Before/After)
    IEnumerable<SettingsComponent> Read(object source, ReadContext context);
}
```

Override `ConfigureReader` on your settings class to register readers:

```csharp
public override void ConfigureReader(SettingsComponentReader reader)
{
    reader.RegisterReader(new SeverityRangeReader());
}
```

**Primary readers** receive a `FieldInfo` as `source`. Return one or more `SettingsComponent`
instances; the first is treated as the primary component for injection purposes.
Built-in primary readers have known priorities; register at a lower value to take precedence.

**Before and After readers** receive an `Attribute` instance as `source`. Use `Before` for
components above the field, `After` for components below. In the `After` pass
`ReadContext.PrimaryComponent` is set, so after-readers can inspect what the primary produced.

### Example -- custom type reader

```csharp
using System.Collections.Generic;
using System.Reflection;
using ReleaseGuard.Editor.Core.Config.Components;
using ReleaseGuard.Editor.Core.Config.Reader;
using UnityEditor;
using UnityEngine;

public sealed class SeverityRangeReader : IComponentReader
{
    public ComponentReadOrder Order => ComponentReadOrder.Primary;
    public int Priority => 0;

    public bool CanRead(object source) =>
        source is FieldInfo fi && fi.FieldType == typeof(SeverityRange);

    public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
    {
        var fi = (FieldInfo)source;
        yield return new InlineComponent(ObjectNames.NicifyVariableName(fi.Name), renderer =>
        {
            // bind property and draw custom inline layout
            var prop = (context.Instance as UnityEngine.Object) == null ? null
                : new SerializedObject(context.Instance as UnityEngine.Object)
                    .FindProperty(fi.Name);
            if (prop == null) return;
            var min = prop.FindPropertyRelative("min");
            var max = prop.FindPropertyRelative("max");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(prop.displayName,
                GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUILayout.PropertyField(min, GUIContent.none, GUILayout.Width(100));
            EditorGUILayout.LabelField("to", GUILayout.Width(20));
            EditorGUILayout.PropertyField(max, GUIContent.none, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        });
    }
}
```

For most custom types the built-in fallback (`PropertyField` with `includeChildren: true`)
is adequate. Write a custom reader only when the default layout is wrong for your type.

### Example -- custom attribute reader

```csharp
using System;
using System.Collections.Generic;
using ReleaseGuard.Editor.Core.Config.Components;
using ReleaseGuard.Editor.Core.Config.Reader;
using UnityEditor;

[AttributeUsage(AttributeTargets.Field)]
public sealed class RequiresRestartAttribute : Attribute { }

public sealed class RequiresRestartReader : IComponentReader
{
    public ComponentReadOrder Order => ComponentReadOrder.After;
    public int Priority => 0;

    public bool CanRead(object source) => source is RequiresRestartAttribute;

    public IEnumerable<SettingsComponent> Read(object source, ReadContext context)
    {
        yield return new InlineComponent("RequiresRestart", renderer =>
            renderer.HelpBox(
                "Changing this setting requires an Editor restart.",
                MessageType.Info));
    }
}
```

Register it in `ConfigureReader`. Any field annotated with `[RequiresRestart]` now shows the
help box beneath it.

## InjectProperty -- custom field mutations

`InjectProperty` (namespace `ReleaseGuard.Editor.Core.Config.Attributes`) is a
lighter extension point: instead of producing new components, it mutates the primary component
that an existing reader already produced.

```csharp
using System;
using ReleaseGuard.Editor.Core.Config.Components;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public abstract class InjectProperty : Attribute
{
    protected virtual Type TargetComponentType => typeof(SettingsComponent);
    protected abstract void Apply(SettingsComponent component);
}
```

`TargetComponentType` filters by component type. `TryApply` (called by the reader) skips
the component when it does not match, so the cast inside `Apply` is always safe.

The built-in `[SettingsLabel]` is implemented this way: it targets `SerializedFieldComponent`
and sets `component.DisplayName = Label`. To write your own:

```csharp
using System;
using ReleaseGuard.Editor.Core.Config.Attributes;
using ReleaseGuard.Editor.Core.Config.Components;

[AttributeUsage(AttributeTargets.Field)]
public sealed class MyFlagAttribute : InjectProperty
{
    protected override Type TargetComponentType => typeof(SerializedFieldComponent);

    protected override void Apply(SettingsComponent component)
    {
        var sfc = (SerializedFieldComponent)component;
        // mutate sfc here
    }
}
```

No `ConfigureReader` registration is needed. Any `InjectProperty` attribute placed
on a field is applied automatically by `SettingsComponentReader` when that field is
read.

## SettingsRenderer helpers

`InlineComponent` callbacks receive a `SettingsRenderer` (namespace
`ReleaseGuard.Editor.Core.Config.Renderer`). The helpers available on it:

| Method | Signature | Effect |
| --- | --- | --- |
| `HelpBox` | `static void HelpBox(string text, MessageType type)` | Draws a Unity help box (Info/Warning/Error icon). |
| `Label` | `static void Label(string text)` | Draws a plain label field. |
| `Section` | `void Section(string title)` | Draws a bold section heading with top spacing. |
| `Row` | `static void Row(Action draw)` | Wraps `draw` in a horizontal layout group. |
| `DrawPaddedContent` | `void DrawPaddedContent(Action draw)` | Wraps `draw` in the standard page padding and label-width. Use this as the outermost wrapper when drawing a full custom section to match the built-in pages' look. |
| `LineListField` | `void LineListField(SerializedProperty prop, string hint)` | Draws a multiline text area for a `List<string>` serialized property, one entry per line. |
| `LineListField` | `void LineListField(SerializedProperty prop, string displayName, string hint)` | Same, with an explicit display name overriding `prop.displayName`. |
| `LineListField` | `void LineListField(SerializedProperty prop, string hint, float minLines)` | Same, with an explicit minimum line height. |

`MessageType` is `UnityEditor.MessageType`. `SerializedProperty` is from
`UnityEditor`.

## ExclusionList

`ExclusionList` (`ReleaseGuard.Editor.Core.Config.Types`) is a serializable wrapper around
`List<string> patterns`. Use it instead of a bare `List<string>` when the field represents
asset-path exclusions and you want the live "Preview matching assets" foldout. The built-in
reader handles it automatically.

## Component type reference

All types below are in namespace `ReleaseGuard.Editor.Core.Config.Components`.

| Type | Description |
| --- | --- |
| `SettingsComponent` | Abstract base. Exposes `DisplayName` and `Tooltip`. |
| `SerializedFieldComponent` | Wraps a `SerializedProperty`. Base for all field components. |
| `PrimitiveComponent` | Renders simple types (bool, enum, int, float, string, ...). |
| `StringListComponent` | Renders `List<string>` as a multiline text area. |
| `ExclusionListComponent` | Renders `ExclusionList` with a pattern text area and asset preview. |
| `GenericSerializedComponent` | Fallback for unrecognized serializable types (`PropertyField`). |
| `InlineComponent` | Arbitrary draw callback; also the return type for custom readers. |
| `SectionHeaderComponent` | Bold heading produced by `[SettingsHeader]`. |
| `ConditionalWarningComponent` | Warning help box produced by `[ConditionalWarning]`. |
| `ScreenComponent` | Root container for a full settings page. |
| `SectionGroupComponent` | Container that groups a nested sub-object's fields. |
