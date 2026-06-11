# Configuring Release Guard

Release Guard ships with strict defaults that are the right starting point for almost every
release build. For most projects: install, commit the settings asset, ship. The defaults are
not one-size-fits-all, though. This guide explains what each setting does, why the default is
what it is, and when it makes sense to deviate.

Settings live at `Edit > Project Settings > Release Guard`. The asset is
`Assets/ReleaseGuard/ReleaseGuardSettings.asset` - commit it so the whole team and CI share
the same gate.

---

## General

| Setting | Default | Summary |
| --- | --- | --- |
| [`enabled`](#enabled) | on | Master switch. |
| [`skipOnDevelopmentBuilds`](#skipondevelopmentbuilds) | on | Skip all checks for Development builds. |
| [`failureThreshold`](#failurethreshold) | `Error` | Severity at which a build is blocked. |
| [`verboseLogging`](#verboselogging) | off | Extra Console diagnostics. |
| [`profileOverrides`](#profileoverrides) | empty | Per-Build-Profile settings overrides (Unity 6+). |

---

### `enabled`

On by default. The master switch - when off, Release Guard never runs.

Every pipeline stage silently skips: no pre-build audit, no post-build transforms, no
post-processors. **When to disable:** temporarily, to unblock a build during a debugging
session. For environment-specific relaxation, reach for `profileOverrides` instead so the
gate still applies everywhere else.

### `skipOnDevelopmentBuilds`

On by default. Skips all Release Guard checks for builds made with `Development Build` enabled.

Development builds are internal. They intentionally carry debugger attachments, profiler
connections, and test scaffolding - auditing them against release rules produces noise for no
benefit. **When to disable:** you want to enforce specific checks (like `[ReleaseForbidden]`
scanning) across all builds regardless of the development flag.

### `failureThreshold`

Defaults to `Error`. A build is blocked when any finding is at or above this severity.

- `Error` - only hard errors stop the build. Warnings and advisories appear in the Console
  but do not block.
- `Warning` - warnings become blocking too. Good for CI: forces resolution instead of
  accumulation.
- `Info` - everything blocks, including advisories. Usually too strict to be practical.

Note that advisories are `Info`-level, so they pass through at `Warning` and `Error`
thresholds regardless. Use the "Don't show again" button in the audit window to permanently
suppress specific advisories you have reviewed and accepted.

### `verboseLogging`

Off by default. Emits extra diagnostics to the Console: which auditors were discovered, which
skipped and why, finding counts per auditor, and timing. **When to enable:** diagnosing why
a custom check is not running, or verifying a new plugin is being picked up. Leave it off in
production.

### `profileOverrides`

Empty by default. Per-Build-Profile overrides for Unity 6+ Build Profiles. Each entry maps
an exact profile name (case-sensitive) to an `enabled` toggle and a `failureThreshold`, which
replace the global values for that build.

Common patterns:

- A "Staging" profile with `failureThreshold: Warning` to catch problems early without
  blocking the staging pipeline.
- A "QA Distribution" profile with `enabled: false` for internal builds where the full gate
  is not yet appropriate.
- A "Production" profile with a stricter threshold than the global default.

If no profile matches the active Build Profile, the global settings apply. See
[guides/build-profiles](guides/build-profiles.md).

---

## Auditors

| Setting | Default | Summary |
| --- | --- | --- |
| [`requireIl2Cpp`](#requireil2cpp) | on | IL2CPP required (Error). |
| [`forbidDevelopmentBuild`](#forbiddevelopmentbuild) | on | Development Build flag must be off (Error). |
| [`forbidScriptDebugging`](#forbidscriptdebugging) | on | Script debugging must be disabled (Error). |
| [`forbidProfilerConnection`](#forbidprofilerconnection) | on | Autoconnect Profiler must be off (Error). |
| [`minManagedStrippingLevel`](#minmanagedstrippinglevel) | `Medium` | Minimum managed stripping level. |
| [`forbidBroadPreserve`](#forbidbroadpreserve) | on | Broad preserve rules flagged (Error). |
| [`autoDiscoverAuditors`](#autodiscoverauditors) | off | Discover auditors via TypeCache. Keep off. |
| [`disabledAuditorIds`](#disabledauditorids) | empty | Auditor ids to skip. |
| [`excludedAssetPaths`](#excludedassetpaths) | empty | Gitignore-style asset path exclusions. |
| [`releaseForbiddenExcludedAssemblies`](#releaseforbiddenexcludedassemblies) | empty | Assemblies exempt from `[ReleaseForbidden]` scanning. |
| [`suppressedAdvisoryIds`](#suppressedadvisoryids) | empty | Permanently dismissed advisory ids. |

---

### `requireIl2Cpp`

On by default, severity Error. Requires IL2CPP as the scripting backend for release builds.

Mono ships your C# as .NET assemblies that decompile almost trivially with tools like dnSpy
or ILSpy - anyone who downloads your game can read your game logic in close-to-source form.
IL2CPP compiles C# to C++ then to native code, raising the bar for reverse-engineering
significantly. For any game with monetization, anti-cheat, or sensitive IP, IL2CPP is the
standard choice.

**When to disable:** your target platform does not support IL2CPP (some older Android targets,
certain server configurations), or you have explicitly accepted the decompilation risk. If you
do, also review `minManagedStrippingLevel` - stripping reduces what can be extracted even from
Mono builds.

### `forbidDevelopmentBuild`

On by default, severity Error. Blocks the build if the `Development Build` checkbox is on.

Combined with `skipOnDevelopmentBuilds`, this catches a specific failure mode: a CI pipeline
that builds with the development flag set by mistake and reaches the release gate anyway.
**When to disable:** your team intentionally distributes development builds to a tester group
and considers it acceptable. The cleaner approach is a Build Profile override so the check
still applies to production.

### `forbidScriptDebugging`

On by default, severity Error. Blocks the build if `Script Debugging` is enabled.

An open debugger port on the player means an attacker on the same network can attach, pause
execution, and read and modify variables. This is a real attack surface in a shipped build.
**When to disable:** almost never. If you need to debug a field build, use a dedicated Build
Profile rather than shipping with script debugging on.

### `forbidProfilerConnection`

On by default, severity Error. Blocks the build if Autoconnect Profiler is enabled.

The profiler connection exposes internal performance data and some runtime state to any machine
that can connect. It has no purpose in a release build. **When to disable:** a closed-beta
build produced specifically for field profiling with a controlled audience. Use a Build Profile
for that case, not the global setting.

### `minManagedStrippingLevel`

Defaults to `Medium`. Sets a minimum managed code stripping level; the build is blocked if the
project is configured below it.

Stripping removes unreachable types, methods, and fields from compiled assemblies. Less code
ships, the binary is smaller, and reverse-engineering is harder. Options from least to most
aggressive: `Disabled`, `Minimal`, `Low`, `Medium`, `High`.

`Medium` removes unused members without requiring the extensive `[Preserve]` annotations that
`High` demands. **When to change:**

- Set `Disabled` to skip the check entirely if you have a tested compatibility reason not to
  strip (reflection-heavy code, some plugins). Do not use it as a shortcut - validate the
  consequences first.
- Raise to `High` for maximum hardening. Expect to write `link.xml` rules or add `[Preserve]`
  attributes for anything accessed by reflection. Test thoroughly.

### `forbidBroadPreserve`

On by default, severity Error. Flags preservation rules that effectively disable stripping
for entire assemblies.

Specifically it catches assembly-level `[Preserve]` attributes and `link.xml` entries that
preserve a whole assembly or namespace with no type filter. Both mean you could have a high
stripping level set and still be shipping most of an assembly's code untouched - the stripping
configuration becomes misleading. **When to disable:** you are knowingly using broad
preservation as a workaround for a third-party package that breaks under stripping, and you
have accepted that tradeoff. Document it.

### `autoDiscoverAuditors`

Off by default. When on, discovers every `ReleaseAuditor` subclass via TypeCache and runs
them all (excluding built-ins and test fixtures).

Leave this off. Register custom auditors explicitly through a plugin with `[InitializeOnLoad]`.
Auto-discovery picks up any subclass in any Editor-included assembly, including experimental
or in-progress code you may not want running in production. Explicit registration gives you
full control over what runs. See [api/plugins](api/plugins.md).

### `disabledAuditorIds`

Empty by default. Auditor ids to exclude from every run. Works for both built-in and custom
auditors. See [reference/built-in-auditors](reference/built-in-auditors.md) for the id of
each built-in.

Prefer this over removing or commenting out an auditor's source code - it is reversible and
communicates intent clearly in version control.

### `excludedAssetPaths`

Empty by default. Gitignore-style glob patterns matched against asset paths. Any finding tied
to a matching path is silently dropped.

Use this for assets you have deliberately accepted as exceptions: third-party packages,
generated code, or platform-specific assets carrying flags you cannot control. Do not use it
to silence findings you intend to fix - once excluded, the finding disappears and you lose
the reminder to address it. See [guides/asset-exclusions](guides/asset-exclusions.md).

### `releaseForbiddenExcludedAssemblies`

Empty by default. Assembly names to skip entirely during the `[ReleaseForbidden]` scan. Names
are matched case-insensitively without the `.dll` extension.

The main use case: a third-party assembly you cannot modify that contains `[ReleaseForbidden]`
members that do not apply to your build. See [guides/release-forbidden](guides/release-forbidden.md).

### `suppressedAdvisoryIds`

Empty by default. Advisory ids that have been permanently dismissed via the "Don't show again"
button in the audit window. A suppressed advisory is silently dropped on every run and build.

To restore an advisory, remove its id from this list manually. Each advisory-producing
built-in auditor lists its suppress id in [reference/built-in-auditors](reference/built-in-auditors.md).

---

## Post-Processors

| Setting | Default | Summary |
| --- | --- | --- |
| [`debugSymbolSweepEnabled`](#debugsymbolsweepenabled) | on | Scan build output for debug artifacts after a release build. |
| [`debugSymbolSweepDelete`](#debugsymbolsweepdelete) | off | Delete found artifacts. Destructive - read the entry before enabling. |
| [`debugSymbolSweepExtraPatterns`](#debugsymbolsweepextrapatterns) | empty | Extra file/folder names to sweep. |
| [`writeBuildManifest`](#writebuildmanifest) | off | Write a `release-guard-manifest.json` CI artifact next to the build. |
| [`autoDiscoverPostProcessors`](#autodiscoverpostprocessors) | off | Discover post-processors via TypeCache. Keep off. |
| [`disabledPostProcessorIds`](#disabledpostprocessorids) | empty | Post-processor ids to skip. |

---

### `debugSymbolSweepEnabled`

On by default. Scans the build output folder after a release build for debug artifacts Unity
writes alongside the player:

- `*_BackUpThisFolder_ButDontShipItWithYourGame` folders
- `*_BurstDebugInformation_DoNotShip` folders
- Loose `.pdb` files at the output root

Report-only by default - it logs a warning per artifact but does not touch anything. Deletion
is a separate opt-in. **When to disable:** your pipeline already excludes these reliably (for
example, a packaging script that explicitly lists what enters the archive). Otherwise keep it
on; it is easy to accidentally include these folders when zipping the entire output directory.

### `debugSymbolSweepDelete`

Off by default. When on, found artifacts are deleted rather than reported. **Destructive.**

Debug symbol folders are required for crash symbolication. Deleting them before archiving
means you permanently lose the ability to attribute crash addresses to source lines. The safe
sequence before enabling deletion:

1. Run report-only mode first to confirm exactly what the sweep finds.
2. Set up your pipeline to archive symbol folders to your crash-reporting service.
3. Only then enable deletion.

The Project Settings page shows a warning while this is on.

### `debugSymbolSweepExtraPatterns`

Empty by default. Additional file or folder names to treat as debug artifacts. Matched against
entries directly inside the output folder root (not recursive); `*` wildcards are supported.
Examples: `*.map`, `DebugData`. Subject to the same report/delete behavior as the built-in
patterns.

### `writeBuildManifest`

Off by default. When on, writes `release-guard-manifest.json` next to the build output after
every release build. The manifest records the Release Guard version, Unity version, build
target, build GUID, and the active auditors and post-processors.

This is a CI artifact, not a file to ship to players. It answers "what was the exact hardening
configuration for this build?" useful for compliance audits and incident analysis. **When to
enable:** you have a place to store it (CI artifact storage, build logs) and your packaging
step excludes it from the shipped archive.

### `autoDiscoverPostProcessors`

Off by default. Same guidance as `autoDiscoverAuditors` - prefer explicit plugin registration.
Auto-discovery runs every `ReleasePostProcessor` subclass it finds, including experimental ones.

### `disabledPostProcessorIds`

Empty by default. Post-processor ids to skip. Built-in ids: `debug_symbol_sweep`,
`build_manifest`. See [reference/built-in-post-processors](reference/built-in-post-processors.md).

---

## Transformers

| Setting | Default | Summary |
| --- | --- | --- |
| [`autoDiscoverTransformers`](#autodiscovertransformers) | off | Discover transformers via TypeCache. Keep off. |
| [`disabledTransformerIds`](#disabledtransformerids) | empty | Transformer ids to skip. |

---

### `autoDiscoverTransformers`

Off by default. Same guidance as the other auto-discover flags. Transformers operate on build
artifacts at a low level (IL manipulation, binary patching, obfuscation) - unintended
activation has more potential for silent damage than a reporting-only auditor.

### `disabledTransformerIds`

Empty by default. Transformer ids to skip. No built-in transformers ship, so this is only
relevant for custom transformers contributed by a plugin.

---

## Plugins

| Setting | Default | Summary |
| --- | --- | --- |
| [`autoDiscoverPlugins`](#autodiscoverplugins) | off | Discover plugins via TypeCache. Keep off; prefer `[InitializeOnLoad]` registration. |
| [`disabledPluginIds`](#disabledpluginids) | empty | Plugin ids to disable entirely. |

---

### `autoDiscoverPlugins`

Off by default. Discovers every `ReleaseGuardPlugin` subclass via TypeCache and invokes it
without any explicit registration.

Prefer explicit `[InitializeOnLoad]` registration: it is predictable, carries no scanning
overhead, and places no constraints on the plugin constructor. Auto-discovery is useful for
rapid prototyping - you want a plugin active by simply existing in the project, with no
registration ceremony. For anything you ship, register explicitly. See [api/plugins](api/plugins.md).

### `disabledPluginIds`

Empty by default. Plugin ids to disable entirely. When an id appears here, the plugin's
`Register` is not called and none of its contributions enter the registries.

Use this to temporarily disable a third-party plugin without removing it, or to disable a
plugin for a specific environment by committing a modified settings asset for that environment.
