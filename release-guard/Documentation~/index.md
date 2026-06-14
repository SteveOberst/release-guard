# Release Guard Documentation

Release Guard is a Unity editor package that evaluates builds in three lifecycle events:

- `pre-build`: blocking checks that run before the player is written
- `build`: post-build handlers that run after a successful build
- `post-build`: final output-folder handlers that run last

Release Guard uses one component model. A `ReleaseGuardComponent` can subscribe to one event, several events, or all of them.

## Concept map

Use this as the compact mental model:

1. Release Guard initializes in the editor domain and seeds assets under `Assets/ReleaseGuard/`.
2. The registry chooses which profile settings asset applies to a real build.
3. Components are the units of build logic.
4. Plugins are the explicit way to register components and optional plugin settings.
5. The checks window dispatches only the `pre-build` event.
6. Real builds dispatch `pre-build`, then `build`, then `post-build`.

## Start here

- [Quickstart](quickstart.md)  
  Install the package, understand the default profiles, run the checks window, and block your first bad build.

- [Configuring](configuring.md)  
  Minimal map of the Project Settings pages and the component-specific settings they control.

- [CI integration](guides/ci-integration.md)  
  How Release Guard behaves in batchmode, how profile selection works in CI, and how to use the build manifest artifact.

- [Build profiles](build-profiles.md)  
  How Release Guard chooses which settings asset to use for a real build, how the editing profile differs from the build-time profile, and how to create custom profiles safely.

## Core guides

- [Pre-Build Checks window](guides/checks-window.md)
- [Asset exclusions](guides/asset-exclusions.md)
- [Release-forbidden code](guides/release-forbidden.md)
- [Custom components](guides/custom-components.md)
- [Plugin extension workflow](guides/plugin-extension-workflow.md)
- [Advanced plugin settings](guides/advanced-plugin-settings.md)

## API reference

- [Plugins](api/plugins.md)
- [Plugin settings and settings UI](api/settings.md)
- [Runtime attributes](reference/attributes.md)

## Built-in components

- [Built-in components overview](reference/components.md)

Pre-build components:

- [Android debuggable templates](reference/components/android-debuggable.md)
- [Broad preserve rules](reference/components/broad-preserve.md)
- [Burst AOT debug settings](reference/components/burst-debug.md)
- [Development build in CI](reference/components/ci-development-build.md)
- [Development build disabled](reference/components/development-build.md)
- [Insecure HTTP option](reference/components/insecure-http.md)
- [Managed code stripping](reference/components/managed-stripping.md)
- [Profiler connection disabled](reference/components/profiler-connection.md)
- [Release-forbidden members](reference/components/release-forbidden.md)
- [Script debugging disabled](reference/components/script-debugging.md)
- [Scripting backend (IL2CPP)](reference/components/scripting-backend.md)
- [Stack trace log types](reference/components/stack-trace-type.md)
- [Engine code stripping](reference/components/strip-engine-code.md)
- [WebGL exception support](reference/components/webgl-exception-support.md)

Post-build components:

- [Debug symbol sweep](reference/components/debug-symbol-sweep.md)
- [Build manifest](reference/components/build-manifest.md)

## Development and internals

- [Development notes](development.md)
- [Built-in settings reference](reference/settings.md)
