# Built-in Auditors

Auditors are pre-build checks. Each derives from `ReleaseAuditor` and reports
findings (Error, Warning, or Info) through the audit context. A build fails when
any reported issue is at or above the configured failure threshold (see
[settings](settings.md)).

This page documents every auditor that ships with the package. The canonical
list lives in `BuiltInAuditorRegistry.GetAll()` - built-ins are added explicitly
there (not discovered via TypeCache), so this table is the complete shipped set.

To write your own, see [api/custom-auditors](../api/custom-auditors.md).

## Severity model

`ReleaseIssueSeverity` is ordered least to most serious: `Info` (0),
`Warning` (1), `Error` (2). A build is blocked when any issue is at or above the
failure threshold (default `Error`). See [attributes](attributes.md) for the enum
definition.

Auditors report findings in three ways:

- `context.Error(...)`, `context.Warning(...)`, `context.Info(...)` - direct findings.
- `context.Advisory(suppressId, severity, ...)` - a dismissible best-practice
  notice. The user can permanently hide it via "Don't show again" in the Release
  Guard window, which writes the suppress id into
  `auditors.suppressedAdvisoryIds`. If the id is already suppressed the advisory
  is dropped silently.

Every finding that carries an asset path is run through the asset-exclusion list
before being recorded (see [guides/asset-exclusions](../guides/asset-exclusions.md)).

If an auditor throws an uncaught exception, the executor catches it, logs the stack
trace to the Console, and adds a `Warning`-severity finding attributed to that auditor
with the message `"Auditor '{id}' failed to run: {message}"`. The run continues with
the remaining auditors — one bad auditor never aborts the whole audit. Treat this
Warning as a bug in the auditor that needs fixing.

## Execution order

Auditors run in `Priority` order (lower runs first). Only one built-in overrides
the default priority of `0`:

| Auditor | Priority |
| --- | --- |
| `scripting_backend` | -10 (runs first) |
| all other built-ins | 0 |

## Summary table

| Id | DisplayName | What it checks | Severity | Gated by (ShouldRun) |
| --- | --- | --- | --- | --- |
| `scripting_backend` | Scripting backend (IL2CPP) | Scripting backend is IL2CPP, not Mono | Error | `auditors.requireIl2Cpp` |
| `managed_stripping` | Managed code stripping | Stripping level meets the required minimum; advisories below Medium and for deprecated Low | Warning + Advisory (Warning) | `auditors.minManagedStrippingLevel != Disabled` |
| `development_build` | Development build disabled | The build is not a Development Build | Error | `auditors.forbidDevelopmentBuild` |
| `script_debugging` | Script debugging disabled | Script debugging (managed debugger attach) is off | Error | `auditors.forbidScriptDebugging` |
| `profiler_connection` | Profiler connection disabled | Autoconnect Profiler is off | Error | `auditors.forbidProfilerConnection` |
| `broad_preserve` | Broad preserve rules | No broad `[Preserve]` / link.xml whole-assembly rules | Error | `auditors.forbidBroadPreserve` |
| `release_forbidden` | Release-forbidden members | No `[ReleaseForbidden]` members in shipping assemblies | Per-attribute (default Error) | always runs |
| `android_debuggable` | Android debuggable templates | No explicit `debuggable=true` in Android templates | Error | platform == Android |
| `webgl_exception_support` | WebGL exception support | Exception support is not a Full mode | Advisory (Warning or Info) | platform == WebGL |
| `strip_engine_code` | Engine code stripping | Flags `PlayerSettings.stripEngineCode == false` | Advisory (Info) | always runs |
| `stack_trace_type` | Stack trace log types | No log channel uses Full stack traces | Advisory (Info) | always runs |
| `insecure_http` | Insecure HTTP option | Cleartext HTTP is not `AlwaysAllowed` | Advisory (Warning) | always runs |
| `burst_debug` | Burst AOT debug settings | Burst optimizations on, native debug off | Advisory (Warning) | Burst AOT settings type is present |

## Auditor details

### scripting_backend - Scripting backend (IL2CPP)

- DisplayName: `Scripting backend (IL2CPP)`
- Priority: `-10` (runs before all other built-ins)
- ShouldRun: `context.Settings.auditors.requireIl2Cpp`
- Checks: reads `PlayerSettings.GetScriptingBackend` for the target's
  `NamedBuildTarget`. If it is not `IL2CPP`, reports an Error.
- Severity: Error
- Fix hint: "Project Settings > Player > Other Settings > Scripting Backend: set
  to IL2CPP."

### managed_stripping - Managed code stripping

- DisplayName: `Managed code stripping`
- ShouldRun: `context.Settings.auditors.minManagedStrippingLevel != ManagedStrippingLevel.Disabled`
- Checks: compares `PlayerSettings.GetManagedStrippingLevel` against the required
  minimum by semantic aggressiveness (Disabled < Minimal < Low < Medium < High;
  the raw enum order is not used because `Minimal` has a higher numeric value than
  `High`). Reports a Warning when the actual level is below the required level.
- Severity: Warning (for the below-minimum finding)
- Fix hint: "Project Settings > Player > Other Settings > Managed Stripping
  Level: set to {required} or higher."
- Advisories:
  - `managed_stripping.below_medium` (severity Warning) - raised when the actual
    level is below Medium, **independently of the configured minimum**. This means
    if you deliberately lower `minManagedStrippingLevel` to `Low` or `Minimal`,
    you will still receive this advisory. Suppress it with "Don't show again" in
    the audit window once you have consciously accepted the lower stripping level.
  - `managed_stripping.low_deprecated` (severity Warning) - raised when the level
    is exactly `Low`. Unity has marked `Low` for future deprecation; this advisory
    prompts migration before it becomes an error in a later Unity version.

### development_build - Development build disabled

- DisplayName: `Development build disabled`
- ShouldRun: `context.Settings.auditors.forbidDevelopmentBuild`
- Checks: if `context.IsDevelopmentBuild` is true, reports an Error.
- Severity: Error
- Fix hint: "Disable 'Development Build' in Build Settings (or your Build Profile)
  before releasing."

> **Interaction with `skipOnDevelopmentBuilds`:** With the default settings
> (`skipOnDevelopmentBuilds = true`), Release Guard skips every build stage entirely
> when the Development Build flag is set — this auditor never runs during a real
> development build. This auditor becomes meaningful only when `skipOnDevelopmentBuilds`
> is turned off (i.e. you want Release Guard to audit all builds regardless of the
> development flag), and a development build slips through to the gate. With default
> settings its primary value is in the manual audit window, where it flags the current
> Build Settings state.

### script_debugging - Script debugging disabled

- DisplayName: `Script debugging disabled`
- ShouldRun: `context.Settings.auditors.forbidScriptDebugging`
- Checks: reads the script-debugging state from the build report options during a
  build, or from the editor build options for a manual run. If enabled, reports an
  Error.
- Severity: Error
- Fix hint: "Disable 'Script Debugging' in Build Settings (or your Build Profile)
  before releasing."

### profiler_connection - Profiler connection disabled

- DisplayName: `Profiler connection disabled`
- ShouldRun: `context.Settings.auditors.forbidProfilerConnection`
- Checks: reads the Autoconnect Profiler state from the build report options
  during a build, or from the editor build options for a manual run. If enabled,
  reports an Error.
- Severity: Error
- Fix hint: "Disable 'Autoconnect Profiler' in Build Settings (or your Build
  Profile) before releasing."

### broad_preserve - Broad preserve rules

- DisplayName: `Broad preserve rules`
- ShouldRun: `context.Settings.auditors.forbidBroadPreserve`
- Checks: runs `BroadPreserveAnalyzer.AnalyzeProject()` and reports an Error per
  finding (broad `[Preserve]` usage and whole-assembly link.xml preservation
  rules). Each finding carries the offending asset path.
- Severity: Error
- Fix hint: "Prefer targeted [Preserve] usage or explicit link.xml entries for the
  exact members reflection needs."

### release_forbidden - Release-forbidden members

- DisplayName: `Release-forbidden members`
- ShouldRun: always runs (no override).
- Checks: scans loaded assemblies whose names are in the player (shipping)
  assembly set for types, methods, fields, and properties marked with
  `[ReleaseForbidden]` (see [attributes](attributes.md)). Each match is reported
  using the attribute's own `Severity` (default `Error`), with the attribute's
  optional `Reason` appended to the message. Assemblies listed in
  `auditors.releaseForbiddenExcludedAssemblies` are skipped.
- Severity: per-attribute (default Error)
- Message form: ``[ReleaseForbidden] '{member}' must not ship in a release build.``
  plus ``Reason: {reason}`` when a reason is set.

### android_debuggable - Android debuggable templates

- DisplayName: `Android debuggable templates`
- ShouldRun: `context.IsForPlatform(BuildTarget.Android)`
- Checks: scans `Assets/Plugins/Android/` for `AndroidManifest.xml` and `*.gradle`
  files containing an explicit `debuggable=true`. Commented-out occurrences are
  ignored; Unity's generated manifests are not scanned. Reports an Error per
  finding with the file's asset path (so a legitimately debuggable template can be
  excluded via the asset-exclusion list).
- Severity: Error
- Fix hint: "Remove the debuggable declaration from the template, or gate it to
  debug build variants only."

### webgl_exception_support - WebGL exception support

- DisplayName: `WebGL exception support`
- ShouldRun: `context.IsForPlatform(BuildTarget.WebGL)`
- Checks: reads `PlayerSettings.WebGL.exceptionSupport`. If it is
  `FullWithStacktrace` or `FullWithoutStacktrace`, raises an advisory.
- Severity: Advisory. `FullWithStacktrace` -> Warning (it additionally embeds
  managed class and method names); `FullWithoutStacktrace` -> Info.
- Suppress id: `webgl_exception_support.full` (shared by both modes)
- Fix hint: "Project Settings > Player > Publishing Settings > Enable Exceptions:
  set to 'Explicitly Thrown Exceptions Only'."

### strip_engine_code - Engine code stripping

- DisplayName: `Engine code stripping`
- ShouldRun: always runs.
- Checks: if `PlayerSettings.stripEngineCode` is false, raises an advisory
  encouraging engine code stripping.
- Severity: Advisory (Info)
- Suppress id: `strip_engine_code.disabled`
- Fix hint: "Enable 'Strip Engine Code' in Player Settings > Other Settings."

### stack_trace_type - Stack trace log types

- DisplayName: `Stack trace log types`
- ShouldRun: always runs.
- Checks: if any of the five log channels (Log, Warning, Error, Assert,
  Exception) uses `StackTraceLogType.Full`, raises an advisory.
- Severity: Advisory (Info)
- Suppress id: `stack_trace_type.full`
- Fix hint: "Set stack trace log types to 'None' or 'ScriptOnly' in Player
  Settings > Other Settings."

### insecure_http - Insecure HTTP option

- DisplayName: `Insecure HTTP option`
- ShouldRun: always runs.
- Checks: if `PlayerSettings.insecureHttpOption` is
  `InsecureHttpOption.AlwaysAllowed`, raises an advisory. `NotAllowed` and
  `DevelopmentOnly` are not flagged.
- Severity: Advisory (Warning)
- Suppress id: `insecure_http.always_allowed`
- Fix hint: "Project Settings > Player > Other Settings > Allow downloads over
  HTTP: set to 'Not allowed' or 'Allowed in development builds'."

### burst_debug - Burst AOT debug settings

- DisplayName: `Burst AOT debug settings`
- ShouldRun: only when the Burst AOT settings type
  (`Unity.Burst.Editor.BurstPlatformAotSettings`) is present. This is a soft
  dependency read via reflection; if Burst is not installed, or its internals are
  renamed, the check silently skips and never reports anything it cannot verify.
- Checks: reads the AOT settings for the current build target and raises advisories
  for two configurations:
  - Burst optimizations disabled.
  - Burst native debug mode ("Enable debug in all builds") enabled.
- Severity: Advisory (Warning) for both
- Suppress ids:
  - `burst_debug.optimisations_disabled`
  - `burst_debug.debug_enabled`
- Fix hints: enable "Enable Optimisations" / disable "Enable debug in all builds"
  for the platform in Project Settings > Burst AOT Settings.

## See also

- [Settings reference](settings.md) - the fields each auditor is gated on.
- [Attributes reference](attributes.md) - `[ReleaseForbidden]` and
  `ReleaseIssueSeverity`.
- [Built-in post-processors](built-in-post-processors.md)
- [Built-in transformers](built-in-transformers.md)
