# Plugins

A plugin is the recommended way to extend Release Guard. Derive from `ReleaseGuardPlugin`,
register it with `[InitializeOnLoad]`, and contribute your custom
[auditors](custom-auditors.md), [post-processors](custom-post-processors.md), and
[transformers](custom-transformers.md) inside its `Register` method. Release Guard loads
plugins while initializing the Editor-domain `ReleaseGuardEnvironment` and calls each
plugin's `Register` once.

Plugins are first-class in the tooling: every registered plugin appears by name in the
Release Guard window's Plugins foldout, with its id, display name, author, and a list of
everything it contributed. That visibility makes it easy to confirm your extension is active
and to diagnose conflicts when multiple plugins contribute to the same registry.

## Plugin vs direct registration

You can also subclass `ReleaseAuditor`, `ReleasePostProcessor`, or `ReleaseTransformer`
directly and register the instance in the same `[InitializeOnLoad]` pattern without wrapping
it in a plugin. That works for a single-item contribution with no settings and no need for
tooling visibility. As soon as any of the following apply, use a plugin instead:

- contributing multiple items from one identifiable entry point;
- attaching author metadata visible in the Release Guard window;
- reading settings inside contributed items (pass settings into the constructor);
- exposing a Project Settings sub-page for user configuration.

## The plugin base class

`ReleaseGuardPlugin` (namespace `ReleaseGuard.Editor.Core.Plugins`) is the abstract
base.

| Member | Kind | Signature | Notes |
| --- | --- | --- | --- |
| `PluginId` | abstract | `public abstract string PluginId { get; }` | Stable, unique id. Used for logging and disabling. Recommended reverse-domain format, e.g. `"com.example.my-plugin"`. |
| `DisplayName` | abstract | `public abstract string DisplayName { get; }` | Shown in the Plugins foldout. |
| `Author` | virtual | `public virtual string Author { get; }` | Defaults to `null`. Optional, shown in the Plugins foldout. |
| `SettingsType` | virtual | `public virtual System.Type SettingsType { get; }` | Defaults to `null` (no settings). Return `typeof(YourSettings)` to opt into the settings system. |
| `Register` | abstract | `public abstract void Register(PluginRegistrationContext context)` | Called once per environment init. Register contributions; keep it side-effect free otherwise. |
| `GetSettings()` | concrete | `public ReleaseGuardPluginSettings GetSettings()` | The loaded settings instance, or `null`. |
| `GetSettings<T>()` | concrete | `public T GetSettings<T>() where T : ReleaseGuardPluginSettings` | Typed convenience overload. `null` if no settings or wrong type. |

`PluginRegistrationContext` (same namespace) exposes one member:

```csharp
public ReleaseGuardEnvironment ReleaseGuard { get; }
```

Register contributions through `context.ReleaseGuard.Registries`:

```csharp
context.ReleaseGuard.Registries.Auditors.Register(new MyAuditor());
context.ReleaseGuard.Registries.PostProcessors.Register(new MyPostProcessor());
context.ReleaseGuard.Registries.Transformers.Register(new MyTransformer());
```

`Register(item)` returns `bool` and extracts the key from `item.Id`. The first
registration for a given id wins; a later registration with the same id is a
silent no-op that returns `false`. Items are kept in priority-then-id order.

## Full example

This is the shape used by the shipped sample plugin. The plugin, its settings, and
its auditor all live in one Editor-only assembly with an asmdef that references
`ReleaseGuard.Editor` and `ReleaseGuard.Runtime`.

```csharp
using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Plugins;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;

namespace ExamplePlugin
{
    public sealed class ExamplePlugin : ReleaseGuardPlugin
    {
        public override string PluginId    => "com.example.example-plugin";
        public override string DisplayName => "Example Plugin";
        public override string Author      => "Researchy Development";
        public override System.Type SettingsType => typeof(ExamplePluginSettings);

        public override void Register(PluginRegistrationContext context)
        {
            var settings = GetSettings<ExamplePluginSettings>();
            context.ReleaseGuard.Registries.Auditors.Register(new ExampleAuditor(settings));
        }
    }

    [InitializeOnLoad]
    internal static class ExamplePluginLoader
    {
        static ExamplePluginLoader()
        {
            DI.Resolve<ReleaseGuardEnvironment>().RegisterPlugin(new ExamplePlugin());
        }
    }
}
```

The contributed auditor reads the plugin settings to decide whether to run:

```csharp
using ReleaseGuard.Editor.Core.Audit;

namespace ExamplePlugin
{
    public sealed class ExampleAuditor : ReleaseAuditor
    {
        private readonly ExamplePluginSettings _settings;

        public ExampleAuditor(ExamplePluginSettings settings) => _settings = settings;

        public override string Id          => "com.example.example_auditor";
        public override string DisplayName => "Example Auditor";

        public override bool ShouldRun(ReleaseAuditContext context) =>
            _settings != null && _settings.strictMode;

        public override void Evaluate(ReleaseAuditContext context)
        {
            context.Report(
                _settings?.findingSeverity ?? ReleaseGuard.ReleaseIssueSeverity.Warning,
                "Example auditor fired.");
        }
    }
}
```

See [settings.md](settings.md) for the `ExamplePluginSettings` definition.

## InitializeOnLoad registration pattern

The explicit, recommended registration path is an `[InitializeOnLoad]` static
class in your own assembly that resolves the environment and calls
`RegisterPlugin`:

```csharp
[InitializeOnLoad]
internal static class MyPluginLoader
{
    static MyPluginLoader()
    {
        DI.Resolve<ReleaseGuardEnvironment>().RegisterPlugin(new MyPlugin());
    }
}
```

`DI` is `ReleaseGuard.Editor.Core.DI.DI`; `ReleaseGuardEnvironment` is
`ReleaseGuard.Editor.Core.Runtime.ReleaseGuardEnvironment`.

**Ordering contract.** Release Guard initializes its environment synchronously in
its own `[InitializeOnLoad]` static constructor
(`ReleaseGuard.Editor.Core.Runtime.ReleaseGuardStartup`). Unity runs static
constructors in assembly-dependency order, so any assembly that has an explicit
asmdef dependency on `ReleaseGuard.Editor` is guaranteed to run its
`[InitializeOnLoad]` **after** Release Guard has finished initializing. The
environment is therefore fully initialized by the time your loader calls
`RegisterPlugin`. This is why your plugin asmdef must reference
`ReleaseGuard.Editor` even though the type is auto-referenced for source
visibility.

## RegisterPlugin safety behavior

`ReleaseGuardEnvironment.RegisterPlugin` is the documented dynamic registration
entry point:

```csharp
public bool RegisterPlugin(ReleaseGuardPlugin plugin)
```

It never throws. It returns `false` (and registers nothing) when:

- `plugin` is `null`;
- it is called before the environment finished initializing (this also logs a
  warning telling you to add the explicit asmdef dependency described above);
- the plugin returns an empty `PluginId`;
- a plugin with the same id (case-insensitive) is already registered (duplicate);
- the plugin id is listed in
  `Plugins > Discovery > Disabled Plugin Ids` (disabled in settings).

On success it wires settings (loading or creating the settings asset),
calls `plugin.Register(...)`, and adds the plugin to the environment's plugin
list. If `Register` itself throws, the exception is caught and logged - the plugin
is still added.

## Auto-discovery (not recommended for production)

`Plugins > Discovery > Auto Discover Plugins` tells Release Guard to discover every
non-abstract `ReleaseGuardPlugin` subclass via TypeCache and invoke it without any explicit
registration. This is off by default and should stay off in most projects.

The reasons to prefer explicit `[InitializeOnLoad]` registration over auto-discovery:

- **Predictability.** The `[InitializeOnLoad]` pattern gives you full control over when and
  whether a plugin loads. Auto-discovery runs unconditionally at domain startup, which can
  produce surprising behavior if a plugin class exists in a test or experimental assembly.
- **No scanning overhead.** TypeCache scans all loaded assemblies; `[InitializeOnLoad]` is
  a direct constructor call with no scanning.
- **No constructor constraint.** Explicit registration lets you construct the plugin with
  arguments. Auto-discovered plugins require a public parameterless constructor.

Auto-discovery is useful for rapid prototyping or zero-configuration scenarios where you want
a plugin to activate by simply existing in the project. For anything you ship, prefer the
`[InitializeOnLoad]` pattern.

The same flags exist for individual item types (`autoDiscoverAuditors`,
`autoDiscoverPostProcessors`, `autoDiscoverTransformers`). They are independent of plugin
discovery: a plugin's `Register` runs regardless of those flags, and those flags do not
affect items registered through a plugin.

## Disabling and identity

- Disable a whole plugin (all its contributions) by adding its `PluginId` to
  `Plugins > Discovery > Disabled Plugin Ids`.
- `PluginId`, `DisplayName`, and `Author` are surfaced in the Release Guard
  window's Plugins foldout.

## Settings wiring (SettingsType)

Override `SettingsType` and return `typeof(YourSettings)` to opt into the settings
system. The framework then loads or creates the per-plugin asset at
`Assets/ReleaseGuard/Plugins/{PluginId}.asset` and generates a Project Settings
sub-page for it automatically. Inside `Register` (or any contributed item) read it
via `GetSettings<YourSettings>()`. Full details, field widgets, and attributes are
in [settings.md](settings.md).
