# Encodings Commit Workflow Prompt

You are preparing commits in the `encodings` repository.

## Goal

Create clean, logically grouped commits from the current working tree.

## Mandatory Workflow

1. Run the pre-commit quality gate exactly once at the start:
   - `dotnet build Encodings.sln && dotnet test Encodings.sln`
2. If the gate fails, stop and fix issues before any commit.
3. If any test files changed, update `.project/test-register.md` before creating commits.
   - Test files include additions/edits under `test/` (for example `.fs`, `.fsproj`).
   - Update summary counts, project counts, and test listings to match current code.
4. Group changes into related commit sets by concern (for example: core domain, tests, docs, tooling).
5. For each group:
   - Stage only files for that group.
   - Write an imperative commit message.
   - Commit.
6. Repeat until all intended changes are committed.
7. Confirm working tree is clean at the end.

## Commit Quality Rules

- Do not create one giant commit unless all changes are truly one concern.
- Keep commits reviewable and cohesive.
- Do not mix unrelated refactors with feature logic.
- Include test updates in the same commit as the behavior they validate when practical.
- When tests change, include the corresponding `.project/test-register.md` update in the same logical commit set.
- Avoid widening public API unnecessarily; prefer internal members with `InternalsVisibleTo` where needed.

## Message Convention

Use imperative mood and concise scope.

Examples:
- Add parity-set validation for tree encoding edge cases
- Enforce release workflow idempotency for NuGet publish
- Document test coverage register maintenance rules

## Safety

- Never bypass required gates.
- If changes are ambiguous, propose grouping options and choose the simplest coherent split.
- If merge conflicts or partial staging risks appear, stop and explain before proceeding.
