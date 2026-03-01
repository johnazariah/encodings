# ADR-014: Coefficient Factory Pattern for Sparse Integrals

**Date:** —
**Status:** Accepted
**Source:** Code analysis — `Hamiltonian.fs`

## Context

Hamiltonian construction requires looking up one-body (h_ij) and two-body
(h_ijkl) molecular integrals for each index combination. The integrals
can come from many sources:

- JSON files (PySCF / OpenFermion export)
- In-memory arrays
- Databases
- Lazy calculations (e.g., on-the-fly integral evaluation)

Most integral tensors are sparse — for an n-mode system, the full two-body
tensor has n⁴ entries but symmetry and selection rules make most zero.
A dense 4D array wastes memory; a sparse matrix type adds complexity.

## Decision

Define a **coefficient factory** as a simple function:

```fsharp
type CoefficientFactory = string -> Complex option
```

The function takes a comma-separated index key (e.g., `"0,1,2,3"`) and
returns `Some coefficient` if the integral exists, or `None` if it is zero
or absent. Hamiltonian construction calls this function for each index
combination and skips `None` results.

## Consequences

- **Dependency inversion**: Hamiltonian.fs knows nothing about storage
  format. The factory can be backed by a `Dictionary`, a `JsonElement`,
  a closure over an array, or a database query.
- **Natural sparsity**: `None` represents zero without requiring sparse
  matrix infrastructure. The construction loop simply doesn't yield a
  term for missing keys.
- **Composability**: Factories can be composed — e.g., a "union factory"
  that checks multiple sources, or a "scaled factory" that multiplies
  all values by a constant.
- **String key overhead**: Every lookup formats indices into a string. For
  the n⁴ loop this is ~20,000 `sprintf` calls per encoding. Profiling shows
  this is negligible compared to Pauli algebra.
- **Skeleton integration**: The sparse skeleton API (`computeHamiltonianSkeletonFor`)
  uses the factory to discover which keys exist, then builds skeleton entries
  only for those — reducing 17,424 entries to ~1,520 for H₂O.

## Alternatives Considered

- **Dense 2D/4D arrays** — simple indexing but wastes memory for sparse
  systems. Would require separate signatures for one-body (2D) and two-body
  (4D) integrals. Rejected.
- **Sparse matrix types** (`Dictionary<(int*int*int*int), Complex>`) —
  type-safe but requires two distinct factory types for one-body vs two-body.
  The string-keyed approach unifies both under one signature. Rejected.
- **Generic key type** (`'key -> Complex option`) — more flexible but the
  key format must be agreed between producer and consumer. String keys are
  self-describing and debug-friendly. Rejected.

## References

- [Hamiltonian.fs](../../src/Encodings/Hamiltonian.fs) — `computeHamiltonianWith`, coefficient factory usage
- [ADR-001](001-comma-separated-key-format.md) — key format decision
- [ADR-002](002-skeleton-precomputation.md) — skeleton integration with factory
