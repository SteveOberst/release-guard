# Example Release Guard plugin

This sample shows the recommended production extension pattern:

- an Editor-only asmdef that references `ReleaseGuard.Editor` and `ReleaseGuard.Runtime`;
- a `ReleaseGuardPlugin` registered from `[InitializeOnLoad]`;
- a plugin settings asset generated from `ReleaseGuardPluginSettings`;
- one custom `ReleaseAuditor` registered by the plugin.

After importing the sample, open `Edit > Project Settings > Release Guard`. The plugin settings
page appears under `Release Guard > Plugins > Example Plugin`. Enable `Strict Mode`, then run
`Tools > Release Guard > Audit` to see the example auditor report a warning.
