# Attributes Reference

This page documents every public attribute the package defines, plus the
`ReleaseIssueSeverity` enum that attributes and settings depend on.

Attributes live in one of two assemblies:

- `ReleaseGuard.Runtime` (namespace `ReleaseGuard`) - referenced by both runtime
  and editor code. `autoReferenced: true`.
- `ReleaseGuard.Editor` (Editor-only, `autoReferenced: true`) - the settings and
  test-fixture attributes.

## ReleaseIssueSeverity (enum)

- Assembly: `ReleaseGuard.Runtime`
- Namespace: `ReleaseGuard`

Severity of a finding, ordered least to most serious. A build fails when any
issue is at or above the configured failure threshold.

| Value | Underlying int |
| --- | --- |
| `Info` | 0 |
| `Warning` | 1 |
| `Error` | 2 |

Ordering matters: the failure threshold and advisory comparisons rely on the
numeric order.

## ReleaseForbidden

- Assembly: `ReleaseGuard.Runtime`
- Namespace: `ReleaseGuard`
- Valid targets (`AttributeUsage`): `Class`, `Struct`, `Enum`, `Method`, `Field`,
  `Property`. `Inherited = false`.
- Sealed.

Marks a type or member that must not ship in a release build (debug hooks, cheat
commands, test scaffolding, dev-only backdoors). The built-in `release_forbidden`
auditor reports every usage in a shipping assembly so a release build fails before
such code can ship. See [built-in-auditors](built-in-auditors.md) and
[guides/release-forbidden](../guides/release-forbidden.md).

Constructor:

```csharp
public ReleaseForbidden(
    ReleaseIssueSeverity severity = ReleaseIssueSeverity.Error,
    string reason = null)
```

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `severity` | `ReleaseIssueSeverity` | `Error` | How serious it is to ship this member. Surfaced as the finding's severity. |
| `reason` | `string` | `null` | Optional human-readable reason, appended to the report and Console message. |

Properties: `Severity` (get) and `Reason` (get).

Example:

```csharp
[ReleaseForbidden(ReleaseIssueSeverity.Error, "Gives infinite money")]
public static void GrantAllCurrency() { ... }
```

Prefer also wrapping the implementation in a debug-only `#if` so it is physically
excluded from the compiled release.

## Settings attributes

These drive the auto-generated Project Settings UI. All live in
`ReleaseGuard.Editor`, namespace `ReleaseGuard.Editor.Core.Config.Attributes`, and are sealed
unless noted. See [api/settings](../api/settings.md) for the full rendering pipeline.

### SettingsPage

- Targets: `Class`
- Applied to a `ReleaseGuardPluginSettings` subclass (or any settings class). Sets the page
  title, intro text shown at the top of the page, and the short description shown next to
  the link on the root overview page.

```csharp
public SettingsPage(string label, string intro, string description = "")
```

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `label` | `string` | (required) | Display name in the Project Settings sidebar. |
| `intro` | `string` | (required) | One-line description shown at the top of the page. |
| `description` | `string` | `""` | Short description shown next to the link on the root overview page. |

### SettingsContainer

- Targets: `Class`
- Base attribute for settings containers. `SettingsPage` derives from it. Use `SettingsPage`
  for ordinary plugin settings pages. `SettingsContainer` is not applied directly because its
  constructor is protected; derive your own container attribute from it only when building custom
  nested settings-container support.
- Not sealed.

```csharp
public class SettingsContainer : Attribute
{
    protected SettingsContainer(string label, string description = "")
}
```

Properties: `Label` (get) and `Description` (get).

### SettingsHeader

- Targets: `Field`
- Draws a bold section heading above the annotated field.

```csharp
public SettingsHeader(string header)
```

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `header` | `string` | (required) | Heading text. |

### SettingsLabel

- Targets: `Field`
- Overrides the display label for a settings field. Without this attribute the label falls
  back to `ObjectNames.NicifyVariableName` applied to the field name. Derives from
  `InjectProperty` and targets `SerializedFieldComponent`.

```csharp
public SettingsLabel(string label)
```

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `label` | `string` | (required) | Label text shown in the settings UI. |

Example:

```csharp
[SettingsLabel("Require IL2CPP")]
public bool requireIl2Cpp = true;
```

### ConditionalWarning

- Targets: `Field`
- Applied to a `bool` field; while the value is `true`, a warning help box is drawn
  beneath the toggle. The attribute targets `Field` with no type restriction in C# — it is
  the developer's responsibility to apply it only to `bool` fields. Applying it to a
  non-bool field will produce undefined rendering behavior.

```csharp
public ConditionalWarning(string message)
```

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `message` | `string` | (required) | Warning text shown while the field is `true`. |

### InjectProperty

- Targets: `Field`, `AllowMultiple = true`
- Abstract base class for attributes that mutate the primary `SettingsComponent` produced
  by a field's reader. `SettingsComponentReader` calls `TryApply` after the primary pass,
  so injection works for builtin and custom readers alike.
- Not sealed -- derive from it to create custom injection attributes.

```csharp
public abstract class InjectProperty : Attribute
{
    protected virtual Type TargetComponentType => typeof(SettingsComponent);
    protected abstract void Apply(SettingsComponent component);
}
```

`TargetComponentType` restricts which component type receives the injection. `TryApply`
silently skips components that do not match, so casts inside `Apply` are always safe.

Example -- custom injection attribute:

```csharp
[AttributeUsage(AttributeTargets.Field)]
public sealed class MyAttribute : InjectProperty
{
    protected override Type TargetComponentType => typeof(SerializedFieldComponent);
    protected override void Apply(SettingsComponent component)
    {
        var sfc = (SerializedFieldComponent)component;
        // mutate sfc
    }
}
```

No registration is required. Any `InjectProperty` attribute placed on a field is
applied automatically by `SettingsComponentReader` when that field is read.

## Test-fixture attributes

These mark test-only types so they never appear in real audit, post-process, or
transform runs. All live in `ReleaseGuard.Editor`, are sealed, target `Class` with
`Inherited = false`, and take no constructor parameters. Apply them to fixture
types defined in test assemblies.

| Attribute | Namespace | Marks |
| --- | --- | --- |
| `TestAuditorFixture` | `ReleaseGuard.Editor.Core.Audit` | A `ReleaseAuditor` subclass as a test-only fixture. |
| `TestPostProcessorFixture` | `ReleaseGuard.Editor.Core.PostProcessing` | A `ReleasePostProcessor` subclass as a test-only fixture. |
| `TestTransformerFixture` | `ReleaseGuard.Editor.Core.Transforming` | A `ReleaseTransformer` subclass as a test-only fixture. |
| `TestReleaseGuardPlugin` | `ReleaseGuard.Editor.Core.Plugins` | A `ReleaseGuardPlugin` subclass as a test-only fixture (the whole plugin is hidden from discovery). |

## See also

- [Built-in auditors](built-in-auditors.md)
- [Settings reference](settings.md)
- [api/custom-auditors](../api/custom-auditors.md)
- [api/settings](../api/settings.md)
