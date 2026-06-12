# Custom Auditors

An auditor is a single pre-build check. It inspects project settings, player
settings, or assets and reports findings. The build is failed when any finding is
at or above the configured failure threshold (see
[settings reference](../reference/settings.md)).

Derive from `ReleaseAuditor` in any Editor-platform assembly. The auditor only
runs after you either register it through a plugin or enable auditor
auto-discovery in Project Settings. Auto-discovery is off by default; explicit
plugin registration is the recommended production path.

`ReleaseGuard.Editor` is `autoReferenced: true`, so the base type is visible to
Editor assemblies without an explicit asmdef reference. If you register through
an `[InitializeOnLoad]` plugin loader, add an explicit asmdef reference to
`ReleaseGuard.Editor` anyway so Unity's assembly dependency order initializes
Release Guard before your loader runs.

See also: [built-in auditors](../reference/built-in-auditors.md),
[plugins](plugins.md), [the audit window guide](../guides/audit-window.md).

## Minimal auditor

```csharp
using ReleaseGuard.Editor.Core.Audit;

public sealed class MyAuditor : ReleaseAuditor
{
    public override string Id          => "my_auditor";       // stable snake_case id
    public override string DisplayName => "My custom check";  // shown in the window

    public override void Evaluate(ReleaseAuditContext context)
    {
        if (SomethingIsWrong())
            context.Error("What is wrong.", fixHint: "How to fix it.");
    }

    private static bool SomethingIsWrong() => false;
}
```

## Base class members

`ReleaseAuditor` (namespace `ReleaseGuard.Editor.Core.Audit`) is the abstract base.
It implements `IReleaseGuardRegistryItem`.

| Member | Kind | Signature | Notes |
| --- | --- | --- | --- |
| `Id` | abstract | `public abstract string Id { get; }` | Stable, unique snake_case id. Used for logging, disabling, and de-duplication. |
| `DisplayName` | virtual | `public virtual string DisplayName { get; }` | Defaults to the type name. Shown in the audit window. |
| `Priority` | virtual | `public virtual int Priority { get; }` | Default `0`. Lower runs first. Negative runs before built-ins (all default to `0`). |
| `ShouldRun` | virtual | `public virtual bool ShouldRun(ReleaseAuditContext context)` | Default `true`. Return `false` to skip this run. |
| `Evaluate` | abstract | `public abstract void Evaluate(ReleaseAuditContext context)` | Run the check; report via the context. |

## The audit context

`ReleaseAuditContext` (namespace `ReleaseGuard.Editor.Core.Audit`) carries the run
state and the reporting API.

> **`BuildReport` is `null` during manual audits.** Any code path in your auditor that
> touches `context.BuildReport` must null-check it first. Manual audits from the audit
> window run without an active build, so `BuildReport` is always `null` there. Only use
> it when the context of the finding genuinely requires build-time information that is
> not available from `PlayerSettings` or the file system.

### Reporting methods

| Method | Signature |
| --- | --- |
| Report | `public void Report(ReleaseIssueSeverity severity, string message, string assetPath = null, string fixHint = null)` |
| Info | `public void Info(string message, string assetPath = null, string fixHint = null)` |
| Warning | `public void Warning(string message, string assetPath = null, string fixHint = null)` |
| Error | `public void Error(string message, string assetPath = null, string fixHint = null)` |
| Advisory | `public void Advisory(string suppressId, ReleaseIssueSeverity severity, string message, string fixHint = null)` |

- `Info`, `Warning`, and `Error` forward to `Report` with the matching severity.
- When `assetPath` is non-null it is normalized to a canonical Unity asset path.
  If it matches the project asset-exclusion list the issue is dropped here - this
  is the single canonical enforcement point, so exclusion applies uniformly to
  every auditor. Issues with no asset path are never excluded by asset patterns.
- `Advisory` records a dismissible finding shown with a "Don't show again" button.
  If the user already suppressed `suppressId`, the call is a silent no-op.
  `ReleaseIssueSeverity` lives in namespace `ReleaseGuard`.

### Properties

| Member | Signature | Null when |
| --- | --- | --- |
| `Settings` | `public ReleaseGuardSettings Settings { get; }` | Never null. |
| `Configuration` | `public ReleaseGuardConfiguration Configuration { get; }` | Never null. |
| `Logger` | `public ReleaseGuardLogger Logger { get; }` | Never null. |
| `BuildReport` | `public BuildReport BuildReport { get; }` | `null` for a manual audit run from the window; set during a build. |
| `BuildTarget` | `public BuildTarget BuildTarget { get; }` | Always set (build target, or active editor target). |
| `IsDevelopmentBuild` | `public bool IsDevelopmentBuild { get; }` | Always set. |
| `IsForPlatform` | `public bool IsForPlatform(BuildTarget target)` | Returns `BuildTarget == target`. |

`Settings` is a `ReleaseGuard.Editor.Config.ReleaseGuardSettings`. `BuildReport`
and `BuildTarget` come from `UnityEditor.Build.Reporting` / `UnityEditor`.

**`Settings` vs `Configuration`:** these are two distinct objects. `Settings` is the raw
settings asset -- the committed values that live in version control. `Configuration` is the
resolved, per-run state after applying any Build Profile override and the development-build
exemption. In the overwhelming majority of auditors, reading from `context.Settings` is
correct (e.g. checking `context.Settings.auditors.requireIl2Cpp`). Use `context.Configuration`
when you need the resolved gate behavior -- for example, to read the effective failure threshold
or to check whether Release Guard is enabled for this particular run:

```csharp
public override void Evaluate(ReleaseAuditContext context)
{
    // The resolved failure threshold, after any Build Profile override.
    var threshold = context.Configuration.FailureThreshold;

    // The active Build Profile name (null if no profile is active).
    var profile = context.Configuration.BuildProfileName;
}
```

`ReleaseGuardConfiguration` exposes: `Enabled` (`bool`), `IsDevelopmentBuild` (`bool`),
`BuildProfileName` (`string`, nullable), `FailureThreshold` (`ReleaseIssueSeverity`).

## Priority and ShouldRun

`Priority` orders auditors ascending: lower number runs first. The default is `0`, which
runs alongside the built-ins. Use a negative value to run before them; a positive value to
run after. `ShouldRun` is a gate -- return `false` to skip the auditor for the current run.
Use `context.IsForPlatform(...)` to restrict to a platform.

**`ShouldRun` vs early-returning inside `Evaluate`:** both work, but they have different
visibility. An auditor that returns `false` from `ShouldRun` appears in the audit window's
`Registered auditors` foldout with zero findings and a "clean" label -- confirming it ran but
had nothing to report. An auditor that returns early inside `Evaluate` produces the same
outcome. Prefer `ShouldRun` for structural conditions (wrong platform, feature not installed,
check turned off in settings) and early-return inside `Evaluate` for dynamic conditions found
during the check itself.

```csharp
using ReleaseGuard.Editor.Core.Audit;
using UnityEditor;

public sealed class AndroidOnlyAuditor : ReleaseAuditor
{
    public override string Id => "android_only_example";
    public override int Priority => -10; // run before built-ins

    public override bool ShouldRun(ReleaseAuditContext context) =>
        context.IsForPlatform(BuildTarget.Android);

    public override void Evaluate(ReleaseAuditContext context)
    {
        context.Warning("Android-specific finding.");
    }
}
```

## Realistic example

```csharp
using ReleaseGuard.Editor.Core.Audit;
using UnityEditor;

public sealed class CompanyNameAuditor : ReleaseAuditor
{
    public override string Id          => "company_name_set";
    public override string DisplayName => "Company name is configured";

    public override void Evaluate(ReleaseAuditContext context)
    {
        if (string.IsNullOrWhiteSpace(PlayerSettings.companyName) ||
            PlayerSettings.companyName == "DefaultCompany")
        {
            context.Error(
                "Player Settings company name is unset or still 'DefaultCompany'.",
                fixHint: "Set Edit > Project Settings > Player > Company Name.");
        }
    }
}
```

## Platform-specific PlayerSettings

Many `PlayerSettings` APIs take a `NamedBuildTarget` rather than a `BuildTarget`. To convert the
`context.BuildTarget` value:

```csharp
using UnityEditor;
using UnityEditor.Build;

var namedTarget = NamedBuildTarget.FromBuildTargetGroup(
    BuildPipeline.GetBuildTargetGroup(context.BuildTarget));
var backend = PlayerSettings.GetScriptingBackend(namedTarget);
```

`NamedBuildTarget` lives in `UnityEditor.Build`. The two-step conversion
(`BuildTarget` → `BuildTargetGroup` → `NamedBuildTarget`) is the standard idiom for any
`PlayerSettings` method that does not accept a plain `BuildTarget`.

## Registration options

- **Auto-discovery (zero registration).** Enable
  `Auditors > Discovery > Auto Discover Auditors` in Project Settings. Every
  non-abstract `ReleaseAuditor` in the project (excluding test fixtures and
  built-in types) is then instantiated and run. This requires a public
  parameterless constructor.
- **Via a plugin.** When you need constructor arguments (for example a settings
  object), or want to register several auditors from one place, register inside a
  `ReleaseGuardPlugin`. See [plugins.md](plugins.md):

  ```csharp
  context.ReleaseGuard.Registries.Auditors.Register(new MyAuditor(settings));
  ```

Disable a registered or discovered auditor by adding its `Id` to
`Auditors > Discovery > Disabled Auditor Ids`.
