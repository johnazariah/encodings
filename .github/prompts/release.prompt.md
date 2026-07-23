# Release FockMap

You are responsible for preparing and executing a release of the FockMap library.

## Context

- This is an F# library at `/workspaces/encodings`
- The NuGet package name is **FockMap** (PackageId in `src/Encodings/Encodings.fsproj`)
- The repository is `github.com/johnazariah/encodings`
- Releases use [conventional commits](https://www.conventionalcommits.org/) to determine version bumps
- Pushing a `v*` tag triggers `.github/workflows/release.yml` which runs multi-platform tests and publishes to NuGet

> ## 🚀 Releasing the staged 0.9.0 (deliberate BREAKING release)
>
> **0.9.0 is a deliberate breaking release** (the public Hamiltonian API now consumes
> RAW single-bar physicist integrals — see the [0.9.0 migration guide](../../docs/guides/migration-0.9.md)).
> Its version (`0.9.0`), `CITATION.cff` version, and the `## [0.9.0]` CHANGELOG entry
> are **already staged in the repository**. Ship it *without* bumping — do **not** let
> the breaking commit auto-bump it to `1.0.0`:
>
> ```bash
> # Preview (works on macOS and Linux):
> ./scripts/release.sh --dry-run current      # → "would release v0.9.0"
> # Execute the staged release:
> ./scripts/release.sh current
> ```
>
> or via CI: `gh workflow run release-dispatch.yml -f bump=current`.
>
> The `current`/`staged` mode: (1) validates the staged version is `> ` the latest tag
> and that `CITATION.cff`/`CHANGELOG.md` already match it; (2) finalizes the
> `## [0.9.0] - Unreleased` heading to today's date (no duplicate entry); (3) sets
> `CITATION.cff` `date-released` (add-or-replace); (4) tags and packages `v0.9.0`.
> The normal `auto`/`major`/`minor`/`patch` modes below remain for future releases.

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

Choose the mode:
- **`current` / `staged`** — ship the current `.fsproj` version **with no bump**. Use this
  whenever the version + CHANGELOG + CITATION are already staged for a specific release
  (this is how 0.9.0 ships without becoming 1.0.0).
- Otherwise apply the bump rules:
  - If this is the **first release** (no prior tags): use the current version as-is
  - **breaking changes**: bump MAJOR (e.g., `0.9.0` → `1.0.0`)
  - **features**: bump MINOR (e.g., `0.9.0` → `0.10.0`)
  - Otherwise: bump PATCH (e.g., `0.9.0` → `0.9.1`)

Present the analysis and proposed version to the user. **Ask for confirmation before proceeding.**

### Step 3: Update version in .fsproj

In `src/Encodings/Encodings.fsproj`, update the `<Version>` element to the new version.
(In `current`/`staged` mode the version is unchanged, so there is nothing to edit.)

### Step 4: Generate or finalize the CHANGELOG entry

- **Bump modes:** create a new entry at the top (after the header) using the format below.
- **`current`/`staged` mode:** the entry already exists as `## [X.Y.Z] - Unreleased` with
  curated notes — **finalize** it by replacing `Unreleased` with today's date. Do **not**
  prepend a second entry.

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

### Step 5: Update CITATION.cff (date add-or-replace)

In `CITATION.cff`, set:
- `version:` to the new version
- `date-released:` to today's date (YYYY-MM-DD) — **whether or not the field already exists**

The `date-released` field may be absent (it is deliberately absent while a version is
unreleased). The release tooling uses `rl_set_cff_version_and_date`
(`scripts/lib/release-lib.sh`) which **adds it adjacent to `version:` when absent, or
replaces the single existing key**, fails on duplicate/malformed keys, and validates that
the result parses as YAML. Do not hand-edit with a plain `sed` substitution that silently
no-ops when the key is missing.

### Step 6: Verify README freshness

Before committing, review `README.md` for accuracy:

1. **Dead links**: Check that every docs link points to a page that still exists (e.g., guides that were merged or deleted).
2. **New content**: If new documentation tiers were added (cookbook chapters, theory pages, labs), make sure they appear in the "Where to Start" and "Documentation" sections.
3. **Feature table**: If the feature comparison table or "Available Encodings" table is out of date, update it.
4. **Examples table**: Verify that all scripts listed in the "Examples" section still exist in `examples/`.

If any changes are needed, edit `README.md` and include it in the release commit.

### Step 7: Commit, tag, and push

Run these commands in sequence (or just run `./scripts/release.sh current` / the chosen
mode, which performs all of this):

```bash
git add src/Encodings/Encodings.fsproj CHANGELOG.md CITATION.cff README.md
git commit -m "chore(release): vX.Y.Z"
git tag -a "vX.Y.Z" -m "Release vX.Y.Z"
git push origin HEAD
git push origin "vX.Y.Z"
```

### Step 8: Monitor CI

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

### Step 9: Post-release provenance checks

After the package publishes, verify the released artifact matches what was staged:

1. **Version surfaces agree** — `.fsproj` `<Version>`, `CITATION.cff` `version:`, the
   published NuGet version, and the `vX.Y.Z` git tag are all identical.
2. **CITATION finalized** — `CITATION.cff` now has exactly one `date-released:` equal to
   the release date, and the file parses (`python3 -c "import yaml; yaml.safe_load(open('CITATION.cff'))"`).
3. **CHANGELOG finalized** — the `## [X.Y.Z]` heading is dated (no lingering `Unreleased`).
4. **Fixture provenance intact (breaking-release audit)** — the vendored research fixture
   is unchanged: `test/Test.Encodings/fixtures/physicist_spin_integrals.json` still has
   SHA-256 `6539afb3…` and git blob `e0477e70…` (the `HamiltonianFixtureLock` tests assert
   this). A release must never alter the audited fixture.
5. **NuGet smoke test** — install the published package into a scratch project and confirm
   it restores and the public API resolves.

