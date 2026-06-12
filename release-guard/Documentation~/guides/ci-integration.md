# Guide: CI integration

Release Guard does not require a separate command-line tool. It hooks into Unity's normal build
callbacks, so any CI job that creates a non-development player build automatically runs the
pre-build audit and fails when findings meet the configured threshold.

## Recommended CI shape

1. Commit `Assets/ReleaseGuard/ReleaseGuardSettings.asset` and its `.meta` file.
2. Make CI run the same Unity build method used for production builds.
3. Ensure that build method creates a non-development build unless the job is intentionally for
   development builds.
4. Archive the Unity Editor log on failure.
5. If `Post-Processors > Build Manifest > Write Build Manifest` is enabled, archive
   `release-guard-manifest.json` as a CI artifact and exclude it from shipped builds.

> **Missing settings asset in CI.** If the settings asset is not committed, `ReleaseGuardSettings.LoadOrCreate()` creates a fresh asset with all defaults when the Editor starts. This means CI would run with permissive defaults rather than your team's committed policy, and changes to thresholds or disabled auditor ids made locally would have no effect in CI. Always commit the asset before onboarding CI.

When Release Guard blocks a build, Unity throws a `BuildFailedException` with a message like:

```text
[ReleaseGuard] Build blocked: N issue(s) at or above Error. See the Console (or the Release Guard window) for details and fixes.
```

The detailed findings are written to the Unity Console and Editor log.

## Batchmode example

The exact Unity executable path and project path are CI-provider-specific, but the pattern is:

```bash
Unity \
  -batchmode \
  -quit \
  -nographics \
  -projectPath "$PROJECT_PATH" \
  -executeMethod Company.Build.PerformReleaseBuild \
  -logFile "$CI_ARTIFACTS/unity.log"
```

`Company.Build.PerformReleaseBuild` should call `BuildPipeline.BuildPlayer` with production
options. Release Guard runs from Unity's `IPreprocessBuildWithReport` hook before output is
written.

## Development builds

`General > Skip On Development Builds` is on by default. If CI accidentally builds with
`BuildOptions.Development`, Release Guard skips every stage. For production jobs, make the build
method fail fast when the development flag is present so the job does not silently bypass the
release gate.

If you intentionally produce development builds in CI, keep them in a separate job or Build
Profile. Do not use the production job as both a debug-build and release-build pipeline.

## Failure thresholds in CI

The default threshold is `Error`. Warnings and advisories are logged but do not fail the build.
For a stricter CI gate, set `General > Failure Threshold` to `Warning` globally or add a Build
Profile override for the profile used by CI.

There is no "report-only" threshold above `Error`. For report-only CI, run a separate manual-audit
Editor script or use a Build Profile with Release Guard disabled and rely on the log as advisory
output.

**Advisory interaction with a stricter threshold:** several built-in advisories are `Warning`
severity (for example `managed_stripping.below_medium` and `insecure_http.always_allowed`).
If you raise `failureThreshold` to `Warning`, these advisories start blocking the build. You
must either fix the underlying issue, lower the threshold back to `Error` for those builds, or
use "Don't show again" in the audit window to permanently suppress specific advisories you have
reviewed and accepted. Plan for this extra suppression management work before enabling a
`Warning` threshold in CI.

## Multi-platform builds

Release Guard resolves the effective configuration once per build. In batchmode, each call to
`BuildPipeline.BuildPlayer` triggers an independent pre-build audit (targeting that call's
`BuildTarget`) and an independent post-build pipeline. There is no cross-build state. If your
CI script builds for multiple targets sequentially in one Editor session, each build gets a
fresh audit against the current build target with the same settings asset. You will see one set
of audit log entries per target in the Editor log.

## Post-build artifacts

The debug symbol sweep post-processor is report-only by default. If you enable deletion, archive
symbol folders to your crash-reporting storage before Release Guard deletes them from the build
output.

The build manifest is intended for CI artifact storage. It records Unity version, build target,
active registry ids, disabled ids, suppressions, and build summary fields when a build report is
available. It deliberately does not record VCS revision or absolute paths; add those in your own
post-processor with a priority greater than `100` if it must amend the built-in manifest, or keep
them in CI metadata.

Useful ways to use the manifest in CI:

- Attach it to every build artifact so you can later answer which Release Guard version, Unity
  version, target, threshold, auditors, post-processors, transformers, disabled ids, and suppressed
  advisories produced that artifact.
- Compare manifests between staging and production builds to catch policy drift before promotion,
  for example a plugin disabled in staging but enabled in production, or a new advisory suppression
  added without review.
- Fail a promotion job if the manifest is missing, belongs to the wrong build target, or was
  produced with a weaker threshold than the destination environment requires.
- Include the manifest in release evidence for security reviews, store submissions, or incident
  response. It gives reviewers a compact, machine-readable snapshot of the hardening gate.
- Use it as input to dashboards or release notes that show which hardening checks were active for
  each build.

If you need commit SHA, branch, CI run id, signing status, or artifact hashes in the same evidence
bundle, either store those beside the manifest in CI metadata or write a custom post-processor that
adds a second project-specific manifest file. The built-in manifest intentionally avoids repository
and machine-specific data.

## Ordering with other build callbacks

Release Guard hooks at `callbackOrder = 0` for the pre-build gate (`IPreprocessBuildWithReport`)
and at `callbackOrder = int.MaxValue` for post-processors (`IPostprocessBuildWithReport`). If
you have custom build scripts that also implement `IPreprocessBuildWithReport`, their
`callbackOrder` value determines whether they run before or after the Release Guard gate:

- `callbackOrder < 0`: runs before the Release Guard gate. Use this for scripts that prepare
  the build (e.g. setting `BuildOptions`).
- `callbackOrder > 0`: runs after the gate. Those scripts will not run when a blocked build throws.
- `callbackOrder = 0` (the default): runs alongside Release Guard; Unity does not guarantee
  order among same-priority callbacks.

If a script must fire before the Release Guard check (e.g. to set a `Development Build` flag
that the gate then reads), give it a negative `callbackOrder`.

## Troubleshooting

- No Release Guard output: confirm the package is installed and `General > Enabled` is on.
- Build unexpectedly skipped: confirm the build is not a Development build and no Build Profile
  override disables Release Guard.
- Different behavior between local and CI: confirm `Assets/ReleaseGuard/ReleaseGuardSettings.asset`
  is committed. Without the asset, CI uses freshly-created defaults, not your local settings.
- Custom plugin missing: confirm the plugin assembly has an explicit asmdef reference to
  `ReleaseGuard.Editor` and that the plugin id is not listed in `Plugins > Disabled Plugin Ids`.
- Custom auditor missing: confirm it is registered by a plugin, or enable
  `Auditors > Discovery > Auto Discover Auditors` for prototypes.
