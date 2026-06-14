# Debug symbol sweep

| Field | Value |
|---|---|
| Id | `debug_symbol_sweep` |
| Phase | `post-build` |
| Default posture | warning/report-only unless delete mode is enabled |
| Blocks build | no |

## Overview

This component scans the resolved build output folder for top-level debug artifacts that teams commonly ship by accident when they zip the build folder as-is. It exists for pipeline hygiene, not player-facing behavior.

## How it evaluates

After a successful build, the component scans only the top level of the resolved output folder for:

- `*_BackUpThisFolder_ButDontShipItWithYourGame`
- `*_BurstDebugInformation_DoNotShip`
- `*.pdb`
- any extra patterns configured in settings

Default mode reports each matching artifact as a warning.

Delete mode removes matching files and folders, logs each deletion, and refuses to delete anything outside the resolved output folder.

## Settings

| Setting | Effect | Typical use |
|---|---|---|
| `enabled` | Enables or disables the sweep. | Leave enabled if you want visibility into output-folder debug artifacts. |
| `delete` | Switches from report-only mode to deletion mode. | Enable only after deciding how symbols are archived elsewhere. |
| `extraPatterns` | Adds extra top-level filename or folder patterns. | Use for project-specific symbol or debug outputs. |

## Active-profile defaults

Behavior depends on the active profile's component settings entry.

In the seeded profile assets:

- `release.asset` keeps the component enabled in report-only mode
- `development.asset` also keeps the component enabled in report-only mode

## Common pitfalls

- The sweep is top-level only. It does not recurse through the entire build output tree.
- Report-only mode is the default. The component does not delete anything unless you explicitly turn delete mode on.
- This is post-build behavior, so the checks window will never exercise it.

## Troubleshooting

### "Why didn't the checks window catch this?"

Because the checks window dispatches only the `pre-build` event. This component runs only after a successful real build.

### "What makes deletion safe?"

The component normalizes full paths and refuses to delete anything that escapes the resolved output folder. That guard exists specifically to prevent accidental or malicious path abuse.

### "Should we enable deletion in CI?"

Only after you have a separate symbol archival story. The component cannot regenerate deleted symbols for crash analysis.

## Related docs

- [CI integration](../../guides/ci-integration.md)
- [Pre-Build Checks window](../../guides/checks-window.md)
- [Build manifest](build-manifest.md)
