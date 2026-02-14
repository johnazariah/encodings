# FockMap

[![CI](https://github.com/johnazariah/encodings/actions/workflows/ci.yml/badge.svg)](https://github.com/johnazariah/encodings/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/johnazariah/encodings/graph/badge.svg)](https://codecov.io/gh/johnazariah/encodings)
[![NuGet](https://img.shields.io/nuget/v/FockMap.svg)](https://www.nuget.org/packages/FockMap)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey)

**A composable functional framework for encoding quantum operators as qubit Pauli strings.**

> Map creation and annihilation operators on Fock space to Pauli operators on qubits — using algebraic data types, pure functions, and zero dependencies.

## What is this?

Quantum computers operate on qubits, but quantum chemistry deals with electrons (fermions). Fermions obey anti-commutation relations that qubits don't naturally respect. **Fermion-to-qubit encodings** bridge this gap by mapping fermionic operators to qubit Pauli operators while preserving the correct algebraic structure.

This library provides a unified framework for multiple encoding schemes, implemented as pure F# functions over algebraic data types. You can use the built-in encodings (Jordan-Wigner, Bravyi-Kitaev, etc.) or define custom encodings in just a few lines of code.

## Cross-Platform

This library runs on **Windows**, **macOS**, and **Linux** via [.NET 8](https://dotnet.microsoft.com/), Microsoft's open-source, cross-platform runtime. It is written in [F#](https://fsharp.org/), a functional-first language that is fully open-source under the [F# Software Foundation](https://foundation.fsharp.org/) and the [.NET Foundation](https://dotnetfoundation.org/).

## Installation

### As a NuGet package (recommended)

```bash
dotnet add package FockMap
```

### From source

```bash
git clone https://github.com/johnazariah/encodings.git
cd FockMap
dotnet build
dotnet test
```

## Quick Start

```fsharp
open Encodings

// Encode the creation operator a†₂ on 4 modes using Jordan-Wigner
let pauliJW = jordanWignerTerms Raise 2u 4u
// → ½(ZZXI) − ½i(ZZYI)

// Same operator under Bravyi-Kitaev (O(log n) weight)
let pauliBK = bravyiKitaevTerms Raise 2u 4u
```

## Available Encodings

| Encoding | Worst-Case Weight | Function |
|----------|:-----------------:|----------|
| Jordan-Wigner | O(n) | `jordanWignerTerms` |
| Bravyi-Kitaev | O(log₂ n) | `bravyiKitaevTerms` |
| Parity | O(n) | `parityTerms` |
| Balanced Binary Tree | O(log₂ n) | `balancedBinaryTreeTerms` |
| Balanced Ternary Tree | O(log₃ n) | `ternaryTreeTerms` |

## Architecture

The library provides two complementary encoding frameworks:

1. **Index-Set Framework**: Encodings are defined by three functions that compute Majorana index sets (update, parity, occupation). Good for classical encodings like Jordan-Wigner and Bravyi-Kitaev.

2. **Tree-Based Framework**: Encodings are defined by tree structures where paths from leaves to root determine Pauli strings. Enables exploration of novel O(log n) encodings.

Both frameworks produce the same output type (`PauliRegisterSequence`), so encoded Hamiltonians are interoperable regardless of which encoding was used.

## API Overview

### Core Types

- `C<'T>` — Coefficient × operator term (e.g., `0.5 × a†₂`)
- `P<'T>` — Product of terms (e.g., `a†₀ a₁` — a hopping term)
- `S<'T>` — Sum of products (full Hamiltonian expressions)
- `LadderOperatorUnit` — `Raise j` (a†ⱼ) or `Lower j` (aⱼ)
- `PauliRegister` — Multi-qubit Pauli string with phase
- `PauliRegisterSequence` — Sum of Pauli strings with coefficients

### Encoding Functions

```fsharp
// Single operator encodings
jordanWignerTerms : LadderOperatorType -> uint -> uint -> PauliRegisterSequence
bravyiKitaevTerms : LadderOperatorType -> uint -> uint -> PauliRegisterSequence
parityTerms : LadderOperatorType -> uint -> uint -> PauliRegisterSequence

// Full Hamiltonian encoding
computeHamiltonian : EncoderFn -> float[,] -> float[,,,] -> PauliRegisterSequence
```

## Running Tests

```bash
# Run all 303+ tests
dotnet test

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

Tests cover all encoding schemes with both unit tests and property-based tests (via FsCheck).

## Documentation

- **[API Reference](https://johnazariah.github.io/encodings/)** — Full documentation with mathematical notation
- **Background Theory** — Educational content on fermion-to-qubit encodings
- **Tutorials** — Literate F# scripts with worked examples

## Citation

If you use this library in your research, please cite:

```bibtex
@software{fockmap2026,
  author = {Azariah, John},
  title = {FockMap: A Composable Framework for Quantum Operator Encodings},
  year = {2026},
  url = {https://github.com/johnazariah/encodings}
}
```

## License

[MIT](LICENSE)

## Acknowledgements

This library is dedicated to Dr. Guang Hao Low for his guidance and inspiration in the field of quantum algorithms.
