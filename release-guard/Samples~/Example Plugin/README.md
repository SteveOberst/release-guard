# Example Release Guard plugin

This sample shows the current recommended extension pattern:

- an Editor-only asmdef that references `ReleaseGuard.Editor` and `ReleaseGuard.Runtime`
- a `ReleaseGuardPlugin` registered from `[InitializeOnLoad]`
- a plugin settings asset derived from `ReleaseGuardPluginSettings`
- one custom `ReleaseGuardComponent`

## Importing the sample

1. Open `Window > Package Manager`.
2. Select `Release Guard`.
3. Open the `Samples` tab and import `Example Plugin`.

Unity imports the sample into your project under:

`Assets/Samples/Release Guard/<package version>/Example Plugin/`

## Trying it

After importing the sample:

1. open `Edit > Project Settings > Release Guard`
2. open `Release Guard > Plugins > Example Plugin`
3. enable `Strict Mode`
4. open `Tools > Release Guard > Pre-Build Checks`
5. click `Run Checks`

The sample component subscribes to the `pre-build` event and reports a finding using the configured severity when `Strict Mode` is on.

## Turning this into your plugin

If you want to use this sample as a starting point, rename these pieces together:

- asmdef name
- namespace
- plugin id
- component id

Keep these patterns unless you intentionally want a different startup model:

- the `[InitializeOnLoad]` loader
- `ReleaseGuardPlugin` registration
- `SettingsType` if you want a generated plugin settings page

After the plugin loads, expect the settings asset at:

`Assets/ReleaseGuard/Plugins/{your-plugin-id}.asset`

and a matching Project Settings page under:

`Edit > Project Settings > Release Guard > Plugins > <your plugin name>`
