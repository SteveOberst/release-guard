# Custom Components

The extension point is `ReleaseGuardComponent`.

A component can subscribe to any combination of:

- `OnPreBuild(...)`
- `OnBuild(...)`
- `OnPostBuild(...)`

That is the intended model now. Treat a component as one unit of build logic that can subscribe wherever it needs to.

The practical distinction between the two successful post-build events is:

- `OnBuild(...)` runs first after a successful build.
- `OnPostBuild(...)` runs last.

Use `OnBuild(...)` for general post-build reactions that do not need to be the final step. Use `OnPostBuild(...)` for final output-folder work that should run after earlier build-phase handlers.

## Canonical handler shape

The binder API is event-based.

That means the authoritative signatures are:

```csharp
void OnPreBuild(ReleaseGuardPreBuildEvent releaseEvent)
void OnBuild(ReleaseGuardBuildEvent releaseEvent)
void OnPostBuild(ReleaseGuardPostBuildEvent releaseEvent)
```

Then you reach the phase-specific context through the event:

- `releaseEvent.Context`

If you see older examples that take a context object directly, treat them as outdated.

## Minimal pre-build component

```csharp
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.PreBuild;
using UnityEditor;

public sealed class CompanyNameComponent : ReleaseGuardComponent
{
    public override string Id => "com.example.company_name";
    public override string DisplayName => "Company name configured";

    public override void Register(ReleaseGuardComponentBinder binder)
    {
        binder.OnPreBuild(OnPreBuild);
    }

    private static void OnPreBuild(ReleaseGuardPreBuildEvent releaseEvent)
    {
        var context = releaseEvent.Context;

        if (string.IsNullOrWhiteSpace(PlayerSettings.companyName) ||
            PlayerSettings.companyName == "DefaultCompany")
        {
            context.Error(
                "Company name is unset or still 'DefaultCompany'.",
                fixHint: "Set Project Settings > Player > Company Name.");
        }
    }
}
```

## One component, multiple phases

```csharp
using ReleaseGuard.Editor.Core.Build;
using ReleaseGuard.Editor.Core.Components;
using ReleaseGuard.Editor.Core.PostBuild;
using ReleaseGuard.Editor.Core.PreBuild;

public sealed class SymbolsPolicyComponent : ReleaseGuardComponent
{
    public override string Id => "com.example.symbols-policy";
    public override string DisplayName => "Symbols policy";

    public override void Register(ReleaseGuardComponentBinder binder)
    {
        binder.OnPreBuild(OnPreBuild);
        binder.OnPostBuild(OnPostBuild, priority: 200);
    }

    private static void OnPreBuild(ReleaseGuardPreBuildEvent releaseEvent)
    {
        var context = releaseEvent.Context;
        context.Warning("Example pre-build check.");
    }

    private static void OnPostBuild(ReleaseGuardPostBuildEvent releaseEvent)
    {
        var context = releaseEvent.Context;
        context.Info("Example post-build action.");
    }
}
```

## Assembly/reference model

| What you are writing | Assembly type | Typical references |
|---|---|---|
| Gameplay code using `[ReleaseForbidden]` | runtime asmdef or no asmdef | `ReleaseGuard.Runtime` |
| Custom `ReleaseGuardComponent` | Editor asmdef | `ReleaseGuard.Editor`, often `ReleaseGuard.Runtime` |
| Custom `ReleaseGuardPlugin` | Editor asmdef | `ReleaseGuard.Editor`, often `ReleaseGuard.Runtime` |
| `ReleaseGuardPluginSettings` | Editor asmdef | `ReleaseGuard.Editor`, `ReleaseGuard.Runtime` if you use `ReleaseIssueSeverity` |

In practice, most extension authors want one Editor-only asmdef that references:

- `ReleaseGuard.Editor`
- `ReleaseGuard.Runtime`

## `priority`

Use `priority` only when ordering really matters.

Ordering rules inside the event bus are:

1. event kind
2. priority
3. component id
4. registration sequence

Lower priority values run earlier.

## Registration choices

You have two supported ways to get a component into the environment:

### Recommended: explicit plugin registration

Create a `ReleaseGuardPlugin`, register the component there, and call:

`ReleaseGuardDI.Resolve<ReleaseGuardEnvironment>().RegisterPlugin(new MyPlugin())`

from an `[InitializeOnLoad]` loader.

This gives you a stable plugin id, optional plugin settings, and predictable initialization order.

### Optional: auto-discovery

Enable `components.autoDiscoverComponents`.

Then Release Guard uses TypeCache to instantiate concrete `ReleaseGuardComponent` subclasses with a public parameterless constructor that are outside the package assembly and not marked as test fixtures.

This is convenient, but less explicit.

## Important behavioral constraints

- `Register(...)` should only subscribe handlers. Do not perform build work there.
- ids must be stable and unique
- component failures during event dispatch are logged
- pre-build findings can block a build
- build and post-build failures are non-blocking by default

## Next step

If you need multiple components, plugin identity, or a settings page, continue with [Plugins](../api/plugins.md).
If you want the simple end-to-end plugin path, continue with [Plugin extension workflow](plugin-extension-workflow.md).
If you want the deeper settings discussion, continue with [Advanced plugin settings](advanced-plugin-settings.md).
