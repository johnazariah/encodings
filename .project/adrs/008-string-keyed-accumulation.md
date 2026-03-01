# ADR-008: String Representation as Canonical Identity Key

**Date:** —
**Status:** Accepted
**Source:** Code analysis — `Terms.fs` (S.ApplyInternal), `PauliRegister.fs` (AddToDictionary)

## Context

Like-term combination is the most fundamental operation in the algebra:
when two products have identical operator content, their coefficients
should be summed. The question is how to determine "identical operator
content" efficiently.

Structural equality comparison (comparing array contents element by element)
is correct but slow for map-based accumulation — it requires custom
`IEqualityComparer` implementations and doesn't produce a natural ordering
for `Map` keys.

## Decision

Use `ToString()` as the canonical identity key at two levels:

1. **`S<'unit>`**: Like-term combination in `S.ApplyInternal` uses
   `P.ToString()` as the `Map` key. Two products that print identically
   are combined by summing coefficients.

2. **`PauliRegisterSequence`**: Uses `PauliRegister.Signature` (a string
   like `"XYZII"`) as the `Dictionary` key. Two Pauli strings with the
   same operator content (ignoring phase) accumulate into one entry.

This means **correctness of the entire algebra depends on `ToString()`
being a faithful canonical representation**. Any bug in `ToString()` would
silently corrupt like-term combination.

## Consequences

- **Performance**: String comparison and hashing are highly optimised in .NET.
  `Map<string, _>` and `Dictionary<string, _>` are fast.
- **Debugging**: Keys are human-readable — you can inspect the map to see
  exactly which terms are present.
- **Fragility**: Two semantically identical terms that format differently
  (e.g., different whitespace, different coefficient formatting) will not
  be combined. The `Reduce` operation must normalise format before
  `ToString()` is used as a key.
- **Skeleton optimisation**: The skeleton API pre-computes `Signature` strings
  at build time and reuses them during coefficient application — this decision
  enabled the 71× speedup in ADR-002.

## Alternatives Considered

- **Structural equality** (`IEquatable<T>`, `IStructuralEquatable`) — correct
  but requires implementing custom comparers for every type. Slower for
  map operations. Rejected.
- **Hash-based identity** (integer hash of operator content) — risk of
  collisions. String keys are collision-free (they *are* the canonical form).
  Rejected.
- **Canonical integer encoding** (e.g., ternary encoding of Pauli strings) —
  compact but loses human readability. Rejected.

## References

- [Terms.fs](../../src/Encodings/Terms.fs) — `S.ApplyInternal`, `P.ToString()`
- [PauliRegister.fs](../../src/Encodings/PauliRegister.fs) — `Signature`, `AddToDictionary`
- [Hamiltonian.fs](../../src/Encodings/Hamiltonian.fs) — `SkeletonTerm.Signature`
