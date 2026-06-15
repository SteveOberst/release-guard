# Changelog

All notable changes to this package are documented in this file.

This file is maintained automatically by [release-please](https://github.com/googleapis/release-please)
from [Conventional Commits](https://www.conventionalcommits.org/). Do not edit it by hand.

## [0.2.0](https://github.com/SteveOberst/release-guard/compare/v0.1.0...v0.2.0) (2026-06-15)


### ⚠ BREAKING CHANGES

* Removes ReleaseAuditor, ReleasePostProcessor, ReleaseTransformer and all related context types, fixture bases, and registries. Replace with ReleaseGuardComponent (single class, Register method, OnPreBuild/OnPostBuild binder). All built-in checks have been migrated to the new model.

### Features

* add build profile system with per-profile settings and automatic activation ([17bd158](https://github.com/SteveOberst/release-guard/commit/17bd1587135e408a0e39c4a6b437390e3eeb8d8a))
* add configurable outputPath to BuildManifestWriter ([#2](https://github.com/SteveOberst/release-guard/issues/2)) ([33a16bf](https://github.com/SteveOberst/release-guard/commit/33a16bf9c823ba566bf00ccdfadeda7c33bfd886))
* enrich advisory suppression store with context records and dedicated Advisories settings page ([f9662e9](https://github.com/SteveOberst/release-guard/commit/f9662e9fbba22110dc090ba6cb0f3de20fd885a2))
* replace auditor/post-processor/transformer model with ReleaseGuardComponent lifecycle ([4c3f76f](https://github.com/SteveOberst/release-guard/commit/4c3f76fa8ccc6010978796c18d9449ceea932abb))


### Bug Fixes

* apply disabled filter to plugin-contributed registry items ([383e872](https://github.com/SteveOberst/release-guard/commit/383e8726ab2cfc91063a44e419a8730797024bfe))
* exclude package sub-assemblies from type auto-discovery ([e07e9f5](https://github.com/SteveOberst/release-guard/commit/e07e9f50b7f039fa372d77f88be2cee9351ba686))
* resolve Unity console errors ([#18](https://github.com/SteveOberst/release-guard/issues/18)) ([e2e876b](https://github.com/SteveOberst/release-guard/commit/e2e876b3e0191c0ae541a73ea9c3988ac89ff109))

## 0.1.0 (2026-06-11)

### Added

- **Auditor pipeline** -- pre-build checks via `IPreprocessBuildWithReport`. Subclass
  `ReleaseAuditor` and implement `Audit(ReleaseGuardPreBuildContext)` to contribute custom rules.
  Discovery is automatic via TypeCache; built-ins are registered explicitly through
  `BuiltInAuditorRegistry`.
- **Post-processor pipeline** -- late post-build operations via `IPostprocessBuildWithReport`
  (`callbackOrder = int.MaxValue`). Subclass `ReleasePostProcessor` and implement
  `PostProcess(ReleaseGuardPostBuildContext)` for output-folder cleanup, metadata writing, and
  similar tasks that must run after the build is fully assembled.
- **Transformer pipeline** -- early post-build artifact transformations (`callbackOrder = 0`,
  runs before post-processors). Subclass `ReleaseTransformer` and implement
  `Transform(ReleaseGuardBuildContext)` for IL manipulation, obfuscation, binary patching, and
  similar artifact-level transforms. No built-in transformers ship in this release.
- **Plugin architecture** -- `ReleaseGuardPlugin` base class provides a single `Register()`
  entry point to contribute auditors, post-processors, and/or transformers from one place.
  `PluginRegistrationContext` exposes the initialized `ReleaseGuardContext`, whose typed
  registries accept contributions via `Register()`. Plugins have identity metadata (`PluginId`,
  `DisplayName`, `Author`).
- **13 built-in auditors:**
  - `ScriptingBackendAuditor` (id: `scripting_backend`) -- requires IL2CPP.
  - `ManagedStrippingAuditor` (id: `managed_stripping`) -- requires Medium or High stripping.
  - `DevelopmentBuildAuditor` (id: `development_build`) -- blocks Development Build flag.
  - `ScriptDebuggingAuditor` (id: `script_debugging`) -- blocks script debugger attachment.
  - `ProfilerConnectionAuditor` (id: `profiler_connection`) -- blocks profiler attachment.
  - `BroadPreserveAuditor` (id: `broad_preserve`) -- flags overly broad `[Preserve]` usage.
  - `ReleaseForbiddenAuditor` (id: `release_forbidden`) -- flags types/members decorated with
    `[ReleaseForbidden]`.
  - `AndroidDebuggableAuditor` (id: `android_debuggable`, Android only) -- flags explicit
    `debuggable=true` in custom `AndroidManifest.xml` and Gradle templates.
  - `WebGLExceptionSupportAuditor` (id: `webgl_exception_support`, advisory, WebGL only) --
    flags Full exception support modes (`FullWithStacktrace` at Warning,
    `FullWithoutStacktrace` at Info).
  - `StripEngineCodeAuditor` (id: `strip_engine_code`, advisory Info) -- flags disabled Engine
    Code Stripping.
  - `StackTraceTypeAuditor` (id: `stack_trace_type`, advisory Info) -- flags full stack trace
    collection on any log channel.
  - `InsecureHttpAuditor` (id: `insecure_http`, advisory Warning) -- flags
    `insecureHttpOption == AlwaysAllowed`.
  - `BurstDebugAuditor` (id: `burst_debug`, advisory Warning) -- flags disabled Burst
    optimizations and native debug mode. Reads Burst AOT settings via reflection; silently
    skips when unreadable.
- **2 built-in post-processors:**
  - `DebugSymbolSweepPostProcessor` (id: `debug_symbol_sweep`, on by default) -- scans the
    build output folder for `*_BackUpThisFolder_ButDontShipItWithYourGame`,
    `*_BurstDebugInformation_DoNotShip`, and loose `*.pdb` files. Report-only by default;
    deletion is opt-in via `debugSymbolSweepDelete`, with per-file logging and an
    output-folder containment check. Extra name patterns via `debugSymbolSweepExtraPatterns`.
  - `BuildManifestPostProcessor` (id: `build_manifest`, off by default) -- writes
    `release-guard-manifest.json` next to the build output, recording Release Guard version,
    Unity version, build target, build GUID/timing/size, and active auditor, post-processor,
    transformer, and suppression configuration. Opt-in because the manifest documents
    hardening configuration and must not ship to players.
- **Project Settings UI** -- attribute-driven multi-section tree under `Project/Release Guard`.
  The root page is an overview (status, section directory, quick actions); settings live in leaf
  pages that mirror the build pipeline stages:
  - `General` -- build gate (master switch, failure threshold), logging, profile overrides.
  - `Auditors` -- built-in rule toggles, discovery, asset exclusions (gitignore-style text
    editor with a live preview of matching assets), advisory suppressions.
  - `Post-Processors` -- debug sweep and build manifest settings, discovery.
  - `Transformers` -- discovery for artifact-transform registry items.
  - `Plugins` -- plugin discovery and disabled plugin ids.

  Pattern and id lists are edited as multiline text, one entry per line, with `#` comments --
  the gitignore mental model -- instead of Unity's reorderable list widget.
- **`ISettingsRenderer` interface** -- minimal contract (`Draw(Object)`) for plugin settings
  rendering. `SettingsRenderer` (concrete class) implements it and provides a full suite of IMGUI
  layout helpers and reflection-based rendering. Plugins return an `ISettingsRenderer` from
  `ReleaseGuardPluginSettings.Renderer` for custom Project Settings pages.
- **`ExclusionList` type** -- `[Serializable]` wrapper around `List<string>` for gitignore-style
  pattern fields. Fields of this type are automatically rendered as a multiline text area with a
  collapsible live "Preview matching assets" foldout. Use for any pattern list where asset
  coverage feedback is useful.
- **Settings renderer attributes** for declarative per-field and per-class IMGUI behavior in
  any `ScriptableObject` rendered by `SettingsRenderer`:
  - `[SettingsSection("heading")]` -- bold section heading before any field type (works
    consistently for scalars, lists, and `ExclusionList` fields).
  - `[SettingsIntro("text")]` -- intro text shown at the top of the auto-generated overview page.
  - `[SettingsStatus]` -- string property shown in the overview's Status section.
  - `[SettingsAction("label", order)]` -- parameterless instance method drawn as a button in the
    overview's Actions section.
  - `[SettingsConditionalWarning("msg")]` -- warning help box shown beneath a `bool` field while
    the field is `true`.
- **Release Guard window** (`Tools > Release Guard > Audit`) -- lists discovered auditors,
  post-processors, transformers, and plugins with their ids and display names.
- **`[ReleaseForbidden]` attribute** -- marks types and members that must not appear in release
  builds. `ReleaseForbiddenAuditor` reports violations; `releaseForbiddenExcludedAssemblies`
  lists third-party assemblies to skip.
- **Advisory suppression** -- `suppressedAdvisoryIds` in settings suppresses specific
  advisory findings by id (e.g. `managed_stripping.low`).
- **Profile overrides** -- `profileOverrides` list maps Unity Build Profile names to
  per-profile `enabled` and `failureThreshold` overrides.
- **Test fixture attributes** -- `TestAuditorFixtureAttribute`, `TestPostProcessorFixtureAttribute`,
  `TestTransformerFixtureAttribute`, and `TestReleaseGuardPluginAttribute` mark subclasses as
  test-only and exclude them from auto-discovery.
- **`autoDiscoverPlugins` / `disabledPluginIds`** -- control automatic TypeCache-based plugin
  discovery and per-plugin disabling.
- **`autoDiscoverAuditors` / `disabledAuditorIds`** -- control TypeCache-based custom auditor
  discovery and per-auditor disabling.
- **`autoDiscoverPostProcessors` / `disabledPostProcessorIds`** -- control post-processor
  discovery and per-post-processor disabling.
- **`autoDiscoverTransformers` / `disabledTransformerIds`** -- control transformer discovery
  and per-transformer disabling.
