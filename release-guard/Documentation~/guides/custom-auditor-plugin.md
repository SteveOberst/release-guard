# Guide: first custom auditor plugin

This guide builds the smallest production-style extension: a plugin that registers one custom
auditor and exposes one settings toggle. Use this path when the auditor needs settings,
constructor arguments, or predictable loading.

For quick experiments, you can instead enable `Auditors > Discovery > Auto Discover Auditors`
and give your auditor a public parameterless constructor. Auto-discovery is off by default and
is not the recommended production path.

## 1. Create an Editor assembly

Create an Editor-only asmdef in your project, for example
`Assets/MyReleaseGuardPlugin/MyReleaseGuardPlugin.asmdef`:

```json
{
  "name": "MyReleaseGuardPlugin",
  "rootNamespace": "MyReleaseGuardPlugin",
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

`ReleaseGuard.Editor` is auto-referenced for type visibility, but the explicit asmdef reference
matters for `[InitializeOnLoad]`: Unity initializes assemblies in dependency order, so Release
Guard's environment exists before your plugin loader resolves it.

## 2. Add plugin settings

```csharp
using ReleaseGuard;
using ReleaseGuard.Editor.Core.Config.Attributes;
using ReleaseGuard.Editor.Core.Plugins;
using UnityEngine;

namespace MyReleaseGuardPlugin
{
    [SettingsPage("My Checks", intro: "Project-specific release checks.")]
    public sealed class MyPluginSettings : ReleaseGuardPluginSettings
    {
        [SettingsHeader("Company Name")]
        [Tooltip("When enabled, release builds require a non-default company name.")]
        public bool requireCompanyName = true;

        [Tooltip("Severity reported when the company name is missing.")]
        public ReleaseIssueSeverity severity = ReleaseIssueSeverity.Error;
    }
}
```

The settings asset is created at `Assets/ReleaseGuard/Plugins/{PluginId}.asset` the first time
the plugin loads. Commit that asset and its `.meta` file if the settings should apply to the
whole team and CI.

## 3. Add the auditor

```csharp
using ReleaseGuard.Editor.Core.Audit;
using UnityEditor;

namespace MyReleaseGuardPlugin
{
    public sealed class CompanyNameAuditor : ReleaseAuditor
    {
        private readonly MyPluginSettings _settings;

        public CompanyNameAuditor(MyPluginSettings settings) => _settings = settings;

        public override string Id => "myteam.company_name";
        public override string DisplayName => "Company name";

        // ShouldRun returning false prevents Evaluate from being called, so the
        // _settings != null guard here also protects Evaluate from a null dereference.
        public override bool ShouldRun(ReleaseAuditContext context) =>
            _settings != null && _settings.requireCompanyName;

        public override void Evaluate(ReleaseAuditContext context)
        {
            if (!string.IsNullOrWhiteSpace(PlayerSettings.companyName) &&
                PlayerSettings.companyName != "DefaultCompany")
                return;

            context.Report(
                _settings.severity,
                "Player Settings company name is unset or still 'DefaultCompany'.",
                fixHint: "Set Edit > Project Settings > Player > Company Name.");
        }
    }
}
```

Pick a stable, unique `Id`. Users disable custom auditors by putting that id in
`Auditors > Discovery > Disabled Auditor Ids`, and duplicate registrations keep the first item
that registered that id.

> **`context.Settings` vs `context.Configuration`.** `context.Settings` is the raw settings
> asset (committed values). `context.Configuration` is the resolved per-run state after
> applying Build Profile overrides. For most auditors - reading a feature toggle, checking a
> threshold - use `context.Settings`. Use `context.Configuration` only when you need the
> resolved `FailureThreshold` or `BuildProfileName`. See the
> [custom auditors API](../api/custom-auditors.md) for details.

## 4. Register the plugin

```csharp
using ReleaseGuard.Editor.Core.DI;
using ReleaseGuard.Editor.Core.Plugins;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;

namespace MyReleaseGuardPlugin
{
    public sealed class MyPlugin : ReleaseGuardPlugin
    {
        public override string PluginId => "com.myteam.release-guard";
        public override string DisplayName => "My Release Guard Checks";
        public override string Author => "My Team";
        public override System.Type SettingsType => typeof(MyPluginSettings);

        public override void Register(PluginRegistrationContext context)
        {
            var settings = GetSettings<MyPluginSettings>();
            context.ReleaseGuard.Registries.Auditors.Register(new CompanyNameAuditor(settings));
        }
    }

    [InitializeOnLoad]
    internal static class MyPluginLoader
    {
        static MyPluginLoader()
        {
            DI.Resolve<ReleaseGuardEnvironment>().RegisterPlugin(new MyPlugin());
        }
    }
}
```

`RegisterPlugin` returns `false` instead of throwing when the plugin is disabled, duplicated,
or called too early. If it logs that initialization has not completed, check the asmdef reference
to `ReleaseGuard.Editor`.

## 5. Verify it loaded

1. Open `Edit > Project Settings > Release Guard`.
2. Confirm your plugin settings page appears under `Release Guard > Plugins`.
3. Open `Tools > Release Guard > Audit`.
4. Click `Run Audit`.
5. Expand `Plugins` and `Registered auditors` to confirm the plugin and auditor are registered.

Manual audits run only auditors. Transformers and post-processors appear in the window, but they
run only after successful builds.

**Troubleshooting plugin loading:** if your plugin does not appear in the Plugins foldout, enable
`General > Verbose Logging` in Project Settings, then trigger a domain reload (make any script
change, save, and wait for recompile). Look in the Console for `[ReleaseGuard]`-prefixed log lines
that mention your plugin id - they show whether the plugin was found, skipped, or produced an
error. The most common causes are a missing asmdef reference to `ReleaseGuard.Editor` (described
in step 1) and a `PluginId` that matches an entry in `Plugins > Disabled Plugin Ids`.

## 6. Import and use the bundled sample

The package ships a working example under `Samples~/Example Plugin`. To import it:

1. Open `Window > Package Manager`.
2. Select **Release Guard** in the package list.
3. Go to the **Samples** tab.
4. Click **Import** next to **Example Plugin**.

Unity copies the sample into `Assets/Samples/Release Guard/<version>/Example Plugin/`. The
sample contains an asmdef, a plugin class, a settings class, and an auditor - the exact
same pattern as this guide. After import, open `Edit > Project Settings > Release Guard`;
the Example Plugin settings page appears under `Release Guard > Plugins`. Enable **Strict Mode**,
then run `Tools > Release Guard > Audit` to see the example auditor fire a Warning.

## 7. Write tests for your auditor

Place EditMode tests in a test assembly that has `"UNITY_INCLUDE_TESTS"` as an asmdef
`defineConstraints` and references `ReleaseGuard.Editor`, `ReleaseGuard.Runtime`, and the
NUnit test runner. Use `[TestAuditorFixture]` on any `ReleaseAuditor` subclass that is only
for tests, so it is excluded from auto-discovery and real audit runs.

A minimal test creates a `ReleaseGuardSettings` instance, builds a
`ReleaseAuditContext` manually, runs `auditor.Evaluate(context)`, and asserts on the
collected issues:

> **`LoadOrCreate()` creates a real asset.** Calling `ReleaseGuardSettings.LoadOrCreate()`
> in a test creates `Assets/ReleaseGuard/ReleaseGuardSettings.asset` on disk if it does not
> already exist. In the `UnityDevHost` project this asset is already committed, so the call
> just loads it. In a fresh project or on a CI machine without a committed settings asset, the
> call creates the file - you may want to clean it up after the test or use
> `ScriptableObject.CreateInstance<ReleaseGuardSettings>()` instead for fully isolated tests
> that don't touch the disk.

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Audit;
using ReleaseGuard.Editor.Core.Runtime;
using UnityEditor;

namespace MyReleaseGuardPlugin.Tests
{
    public sealed class CompanyNameAuditorTests
    {
        [Test]
        public void Reports_error_when_company_name_is_DefaultCompany()
        {
            var settings  = ReleaseGuardSettings.LoadOrCreate();
            var config    = ReleaseGuardConfiguration.Resolve(settings, report: null);
            var logger    = new ReleaseGuardLogger(verbose: false);
            var issues    = new List<ReleaseIssue>();
            var context   = new ReleaseAuditContext(
                settings, config, logger,
                buildReport:  null,
                buildTarget:  BuildTarget.StandaloneWindows64,
                issues:       issues);

            // Arrange: exercise the auditor with settings that enable the check
            var pluginSettings = ScriptableObject.CreateInstance<MyPluginSettings>();
            pluginSettings.requireCompanyName = true;

            var auditor = new CompanyNameAuditor(pluginSettings);
            auditor.Evaluate(context);

            Assert.IsTrue(issues.Count > 0, "Expected at least one issue");
            Assert.AreEqual(ReleaseIssueSeverity.Error, issues[0].Severity);
        }
    }
}
```

Run tests from `Window > General > Test Runner`, EditMode tab, in the `UnityDevHost` project
(or your own project if it is the consuming project).

## See also

- [Plugins](../api/plugins.md)
- [Custom auditors](../api/custom-auditors.md)
- [Plugin settings and custom readers](../api/settings.md)
