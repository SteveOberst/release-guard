# Release-forbidden code

Some code must never ship in a release build: debug hooks, cheat commands, test scaffolding, dev-only backdoors. Release Guard ships an attribute that marks such code and a built-in auditor that scans for it, so a release build fails before that code can reach production.

## The `[ReleaseForbidden]` attribute

`ReleaseForbiddenAttribute` lives in the `ReleaseGuard` runtime assembly (`ReleaseGuard.Runtime`), so you can apply it from gameplay and runtime code without referencing any Editor-only types. The assembly is `autoReferenced`, so it is visible from your scripts with no asmdef changes.

Apply it to a type or member that must not ship:

```csharp
using ReleaseGuard;

[ReleaseForbidden(ReleaseIssueSeverity.Error, "Gives infinite money")]
public static void GrantAllCurrency() { /* ... */ }
```

Constructor parameters:

- `severity` - how serious shipping the member is. Defaults to `ReleaseIssueSeverity.Error`, which blocks a release build under the default failure threshold. Use `Warning` or `Info` for less serious markers.
- `reason` - an optional human-readable explanation. When present it is appended to the reported message and the Console log.

Valid targets: classes, structs, enums, methods, fields, and properties. The attribute is not inherited (`Inherited = false`), so a subclass does not pick up a base type's marker.

Marking code is detection, not removal. The attribute does not strip anything from the build - it makes the build fail so you notice. The recommended practice is to also wrap the implementation in a debug-only `#if` so the code is physically excluded from the compiled release in addition to being flagged.

## The auditor

The built-in `ReleaseForbiddenAuditor` (id `release_forbidden`, display name "Release-forbidden members") performs the scan.

### What it scans

The auditor scans the assemblies that would ship in the player build. "Shipping" assemblies are determined from `CompilationPipeline.GetAssemblies(AssembliesType.Player)`; their names form the shipping set. The auditor then walks every currently loaded assembly whose name is in that set and inspects its types and members. For each type it checks the type-level attribute and every declared method, field, and property (public and non-public, instance and static, declared-only) for `[ReleaseForbidden]`. Every match is reported at the severity carried by the attribute, with the member's full name and the optional reason.

If a finding is at or above the failure threshold, the build is blocked like any other finding.

### Per-assembly exclusion

Sometimes a third-party assembly you cannot modify legitimately contains a `[ReleaseForbidden]` member. To stop the auditor flagging it, add the assembly name to `releaseForbiddenExcludedAssemblies` on the Auditors settings page. Names are compared case-insensitively and without the `.dll` extension - for example `MyPlugin.Runtime`. Any assembly in that list is skipped entirely by this auditor.

This per-assembly exclusion is independent of the [asset-path exclusion list](asset-exclusions.md): release-forbidden findings are attributed to a code member, not an asset path, so they are not affected by the gitignore-style patterns.

## Workflow

1. Mark debug-only or dangerous code with `[ReleaseForbidden]`, giving a clear `reason`.
2. Wrap the implementation in a debug-only `#if` where practical so it is also physically excluded.
3. Run an audit from `Tools > Release Guard > Audit` to confirm the marker is detected.
4. Make a release build. The build fails listing every release-forbidden member that would have shipped.
5. Remove or guard the code, then rebuild. Only add an assembly to `releaseForbiddenExcludedAssemblies` when you cannot change the offending assembly yourself.
