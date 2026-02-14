# Release FockMap

You are responsible for preparing and executing a release of the FockMap library.

## Context

- This is an F# library at `/workspaces/encodings`
- The NuGet package name is **FockMap** (PackageId in `src/Encodings/Encodings.fsproj`)
- The repository is `github.com/johnazariah/encodings`
- Releases use [conventional commits](https://www.conventionalcommits.org/) to determine version bumps
- Pushing a `v*` tag triggers `.github/workflows/release.yml` which runs multi-platform tests and publishes to NuGet

## Instructions

### Step 1: Analyze commits since last release

Run `git describe --tags --abbrev=0` to find the last tag. If there is no tag, this is the first release.

Then run `git log <last-tag>..HEAD --oneline` (or `git log --oneline` for first release) to get all commits since the last release.

Categorize commits by conventional commit type:
- `feat:` → features (MINOR bump)
- `fix:` → bug fixes (PATCH bump)
- `BREAKING` or `!:` → breaking changes (MAJOR bump)
- `docs:`, `chore:`, `refactor:`, `ci:`, `test:` → maintenance (no bump impact)

### Step 2: Determine version

Read the current version from `src/Encodings/Encodings.fsproj` (inside `<Version>` tag).

Apply the bump rules:
- If this is the **first release** (no prior tags): use the current version from `.fsproj` as-is
- If there are **breaking changes**: bump MAJOR (e.g., `0.1.0` → `1.0.0`)
- If there are **features**: bump MINOR (e.g., `0.1.0` → `0.2.0`)
- Otherwise: bump PATCH (e.g., `0.1.0` → `0.1.1`)

Present the analysis and proposed version to the user. **Ask for confirmation before proceeding.**

### Step 3: Update version in .fsproj

In `src/Encodings/Encodings.fsproj`, update the `<Version>` element to the new version.

### Step 4: Generate CHANGELOG entry

Create or update `CHANGELOG.md` with a new entry at the top (after the header). Format:

```markdown
## [X.Y.Z] - YYYY-MM-DD

### ⚠ BREAKING CHANGES
- (if any)

### ✨ Features
- (if any)

### 🐛 Bug Fixes
- (if any)

### 📚 Documentation
- (if any)

### 🔧 Maintenance
- (if any)
```

Strip the commit hash prefix from each line — only include the commit message.

If `CHANGELOG.md` doesn't exist, create it with this header:

```markdown
# Changelog

All notable changes to FockMap will be documented in this file.
```

### Step 5: Update CITATION.cff

In `CITATION.cff`, update:
- `version:` to the new version
- `date-released:` to today's date (YYYY-MM-DD)

### Step 6: Commit, tag, and push

Run these commands in sequence:

```bash
git add src/Encodings/Encodings.fsproj CHANGELOG.md CITATION.cff
git commit -m "chore(release): vX.Y.Z"
git tag -a "vX.Y.Z" -m "Release vX.Y.Z"
git push origin HEAD
git push origin "vX.Y.Z"
```

### Step 7: Monitor CI

After pushing the tag, monitor the release workflow:

```bash
sleep 10
gh run list --workflow=release.yml --limit=1
gh run watch $(gh run list --workflow=release.yml --limit=1 --json databaseId -q '.[0].databaseId') --exit-status
```

Report the result to the user. On success, provide links:
- NuGet: `https://www.nuget.org/packages/FockMap/X.Y.Z`
- GitHub Release: `https://github.com/johnazariah/encodings/releases/tag/vX.Y.Z`

On failure, show the failed logs:
```bash
gh run view <run-id> --log-failed
```
