# Runtime Attributes

## `ReleaseForbidden`

Namespace:

`ReleaseGuard`

Purpose:

Marks a type or member that must not ship in a release build.

Supported targets:

- class
- struct
- enum
- method
- field
- property

Constructor:

```csharp
[ReleaseForbidden(
    ReleaseIssueSeverity severity = ReleaseIssueSeverity.Error,
    string reason = null)]
```

Behavior:

- the attribute itself lives in the runtime assembly, so gameplay code can reference it without any editor dependency
- the built-in `release_forbidden` component scans player-shipping assemblies for it during the `pre-build` event
- the severity used in the report comes from the attribute instance

Recommended pattern:

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
[ReleaseForbidden(ReleaseIssueSeverity.Error, "Debug-only admin command")]
public static void GrantAllCurrency() { }
#endif
```

Why both pieces matter:

- the attribute makes Release Guard fail the build if the member would ship
- the preprocessor guard stops the code from being compiled into the release player in the first place

See [Release-forbidden code](../guides/release-forbidden.md).
