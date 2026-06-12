# Release-forbidden code

Some code must never ship in a release build: debug hooks, cheat commands, test scaffolding, dev-only backdoors. Release Guard ships an attribute that marks such code and a built-in auditor that scans for it, so a release build fails before that code can reach production.

## The `[ReleaseForbidden]` attribute

> **Detection, not removal.** `[ReleaseForbidden]` does not strip, exclude, or conditionally
> compile anything. It is a build-time tripwire: if marked code exists in a shipping assembly
> when you run a release build, the build fails. The recommended practice is to also wrap the
> implementation in a `#if` guard so the code is physically excluded from the compiled release
> in addition to being flagged — but that is separate from the attribute itself.

`ReleaseForbidden` lives in the `ReleaseGuard` runtime assembly (`ReleaseGuard.Runtime`), so you can apply it from gameplay and runtime code without referencing any Editor-only types. The assembly is `autoReferenced`, so it is visible from your scripts with no asmdef changes. In C# attribute usage, write it as `[ReleaseForbidden]`.

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

The attribute does not strip anything from the build — it makes the build fail so you notice. Also wrap the implementation in a debug-only `#if` so the code is physically excluded from the compiled release in addition to being flagged.

## The auditor

The built-in `ReleaseForbiddenAuditor` (id `release_forbidden`, display name "Release-forbidden members") performs the scan.

### What it scans

> **Silent pass on compilation failure.** If `CompilationPipeline.GetAssemblies()` throws (which
> can happen in certain Editor states, such as immediately after a script error), the auditor
> catches the exception and silently returns an empty shipping set. This means it reports zero
> findings — as if no `[ReleaseForbidden]` members exist — rather than reporting an error. If
> you run a manual audit and see no `release_forbidden` findings when you expect some, check the
> Console for compilation errors and resolve them before trusting the audit result.

The auditor scans the assemblies that would ship in the player build. "Shipping" assemblies are
determined from `CompilationPipeline.GetAssemblies(AssembliesType.Player)`; their names form the
shipping set.

In practice this includes:
- Your runtime assemblies in `Assets/` (any asmdef without an Editor-only `includePlatforms`).
- Runtime assemblies from packages in `Packages/` (same rule).
- Any assembly that compiles into the player without an Editor-only or `UNITY_EDITOR` constraint.

It explicitly **excludes**:
- Editor-only assemblies (those with `"Editor"` in `includePlatforms` or under an `Editor/`
  folder without an explicit asmdef).
- Test assemblies (those gated by `UNITY_INCLUDE_TESTS`).

The auditor then walks every currently loaded assembly whose name is in the shipping set and
inspects its types and members. For each type it checks the type-level attribute and every
declared method, field, and property (public and non-public, instance and static, declared-only)
for `[ReleaseForbidden]`. Every match is reported at the severity carried by the attribute, with
the member's full name and the optional reason.

If a finding is at or above the failure threshold, the build is blocked like any other finding.

### Per-assembly exclusion

Sometimes a third-party assembly you cannot modify legitimately contains a `[ReleaseForbidden]` member. To stop the auditor flagging it, add the assembly name to `releaseForbiddenExcludedAssemblies` on the Auditors settings page. Enter names without the `.dll` extension - for example `MyPlugin.Runtime`; matching is case-insensitive. Any assembly in that list is skipped entirely by this auditor.

This per-assembly exclusion is independent of the [asset-path exclusion list](asset-exclusions.md): release-forbidden findings are attributed to a code member, not an asset path, so they are not affected by the gitignore-style patterns.

## Workflow

1. Mark debug-only or dangerous code with `[ReleaseForbidden]`, giving a clear `reason`.
2. Wrap the implementation in a debug-only `#if` where practical so it is also physically excluded.
   The most common guards:
   - `UNITY_EDITOR` — code exists only in the Editor. Use this for Editor-only helpers.
   - `DEVELOPMENT_BUILD` — code compiles into a player only when `Development Build` is checked.
   - `UNITY_EDITOR || DEVELOPMENT_BUILD` — the typical combination that covers both Editor and
     development player builds.

   ```csharp
   using ReleaseGuard;

   // The attribute fires during the build gate if the code is in a shipping assembly.
   // The #if ensures the code is not compiled into a release player at all.
   [ReleaseForbidden(ReleaseIssueSeverity.Error, "Gives infinite money")]
   #if UNITY_EDITOR || DEVELOPMENT_BUILD
   public static void GrantAllCurrency() { /* ... */ }
   #endif
   ```

   Without the `#if`, a release build still fails — but the code exists in the compiled assembly
   until compilation is excluded. With the `#if`, the code simply does not compile into release
   builds, so the attribute is an additional safety net rather than the only gate.

3. Run an audit from `Tools > Release Guard > Audit` to confirm the marker is detected.
4. Make a release build. The build fails listing every release-forbidden member that would have shipped.
5. Remove or guard the code, then rebuild. Only add an assembly to `releaseForbiddenExcludedAssemblies` when you cannot change the offending assembly yourself.
