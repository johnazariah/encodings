# ADR-012: Algebra Selection via Typeclass-Style Constraint

**Date:** —
**Status:** Accepted
**Source:** Code analysis — `LadderOperatorSequence.fs`, `Bosonic.fs`, `MixedSystems.fs`

## Context

The library must support fermionic (CAR) and bosonic (CCR) normal ordering
using the same structural pipeline but different combining rules:

- **Fermionic**: swapping two operators produces a −1 sign and a δ contraction
- **Bosonic**: swapping produces +1 sign and a δ contraction (no sign change)

The question is how to parameterize the normal-ordering pipeline over the
algebra type without runtime overhead or loss of type safety.

## Decision

Use a typeclass-via-constraint pattern. `LadderOperatorSumExpr<'algebra>`
takes a type parameter with constraints:

```fsharp
type LadderOperatorSumExpr<'algebra
    when 'algebra :> ICombiningAlgebra<LadderOperatorUnit>
    and  'algebra : (new : unit -> 'algebra)> =
    static member private CombiningAlgebra = new 'algebra()
```

The algebra is instantiated once (static member) and accessed internally.
Domain types are then aliased:

```fsharp
type LadderOperatorSumExpression = LadderOperatorSumExpr<FermionicAlgebra>
type BosonicLadderOperatorSumExpression = LadderOperatorSumExpr<BosonicAlgebra>
```

These are **completely different types at compile time** — you cannot pass
a fermionic expression where a bosonic one is expected.

## Consequences

- **Type safety**: Mixing fermionic and bosonic expressions is a compile-time
  error, not a runtime bug. This is critical for mixed fermion-boson systems
  where operators from different sectors must be processed separately.
- **Zero runtime overhead**: The algebra is a static member — no dictionary
  lookup, no virtual dispatch. The JIT can inline the combining function.
- **Mixed systems**: `MixedSystems.fs` handles cross-sector interactions by
  partitioning expressions, normal-ordering each sector independently, then
  combining. Cross-sector swaps are trivially commuting (no sign change).
- **Extensibility**: Adding a new algebra (e.g., anyonic) requires only
  implementing `ICombiningAlgebra` — the entire pipeline is free.

## Alternatives Considered

- **Runtime polymorphism** (passing algebra as constructor argument) — allows
  mixing types at runtime, which is a bug source for physics code. Rejected.
- **F# SRTP** (statically resolved type parameters) — more idiomatic F# but
  harder to express the constraint cleanly. Current approach uses standard
  .NET interface + constructor constraint. Rejected.
- **Separate implementations** for fermionic and bosonic — massive code
  duplication. Rejected.

## References

- [LadderOperatorSequence.fs](../../src/Encodings/LadderOperatorSequence.fs) — `LadderOperatorSumExpr<'algebra>`
- [CombiningAlgebra.fs](../../src/Encodings/CombiningAlgebra.fs) — `ICombiningAlgebra`, `FermionicAlgebra`, `BosonicAlgebra`
- [Bosonic.fs](../../src/Encodings/Bosonic.fs) — `BosonicLadderOperatorSumExpression` alias
- [MixedSystems.fs](../../src/Encodings/MixedSystems.fs) — sector partitioning
