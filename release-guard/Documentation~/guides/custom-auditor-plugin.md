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
5. Expand `Plugins` and `Discovered auditors` to confirm the plugin and auditor are registered.

Manual audits run only auditors. Transformers and post-processors appear in the window, but they
run only after successful builds.

## See also

- [Plugins](../api/plugins.md)
- [Custom auditors](../api/custom-auditors.md)
- [Plugin settings and custom renderers](../api/settings.md)
