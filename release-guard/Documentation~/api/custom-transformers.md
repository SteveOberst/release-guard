# Custom Transformers

A transformer modifies the build artifacts at a low level - IL manipulation,
binary patching, code obfuscation, native-library processing, and similar.
Transformers run via Unity's `IPostprocessBuildWithReport` after the build
succeeds and **before** [post-processors](custom-post-processors.md), so their
output is what the post-processor pipeline (cleanup, manifest writing, and so on)
then operates on.

No built-in transformers ship with Release Guard - this is the base type for
advanced, project-specific build hardening beyond the auditor checks. Exceptions
are caught by the executor and turned into warnings, so one bad transformer never
silently prevents others from running. Keep implementations non-destructive by
default; make any irreversible modification opt-in via settings.

See also: [built-in transformers](../reference/built-in-transformers.md),
[plugins](plugins.md).

## Minimal transformer

```csharp
using ReleaseGuard.Editor.Core.Transforming;

public sealed class MyTransformer : ReleaseTransformer
{
    public override string Id => "myteam.my_transformer";

    public override void Transform(ReleaseTransformContext context)
    {
        context.Info($"Transforming {context.OutputPath}");
        // Modify assemblies, patch binaries, etc.
    }
}
```

## Base class members

`ReleaseTransformer` (namespace `ReleaseGuard.Editor.Core.Transforming`) is the
abstract base. It implements `IReleaseGuardRegistryItem`.

| Member | Kind | Signature | Notes |
| --- | --- | --- | --- |
| `Id` | abstract | `public abstract string Id { get; }` | Stable, unique snake_case id. Used for logging, disabling, and de-duplication. |
| `DisplayName` | virtual | `public virtual string DisplayName { get; }` | Defaults to the type name. |
| `Priority` | virtual | `public virtual int Priority { get; }` | Default `0`. Lower runs first. Negative runs before any default-priority transformers. |
| `ShouldRun` | virtual | `public virtual bool ShouldRun(ReleaseTransformContext context)` | Default `true`. Return `false` to skip this run. |
| `Transform` | abstract | `public abstract void Transform(ReleaseTransformContext context)` | Do the work; record via the context. Do not throw. |

## The transform context

`ReleaseTransformContext` (namespace `ReleaseGuard.Editor.Core.Transforming`).

### Logging methods

| Method | Signature |
| --- | --- |
| Info | `public void Info(string message)` |
| Warning | `public void Warning(string message)` |
| Error | `public void Error(string message)` |

Each entry is automatically attributed to the running transformer.

### Properties

| Member | Signature | Null when |
| --- | --- | --- |
| `Settings` | `public ReleaseGuardSettings Settings { get; }` | Never null. Project-wide Release Guard settings. |
| `BuildReport` | `public BuildReport BuildReport { get; }` | `null` when running against an existing output (outside an active Unity build). Always null-check. |
| `BuildTarget` | `public BuildTarget BuildTarget { get; }` | Always set, regardless of whether a `BuildReport` is present. |
| `OutputPath` | `public string OutputPath { get; }` | Always set. Path to the built product, e.g. `Builds/Windows/MyGame.exe`. |

On platforms that output a folder (Android APK, WebGL) `OutputPath` is the product
file or folder itself. `Settings` is a
`ReleaseGuard.Editor.Config.ReleaseGuardSettings`; `BuildReport` and `BuildTarget`
come from `UnityEditor.Build.Reporting` / `UnityEditor`.

## Priority and ShouldRun

Lower `Priority` runs first. `ShouldRun` gates the transformer.

```csharp
using ReleaseGuard.Editor.Core.Transforming;
using UnityEditor;

public sealed class DesktopOnlyTransformer : ReleaseTransformer
{
    public override string Id => "myteam.desktop_only_transformer";
    public override int Priority => -5; // run before default-priority transformers

    public override bool ShouldRun(ReleaseTransformContext context) =>
        context.BuildTarget == BuildTarget.StandaloneWindows64;

    public override void Transform(ReleaseTransformContext context)
    {
        context.Info($"Transforming {context.OutputPath}");
    }
}
```

## Realistic example

```csharp
using System.IO;
using ReleaseGuard.Editor.Core.Transforming;

public sealed class StripPdbTransformer : ReleaseTransformer
{
    public override string Id          => "myteam.strip_pdb";
    public override string DisplayName => "Strip loose .pdb files";

    public override void Transform(ReleaseTransformContext context)
    {
        var dir = Path.GetDirectoryName(context.OutputPath);
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
        {
            context.Warning("Could not resolve the build output directory.");
            return;
        }

        var count = 0;
        foreach (var pdb in Directory.GetFiles(dir, "*.pdb", SearchOption.TopDirectoryOnly))
        {
            File.Delete(pdb);
            count++;
        }

        context.Info($"Removed {count} loose .pdb file(s).");
    }
}
```

This example deletes files unconditionally for brevity. In production, gate
destructive behavior behind a plugin settings flag - see
[settings.md](settings.md).

## Registration options

- **Auto-discovery.** Enable
  `Transformers > Discovery > Auto Discover Transformers` in Project Settings.
  Every non-abstract `ReleaseTransformer` (excluding test fixtures) is then run
  after a release build, before the post-processor pipeline. Requires a public
  parameterless constructor.
- **Via a plugin.** Register inside a `ReleaseGuardPlugin` when you need
  constructor arguments or want to group contributions. See [plugins.md](plugins.md):

  ```csharp
  context.ReleaseGuard.Registries.Transformers.Register(new MyTransformer());
  ```

Disable a discovered transformer by adding its `Id` to
`Transformers > Discovery > Disabled Transformer Ids`.
