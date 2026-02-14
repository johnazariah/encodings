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

Runs on **Windows**, **macOS**, and **Linux** via [.NET 8](https://dotnet.microsoft.com/).
Written in [F#](https://fsharp.org/), fully open-source under the [F# Software Foundation](https://foundation.fsharp.org/) and the [.NET Foundation](https://dotnetfoundation.org/).

## Learn More

- **New to encodings?** Start with [Why Encodings?](background/01-why-encodings.html)
- **Want to try it?** Jump to [Your First Encoding](tutorials/01-first-encoding.html)
- **Full walkthrough:** [Encoding the H₂ Molecule](tutorials/02-h2-molecule.html)
- **Library internals:** [Architecture Guide](guides/architecture.html)
- **API Reference:** [All types and functions](reference/index.html)

## Quick Links

### Background & Theory
- [Why Encodings?](background/01-why-encodings.html) — The problem encodings solve
- [Second Quantization](background/02-second-quantization.html) — Fock space, creation/annihilation operators
- [Pauli Algebra](background/03-pauli-algebra.html) — Pauli matrices and strings
- [Jordan-Wigner](background/04-jordan-wigner.html) — The classic encoding
- [Beyond Jordan-Wigner](background/05-beyond-jordan-wigner.html) — BK, trees, and O(log n) scaling

### Tutorials
- [First Encoding](tutorials/01-first-encoding.html) — Encode your first operator (5 min)
- [H₂ Molecule](tutorials/02-h2-molecule.html) — Full molecular Hamiltonian
- [Compare Encodings](tutorials/03-compare-encodings.html) — Side-by-side comparison
- [Custom Encoding](tutorials/04-custom-encoding.html) — Define your own scheme
- [Custom Tree](tutorials/05-custom-tree.html) — Build tree-based encodings

### Guides
- [Architecture](guides/architecture.html) — Two-framework design
- [Type System](guides/type-system.html) — C/P/S algebra explained
- [Cross-Platform](guides/cross-platform.html) — .NET 8 and F# on all platforms
