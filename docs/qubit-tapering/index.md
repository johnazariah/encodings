# Qubit Tapering

_A compact guide to reducing qubit count via symmetry after fermion-to-qubit encoding._

The quantum simulation pipeline delivers a **qubit Hamiltonian** — a Pauli sum ready for VQE, QPE, or other quantum algorithms. Before running that algorithm, there is often a hidden opportunity: the encoded Hamiltonian frequently respects **symmetries** that allow you to permanently remove qubits without losing physics.

This is **qubit tapering**.

A tapered system needs fewer qubits, smaller circuits, and less quantum noise — a direct path to solving larger problems on near-term quantum hardware. This tutorial covers FockMap's tapering capabilities: v1 diagonal Z₂ tapering for single-qubit generators, and v2 general Clifford tapering for arbitrary multi-qubit Z₂ symmetries.

> **Prerequisites:** You should have seen a fermion-to-qubit encoding (e.g., Jordan–Wigner or Bravyi–Kitaev) and understand Pauli strings. Read [From Molecules to Qubits](../from-molecules-to-qubits/index.html) first if these are unfamiliar.

## The Tapering Pipeline

```
Encoded Hamiltonian → Detect Symmetries → Fix Sectors → Remove Qubits → Tapered Hamiltonian
```

At each stage, the Pauli sum structure is preserved — you continue to work with exact symbolic operators, not matrices. Only the number of qubits shrinks.

## Chapters

| # | Chapter | What you'll learn |
|:--|:--------|:------------------|
| 1 | [Why Tapering?](01-why-tapering.html) | How encoding creates Z₂ symmetries and why removing qubits is safe |
| 2 | [The Diagonal Z₂ Approach](02-diagonal-z2-approach.html) | Detecting symmetries, sectors, and fixing eigenvalues |
| 3 | [FockMap Implementation](03-fockmap-implementation.html) | The v1 tapering API in practice with step-by-step examples |
| 4 | [General Z₂ and Clifford Tapering](04-clifford-tapering.html) | Symplectic representation, general symmetry detection, and the unified pipeline |

## Quick Example

```fsharp
open System.Numerics
open Encodings

// A 4-qubit Hamiltonian with diagonal Z₂ symmetries
let h =
    [| PauliRegister("ZIZI", Complex(0.8, 0.0))
       PauliRegister("ZZII", Complex(-0.4, 0.0))
       PauliRegister("IIZZ", Complex(0.3, 0.0)) |]
    |> PauliRegisterSequence

// Detect which qubits are diagonal Z₂ symmetric
let symQubits = diagonalZ2SymmetryQubits h
// → [| 0; 1; 2; 3 |]

// Taper qubits 1 and 3 in the +1/−1 sector
let tapered = taperDiagonalZ2 [(1, 1); (3, -1)] h
// → 2 qubits removed, result is a 2-qubit Hamiltonian
```

## Related Resources

- **Interactive example:** [Qubit Tapering Lab](../labs/09-qubit-tapering.html)
- **Full pipeline:** [From Molecules to Qubits](../from-molecules-to-qubits/index.html) — encoding comes before tapering
- **Theory:** [Why Encodings?](../theory/01-why-encodings.html) — motivation for encoding schemes

---

**Next:** [Chapter 1 — Why Tapering?](01-why-tapering.html)
