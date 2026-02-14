<p align="center">
	<img src="content/img/fockmap-logo.svg" alt="FockMap logo" width="520" />
</p>

A composable functional framework for symbolic operator algebra, including fermionic and bosonic normal ordering, mixed-sector canonicalization, and fermion-to-qubit encodings.

> Build, normalize, and encode operator expressions on Fock space using algebraic data types, pure functions, and zero dependencies.

## Install

```bash
dotnet add package FockMap
```

## Brand Assets

- [Primary logo (SVG)](content/img/fockmap-logo.svg)
- [Square icon (SVG)](content/img/fockmap-icon.svg)

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
| Symbolic CAR + CCR normal ordering | ❌ | Partial | **✅** |

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

- **Start here:** [From Molecules to Qubits](from-molecules-to-qubits/index.html) — complete H₂ pipeline
- **Theory first:** [Why Encodings?](theory/01-why-encodings.html)
- **Hands-on first:** [Your First Encoding](labs/01-first-encoding.fsx)
- **Library internals:** [Architecture Guide](guides/architecture.html)
- **API Reference:** [All types and functions](reference/index.html)

## Explore the Docs

### From Molecules to Qubits
A step-by-step guide using H₂ as a worked example.
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
- [Bosonic Operators](theory/06-bosonic-preview.html) — CCR algebra and truncation strategies
- [Mixed Systems](theory/07-mixed-systems.html) — Canonical sector ordering and hybrid workflows

### Interactive Labs
- [First Encoding](labs/01-first-encoding.fsx) — Encode your first operator (5 min)
- [H₂ Molecule](labs/02-h2-molecule.fsx) — Full molecular Hamiltonian
- [Compare Encodings](labs/03-compare-encodings.fsx) — Side-by-side comparison
- [Custom Encoding](labs/04-custom-encoding.fsx) — Define your own scheme
- [Custom Tree](labs/05-custom-tree.fsx) — Build tree-based encodings
- [Scaling Analysis](labs/06-scaling.fsx) — Pauli weight scaling with system size

### Guides
- [Architecture](guides/architecture.html) — Two-framework design
- [Type System](guides/type-system.html) — C/P/S algebra explained
- [Mixed Registers](guides/mixed-registers.html) — Bosonic + fermionic components with runnable examples
- [Advanced Operations](guides/advanced-operations.html) — Hybrid pipelines, projections, and encoding comparisons
- [Cross-Platform](guides/cross-platform.html) — .NET 10 and F# on all platforms
