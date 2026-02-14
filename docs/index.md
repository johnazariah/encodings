# FockMap

A composable functional framework for encoding quantum operators as qubit Pauli strings.

> Map creation and annihilation operators on Fock space to Pauli operators on qubits — using algebraic data types, pure functions, and zero dependencies.

## Install

```bash
dotnet add package FockMap
```

## 30-Second Example

```fsharp
open Encodings

// Encode the creation operator a†₂ on 4 modes using Jordan-Wigner
let pauli = jordanWignerTerms Raise 2u 4u
// → ½(ZZXI) − ½i(ZZYI)

// Same operator under Bravyi-Kitaev (O(log n) weight)
let pauliBK = bravyiKitaevTerms Raise 2u 4u
```

## Why This Library?

| Feature | OpenFermion | Qiskit Nature | **FockMap** |
|---------|:-----------:|:------------:|:-----------:|
| Define a new encoding | ~200 lines | Not supported | **3–5 lines** |
| Tree → encoding pipeline | ❌ | ❌ | **✅** |
| Type-safe operator algebra | ❌ | ❌ | **✅** |
| Pure functional, zero mutation | ❌ | ❌ | **✅** |

## Available Encodings

| Encoding | Worst-Case Weight | Function |
|----------|:-----------------:|----------|
| Jordan-Wigner | $O(n)$ | `jordanWignerTerms` |
| Bravyi-Kitaev | $O(\log_2 n)$ | `bravyiKitaevTerms` |
| Parity | $O(n)$ | `parityTerms` |
| Balanced Binary Tree | $O(\log_2 n)$ | `balancedBinaryTreeTerms` |
| Balanced Ternary Tree | $O(\log_3 n)$ | `ternaryTreeTerms` |

## Cross-Platform

Runs on **Windows**, **macOS**, and **Linux** via [.NET 10](https://dotnet.microsoft.com/) (LTS).
Written in [F#](https://fsharp.org/), fully open-source under the [F# Software Foundation](https://foundation.fsharp.org/) and the [.NET Foundation](https://dotnetfoundation.org/).

## Learn More

- **Complete tutorial:** [From Molecules to Qubits](from-molecules-to-qubits/index.html) — the full pipeline, H₂ worked example
- **New to encodings?** Start with [Why Encodings?](theory/01-why-encodings.html)
- **Want to try it?** Jump to [Your First Encoding](labs/01-first-encoding.html)
- **Full walkthrough:** [Encoding the H₂ Molecule](labs/02-h2-molecule.html)
- **Library internals:** [Architecture Guide](guides/architecture.html)
- **API Reference:** [All types and functions](reference/index.html)

## Quick Links

### From Molecules to Qubits
A complete step-by-step guide using H₂ as a worked example — every integral, every sign, every coefficient.
- [Overview](from-molecules-to-qubits/index.html) — The complete pipeline at a glance
- [The Electronic Structure Problem](from-molecules-to-qubits/01-electronic-structure.html) — Born–Oppenheimer, basis sets, configurations
- [The Notation Minefield](from-molecules-to-qubits/02-notation.html) — Chemist's vs. physicist's integrals
- [Spatial to Spin-Orbital Integrals](from-molecules-to-qubits/03-spin-orbitals.html) — Doubling the index space
- [Building the H₂ Hamiltonian](from-molecules-to-qubits/04-building-hamiltonian.html) — The 15-term qubit Hamiltonian
- [Checking Our Answer](from-molecules-to-qubits/05-verification.html) — Diagonalisation and cross-encoding comparison
- [What Comes Next](from-molecules-to-qubits/06-outlook.html) — VQE, QPE, and scaling

### Theory
- [Why Encodings?](theory/01-why-encodings.html) — The problem encodings solve
- [Second Quantization](theory/02-second-quantization.html) — Fock space, creation/annihilation operators
- [Pauli Algebra](theory/03-pauli-algebra.html) — Pauli matrices and strings
- [Jordan-Wigner](theory/04-jordan-wigner.html) — The classic encoding
- [Beyond Jordan-Wigner](theory/05-beyond-jordan-wigner.html) — BK, trees, and O(log n) scaling

### Interactive Labs
- [First Encoding](labs/01-first-encoding.html) — Encode your first operator (5 min)
- [H₂ Molecule](labs/02-h2-molecule.html) — Full molecular Hamiltonian
- [Compare Encodings](labs/03-compare-encodings.html) — Side-by-side comparison
- [Custom Encoding](labs/04-custom-encoding.html) — Define your own scheme
- [Custom Tree](labs/05-custom-tree.html) — Build tree-based encodings

### Guides
- [Architecture](guides/architecture.html) — Two-framework design
- [Type System](guides/type-system.html) — C/P/S algebra explained
- [Cross-Platform](guides/cross-platform.html) — .NET 10 and F# on all platforms
