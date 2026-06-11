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

## ReleaseForbiddenAttribute

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
public ReleaseForbiddenAttribute(
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
`ReleaseGuard.Editor`, namespace `ReleaseGuard.Editor.Core.Config`, and are sealed.
See [api/settings](../api/settings.md).

### SettingsPageAttribute

- Targets: `Field`
- Marks a settings sub-object field as its own page in the Project Settings tree.
  One `SettingsProvider` is generated per annotated field.

```csharp
public SettingsPageAttribute(int order, string label, string intro, string description = null)
```

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `order` | `int` | (required) | Sort order within the parent (lower = higher in the list). |
| `label` | `string` | (required) | Display name in the Project Settings sidebar. |
| `intro` | `string` | (required) | One-line description shown at the top of the page. |
| `description` | `string` | `null` | Short description shown next to the link on the root overview page. |

### SettingsSectionAttribute

- Targets: `Field`
- A plain section heading drawn before a field (unlike Unity's `[Header]`).

```csharp
public SettingsSectionAttribute(string header)
```

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `header` | `string` | (required) | Heading text. |

### SettingsConditionalWarningAttribute

- Targets: `Field`
- Applied to a `bool` field; while the value is `true`, the renderer draws a
  warning help box beneath the toggle.

```csharp
public SettingsConditionalWarningAttribute(string message)
```

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `message` | `string` | (required) | Warning text shown while the field is true. |

### SettingsIntroAttribute

- Targets: `Class`
- Applied to a settings ScriptableObject class; the text is shown at the top of the
  auto-generated root overview page.

```csharp
public SettingsIntroAttribute(string text)
```

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `text` | `string` | (required) | Intro text for the overview page. |

### SettingsStatusAttribute

- Targets: `Property`
- `AllowMultiple`: no (default).
- Applied to a string-returning property; its value is shown in the "Status"
  section of the root overview page. Multiple properties appear in declaration
  order. No constructor parameters.

### SettingsActionAttribute

- Targets: `Method`
- `AllowMultiple = true`.
- Applied to a parameterless instance method; the renderer draws a button in the
  "Actions" section of the overview page.

```csharp
public SettingsActionAttribute(string label, int order = 0)
```

| Parameter | Type | Default | Description |
| --- | --- | --- | --- |
| `label` | `string` | (required) | Button label. |
| `order` | `int` | `0` | Button order within the actions row. |

## Test-fixture attributes

These mark test-only types so they never appear in real audit, post-process, or
transform runs. All live in `ReleaseGuard.Editor`, are sealed, target `Class` with
`Inherited = false`, and take no constructor parameters. Apply them to fixture
types defined in test assemblies.

| Attribute | Namespace | Marks |
| --- | --- | --- |
| `TestAuditorFixtureAttribute` | `ReleaseGuard.Editor.Core.Audit` | A `ReleaseAuditor` subclass as a test-only fixture. |
| `TestPostProcessorFixtureAttribute` | `ReleaseGuard.Editor.Core.PostProcessing` | A `ReleasePostProcessor` subclass as a test-only fixture. |
| `TestTransformerFixtureAttribute` | `ReleaseGuard.Editor.Core.Transforming` | A `ReleaseTransformer` subclass as a test-only fixture. |
| `TestReleaseGuardPluginAttribute` | `ReleaseGuard.Editor.Core.Plugins` | A `ReleaseGuardPlugin` subclass as a test-only fixture (the whole plugin is hidden from discovery). |

## See also

- [Built-in auditors](built-in-auditors.md)
- [Settings reference](settings.md)
- [api/custom-auditors](../api/custom-auditors.md)
- [api/settings](../api/settings.md)
