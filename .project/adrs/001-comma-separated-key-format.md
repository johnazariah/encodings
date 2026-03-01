# ADR-001: Comma-Separated Key Format for Integral Indices

**Date:** 2026-02-28
**Status:** Accepted
**Source:** Debugging session — key collision bug discovered during H₂O workshop

## Context

Hamiltonian construction maps one-body and two-body integrals to Pauli
strings. Each integral is identified by a string key built from mode
indices (e.g., `"01"` for h₁₂, `"0123"` for h₁₂₃₄). The coefficient
factory receives this key and returns the corresponding integral value.

The original key format used `sprintf "%u%u"` (no separator), producing
keys like `"01"`, `"12"`, `"123"`. For two-body integrals with
`sprintf "%u%u%u%u"`, this created ambiguous keys: `"1234"` could mean
indices (1,2,3,4) or (12,3,4) or (1,23,4) etc.

With H₂O's 12 spin-orbitals, indices 10 and 11 appeared, and keys like
`"1011"` (indices 10,11) collided with `"10,1,1"` under the old format.
This caused **silent coefficient errors** — wrong integrals mapped to wrong
operator terms, producing an incorrect Hamiltonian with no error or warning.

## Decision

Use comma-separated keys: `sprintf "%u,%u"` for one-body, `sprintf "%u,%u,%u,%u"`
for two-body. Keys become `"0,1"`, `"10,11"`, `"0,1,2,3"` — unambiguous for
any number of modes.

## Consequences

- **Correctness**: Keys are unambiguous for any index range — no upper limit on n.
- **Breaking change**: Any external code that builds keys without commas will
  fail to match. This is intentional — silent mismatches were the original bug.
- **Performance**: String formatting cost is negligible vs. Pauli algebra.
- **Convention**: All key-building code (Hamiltonian.fs, workshop scripts,
  coefficient factories) must use the same comma-separated format.

## Alternatives Considered

- **Fixed-width zero-padded indices** (`sprintf "%03u%03u"`) — avoids ambiguity
  but wastes characters and imposes an upper limit on n. Rejected.
- **Tuple keys** (`(int * int)` or `(int * int * int * int)`) — type-safe but
  requires two separate factory signatures. String keys unify one-body and
  two-body under a single `string -> Complex option` interface. Rejected.
- **Integer hash** (Cantor pairing or similar) — fast but opaque for debugging.
  String keys are human-readable in error messages and JSON files. Rejected.

## References

- Commit `919fac8` — bugfix
- [Hamiltonian.fs](../../src/Encodings/Hamiltonian.fs) — key construction
