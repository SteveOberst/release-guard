# Guide: first custom post-processor plugin

This guide builds the smallest production-style post-processor: a plugin that runs after every
release build, reads a settings toggle, and writes a file to the build output folder. The
pattern is identical to the [custom auditor guide](custom-auditor-plugin.md); only the base
class and context type differ.

Post-processors run via Unity's `IPostprocessBuildWithReport` after the build succeeds and
after every transformer, so they always see the final build output. A post-processor must not
throw -- exceptions are caught by the executor and recorded as post-process errors.

## 1. Create an Editor assembly

Create an Editor-only asmdef, for example
`Assets/MyBuildReporter/MyBuildReporter.asmdef`:

```json
{
  "name": "MyBuildReporter",
  "rootNamespace": "MyBuildReporter",
  "references": [
    "ReleaseGuard.Editor",
    "ReleaseGuard.Runtime"
  ],
  "includePlatforms": [
    "Editor"
  ],
  "autoReferenced": false
}
```

The explicit reference to `ReleaseGuard.Editor` (not just auto-reference) gives the assembly
dependency order guarantee for `[InitializeOnLoad]`.

## 2. Add plugin settings

```csharp
using ReleaseGuard.Editor.Core.Config.Attributes;
using ReleaseGuard.Editor.Core.Plugins;
using UnityEngine;

namespace MyBuildReporter
{
    [SettingsPage("Build Reporter", intro: "Post-build report configuration.")]
    public sealed class BuildReporterSettings : ReleaseGuardPluginSettings
    {
        [SettingsHeader("Output")]
        [Tooltip("When enabled, writes build-report.txt to the build output folder after every release build.")]
        public bool writeBuildReport = true;

        [Tooltip("File name for the build report (relative to the build output folder).")]
        public string fileName = "build-report.txt";
    }
}
```

The settings asset is created at `Assets/ReleaseGuard/Plugins/{PluginId}.asset` on first use.
Commit it so the settings apply to the whole team and CI.

## 3. Add the post-processor

```csharp
using System.IO;
using ReleaseGuard.Editor.Core.PostProcessing;

namespace MyBuildReporter
{
    public sealed class BuildReportPostProcessor : ReleasePostProcessor
    {
        private readonly BuildReporterSettings _settings;

        public BuildReportPostProcessor(BuildReporterSettings settings) => _settings = settings;

        public override string Id          => "myteam.build_report";
        public override string DisplayName => "Build report writer";

        public override bool ShouldRun(ReleasePostProcessContext context) =>
            _settings != null && _settings.writeBuildReport;

        public override void PostProcess(ReleasePostProcessContext context)
        {
            var outputFolder = ResolveOutputFolder(context.OutputPath);
            if (string.IsNullOrEmpty(outputFolder) || !Directory.Exists(outputFolder))
            {
                context.Warning("Could not resolve build output folder -- report not written.");
                return;
            }

            var name     = _settings?.fileName ?? "build-report.txt";
            var path     = Path.Combine(outputFolder, name);
            var contents = $"Build target: {context.BuildTarget}\nBuilt: {System.DateTime.UtcNow:u}\n";

            File.WriteAllText(path, contents);
            context.Info($"Wrote {name} to build output.");
        }

        private static string ResolveOutputFolder(string outputPath) =>
            Directory.Exists(outputPath) ? outputPath : Path.GetDirectoryName(outputPath);
    }
}
```

The `ResolveOutputFolder` helper handles both file outputs (e.g. `Builds/Win/Game.exe`) and
folder outputs (e.g. `Builds/WebGL/`), where `OutputPath` is the folder itself.

## 4. Register the plugin

```csharp
using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Plugins;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;

namespace MyBuildReporter
{
    public sealed class BuildReporterPlugin : ReleaseGuardPlugin
    {
        public override string PluginId    => "com.myteam.build-reporter";
        public override string DisplayName => "Build Reporter";
        public override string Author      => "My Team";
        public override System.Type SettingsType => typeof(BuildReporterSettings);

        public override void Register(PluginRegistrationContext context)
        {
            var settings = GetSettings<BuildReporterSettings>();
            context.ReleaseGuard.Registries.PostProcessors.Register(
                new BuildReportPostProcessor(settings));
        }
    }

    [InitializeOnLoad]
    internal static class BuildReporterLoader
    {
        static BuildReporterLoader()
        {
            DI.Resolve<ReleaseGuardEnvironment>().RegisterPlugin(new BuildReporterPlugin());
        }
    }
}
```

## 5. Verify it loaded

1. Open `Edit > Project Settings > Release Guard`.
2. Confirm **Build Reporter** appears under `Release Guard > Plugins`.
3. Open `Tools > Release Guard > Audit` and click `Run Audit`.
4. Expand `Post-processors` -- the post-processor should be listed (but note: post-processors
   do not run during a manual audit, only after a real build).
5. Make a non-development release build. After a successful build, check the build output
   folder for `build-report.txt` and confirm a log line like
   `[ReleaseGuard] [BuildReport] Wrote build-report.txt to build output.` in the Console.

If the plugin does not appear, enable `General > Verbose Logging`, trigger a domain reload,
and look for `[ReleaseGuard]` log lines that mention your plugin id.

## Key differences from auditors

| | Auditor | Post-processor |
|---|---|---|
| When it runs | Pre-build (before output is written) | Post-build (on the finished output folder) |
| Can block the build | Yes (via findings at `failureThreshold`) | No (exceptions are caught; build already succeeded) |
| Context type | `ReleaseAuditContext` | `ReleasePostProcessContext` |
| Has `Configuration` | Yes | No -- use `Settings` directly |
| `BuildReport` | `null` during manual audit | `null` outside an active Unity build |
| Reporting | `context.Error(...)`, `context.Warning(...)` with messages, asset paths, fix hints | `context.Error(...)`, `context.Warning(...)`, `context.Info(...)` -- message only |

## See also

- [Plugins](../api/plugins.md)
- [Custom post-processors](../api/custom-post-processors.md)
- [Custom auditor guide](custom-auditor-plugin.md)
