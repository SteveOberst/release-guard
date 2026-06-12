# Development

This page is for contributors working on the Release Guard package itself. It covers the package layout, the development host project, running tests, and the procedures for adding new built-in auditors and post-processors.

## Package and dependencies

The package manifest (`package.json`) declares:

- `name`: `org.researchy.release-guard`
- `displayName`: `Release Guard`
- `unity`: `2022.3` (with `unityRelease` `0f1`)
- `dependencies`: none.
- `testDependencies`: `com.unity.test-framework` `1.6.0` and `com.unity.ext.nunit` `2.0.5`.

## Layout

```
release-guard/
  package.json
  Runtime/                         ReleaseGuard.Runtime asmdef (attribute + severity enum)
  Editor/                          ReleaseGuard.Editor asmdef (everything else)
    Builtins/Auditor/              built-in ReleaseAuditor implementations
    Builtins/PostProcessor/        built-in ReleasePostProcessor implementations
    Builtins/BuiltInAuditorRegistry.cs
    Builtins/BuiltInPostProcessorRegistry.cs
    Builtins/BuiltInTransformerRegistry.cs
    Core/                          audit, post-processing, transforming, registries, runtime, config, DI, plugins
    Config/                        settings asset and Project Settings provider
    Hooks/                         the three build callbacks
    UI/                            ReleaseGuardWindow
    Util/                          matchers, resolvers, analyzers
  Tests/Editor/                    ReleaseGuard.Editor.Tests asmdef
  Tests/Editor/Fixtures/           ReleaseGuard.Editor.TestFixtures asmdef
  Documentation~/                  this documentation tree
```

The runtime assembly (`ReleaseGuard.Runtime`) holds only the public `ReleaseForbidden` attribute and `ReleaseIssueSeverity` so gameplay code can reference them without pulling in Editor types. The Editor assembly (`ReleaseGuard.Editor`) holds the pipeline, settings, UI, and built-ins; it is `autoReferenced`, so any Editor assembly in the consuming project can see `ReleaseAuditor`, `ReleasePostProcessor`, and `ReleaseTransformer`.

For explicit plugin registration with `[InitializeOnLoad]`, add an asmdef reference to `ReleaseGuard.Editor`. That dependency makes Unity run Release Guard's startup before your plugin loader, which keeps `DI.Resolve<ReleaseGuardEnvironment>().RegisterPlugin(...)` deterministic. Auto-reference is enough for type visibility, but explicit asmdef dependency is what gives the startup-order guarantee.

## The development host

`UnityDevHost` is a Unity project that consumes the package locally. Its `Packages/manifest.json` references the package by relative path and marks it testable:

```json
{
  "dependencies": {
    "org.researchy.release-guard": "file:../../release-guard"
  },
  "testables": [
    "org.researchy.release-guard"
  ]
}
```

The `file:` dependency points at the package source folder so edits show up live in the host. Listing the package under `testables` makes its Editor tests appear in the host's Test Runner. Open `UnityDevHost` in the Editor to work on the package with full play-in, settings UI, and test support.

## Tests

Tests live under `Tests/Editor` and compile into the `ReleaseGuard.Editor.Tests` assembly. That asmdef is Editor-only, references `ReleaseGuard.Editor`, `ReleaseGuard.Runtime`, and the test runner assemblies, and is gated by the `UNITY_INCLUDE_TESTS` define constraint, so it never ships in a player build. Test fixtures that must be discoverable as registry items (for discovery tests) live in a separate `ReleaseGuard.Editor.TestFixtures` assembly under `Tests/Editor/Fixtures`.

Run the tests from `Window > General > Test Runner`, in the EditMode tab, with `UnityDevHost` open. Existing coverage includes the exclusion matcher, the configuration resolver, the report aggregates, the release-forbidden auditor, settings, and registry loading.

## Repository CI

The repository has two GitHub Actions workflows:

- `.github/workflows/validate.yml` parses `release-guard/package.json`, every `.json`, and every
  `.asmdef`, and verifies `release-guard/Documentation~/index.md` exists. This workflow does not
  require a Unity license.
- `.github/workflows/test.yml` runs the package EditMode tests through GameCI against
  `UnityDevHost`. It uses the `unityci/editor:ubuntu-6000.4.8f1-base-3` image. The package
  minimum remains Unity `2022.3`; CI runs on Unity 6 so the test host also exercises newer Editor
  APIs such as Build Profiles.

The test workflow needs Unity activation secrets before it can pass:

- `UNITY_LICENSE` containing a `.ulf` file for a Personal license, or
- `UNITY_SERIAL`, `UNITY_EMAIL`, and `UNITY_PASSWORD` for a Plus/Pro license.

GitHub does not expose repository secrets to forked pull requests, so the test workflow skips those
fork PRs instead of failing with a missing-license error. Use `workflow_dispatch` or a same-repo
branch when you need to run the full EditMode suite in CI.

## Built-in registries

The built-in sets are intentionally explicit static lists, not discovered via `TypeCache`. This keeps the shipped set auditable at a glance and prevents test fixtures (which `TypeCache` would include in the Editor domain when the Test Framework is installed) from leaking into real audit runs.

- `BuiltInAuditorRegistry.GetAll()` returns a fresh array of every shipped `ReleaseAuditor`, in canonical priority order.
- `BuiltInPostProcessorRegistry.GetAll()` returns every shipped `ReleasePostProcessor`.
- `BuiltInTransformerRegistry.GetAll()` returns every shipped `ReleaseTransformer` (currently empty - transformers are an extension point with no built-ins).

All three registry classes are `internal` to `ReleaseGuard.Editor`. They are contributor APIs,
not consumer APIs -- plugin authors cannot call `BuiltInAuditorRegistry.GetAll()` from their own
assemblies.

At startup `ReleaseGuardEnvironment.Initialize` feeds each `GetAll()` list into the matching `WeightedRegistry` through a registry definition, together with the auto-discover flag and the per-id "disabled" predicate from settings. The registry deduplicates by id (first registration for an id wins) and keeps items in priority-then-id order.

## Adding a built-in auditor

1. Create the auditor class under `Editor/Builtins/Auditor/`, deriving from `ReleaseAuditor`.
2. Implement `Id` with a stable, unique snake_case id (this is what users put in `disabledAuditorIds` to disable it).
3. Override `DisplayName` for a human-friendly name shown in the audit window (it defaults to the type name).
4. Optionally override `Priority` (lower runs first; built-ins default to `0`, and the scripting backend check uses `-10` to run first) and `ShouldRun` to gate by platform or settings.
5. Implement `Evaluate(ReleaseAuditContext context)` and report findings with `context.Error`, `context.Warning`, `context.Info`, or `context.Advisory`. Never touch the issue list directly - reporting through the context is what applies asset exclusions and auditor attribution.
6. Add one instantiation line for the new auditor to the array returned by `BuiltInAuditorRegistry.GetAll()`, placing it in the order group that matches its priority.
7. If the auditor is toggled by a settings field, add that field to `AuditorSettings` and read it inside `Evaluate` via `context.Settings`.
8. Add EditMode tests under `Tests/Editor` and run them in the dev host.

## Adding a built-in post-processor

1. Create the post-processor class under `Editor/Builtins/PostProcessor/`, deriving from `ReleasePostProcessor`.
2. Implement `Id` with a stable, unique snake_case id (this is what users put in `disabledPostProcessorIds` to disable it).
3. Override `DisplayName` for a human-friendly name (it defaults to the type name).
4. Optionally override `Priority` (lower runs first) and `ShouldRun`. Built-ins use priority to order their work - for example the debug symbol sweep runs at `0` and the build manifest at `100` so the manifest records the post-sweep state.
5. Implement `PostProcess(ReleasePostProcessContext context)` and record what you did with `context.Info`, `context.Warning`, and `context.Error`. Do not throw - the executor catches exceptions and records them as post-process errors, and a post-processor must never fail the (already successful) build.
6. Keep it non-destructive by default. Any modification of the build output must be opt-in via a settings field.
7. Add one instantiation line for the new post-processor to the array returned by `BuiltInPostProcessorRegistry.GetAll()`.
8. If the post-processor is configurable, add the settings fields to `PostProcessorSettings` and read them in `PostProcess`.
9. Add EditMode tests under `Tests/Editor` and run them in the dev host.

For project-specific (non-built-in) extensions, derive from the same base types in an Editor
assembly and register them through a `ReleaseGuardPlugin`. Auto-discovery is available for
prototypes, but explicit plugin registration is the recommended production path. Do not edit the
built-in registries for project-specific rules.
