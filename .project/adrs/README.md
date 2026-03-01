# Architecture Decision Records

This directory captures key design decisions for the FockMap library —
both forward-looking choices and retrospective documentation of decisions
visible in the codebase.

## Format

Each ADR follows a lightweight template:

- **Date** — when the decision was made (or first visible in code)
- **Status** — Accepted / Superseded / Proposed
- **Source** — how the decision was discovered (journal, code analysis, session)
- **Context** — the problem or situation that prompted the decision
- **Decision** — what was decided
- **Consequences** — what follows from this decision
- **Alternatives Considered** — what was rejected and why

## Index

### Session & Journal Decisions

| # | Title | Status | Date |
|---|-------|--------|------|
| [001](001-comma-separated-key-format.md) | Comma-separated key format for integral indices | Accepted | 2026-02-28 |
| [002](002-skeleton-precomputation.md) | Skeleton precomputation and parallel Hamiltonian construction | Accepted | 2026-02-28 |
| [003](003-autoopen-modules.md) | AutoOpen on all modules | Accepted | 2026-02-09 |
| [004](004-three-constructions.md) | Three encoding constructions (A / B / F) | Accepted | 2026-02-09 |
| [005](005-three-paper-consolidation.md) | Three-paper architecture consolidation | Accepted | 2026-02-27 |
| [006](006-z4-phase-tracking.md) | Z₄ phase tracking via discriminated union | Accepted | 2026-02-09 |

### Code-Deduced Decisions

| # | Title | Status | Date |
|---|-------|--------|------|
| [007](007-generic-algebraic-framework.md) | Generic algebraic framework with `'unit` parameter | Accepted | — |
| [008](008-string-keyed-accumulation.md) | String representation as canonical identity key | Accepted | — |
| [009](009-lazy-reduce-and-nan-collapse.md) | Lazy evaluation for normalization; NaN/Inf → zero | Accepted | — |
| [010](010-encoding-scheme-as-function-record.md) | Encoding scheme as record of three functions | Accepted | — |
| [011](011-swap-tracking-sort.md) | O(n²) selection sort with swap tracking | Accepted | — |
| [012](012-typeclass-via-constraint.md) | Algebra selection via typeclass-style constraint | Accepted | — |
| [013](013-symbolic-over-matrices.md) | Symbolic algebra over matrix decomposition | Accepted | — |
| [014](014-coefficient-factory-pattern.md) | Coefficient factory pattern for sparse integrals | Accepted | — |
