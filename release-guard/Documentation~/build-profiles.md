# Build Profiles and Release Guard Profiles

There are two different profile systems involved:

1. Unity Build Profiles  
   Unity's own named build configurations.

2. Release Guard profiles  
   Release Guard's own registry of settings assets and activation conditions.

They are related, but they are not the same thing.

## What Release Guard stores

Release Guard keeps a registry at:

`Assets/ReleaseGuard/registry.asset`

Each entry points at a separate settings asset under:

`Assets/ReleaseGuard/Profiles/{profileId}.asset`

## How a real build chooses a profile

`ProfileSettingsResolver.ResolveForBuild(...)` walks the Release Guard profile list from top to bottom and picks the first matching profile.

Available activation strategies:

- `IsReleaseBuild`
- `IsDevelopmentBuild`
- `IsCI`
- `IsCIAndDevelopmentBuild`
- `UnityBuildProfileNames`
- `Always`

Those bullets are not a built-in priority order. They are just the available activation strategies. Registry order matters. If two profiles can both match, the one higher in the list wins.

The Profiles page warns about obvious conflicts, but it does not remove the ambiguity for you.

## Quick mental model

Here is the default behavior with the seeded built-in profiles:

| Situation | How Release Guard resolves settings |
|---|---|
| Checks window | Uses the currently edited profile in Project Settings |
| Local editor build | Uses the first profile whose activation matches the actual build |
| Local batchmode build | Uses the first matching profile and treats the environment as CI |
| CI build | Uses the first matching profile and treats the environment as CI |

| Build situation | Matches `Release` | Matches `Development` | Effective profile |
|---|---:|---:|---|
| Local editor build, Development Build off | yes | no | `Release` |
| Local editor build, Development Build on | no | yes | `Development` |
| Local batchmode build, Development Build off | yes | no | `Release` |
| Local batchmode build, Development Build on | no | yes | `Development` unless a higher `IsCIAndDevelopmentBuild` or `IsCI` profile appears first |
| CI build, Development Build off | yes | no | `Release` |
| CI build, Development Build on | no | yes | `Development` unless a higher `IsCIAndDevelopmentBuild` or `IsCI` profile appears first |

If you add custom profiles, keep reading that table as "first matching row in the registry wins".

## Example of bad ordering

This order is misleading:

1. `Always`
2. `Release`
3. `Development`

The `Always` profile wins every time, so the other two never apply.

Safer ordering:

1. `Release`
2. `Development`
3. any narrow CI or Unity Build Profile-name matches
4. `Always` only as a true fallback

The list order is editable in the Profiles page. Drag and drop changes the registry order, and that directly changes which profile wins.

![Profiles can be reordered by drag and drop](../assets/profile_drag_and_drop.png)

## What the header dropdown means

The profile dropdown in `Edit > Project Settings > Release Guard` controls only the profile you are editing in the UI.

It does not force builds to use that profile.

The page header warns when:

- your current Build Settings would activate one profile
- but you are editing another

That warning exists because this is an easy place to get confused.

![Warning that the current build would use a different profile than the one being edited](../assets/current_build_would_use_other_profile.png)

Important limit: the current warning path does not resolve `UnityBuildProfileNames` matches. Treat it as a helpful editor-side warning for the common development/CI cases, not as a complete validator for every custom activation strategy.

## Built-in defaults

On startup, `ProfileMigration` guarantees two built-in profiles exist:

- `Release`  
  activation: `IsReleaseBuild`

- `Development`  
  activation: `IsDevelopmentBuild`

Those two built-in profiles are also UI-constrained:

- their activation conditions are fixed in the Profiles page
- they cannot be deleted

The Development profile disables several release-only checks by default, but it still leaves some guardrails active.

Seeded Development defaults:

| Built-in component | Default state | Reason |
|---|---|---|
| `development_build` | disabled | A local development build should not fail just because it is a development build. |
| `script_debugging` | disabled | Debug-oriented local workflows would be too noisy otherwise. |
| `profiler_connection` | disabled | Same reason as script debugging. |
| `managed_stripping` | disabled | Development builds are intentionally looser here. |
| `scripting_backend` | disabled | Development builds can intentionally tolerate a different backend posture. |
| `broad_preserve` | disabled | Development builds are intentionally looser here. |
| `release_forbidden` | disabled | Runtime debug helpers may exist during development. |
| `android_debuggable` | disabled | Development builds intentionally allow debugging paths. |
| `webgl_exception_support` | disabled | Development builds can intentionally tolerate a noisier WebGL exception posture. |
| `insecure_http` | disabled | Development builds often talk to non-production services. |
| `burst_debug` | disabled | Development builds can intentionally keep Burst in a more debug-friendly posture. |
| `stack_trace_type` | enabled | Stack trace policy is still explicit, not accidental. |
| `strip_engine_code` | enabled | Engine stripping policy still matters. |
| `ci_development_build` | enabled | CI should still catch a development build being shipped. |

Other built-ins remain at their default settings unless the profile asset is edited.

## Creating custom profiles

The Profiles page lets you create:

- a copy of `Release`
- a copy of `Development`
- a blank profile

The profile-creation paths are not all the same:

- `Copy of Release` and `Copy of Development` create a new settings asset immediately, but the new registry entry starts with activation `Always`
- duplicating an existing row copies that row's activation strategy and settings shape
- `Blank` also starts with activation `Always`

![Creating a new profile from the Profiles page](../assets/create_new_profile.png)

So after creating a new profile from the add menu, always review its activation condition before assuming it behaves like the source profile.

When you configure the new profile, the activation condition is the most important field to verify. It decides whether that profile can match a build at all.

![Choosing an activation condition for a profile](../assets/profile_choose_activation_conditions.png)

## Unity Build Profile name matching

`ActivationStrategy.UnityBuildProfileNames` compares the active Unity Build Profile name via reflection.

Important implications:

- it only works on editor versions that expose Unity Build Profiles
- if the API is unavailable, the active Unity Build Profile name resolves to `null`
- the comparison uses the Build Profile object's `.name`

Use this strategy when you already rely on Unity Build Profiles for environments like `Production`, `Staging`, or `QA`, and you want Release Guard to follow the same naming.

## Local batchmode caveat

Release Guard treats any batchmode run as CI, even when no known CI vendor variable is present.

That means a local scripted build started in batchmode can:

- match `IsCI`
- match `IsCIAndDevelopmentBuild`
- trigger `ci_development_build`

and the environment will be labeled `CI_Unknown`.

## Recommendation

Keep the rules simple:

- one obvious release profile
- one obvious development profile
- add named environment profiles only when a real build pipeline needs them

Overlapping profile conditions are possible, but they make the effective configuration harder to reason about in CI and harder to explain to the next developer.

