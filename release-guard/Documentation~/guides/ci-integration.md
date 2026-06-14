# CI Integration

Release Guard does not need a special CI entry point.

If your CI job triggers a Unity build through the normal build pipeline, the package hooks run automatically.

## What actually runs in CI

During a build, Release Guard uses:

- `IPreprocessBuildWithReport` for the blocking `pre-build` event
- `IPostprocessBuildWithReport` for the `build` event
- `IPostprocessBuildWithReport` again, at `callbackOrder = int.MaxValue`, for the final `post-build` event

If the pre-build report contains any issue at or above the configured failure threshold, Release Guard throws `BuildFailedException` and Unity returns a failed build.

## CI environment detection

Release Guard treats batchmode as the CI boundary.

The current implementation is:

- when `Application.isBatchMode` is false, the environment is `UnityEditor`
- when `Application.isBatchMode` is true, the run is treated as CI
- after that, Release Guard tries to refine the CI provider label

Provider labeling uses common CI variables for:

- GitHub Actions
- GitLab CI
- Jenkins
- CircleCI
- Azure DevOps
- TeamCity

If none match, the environment is still treated as CI, but labeled `CI_Unknown`.

Unity Cloud Build is separate: the current implementation detects it through the `UNITY_CLOUD_BUILD` compile symbol, not through an environment-variable probe.

This matters mainly for profile activation and for the built-in `ci_development_build` component.

## Important consequence: local batchmode also counts

This is easy to miss on a first rollout.

If you run Unity locally in batchmode, Release Guard still treats that build as CI even if no vendor-specific CI variable is present.

So a local command-line build can:

- activate `IsCI` profiles
- activate `IsCIAndDevelopmentBuild` profiles
- fail on `ci_development_build`

with the environment labeled `CI_Unknown`.

Example shape:

```text
Unity.exe -batchmode -quit -projectPath <project> -executeMethod <your build method>
```

If that build also sets Development Build, the default Development profile can still be selected, and `ci_development_build` can still report because batchmode alone is enough to classify the run as CI.

## Recommended profile strategy for CI

The simplest stable setup is:

- `Release` profile for non-development builds
- `Development` profile for development builds
- optional CI-specific profile only when your pipeline really needs different Release Guard behavior than local builds

Do not assume the profile selected in the Project Settings header affects CI. It does not.

## Build manifest

The `build_manifest` component is off by default.

When enabled, it writes `release-guard-manifest.json` into the resolved build output folder after a successful build and after the default-priority post-build work has already run.

Use it as a CI artifact, not a shipped file.

Useful downstream uses:

- artifact validation
- provenance records
- packaging assertions
- verifying that the expected component set was active for a given build
- release pipeline checks that compare expected disabled components or suppressed advisories against what actually produced the build

The manifest records:

- Release Guard package version
- Unity version
- build target
- product name
- output file name
- failure threshold
- build GUID and timestamps when a `BuildReport` is available
- registered components and their subscribed phases
- disabled component ids
- disabled plugin ids
- suppressed advisory ids

What it deliberately does not record:

- absolute file system paths
- VCS revision information

If you want commit metadata in the manifest, add your own post-build component with a higher priority than the built-in writer.

## Debug symbol sweep in CI

`debug_symbol_sweep` is the other CI-relevant built-in post-build component.

Default behavior:

- enabled
- report-only

Optional behavior:

- delete matched artifacts from the output folder

Only enable deletion after you have decided where symbol folders and `.pdb` files should be archived for crash symbolication. Once they are deleted from the output folder, they are gone unless you rebuild.

## Troubleshooting

### "Why did CI use different settings than my manual run?"

That depends on which "manual run" you mean:

| Action | How settings are chosen |
|---|---|
| Checks window | Uses the currently edited profile |
| Local editor build | Uses the first profile whose activation matches the real build |
| Local batchmode build | Uses the first matching profile and counts as CI |
| CI build | Uses the first matching profile and counts as CI |

So the common mismatch is not "manual versus CI". It is "checks window versus real build".

### "Why did the checks window not catch a post-build problem?"

Because the checks window only dispatches the `pre-build` event.

### "Why did a development build still fail in CI?"

The built-in `ci_development_build` component exists specifically to catch that case.
