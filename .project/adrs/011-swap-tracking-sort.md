# ADR-011: O(n²) Selection Sort with Swap Tracking

**Date:** —
**Status:** Accepted
**Source:** Code analysis — `SwapTrackingSort.fs`

## Context

Fermionic normal ordering requires sorting ladder operators (all creation
operators left, annihilation operators right, within each group sorted by
index). Each adjacent transposition of two fermionic operators introduces a
factor of −1 due to the canonical anticommutation relations (CAR).

The sign of the final expression depends on the **total number of
transpositions** performed — equivalently, the parity of the permutation.
A standard sort algorithm (merge sort, quicksort) doesn't expose this count.

## Decision

Use O(n²) selection sort, parameterised by a tracking function that
accumulates how far each element moves:

```fsharp
type SwapTrackingSort<'a, 'coeff>
    (compareFunction, zero, trackingFunction) =
    // For each element placed, records the distance it traveled
```

The tracking function receives the displacement (number of positions moved)
for each element insertion. For fermionic normal ordering, each position
of displacement corresponds to one adjacent swap, so the total displacement
determines the (−1)ⁿ sign factor.

## Consequences

- **Correctness**: The swap count is exact — selection sort performs the
  minimum number of moves to place each element, and the tracking function
  sees every one.
- **Performance**: O(n²) where n is the number of operators in a single
  product term. Since n is typically 2–4 (one-body: 2 operators, two-body:
  4 operators), the quadratic cost is irrelevant. Even n = 100 would take
  microseconds.
- **Generality**: The tracking function is generic — it could count swaps,
  accumulate phases, or record the full transposition sequence. The
  fermionic case uses it for sign tracking; the bosonic case uses it for
  combinatorial factors.
- **Separation of concerns**: The sort knows nothing about physics. The
  physics (CAR signs, CCR commutators) lives in the tracking function.

## Alternatives Considered

- **Merge sort with swap counting** — O(n log n) but harder to instrument
  for exact displacement tracking. Overkill for n ≤ 4. Rejected.
- **Permutation parity via cycle decomposition** — compute the permutation,
  decompose into cycles, count parity. More mathematically elegant but
  requires the final sorted order to be known in advance. The sort-based
  approach works online (streaming). Rejected.
- **Direct anticommutator application** — swap operators one at a time,
  applying {aᵢ, aⱼ†} = δᵢⱼ at each step. Correct but generates many
  intermediate terms. The sort approach avoids this by computing the sign
  factor directly. Rejected for normal ordering; used separately for
  operator algebra where intermediate terms matter.

## References

- [SwapTrackingSort.fs](../../src/Encodings/SwapTrackingSort.fs) — implementation
- [CombiningAlgebra.fs](../../src/Encodings/CombiningAlgebra.fs) — `ICombiningAlgebra`, fermionic/bosonic tracking functions
