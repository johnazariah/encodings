# FockMap

[![CI](https://github.com/johnazariah/encodings/actions/workflows/ci.yml/badge.svg)](https://github.com/johnazariah/encodings/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/johnazariah/encodings/graph/badge.svg)](https://codecov.io/gh/johnazariah/encodings)
[![NuGet](https://img.shields.io/nuget/v/FockMap.svg)](https://www.nuget.org/packages/FockMap)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey)

**A composable functional framework for encoding quantum operators on Fock space as qubit Pauli operators.**

> Map creation and annihilation operators to Pauli strings — using algebraic data types, pure functions, and zero dependencies.

---

## The Problem

Quantum computers operate on **qubits**, but quantum chemistry deals with **electrons** (fermions). Fermions obey anti-commutation relations that qubits don't naturally respect:

$$\{a_i^\dagger, a_j\} = \delta_{ij} I, \quad \{a_i^\dagger, a_j^\dagger\} = 0, \quad \{a_i, a_j\} = 0$$

**Fermion-to-qubit encodings** bridge this gap by mapping fermionic ladder operators ($a_j^\dagger$, $a_j$) to multi-qubit Pauli operators ($X$, $Y$, $Z$, $I$) while preserving the canonical anti-commutation relations (CAR). Different encodings make different tradeoffs between Pauli weight (how many qubits each operator touches) and locality.

This library provides a **unified framework** for five encoding schemes, plus the infrastructure to define custom encodings in a few lines of code.

## Why FockMap?

| Feature | OpenFermion | Qiskit Nature | **FockMap** |
|---------|:-----------:|:------------:|:-----------:|
| Define a new encoding | ~200 lines Python | Not supported | **3–5 lines F#** |
| Tree → encoding pipeline | ❌ | ❌ | **✅** |
| Type-safe operator algebra | ❌ | ❌ | **✅** |
| Pure functional, zero mutation | ❌ | ❌ | **✅** |
| Symbolic Pauli algebra (no matrices) | ❌ | Partial | **✅** |
| Runtime dependencies | NumPy, SciPy | Many | **None** |

FockMap works entirely with **symbolic Pauli algebra** — the Pauli group is a finite group, so multiplication is exact. There are no floating-point matrices, no numerical linear algebra, and no approximations. This means you can compute parity operators for $n = 100$ modes in seconds, not hours.

## Available Encodings

| Encoding | Worst-Case Pauli Weight | Framework | Function |
|----------|:-----------------------:|:---------:|----------|
| Jordan-Wigner | $O(n)$ | Index-set | `jordanWignerTerms` |
| Bravyi-Kitaev | $O(\log_2 n)$ | Index-set | `bravyiKitaevTerms` |
| Parity | $O(n)$ | Index-set | `parityTerms` |
| Balanced Binary Tree | $O(\log_2 n)$ | Path-based | `balancedBinaryTreeTerms` |
| Balanced Ternary Tree | $O(\log_3 n)$ | Path-based | `ternaryTreeTerms` |

All five encodings produce the same output type (`PauliRegisterSequence`), so encoded Hamiltonians are interoperable regardless of which encoding was used. All five have been verified to produce **identical eigenspectra** for the H₂ molecule to machine precision ($|\Delta\lambda| = 4.44 \times 10^{-16}$).

## Installation

### NuGet (recommended)

```bash
dotnet add package FockMap
```

### From source

```bash
git clone https://github.com/johnazariah/encodings.git
cd encodings
dotnet build
dotnet test   # 303 tests
```

### Dev Container (recommended for contributors)

This repository includes a full [dev container](https://containers.dev/) configuration with .NET 10, F#, Python, LaTeX, and all required tooling pre-installed. To use it:

1. Install [Docker](https://www.docker.com/) and [VS Code](https://code.visualstudio.com/) with the [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)
2. Clone the repository and open it in VS Code
3. When prompted, click **"Reopen in Container"** (or run `Dev Containers: Reopen in Container` from the command palette)
4. The container builds, restores packages, compiles the project, and runs all 303 tests automatically

Everything is pre-configured — Ionide (F# IDE), Copilot, coverage tools, `fsdocs`, `gh` CLI, and `dotnet-repl`.

## Quick Start

```fsharp
open Encodings

// Encode the creation operator a†₂ on 4 modes using Jordan-Wigner
let pauliJW = jordanWignerTerms Raise 2u 4u
// → ½(ZZXI) − ½i(ZZYI)

// Same operator under Bravyi-Kitaev (O(log n) weight)
let pauliBK = bravyiKitaevTerms Raise 2u 4u

// Or Parity encoding
let pauliP = parityTerms Raise 2u 4u

// Tree-based encodings
let pauliBBT = balancedBinaryTreeTerms Raise 2u 4u
let pauliBTT = ternaryTreeTerms Raise 2u 4u
```

### Encode a Full Hamiltonian

```fsharp
open Encodings

// One-electron (h) and two-electron (g) integrals for H₂ in STO-3G basis
let h = Array2D.init 4 4 (fun i j -> (* your integrals *) 0.0)
let g = Array4D.init 4 4 4 4 (fun i j k l -> (* your integrals *) 0.0)

// Encode with any scheme
let hamiltonian = computeHamiltonianWith jordanWignerTerms h g 4u
```

### Define a Custom Encoding in 5 Lines

```fsharp
open Encodings

// Build a custom tree and derive an encoding from it
let myTree = balancedBinaryTree 8
let myScheme = treeEncodingScheme myTree
let myEncode op j n = encodeOperator myScheme op j n
```

## Architecture

FockMap provides **two complementary encoding frameworks**, each suited to different encoding strategies:

### 1. Index-Set Framework (Seeley-Richard-Love)

Encodings are defined by three functions over index sets:

- **Update set** $U(j)$: qubits whose occupation parity changes when mode $j$ is toggled
- **Parity set** $P(j)$: qubits that store the parity of modes $< j$
- **Occupation set** $F(j)$: qubits that encode the occupation of mode $j$

These sets plug into the Majorana operators $c_j$ and $d_j$:

$$c_j = X_{U(j) \cup \{j\}} \cdot Z_{P(j)}, \qquad d_j = Y_j \cdot X_{U(j)} \cdot Z_{(P(j) \oplus F(j)) \setminus \{j\}}$$

This framework implements Jordan-Wigner, Bravyi-Kitaev, and Parity.

### 2. Path-Based Framework (Jiang et al.)

Encodings are defined by **tree structures** where:
- Each leaf corresponds to a fermionic mode
- Each edge carries a Pauli label ($X$, $Y$, or $Z$)
- Root-to-leaf paths determine Majorana operator strings

This framework implements balanced binary and balanced ternary tree encodings, and supports **arbitrary custom trees** — any rooted tree you can define produces a valid encoding.

### Type System

The library's operator algebra is built on three generic types:

| Type | Meaning | Example |
|------|---------|---------|
| `C<'T>` | Coefficient × term | $0.5 \times a_2^\dagger$ |
| `P<'T>` | Product of terms | $a_0^\dagger a_1$ (hopping) |
| `S<'T>` | Sum of products | Full Hamiltonian $H = \sum_{ij} h_{ij} a_i^\dagger a_j + \ldots$ |

These compose with:
- `LadderOperatorUnit` — `Raise j` ($a_j^\dagger$) or `Lower j` ($a_j$)
- `PauliRegister` — Multi-qubit Pauli string with phase (e.g., $iXZYI$)
- `PauliRegisterSequence` — Linear combination of Pauli strings

## Source Modules

| Module | Description |
|--------|-------------|
| `Terms.fs` | Generic `C<'T>`, `P<'T>`, `S<'T>` combining algebra |
| `IndexedTerms.fs` | Indexed terms with operator ordering |
| `PauliRegister.fs` | Multi-qubit Pauli string with exact symbolic multiplication |
| `CombiningAlgebra.fs` | Pauli string collection and simplification |
| `LadderOperatorSequence.fs` | Fermionic operator product manipulation |
| `JordanWigner.fs` | Jordan-Wigner encoding (direct implementation) |
| `FenwickTree.fs` | Pure functional Fenwick tree (binary indexed tree) ADT |
| `MajoranaEncoding.fs` | Generic Majorana encoding framework with index-set schemes |
| `BravyiKitaev.fs` | Bravyi-Kitaev encoding via Fenwick tree |
| `TreeEncoding.fs` | Tree-based encodings (index-set and path-based constructions) |
| `Hamiltonian.fs` | Full Hamiltonian encoding from one/two-electron integrals |
| `SwapTrackingSort.fs` | Parity-tracking sort for operator reordering |

## Examples

Runnable F# scripts in the [`examples/`](examples/) directory:

| Script | What it does |
|--------|-------------|
| [`H2_Encoding.fsx`](examples/H2_Encoding.fsx) | Encode the H₂ molecular Hamiltonian with all 5 encodings |
| [`Compare_Encodings.fsx`](examples/Compare_Encodings.fsx) | Side-by-side Pauli weight comparison across encodings |
| [`Custom_Encoding.fsx`](examples/Custom_Encoding.fsx) | Build a custom Majorana encoding from index-set functions |
| [`Custom_Tree.fsx`](examples/Custom_Tree.fsx) | Construct a custom tree and derive an encoding from it |

Run any example with:
```bash
dotnet fsi examples/H2_Encoding.fsx
```

## Documentation

Full documentation is available at **[johnazariah.github.io/encodings](https://johnazariah.github.io/encodings/)**.

### Theory
- [Why Encodings?](https://johnazariah.github.io/encodings/theory/01-why-encodings.html) — The problem encodings solve
- [Second Quantization](https://johnazariah.github.io/encodings/theory/02-second-quantization.html) — Fock space, creation/annihilation operators
- [Pauli Algebra](https://johnazariah.github.io/encodings/theory/03-pauli-algebra.html) — Pauli matrices and multi-qubit strings
- [Jordan-Wigner](https://johnazariah.github.io/encodings/theory/04-jordan-wigner.html) — The classic encoding, derived step by step
- [Beyond Jordan-Wigner](https://johnazariah.github.io/encodings/theory/05-beyond-jordan-wigner.html) — BK, trees, and $O(\log n)$ scaling
- [Bosonic Preview](https://johnazariah.github.io/encodings/theory/06-bosonic-preview.html) — Future extensions to bosonic modes

### Labs (literate F# scripts)
- [Your First Encoding](https://johnazariah.github.io/encodings/labs/01-first-encoding.html) — Encode an operator in 5 minutes
- [H₂ Molecule](https://johnazariah.github.io/encodings/labs/02-h2-molecule.html) — Full molecular Hamiltonian walkthrough
- [Compare Encodings](https://johnazariah.github.io/encodings/labs/03-compare-encodings.html) — Side-by-side weight analysis
- [Custom Encoding](https://johnazariah.github.io/encodings/labs/04-custom-encoding.html) — Define your own scheme
- [Custom Tree](https://johnazariah.github.io/encodings/labs/05-custom-tree.html) — Build tree-based encodings
- [Scaling Analysis](https://johnazariah.github.io/encodings/labs/06-scaling.html) — Pauli weight scaling with system size

### Guides
- [Architecture](https://johnazariah.github.io/encodings/guides/architecture.html) — Two-framework design explained
- [Type System](https://johnazariah.github.io/encodings/guides/type-system.html) — The C/P/S algebra in depth
- [Cross-Platform](https://johnazariah.github.io/encodings/guides/cross-platform.html) — Running on Windows, macOS, and Linux

## Testing

```bash
# Run all 303 tests
dotnet test

# With detailed output
dotnet test --logger "console;verbosity=detailed"

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

The test suite includes:
- **Unit tests** for every encoding scheme and every algebraic operation
- **Property-based tests** via [FsCheck](https://fscheck.github.io/FsCheck/) for Pauli algebra laws
- **Cross-encoding verification**: all 5 encodings produce identical eigenspectra for H₂
- **CAR verification**: canonical anti-commutation relations checked symbolically

Current coverage: **78% line / 66% branch** across 303 tests.

## Cross-Platform

This library runs on **Windows**, **macOS**, and **Linux** via [.NET 10](https://dotnet.microsoft.com/) (LTS), Microsoft's open-source, cross-platform runtime. It is written in [F#](https://fsharp.org/), a functional-first language that is fully open-source under the [F# Software Foundation](https://foundation.fsharp.org/) and the [.NET Foundation](https://dotnetfoundation.org/).

No platform-specific code. No native dependencies. No runtime downloads beyond the .NET SDK.

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

A machine-readable citation file is available at [`CITATION.cff`](CITATION.cff).

## Contributing

Contributions are welcome! See [`CONTRIBUTING.md`](CONTRIBUTING.md) for:
- How to report bugs and propose features
- Development setup instructions
- Coding conventions (pure functions, immutable data, XML docs)
- Pull request process

## License

[MIT](LICENSE)

## Acknowledgements

This library is dedicated to **Dr. Guang Hao Low** for his guidance and inspiration in the field of quantum algorithms.
