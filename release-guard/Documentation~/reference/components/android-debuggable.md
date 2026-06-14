# Android debuggable templates

| Field | Value |
|---|---|
| Id | `android_debuggable` |
| Phase | `pre-build` |
| Default posture | blocking check |
| Blocks build | yes |
| Scope | Android only |

## Overview

This component scans project-owned Android templates for explicit `debuggable=true` declarations. It exists because a debuggable release APK or AAB allows debugger attachment and memory dumping, and app stores such as Google Play reject debuggable release uploads.

## How it evaluates

The component scans `Assets/Plugins/Android` recursively for:

- `AndroidManifest.xml`
- `*.gradle`

It uses `AndroidDebuggableAnalyzer` to find real `debuggable=true` declarations and reports one error per finding with asset path and line number.

Important limits:

- commented-out text is ignored
- Unity-generated manifests are not scanned
- the component does not infer debuggable state from the Development Build flag

## Settings

| Setting | Effect | Typical use |
|---|---|---|
| `components.excludedAssetPaths` | Can suppress findings for specific template files. | Use for noisy third-party templates you cannot safely edit. |
| `enabled` | Disables the component entirely for the active profile when turned off in that component's `componentToggles` entry. | Use only when a profile intentionally ignores template-level debuggable declarations. |

There is no dedicated component-specific settings object for this check.

## Active-profile defaults

Behavior depends on the active profile's disable list.

In the seeded profile assets:

- `release.asset` leaves this component enabled
- `development.asset` disables this component

## Common pitfalls

- This does not check Unity-generated output. It checks project-owned templates under `Assets/Plugins/Android`.
- This is separate from `development_build`. A project can accidentally declare `debuggable=true` in templates regardless of the broader build flag.

## Troubleshooting

### "Why didn't this catch a debuggable generated manifest?"

Because generated manifests are outside this component's scope. Template declarations are what it audits.

### "Why is this firing on a file we do not own?"

Use asset exclusions for the specific path if the template is third-party and intentionally immutable.

## Related docs

- [Asset exclusions](../../guides/asset-exclusions.md)
- [Development build disabled](development-build.md)
- [CI integration](../../guides/ci-integration.md)
