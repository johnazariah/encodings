# ADR-010: Encoding Scheme as Record of Three Functions

**Date:** —
**Status:** Accepted
**Source:** Code analysis — `MajoranaEncoding.fs`

## Context

An index-set encoding (Construction A) is defined by three functions that map
a mode index `j` to sets of qubit indices:

- **Update(j)** — qubits to flip when occupation of mode j changes
- **Parity(j)** — qubits whose parity encodes the parity of modes < j
- **Occupation(j)** — qubits that store the occupation number of mode j

Different encodings (JW, BK, Parity) differ only in these three set-valued
functions. The design question is how to represent "an encoding."

## Decision

Use a plain F# record holding three functions:

```fsharp
type EncodingScheme =
    { Update     : int -> int -> Set<int>
      Parity     : int -> Set<int>
      Occupation  : int -> Set<int> }
```

Specific encodings are record literals:

```fsharp
let jordanWignerScheme : EncodingScheme =
    { Update = fun _ _ -> Set.empty
      Parity = fun j -> set [0..j-1]
      Occupation = fun j -> Set.singleton j }
```

## Consequences

- **Simplicity**: No interfaces, no virtual dispatch, no class hierarchy.
  Adding a new encoding is one `let` binding.
- **First-class functions**: Encoding schemes can be composed, partially
  applied, or computed at runtime (e.g., from a tree structure).
- **No extensibility protocol**: There's no `IEncoding` interface to
  implement. This is fine for a library where encodings are defined
  centrally, not by downstream users.
- **Testability**: Each function can be tested independently — verify that
  `bravyiKitaevScheme.Parity 5` returns the expected set.
- **Parametric Fenwick tree**: The BK scheme's functions are computed from a
  `FenwickTree` that is itself parametric over combine/identity — the record
  approach composes naturally with this.

## Alternatives Considered

- **Interface/abstract class** (`IEncodingScheme`) — OOP pattern. Adds
  ceremony (class declaration, constructor, method signatures) for no
  benefit. F# functions are already first-class. Rejected.
- **Discriminated union** (`JW | BK | Parity`) — closed set of encodings.
  Prevents users from defining custom schemes. Rejected.
- **Module with functions** (e.g., `JordanWigner.update`, `JordanWigner.parity`)
  — can't pass "an encoding" as a single value. Rejected.

## References

- [MajoranaEncoding.fs](../../src/Encodings/MajoranaEncoding.fs) — `EncodingScheme` definition
- [BravyiKitaev.fs](../../src/Encodings/BravyiKitaev.fs) — BK scheme constructed from Fenwick tree
