using ReleaseGuard;
using ReleaseGuard.Editor.Core.Config.Attributes;
using ReleaseGuard.Editor.Core.Plugins;
using UnityEngine;

namespace ReleaseGuard.ExamplePlugin
{
    [SettingsPage("Example Plugin", intro: "Controls for the example component.")]
    public sealed class ExamplePluginSettings : ReleaseGuardPluginSettings
    {
        [SettingsHeader("Behavior")]
        [Tooltip("When enabled, the example component reports a finding on every release build.")]
        public bool strictMode = false;

        [SettingsHeader("Reporting")]
        [Tooltip("Severity of findings reported by the example component when strict mode is on.")]
        public ReleaseIssueSeverity findingSeverity = ReleaseIssueSeverity.Warning;
    }
}
