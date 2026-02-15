# Release Checklist

Use this checklist before every release to ensure nothing is missed.
The automated release script (`scripts/release.sh`) handles version
bumping, changelog, tagging, and pushing — but it cannot verify that
the *content* is ready.

---

## 1. Code Quality

- [ ] All tests pass: `dotnet test`
- [ ] Build succeeds in Release configuration: `dotnet build -c Release`
- [ ] No compiler warnings in core library (`src/Encodings/`)
- [ ] New public API has XML doc comments
- [ ] Examples run without error:
  ```bash
  for f in examples/*.fsx; do dotnet fsi "$f" && echo "✓ $f" || echo "✗ $f"; done
  ```

## 2. Test Register

- [ ] `.project/test-register.md` header count matches actual test count
  ```bash
  dotnet test --logger "console;verbosity=minimal" 2>&1 | grep -oP '\d+ Passed'
  ```
- [ ] Any new test sections are documented with plain-English descriptions
- [ ] Coverage summary table is up to date

## 3. Documentation (Cookbook)

- [ ] Cookbook chapter index (`docs/guides/cookbook/index.md`) lists all chapters
- [ ] Quick-reference encoding table includes all public encoding functions
- [ ] Quick-reference type table includes all public types
- [ ] New features have a corresponding cookbook chapter
- [ ] All `Next:`/`Back to:` navigation links are correct

## 4. Documentation (Theory & Guides)

- [ ] Theory pages reflect current capabilities (`docs/theory/`)
- [ ] Architecture guide matches current module structure
- [ ] Visual encoding guide is up to date (if encodings changed)
- [ ] Cross-platform guide references correct .NET SDK version

## 5. Documentation Site

- [ ] Docs build without errors:
  ```bash
  dotnet build && dotnet fsdocs build --clean --strict
  ```
- [ ] No broken links (check `.fsx` → `.html` references)
- [ ] Navigation links resolve (spot-check in `docs-output/`)

## 6. Papers

- [ ] **JOSS paper** (`paper-software/paper.md`):
  - [ ] YAML frontmatter has correct author, affiliation, date, tags
  - [ ] Test count matches actual count
  - [ ] Feature comparison table is current
  - [ ] All encoding types are mentioned
  - [ ] State of the field section is up to date
  - [ ] AI usage disclosure is accurate
  - [ ] Bibliography (`paper.bib`) includes all referenced works
  - [ ] Paper compiles via JOSS Draft Action or Docker:
    ```bash
    docker run --rm --volume $PWD/.research/paper-software:/data \
      --user $(id -u):$(id -g) --env JOURNAL=joss openjournals/inara
    ```
- [ ] **Cookbook paper** (`paper-cookbook/paper.tex`):
  - [ ] Chapter count in abstract matches actual
  - [ ] Quick-reference tables match cookbook index
  - [ ] New chapters have corresponding LaTeX sections
  - [ ] Bibliography includes all referenced works
- [ ] Papers compile:
  ```bash
  make -C .research clean && make -C .research all
  ```

## 7. Package Metadata

- [ ] `Encodings.fsproj` description is accurate
- [ ] Package tags include new feature keywords
- [ ] README summary matches current capabilities
- [ ] `CITATION.cff` has correct author info and repo URL

## 8. Changelog & Versioning

- [ ] Commits since last release follow conventional commit format
- [ ] Breaking changes (if any) are documented
- [ ] Version bump matches scope of changes:
  - **PATCH**: bug fixes, doc tweaks
  - **MINOR**: new features (backward compatible)
  - **MAJOR**: breaking API changes

## 9. Release Execution

- [ ] Dry run passes: `./scripts/release.sh --dry-run`
- [ ] Review proposed version and changelog entry
- [ ] Execute release: `./scripts/release.sh`
  - Or dispatch via GitHub: VS Code task "Release: Dispatch via GitHub (auto)"
- [ ] CI passes on all platforms (ubuntu, windows, macos)
- [ ] NuGet package published
- [ ] GitHub Release created with PDFs attached

## 10. Post-Release

- [ ] Verify NuGet listing: `https://www.nuget.org/packages/FockMap/`
- [ ] Verify GitHub Release page has correct assets
- [ ] Documentation site updated (GitHub Pages deploy)
- [ ] Announce release (if applicable)

---

## Quick Commands

| Task | Command |
|------|---------|
| Run all tests | `dotnet test` |
| Build Release | `dotnet build -c Release` |
| Build docs | `dotnet build && dotnet fsdocs build --clean --strict` |
| Build papers | `make -C .research clean && make -C .research all` |
| Dry run release | `./scripts/release.sh --dry-run` |
| Execute release | `./scripts/release.sh` |
| Count tests | `dotnet test --logger "console;verbosity=minimal" 2>&1 \| grep -oP '\d+ Passed'` |
| Run examples | `for f in examples/*.fsx; do dotnet fsi "$f"; done` |

## Files Updated by Release Script

The `scripts/release.sh` script automatically updates:

1. `src/Encodings/Encodings.fsproj` — `<Version>` tag
2. `CHANGELOG.md` — prepends new release section
3. `CITATION.cff` — `version` and `date-released` fields

It then commits, tags (`vX.Y.Z`), and pushes. The `release.yml` workflow
builds, tests (3 platforms), packs NuGet, compiles papers, and publishes.
