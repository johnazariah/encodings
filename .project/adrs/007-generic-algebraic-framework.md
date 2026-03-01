# ADR-007: Generic Algebraic Framework with `'unit` Type Parameter

**Date:** —
**Status:** Accepted
**Source:** Code analysis — `Terms.fs`, `IndexedTerms.fs`

## Context

The library needs to represent symbolic algebraic expressions in two domains:

1. **Second-quantized**: sums of products of ladder operators (a†, a)
2. **Qubit**: sums of products of Pauli operators (X, Y, Z, I)

Both domains share the same algebraic structure: coefficient × product of
units, summed with like-term combination. The question is whether to implement
this twice or abstract over the unit type.

## Decision

Parameterise the entire algebraic framework over a generic `'unit` type:

```fsharp
type C<'unit> = { Coeff : Complex; Item : 'unit }     // coefficient × unit
type P<'unit> = { Coeff : Complex; Units : C<'unit>[] } // product of units
type S<'unit> = { Coeff : Complex; Terms : Map<string, P<'unit>> } // sum of products
```

The only constraint is `'unit : equality`. The same `*`, `+`, `Reduce`,
`Apply` implementations serve both `S<LadderOperatorUnit>` (pre-encoding)
and `S<PauliRegister>` (post-encoding).

### Multi-level coefficients

Coefficients exist at three levels: each `C` has a coefficient, each `P` has
an overall coefficient, and `S` has a global coefficient. During `Reduce`,
unit-level coefficients fold upward into the product coefficient. This
supports lazy accumulation — intermediate computations attach coefficients at
whatever level is convenient.

### Static `Apply` factories

All types use overloaded `static member Apply` as the universal factory method
(7+ overloads on `P`, 10+ on `S`). This was likely inspired by Scala's
`apply()` companion object pattern.

### Single-case DU wrappers for domain safety

Domain-specific types like `LadderOperatorProductTerm` and `PIxOp` are
single-case discriminated unions wrapping the generic records. This adds
type safety — you cannot accidentally mix a `PIxOp<uint32, Pauli>` with
a `PIxOp<uint32, LadderOperatorUnit>`.

## Consequences

- **Code reuse**: One implementation of algebraic operations for all domains.
  No duplication between fermionic and Pauli algebra.
- **Extensibility**: Adding a new operator type (e.g., Majorana operators,
  spin operators) requires only defining the unit type — the algebra is free.
- **Terse names**: `C`, `P`, `S` are intentionally short — idiomatic for
  algebraic types in ML-family languages, but opaque to newcomers.
  Documented extensively; renaming deferred post-JOSS (would break 300+ tests).
- **Overload complexity**: F# resolves overloads less eagerly than C#.
  Callers sometimes need type annotations to disambiguate `Apply` variants.

## Alternatives Considered

- **Separate type hierarchies** per domain — duplicates all algebraic logic.
  Rejected.
- **Object-oriented inheritance** (abstract base class for algebraic
  expressions) — poor fit for F# idioms. DUs and records are more natural.
  Rejected.
- **Type classes** (via SRTP or interfaces) — F# support is incomplete.
  The generic parameter approach is simpler and sufficient. Rejected.

## References

- [Terms.fs](../../src/Encodings/Terms.fs) — `C<'unit>`, `P<'unit>`, `S<'unit>`
- [IndexedTerms.fs](../../src/Encodings/IndexedTerms.fs) — `IxOp`, `PIxOp`, `SIxOp`
