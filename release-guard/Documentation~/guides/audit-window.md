# Audit window

The audit window runs the auditor pipeline on demand and shows the findings, so you can check release readiness without making a build. It runs exactly the same auditors that gate a real build, but it never blocks anything and never runs transformers or post-processors.

## Opening it

Open the window from the menu:

```
Tools > Release Guard > Audit
```

You can also open it from the Release Guard settings overview page with the `Open Audit Window` action button, or from the window's own toolbar via the `Settings` button (which jumps to `Project/Release Guard`).

To open the window and immediately run an audit in one call - handy for menu shortcuts or CI-driven Editor scripts - call the public method `ReleaseGuard.Editor.UI.ReleaseGuardWindow.ShowWindowAndRunAudit()`. The plain `ReleaseGuardWindow.ShowWindow()` opens the window without running an audit.

## Running an audit

Click `Run Audit` in the toolbar. The audit resolves the current configuration (using the Build Settings development checkbox in place of a build report) and runs every discovered auditor against the active build target. Until you run an audit the window shows an info box reminding you that the same checks run automatically before every non-development build.

## What it shows

After a run the window displays:

- A summary box with the error, warning, and info counts, colored by the highest severity present.
- A `Discovered auditors` foldout listing every auditor that ran, with its id, display name, and how many findings it produced (or "clean"). This is the quickest way to confirm a custom auditor is being picked up.
- Foldouts for `Post-processors`, `Transformers`, and `Plugins` that list what is registered. These run only during a real build, not from the manual audit; the post-processor foldout says so explicitly.
- The toolbar shows the auditor count and the highest severity.

## Severity filtering

Below the foldouts is a `Show:` row with three toggle buttons - `Errors`, `Warnings`, and `Info` - each showing its count. Toggle any of them to filter which findings appear in the list below. The list is sorted most-serious-first. If filters hide everything while findings exist, the window tells you no issues match the current filters.

Each finding shows its message (styled by severity), the auditor id that produced it, a `Ping asset` button when the finding has an asset path, and a `Fix` hint when one was supplied.

## Advisory dismissal ("Don't show again")

Some findings are advisories - dismissible best-practice suggestions. An advisory carries a stable suppress id and shows a `Don't show again` button. Clicking it:

1. Writes the advisory's suppress id into `suppressedAdvisoryIds` on the Auditors settings page and saves the settings asset immediately.
2. Reloads the Release Guard environment so the new suppression takes effect.
3. Re-runs the audit so the dismissed advisory disappears from the list right away.

Dismissal persists because it is stored in the committed settings asset. To bring an advisory back, remove its id from `suppressedAdvisoryIds` in `Edit > Project Settings > Release Guard` under Auditors. While an advisory is suppressed it is dropped silently on every run - it is never recorded, in the window or during a build.

Advisories never carry an asset path, so the [asset-exclusion list](asset-exclusions.md) does not affect them; the suppress mechanism above is the way to dismiss them.
