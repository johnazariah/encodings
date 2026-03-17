# ADR-016: Qubitization as a Second Compilation Backend

**Date:** 2026-03-11
**Status:** Proposed (design phase)
**Source:** Discussion during book update

## Context

FockMap's current pipeline is:

```
Integrals → Encoding → Pauli Sum → Tapering → Trotter → CNOT staircase → Circuit
```

Trotterization is the only circuit-compilation backend. It targets
near-term (NISQ) hardware and has query complexity
$O(\lambda^2 t^2 / \epsilon)$ for first-order product formulas.

Qubitization (Low & Chuang, 2019) is an alternative that achieves
optimal query complexity $O(\lambda t / \epsilon)$ using a
linear-combination-of-unitaries (LCU) decomposition. It produces a
qualitatively different circuit structure based on PREPARE/SELECT
oracles and a quantum walk operator.

Both backends consume the same `PauliRegisterSequence` output from
encoding and tapering — the shared infrastructure is reused.

## Decision

**Add qubitization as a second compilation backend**, forking the
pipeline after tapering:

```
Integrals → Encoding → Pauli Sum → Tapering ─┬→ Trotter → CNOT staircase → NISQ circuit
                                               └→ Qubitization → LCU oracles → FT circuit
```

### Phased approach

**Phase 1 (near-term):** Resource estimation only.
- Compute the 1-norm $\lambda = \sum |c_k|$ from the Pauli sum
- Return theoretical query count $O(\lambda t / \epsilon)$
- Compare with Trotter cost for the same Hamiltonian
- This is a small addition to the existing library

**Phase 2 (future):** PREPARE/SELECT oracle synthesis.
- PREPARE: amplitude-encode the coefficient vector $c_k / \lambda$
- SELECT: multiplexed application of Pauli strings
- Ancilla management and circuit output
- This may warrant a separate library dependency rather than
  building the synthesis machinery inside FockMap

**Phase 3 (future):** Book chapter walking both paths on H₂.
- Same molecule, same encoding, two compilation routes
- Empirical query-count comparison on a simulator
- Novel as a tutorial — no existing resource does this

### Open questions

1. **Library boundary.** Should PREPARE/SELECT synthesis live in
   FockMap or in a separate library that FockMap consumes? The
   synthesis involves arithmetic circuits, QROM compilation, and
   fault-tolerant gate decomposition — capabilities that serve a
   broader audience than Fock-space algebra.

2. **Circuit output format.** Qubitized circuits are deeper and
   require ancilla qubits. The current OpenQASM/Q# export may need
   extension for ancilla declaration and mid-circuit measurement.

3. **Simulator support.** Qubitized circuits for H₂ (4 qubits +
   ancillas) should run fine on a statevector simulator. For H₂O
   the ancilla overhead may push beyond simulator capacity.

## Consequences

- The `PauliRegisterSequence` type becomes the shared interface
  between encoding and both compilation backends.
- Phase 1 adds minimal code and can ship with the current release.
- Phase 2 is a significant engineering effort and should be planned
  as a separate project milestone.
- The book gains a natural second arc: "Trotter for NISQ, qubitization
  for fault-tolerant" — strengthening the library's position as a
  complete encoding-to-circuit framework.

## References

- Low, G. H. and Chuang, I. L. "Hamiltonian simulation by
  qubitization." *Quantum* 3, 163 (2019).
- Babbush, R. et al. "Encoding electronic spectra in quantum circuits
  with linear T complexity." *Phys. Rev. X* 8, 041015 (2018).
