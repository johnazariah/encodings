# ADR-002: Skeleton Precomputation and Parallel Hamiltonian Construction

**Date:** 2026-02-28
**Status:** Accepted
**Source:** Performance profiling during H₂O PES scan workshop

## Context

Hamiltonian construction has an O(n⁴) two-body loop: for each index tuple
(i,j,k,l), look up the integral coefficient, encode the corresponding
ladder operators into Pauli strings, and accumulate. For H₂O with 12
spin-orbitals, this is 20,736 index combinations per encoding.

Two performance problems emerged during a 25-point bond-angle scan:

1. **Sequential n⁴ loop**: F# `Array.map` uses one core. The user's 20-CPU
   machine was 95% idle. Wall time: ~100s per encoding.

2. **Redundant Pauli algebra**: For a PES scan, the molecular geometry changes
   but the Pauli *structure* (which operators, which signs) is fixed by the
   encoding — only the integral *coefficients* change. Rebuilding 75
   Hamiltonians (25 angles × 3 encodings) repeated identical Pauli algebra
   75 times.

## Decision

Two new API layers:

### Parallel construction

`computeHamiltonianWithParallel` filters valid index tuples sequentially
(cheap), then uses `Array.Parallel.map` over the encoding work (expensive).
Each index tuple's encoding is independent — no shared mutable state.

### Skeleton precomputation (structure/coefficient separation)

`computeHamiltonianSkeleton` builds the Pauli structure once, producing a
`HamiltonianSkeleton` containing `SkeletonEntry[]` for one-body and two-body
terms. Each `SkeletonEntry` holds:

- `Key: string` — the index key (e.g., `"0,1,2,3"`)
- `Terms: SkeletonTerm[]` — pre-computed Pauli terms, each with:
  - `Signature: string` — pre-cached (avoids recomputing `Array.map (sprintf "%A")`)
  - `Operators: Pauli[]` — pre-cached (avoids `ResetPhase` array copy)
  - `StructuralCoeff: Complex` — the encoding's phase contribution

`applyCoefficients` then dresses a skeleton with per-geometry coefficients.
It accumulates directly into a `Dictionary<string, Complex>` keyed by
pre-computed signatures — no `PauliRegister` construction, no `Signature`
recomputation. `PauliRegister` objects are created only for the ~44 final
combined terms.

A sparse variant, `computeHamiltonianSkeletonFor`, takes a coefficient
factory and builds skeleton entries only for keys that have non-zero
coefficients — reducing entries from 17,424 (full n⁴) to ~1,520 for H₂O.

## Consequences

- **Performance**: Total wall time for 75 Hamiltonians dropped from 5:00 to 1:28.
  - Skeleton build: 7.8s (sparse) vs 77s (full) vs 300s (sequential)
  - Coefficient application: 2s for 75 Hamiltonians (was 150s)
- **API surface**: Three new public functions + three new types. The sequential
  API remains unchanged for simple use cases.
- **Correctness**: 26 new tests verify parallel matches sequential, skeleton +
  apply matches sequential, and sparse matches full — for JW, BK, and ternary
  tree encodings.
- **Memory**: Skeleton caches `SkeletonTerm[]` arrays. For H₂O sparse, this is
  ~1,520 entries × ~4 terms each ≈ 6,000 `SkeletonTerm` records in memory.
  Negligible for current problem sizes.

## Alternatives Considered

- **Parallelize the inner loop only** (each index tuple in parallel, but
  rebuild at each geometry) — gives ~4× speedup but not the 71× from
  skeleton reuse. Implemented as the parallel API; skeleton is the bigger win.
- **Cache individual `PauliRegisterSequence` results** (memoization) — would
  require a concurrent dictionary and careful key design. The skeleton approach
  is simpler because it separates the problem cleanly.
- **Pauli-frame tracking** (a circuit-level runtime technique) — different
  abstraction level. Skeleton precomputation is a compile-time classical
  optimization; frame tracking is a quantum runtime technique.

## References

- Commit `25467a4` — implementation
- [Hamiltonian.fs](../../src/Encodings/Hamiltonian.fs) — all new APIs
