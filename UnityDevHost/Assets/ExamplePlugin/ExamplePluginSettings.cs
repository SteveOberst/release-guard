using ReleaseGuard;
using ReleaseGuard.Editor.Config;
using ReleaseGuard.Editor.Core.Config;
using ReleaseGuard.Editor.Core.Config.Attributes;
using ReleaseGuard.Editor.Core.Config.Types;
using ReleaseGuard.Editor.Core.Plugins;
using UnityEngine;

namespace ExamplePlugin
{
    public sealed class ExamplePluginSettings : ReleaseGuardPluginSettings
    {
        [SettingsHeader("Behavior")]
        [Tooltip("When enabled, the example auditor fires on every release build to verify the reporting pipeline.")]
        public bool strictMode = false;

        [ConditionalWarning("Strict mode is on — the example auditor will report a finding on every release build.")]
        [Tooltip("Toggle strict mode off to silence the example auditor.")]
        public bool strictModeAcknowledged = false;

        [SettingsHeader("Exclusions")]
        [Tooltip(
            "Asset paths this plugin should ignore. One gitignore-style glob per line.\nExample: Assets/ThirdParty/**")]
        public ExclusionList ignoredAssets = new();

        [SettingsHeader("Reporting")]
        [Tooltip("Severity of findings reported by the example auditor when strict mode is on.")]
        public ReleaseIssueSeverity findingSeverity = ReleaseIssueSeverity.Warning;
    }
}