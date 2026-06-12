# Custom Post-Processors

A post-processor operates on the finished build output: cleaning up debug
artifacts, writing CI metadata, patching manifests, and similar output-folder
operations. Post-processors run via Unity's `IPostprocessBuildWithReport` after
the build succeeds and after every [transformer](custom-transformers.md), so they
always see the final, transformed state of the build folder.

Derive from `ReleasePostProcessor` in any Editor-platform assembly. The post-processor only
runs after you either register it through a plugin or enable post-processor auto-discovery in
Project Settings. Auto-discovery is off by default; explicit plugin registration is the
recommended production path. Exceptions thrown from a post-processor are caught by the executor
and recorded as post-process errors, so one bad post-processor never silently prevents others from running.
Keep implementations non-destructive by default; make any modification of build output opt-in
via settings.

See also: [built-in post-processors](../reference/built-in-post-processors.md),
[plugins](plugins.md).

## Minimal post-processor

```csharp
using ReleaseGuard.Editor.Core.PostProcessing;

public sealed class MyPostProcessor : ReleasePostProcessor
{
    public override string Id => "myteam.my_postprocessor";

    public override void PostProcess(ReleasePostProcessContext context)
    {
        context.Info($"Post-processing {context.OutputPath}");
    }
}
```

## Base class members

`ReleasePostProcessor` (namespace `ReleaseGuard.Editor.Core.PostProcessing`) is the
abstract base. It implements `IReleaseGuardRegistryItem`.

| Member | Kind | Signature | Notes |
| --- | --- | --- | --- |
| `Id` | abstract | `public abstract string Id { get; }` | Stable, unique snake_case id. Used for logging, disabling, and de-duplication. |
| `DisplayName` | virtual | `public virtual string DisplayName { get; }` | Defaults to the type name. |
| `Priority` | virtual | `public virtual int Priority { get; }` | Default `0`. Lower runs first. Negative runs before built-ins (all default to `0`). |
| `ShouldRun` | virtual | `public virtual bool ShouldRun(ReleasePostProcessContext context)` | Default `true`. Return `false` to skip this run. |
| `PostProcess` | abstract | `public abstract void PostProcess(ReleasePostProcessContext context)` | Do the work; record via the context. Do not throw. |

## The post-process context

`ReleasePostProcessContext` (namespace `ReleaseGuard.Editor.Core.PostProcessing`).

### Logging methods

| Method | Signature |
| --- | --- |
| Info | `public void Info(string message)` |
| Warning | `public void Warning(string message)` |
| Error | `public void Error(string message)` |

Each entry is automatically attributed to the running post-processor. Unlike the
audit context, these methods take only a message - there is no asset-path or
fix-hint overload.

### Properties

| Member | Signature | Null when |
| --- | --- | --- |
| `Settings` | `public ReleaseGuardSettings Settings { get; }` | Never null. Project-wide Release Guard settings. |
| `BuildReport` | `public BuildReport BuildReport { get; }` | `null` when running against an existing build output (outside an active Unity build). Always null-check. |
| `BuildTarget` | `public BuildTarget BuildTarget { get; }` | Always set, regardless of whether a `BuildReport` is present. |
| `OutputPath` | `public string OutputPath { get; }` | Always set. Path to the built product, e.g. `Builds/Windows/MyGame.exe`. |

**No `Configuration` property.** Unlike the audit context, the post-process context exposes
only `Settings` (the raw settings asset) - there is no `Configuration` property giving the
profile-resolved effective values. If your post-processor needs the effective failure threshold
or build profile name for the current run, read them from `Settings.general.failureThreshold`
and use `DI.Resolve<ReleaseGuardEnvironment>().ResolveConfiguration(BuildReport)` if you need
the fully resolved runtime state.

For file outputs, the build output directory is
`System.IO.Path.GetDirectoryName(context.OutputPath)`. For folder outputs (for
example WebGL), `OutputPath` is the product folder itself. Check
`Directory.Exists(context.OutputPath)` before falling back to `Path.GetDirectoryName`.
`Settings` is a
`ReleaseGuard.Editor.Config.ReleaseGuardSettings`; `BuildReport` and `BuildTarget`
come from `UnityEditor.Build.Reporting` / `UnityEditor`.

## Priority and ShouldRun

Lower `Priority` runs first. `ShouldRun` gates the post-processor - for example
limit it to a platform or to a settings flag.

```csharp
using ReleaseGuard.Editor.Core.PostProcessing;
using UnityEditor;

public sealed class AndroidManifestPatcher : ReleasePostProcessor
{
    public override string Id => "myteam.android_manifest_patcher";
    public override int Priority => 10; // run after built-ins

    public override bool ShouldRun(ReleasePostProcessContext context) =>
        context.BuildTarget == BuildTarget.Android;

    public override void PostProcess(ReleasePostProcessContext context)
    {
        context.Info($"Patching manifest for {context.OutputPath}");
    }
}
```

## Realistic example

```csharp
using System.IO;
using ReleaseGuard.Editor.Core.PostProcessing;

public sealed class BuildTimestampPostProcessor : ReleasePostProcessor
{
    public override string Id          => "myteam.build_timestamp";
    public override string DisplayName => "Write build timestamp";

    public override void PostProcess(ReleasePostProcessContext context)
    {
        var dir = ResolveOutputFolder(context.OutputPath);
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
        {
            context.Warning("Could not resolve the build output directory.");
            return;
        }

        var stamp = System.DateTime.UtcNow.ToString("o");
        File.WriteAllText(Path.Combine(dir, "build-timestamp.txt"), stamp);
        context.Info($"Wrote build-timestamp.txt ({stamp}).");
    }

    private static string ResolveOutputFolder(string outputPath) =>
        Directory.Exists(outputPath) ? outputPath : Path.GetDirectoryName(outputPath);
}
```

## Registration options

- **Auto-discovery.** Enable
  `Post-Processors > Discovery > Auto Discover Post-Processors` in Project
  Settings. Every non-abstract `ReleasePostProcessor` (excluding test fixtures and
  built-in types) is then run after a release build. Requires a public
  parameterless constructor.
- **Via a plugin.** Register inside a `ReleaseGuardPlugin` when you need
  constructor arguments or want to group contributions. See [plugins.md](plugins.md):

  ```csharp
  context.ReleaseGuard.Registries.PostProcessors.Register(new MyPostProcessor());
  ```

Disable a registered or discovered post-processor by adding its `Id` to
`Post-Processors > Discovery > Disabled Post-Processor Ids`.
