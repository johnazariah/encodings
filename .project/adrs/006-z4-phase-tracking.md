# ADR-006: Z₄ Phase Tracking via Discriminated Union

**Date:** 2026-02-09
**Status:** Accepted
**Source:** Code analysis — `Pauli.fs`, `PauliRegister.fs`

## Context

Pauli operator multiplication produces phases from the set {+1, −1, +i, −i} —
the cyclic group Z₄. For example: X·Y = iZ, Y·X = −iZ, X·X = I.

The natural implementation in .NET would use `System.Numerics.Complex` for
phase tracking. But complex floating-point arithmetic accumulates rounding
errors: after many multiplications, `i * i` might produce `(-1.0000000000002, 0.0)`
instead of exactly `(-1, 0)`.

For a symbolic algebra library where **algebraic exactness** is a core
requirement, any floating-point drift in phases would silently corrupt
Hamiltonian coefficients.

## Decision

Track Pauli phases as a discriminated union with exact finite-group arithmetic:

```fsharp
type Phase = P1 | Pn1 | Pi | Pni  // +1, −1, +i, −i
```

Phase multiplication is a lookup table (4×4 → 4 entries). Conversion to
`Complex` happens only at the boundary, when constructing final
`PauliRegister` objects with numerical coefficients.

## Consequences

- **Exact arithmetic**: Phase tracking is algebraically exact — no floating-point
  error regardless of multiplication chain length.
- **Performance**: Pattern matching on a 4-variant DU is faster than complex
  multiplication.
- **Type safety**: Cannot accidentally add or subtract phases — only multiply.
  The Z₄ group operation is the only operation defined.
- **Boundary conversion**: Coefficients in `C<'unit>` use `Complex`. The phase
  DU lives only in the Pauli multiplication layer. This is a clean separation:
  exact phase algebra inside, numerical coefficients at the boundary.

## Alternatives Considered

- **`System.Numerics.Complex`** for phases — simpler code, but precision loss.
  Rejected for a library whose raison d'être is exact symbolic computation.
- **Rational arithmetic** (`int * int` fractions) — more general but overkill.
  Phases are always in Z₄, never fractions. Rejected.
- **Integer encoding** (0,1,2,3 for +1,+i,−1,−i with modular addition) —
  equivalent mathematically but less self-documenting. The DU names make
  multiply-table code readable. Rejected.

## References

- [PauliRegister.fs](../../src/Encodings/PauliRegister.fs) — Phase type and multiplication
