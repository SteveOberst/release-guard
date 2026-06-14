# Release-forbidden Code

`[ReleaseForbidden]` is the package's explicit "this must not ship" marker.

It is useful for code that is too dangerous or too embarrassing to accidentally leave in a release build:

- debug menus
- admin commands
- cheat hooks
- developer backdoors
- temporary test scaffolding

## What the attribute is

The attribute lives in the runtime assembly:

`ReleaseGuard.Runtime`

That means gameplay code can use it without any editor-only dependency.

Example:

```csharp
using ReleaseGuard;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
[ReleaseForbidden(ReleaseIssueSeverity.Error, "Debug-only currency grant")]
public static void GrantAllCurrency() { }
#endif
```

Constructor parameters:

- `severity`  
  defaults to `ReleaseIssueSeverity.Error`
- `reason`  
  optional text appended to the reported message

## What the built-in component does

The built-in component is:

- id: `release_forbidden`
- event: `pre-build`

It scans player-shipping assemblies only. That point matters a lot: the component is trying to answer "would this reach the player build?", not "is this loaded somewhere in the editor domain?"

## What counts as a shipping assembly

The component asks Unity's compilation pipeline for `AssembliesType.Player`, then inspects the currently loaded assemblies whose names are in that shipping set.

That generally includes:

- runtime asmdefs in `Assets/`
- runtime package assemblies

and excludes:

- editor-only assemblies
- test assemblies

## What gets scanned

For each matched assembly, the component inspects:

- type attributes
- declared methods
- declared fields
- declared properties

using public and non-public, instance and static members.

## What the attribute does not do

It does not remove code from the player build.

It is a build-time tripwire:

- if the code would ship, Release Guard reports it
- if the report crosses the threshold, the build fails

That is why the recommended pattern is both:

1. mark the code with `[ReleaseForbidden]`
2. wrap the implementation in `#if UNITY_EDITOR || DEVELOPMENT_BUILD` or a similar compile-time guard

The preprocessor guard prevents compilation into release players. The attribute catches mistakes when that guard is missing, removed, or incomplete.

## Third-party assemblies

If a third-party runtime assembly legitimately contains `[ReleaseForbidden]` members and you cannot change it, add its assembly name to the `release_forbidden` component settings entry inside `components.componentToggles`.

Use the assembly name without `.dll`, for example:

`MyPlugin.Runtime`

This exclusion is specific to the release-forbidden scan. Asset exclusions do not apply because most release-forbidden findings are member-based rather than asset-path based.

## Failure modes worth knowing

If Unity's compilation pipeline is unavailable in a bad editor state, the component falls back to an empty shipping set instead of guessing. In practice that means:

- no release-forbidden findings appear
- you should not trust that result until compilation is healthy again

If a loaded assembly throws `ReflectionTypeLoadException`, the component still scans the loadable subset of types rather than failing the entire run.

## Related docs

- [Runtime attributes](../reference/attributes.md)
- [Release-forbidden members](../reference/components/release-forbidden.md)
