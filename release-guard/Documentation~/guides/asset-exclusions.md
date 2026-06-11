# Asset exclusions

Some findings are tied to a specific asset path - for example a finding about a third-party package or a generated file you cannot change. Release Guard lets you exclude those asset paths with a list of gitignore-style glob patterns so they never appear as findings, for both builds and manual audits.

The list lives on the Auditors settings page in the `excludedAssetPaths` field (a multi-line text area, one pattern per line). The Project Settings UI renders a live "Preview matching assets" foldout below the field so you can see which assets your patterns currently match.

## Where exclusion is enforced

Exclusion happens at a single chokepoint: every auditor reports findings through `ReleaseAuditContext.Report` (and its `Info` / `Warning` / `Error` wrappers). When a finding carries an asset path, that path is normalized and tested against the exclusion list there. If it matches, the finding is dropped before it is recorded. Because this is the only place findings are added, exclusion applies uniformly to every auditor - built-in or custom - and to both build-time and manual audit runs.

Findings with no asset path are never excluded by these patterns. Asset patterns only filter findings that are attributed to an asset.

## Pattern syntax

Patterns are modelled on `.gitignore`. The matcher compiles each pattern to a regular expression once. The following is the exact, complete set of features the engine implements:

- `*` matches any run of characters except `/` (a single path segment).
- `**` matches any run of characters including `/` (recursive). The special form `**/` matches zero or more directories.
- `?` matches exactly one character except `/`.
- A pattern with no `/` in it matches by file or folder name at any depth. For example `*.tmp` matches a `.tmp` file anywhere in the project.
- A pattern that contains a `/` (anywhere after trimming a trailing slash), or that starts with a leading `/`, is anchored to the start of the asset path. Asset paths begin with `Assets/`, so anchored patterns are matched from there. A leading `/` is stripped after it marks the pattern as anchored.
- A trailing `/` marks a directory pattern: it matches that directory and everything under it.
- A leading `!` negates the pattern, re-including a path that an earlier pattern excluded.
- Blank lines are ignored. Lines whose first non-space character is `#` are treated as comments and ignored.

Matching is case-insensitive (Unity's primary platforms use case-insensitive filesystems). Backslashes in both the pattern and the asset path are normalized to forward slashes before matching.

### Last match wins

When several patterns match the same path, the last matching pattern in the list decides the outcome: a normal pattern excludes, a `!` pattern re-includes. Order your patterns so broad excludes come first and the `!` re-includes that carve out exceptions come after.

## Examples

Exclude an entire third-party folder and everything under it:

```
Assets/ThirdParty/
```

Exclude all generated C# files anywhere in the project:

```
*.generated.cs
```

Exclude a samples folder using a recursive match:

```
Assets/Samples/**
```

Exclude a vendor folder but keep one subfolder under audit (last match wins, so the `!` line comes last):

```
Assets/Vendor/
!Assets/Vendor/OurFork/
```

Exclude every `.tmp` file at any depth, then comment a reminder:

```
# editor scratch files
*.tmp
```

Anchored vs. unanchored - the first is anchored because it contains a slash, the second matches the folder name at any depth:

```
Assets/Plugins/Acme/
Acme/
```

## Tips

- Use the Preview foldout in Project Settings to confirm a pattern matches what you expect before committing.
- Prefer narrow patterns. Excluding `Assets/` would silence everything, which defeats the gate.
- Exclusions are not a substitute for fixing real issues - use them only for paths you genuinely cannot or should not change.
