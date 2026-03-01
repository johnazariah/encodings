# ADR-013: Symbolic Algebra Over Matrix Decomposition

**Date:** 2026-02-09
**Status:** Accepted
**Source:** Research journal — ParityOperator investigation; code analysis — fermionic vs bosonic encoding paths

## Context

There are two approaches to computing Pauli decompositions of operators:

1. **Matrix decomposition**: Build the operator as a 2ⁿ × 2ⁿ matrix, then
   decompose via Tr(σ_α · O) / 2ⁿ for each Pauli string σ_α. This scales
   as O(4ⁿ) — one trace per Pauli string, with 4ⁿ possible strings.

2. **Symbolic algebra**: Use the algebraic structure of the Pauli group
   (closure under multiplication, finite group) to compute products
   symbolically. This is polynomial-time for sparse operators.

The matrix approach crashed at n = 10 (4¹⁰ ≈ 10⁶ traces, each requiring
a 1024 × 1024 matrix multiply). The symbolic approach computed the n = 100
parity operator in 5.5 seconds.

## Decision

Use symbolic `PauliRegisterSequence` multiplication exclusively for
fermionic encodings. Matrix verification is a cross-check for small n only.

**Exception**: Bosonic binary and Gray-code encodings use matrix decomposition
because no closed-form algebraic construction is known for those encodings.
The unary bosonic encoding does have a closed form (σ⁺σ⁻ pairs) and uses
direct algebraic construction.

This creates a principled split in the codebase:

- `JordanWigner.fs`, `MajoranaEncoding.fs`, `TreeEncoding.fs` →
  purely algebraic
- `BosonicEncoding.fs` (`matrixBosonTerms`) → matrix decomposition for
  binary/Gray, algebraic for unary

## Consequences

- **Scalability**: Fermionic encodings scale to n = 100+ qubits. (Hamiltonian
  construction is dominated by the n⁴ integral loop, not the encoding itself.)
- **Exactness**: Symbolic multiplication preserves algebraic exactness —
  phases are tracked via Z₄ (ADR-006), not floating-point.
- **Bosonic limitation**: Binary/Gray bosonic encodings are limited to small
  truncation cutoffs (d ≲ 16) due to the 2^q matrix size. This is a known
  limitation, not a design flaw — those encodings are intrinsically expensive.
- **No matrix types in core library**: The library has no general matrix type
  or linear algebra dependency. `BosonicEncoding.fs` uses `Complex[,]` directly
  for its local matrix operations.

## Alternatives Considered

- **Matrix decomposition for everything** — simple, universal, but exponential.
  Rejected for fermionic encodings.
- **Symbolic construction for bosonic binary/Gray** — no known closed form.
  An open research problem. Deferred.
- **External linear algebra library** (MathNet.Numerics, etc.) — adds a
  dependency for one module's internal computations. The 2D array approach
  is self-contained. Rejected.

## References

- [PauliRegister.fs](../../src/Encodings/PauliRegister.fs) — symbolic Pauli multiplication
- [BosonicEncoding.fs](../../src/Encodings/BosonicEncoding.fs) — `matrixBosonTerms` (matrix path), `unaryBosonTerms` (algebraic path)
- Research journal, 2026-02-09: ParityOperator investigation
