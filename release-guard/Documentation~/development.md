# Development Notes

This page describes the current implementation, not the historical one.

## Runtime architecture

Release Guard is built around four pieces:

1. `ReleaseGuardEnvironment`  
   Owns the current profile settings, plugin list, component registry, event bus, and pipeline for the current editor domain.

2. `WeightedRegistry<ReleaseGuardComponent>`  
   The single registry for built-in, discovered, and plugin-contributed components.

3. `ReleaseGuardEventBus`  
   Stores event listeners registered by components. Listeners are sorted by event kind, then priority, then component id, then registration sequence.

4. `ReleaseGuardPipeline`  
   Generic dispatcher that runs `Dispatch<TEvent>()` and `DispatchWithResult<TEvent, TResult>()`.

## Root hooks

Only the Unity build hooks know about the three root lifecycle events:

- `ReleasePreBuildRunner`  
  Dispatches `ReleaseGuardPreBuildEvent` and throws `BuildFailedException` when the report crosses the failure threshold.

- `ReleaseBuildRunner`  
  Dispatches `ReleaseGuardBuildEvent` after a successful build, at `callbackOrder = 0`.

- `ReleasePostBuildRunner`  
  Dispatches `ReleaseGuardPostBuildEvent` after a successful build, at `callbackOrder = int.MaxValue`.

Those hooks are where the lifecycle differs. The dispatch logic itself is generic.

The practical distinction is:

- `build` is the first successful post-build hook. Use it for general "the build succeeded, now react to the produced output" work.
- `post-build` runs last. Use it for final output-folder work that should see the state after earlier post-build handlers have already run.

## Event model

Public registration happens through `ReleaseGuardComponentBinder`:

- `OnPreBuild(...)`
- `OnBuild(...)`
- `OnPostBuild(...)`

Each registration takes:

- a handler
- an optional `priority`

The binder stores listeners as typed `ReleaseGuardEventListener<TEvent>` instances. The event bus just keeps sorted listeners; it does not rebuild itself on every dispatch.

## Profiles

At startup, `ProfileMigration.Run()` guarantees that:

- the built-in `Release` profile exists
- the built-in `Development` profile exists
- both backing settings assets exist on disk

The active editing profile is stored in `EditorPrefs` by `ActiveProfileState`.

Actual builds do not care which profile you are editing. `ProfileSettingsResolver.ResolveForBuild(...)` walks the registry top-to-bottom and returns the first matching profile whose activation condition matches:

- `IsReleaseBuild`
- `IsDevelopmentBuild`
- `IsCI`
- `IsCIAndDevelopmentBuild`
- `UnityBuildProfileNames`
- `Always`

That is not a priority ladder baked into the runtime. It is just the set of available activation strategies. Registry order is what decides which matching profile wins.

## Migration behavior

At startup, `ProfileMigration.Run()` also handles upgrading older installs into the current profile-based model.

The important behavior for upgraders is:

- Release Guard seeds `registry.asset` and the two built-in profile settings assets if they do not exist yet.
- seeded `release.asset` and `development.asset` are populated with the current built-in defaults.
- the active editing profile is stored separately in `EditorPrefs` and does not affect build-time profile selection.

## Discovery rules

TypeCache-based auto-discovery for components and plugins only considers types that are:

- non-abstract
- constructible with a public parameterless constructor
- not in the package assembly
- not in sub-assemblies whose names start with the package assembly prefix
- not marked as test fixtures

Disabled ids are enforced centrally by registry guards, so built-ins, discovered types, and plugin registrations all go through the same suppression path.

## Files worth reading

- `Editor/Core/Runtime/ReleaseGuardEnvironment.cs`
- `Editor/Core/Runtime/ReleaseGuardPipeline.cs`
- `Editor/Core/Components/ReleaseGuardEventBus.cs`
- `Editor/Core/Components/ReleaseGuardLifecycleEvents.cs`
- `Editor/Config/ReleaseGuardSettings.cs`
- `Editor/Config/ReleaseGuardRegistry.cs`
- `Editor/Config/ProfileMigration.cs`
- `Editor/Hooks/*.cs`
- `Editor/Builtins/*`

## What still matters when extending the package

- Use component ids as stable public identifiers.
- Keep build-time side effects inside event handlers, not in `Register(...)`.
- Prefer explicit plugin registration via `[InitializeOnLoad]` over discovery unless zero-config onboarding really matters.
- Treat the build manifest as a CI artifact, not something that should end up next to player-delivered files.
