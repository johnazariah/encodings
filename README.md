# FockMap

[![CI](https://github.com/johnazariah/encodings/actions/workflows/ci.yml/badge.svg)](https://github.com/johnazariah/encodings/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/johnazariah/encodings/graph/badge.svg)](https://codecov.io/gh/johnazariah/encodings)
[![NuGet](https://img.shields.io/nuget/v/FockMap.svg)](https://www.nuget.org/packages/FockMap)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![.NET 8](https://img.shields.io/badge/.NET-8.0%20LTS-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey)

**A practical F# library for symbolic operator algebra on Fock space — fermionic and bosonic — with fermion-to-qubit encodings.**

> Learn by doing: pick an encoding, map operators to Pauli strings, and compare results. Supports both fermionic (CAR) and bosonic (CCR) statistics, including mixed fermion–boson systems.

📖 **New here?** Read the [Visual Guide to Fermion-to-Qubit Encodings](https://johnazariah.github.io/encodings/guides/visual-encodings.html) — a diagram-rich introduction for scientists who know chemistry but not quantum computing.

🍳 **Ready to code?** Work through the [Cookbook](https://johnazariah.github.io/encodings/guides/cookbook/) — 13 progressive chapters covering every type and function.

---

## Why FockMap

If you're exploring quantum chemistry on qubits, you usually hit this question quickly: **how do I map fermions to Pauli operators?** And increasingly: **what about phonons and other bosonic modes?**

FockMap gives you one small, consistent API for both. You can:
- use built-in encodings (Jordan-Wigner, Bravyi-Kitaev, Parity, tree-based)
- compare them side-by-side
- define your own encoding with a few lines of F#
- work with bosonic (CCR) operators and mixed fermion–boson systems

| Feature | OpenFermion | Qiskit Nature | PennyLane | **FockMap** |
|---------|:-----------:|:------------:|:---------:|:-----------:|
| Define a new encoding | ~200 lines Python | Not supported | Not supported | **3–5 lines F#** |
| Tree → encoding pipeline | ❌ | ❌ | ❌ | **✅** |
| Bosonic operator algebra (CCR) | ❌ | ❌ | ✅ | **✅** |
| Mixed fermion–boson normal ordering | ❌ | ❌ | ❌ | **✅** |
| Type-safe operator algebra | ❌ | ❌ | ❌ | **✅** |
| Pure functional, zero mutation | ❌ | ❌ | ❌ | **✅** |
| Symbolic Pauli algebra (no matrices) | ❌ | Partial | ✅ | **✅** |
| Runtime dependencies | NumPy, SciPy | Many | NumPy, autograd, … | **None** |

Internally, the library uses exact symbolic Pauli algebra (not floating-point matrix multiplication), so encoded operator manipulation stays fast and predictable.

## Available Encodings

| Encoding | Worst-Case Pauli Weight | Framework | Function |
|----------|:-----------------------:|:---------:|----------|
| Jordan-Wigner | $O(n)$ | Index-set | `jordanWignerTerms` |
| Bravyi-Kitaev | $O(\log_2 n)$ | Index-set | `bravyiKitaevTerms` |
| Parity | $O(n)$ | Index-set | `parityTerms` |
| Balanced Binary Tree | $O(\log_2 n)$ | Path-based | `balancedBinaryTreeTerms` |
| Balanced Ternary Tree | $O(\log_3 n)$ | Path-based | `ternaryTreeTerms` |

All encodings return the same output type (`PauliRegisterSequence`), so you can swap schemes without rewriting downstream code.

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
dotnet test
```

### Dev Container (for contributors)

This repository includes a full [dev container](https://containers.dev/) configuration with .NET 8 (LTS), F#, Python, LaTeX, and all required tooling pre-installed. To use it:

1. Install [Docker](https://www.docker.com/) and [VS Code](https://code.visualstudio.com/) with the [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)
2. Clone the repository and open it in VS Code
3. When prompted, click **"Reopen in Container"** (or run `Dev Containers: Reopen in Container` from the command palette)
4. The container builds, restores packages, compiles the project, and runs tests automatically

Everything is pre-configured, so you can start coding immediately.

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

### Define a Custom Encoding

```fsharp
open Encodings

// Build a custom tree and derive an encoding from it
let myTree = balancedBinaryTree 8
let myScheme = treeEncodingScheme myTree
let myEncode op j n = encodeOperator myScheme op j n
```

## Where to Start

- New to this topic? Start with [Why Encodings?](https://johnazariah.github.io/encodings/theory/01-why-encodings.html)
- Want a progressive walkthrough? Try the [Cookbook](https://johnazariah.github.io/encodings/guides/cookbook/) — 13 chapters covering every type and function
- Want a full worked example? Go to [From Molecules to Qubits](https://johnazariah.github.io/encodings/from-molecules-to-qubits/index.html)
- Prefer hands-on? Try [Your First Encoding](https://johnazariah.github.io/encodings/labs/01-first-encoding.html)
- Need API details? Browse [All types and functions](https://johnazariah.github.io/encodings/reference/index.html)

## Documentation

- **Site**: [johnazariah.github.io/encodings](https://johnazariah.github.io/encodings/)
- **Cookbook**: [13-chapter progressive tutorial](https://johnazariah.github.io/encodings/guides/cookbook/) — covers every type and function with runnable examples
- **Tutorial**: [From Molecules to Qubits](https://johnazariah.github.io/encodings/from-molecules-to-qubits/index.html) — end-to-end worked example
- **Theory**: [Why Encodings?](https://johnazariah.github.io/encodings/theory/01-why-encodings.html) — mathematical foundations
- **Labs**: [Your First Encoding](https://johnazariah.github.io/encodings/labs/01-first-encoding.html) — hands-on F# scripts
- **Architecture**: [Architecture guide](https://johnazariah.github.io/encodings/guides/architecture.html) — module and pipeline overview
- **API Reference**: [All types and functions](https://johnazariah.github.io/encodings/reference/index.html) — generated from source XML docs
- **Test Register**: [What is tested](.project/test-register.md) — plain-English catalogue of all 427 automated tests

Build docs locally (with logo/icon + Mermaid/MathJax runtime injection):

```bash
./scripts/build-docs.sh
```

## How it Works (briefly)

FockMap exposes two fermionic encoding styles:
- **Index-set encodings** (Jordan-Wigner, Bravyi-Kitaev, Parity)
- **Tree/path encodings** (balanced binary and ternary trees, plus custom trees)

It also supports **bosonic ladder-operator normal ordering** via canonical commutation relations (CCR), alongside the fermionic CAR workflow.

For models with both statistics, use sector-tagged operators (`fermion`, `boson`) and `constructMixedNormalOrdered` to canonicalize mixed expressions.

For the full derivations and internals, jump to the [Cookbook](https://johnazariah.github.io/encodings/guides/cookbook/) or the [Architecture guide](https://johnazariah.github.io/encodings/guides/architecture.html).

## Examples

Runnable F# scripts in the [`examples/`](examples/) directory:

| Script | What it does |
|--------|-------------|
| [`H2_Encoding.fsx`](examples/H2_Encoding.fsx) | Encode the H₂ molecular Hamiltonian with all 5 encodings |
| [`Compare_Encodings.fsx`](examples/Compare_Encodings.fsx) | Side-by-side Pauli weight comparison across encodings |
| [`Custom_Encoding.fsx`](examples/Custom_Encoding.fsx) | Build a custom Majorana encoding from index-set functions |
| [`Custom_Tree.fsx`](examples/Custom_Tree.fsx) | Construct a custom tree and derive an encoding from it |
| [`Mixed_NormalOrdering.fsx`](examples/Mixed_NormalOrdering.fsx) | Canonical mixed boson+fermion normal ordering with sector blocks |
| [`Mixed_ElectronPhonon_Toy.fsx`](examples/Mixed_ElectronPhonon_Toy.fsx) | Toy electron-phonon style mixed symbolic workflow |
| [`Mixed_HybridPipeline.fsx`](examples/Mixed_HybridPipeline.fsx) | Encode fermion sector to Pauli while keeping boson sector symbolic |
| [`Mixed_HybridCompare.fsx`](examples/Mixed_HybridCompare.fsx) | Compare JW vs BK on extracted fermionic blocks in mixed terms |

Run any example with:
```bash
dotnet fsi examples/H2_Encoding.fsx
```

## Testing

```bash
# Run all tests
dotnet test

# With detailed output
dotnet test --logger "console;verbosity=detailed"

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

The test suite covers encoding behavior, Pauli algebra laws, and cross-encoding consistency checks.

Coverage and test counts are tracked in CI.

## Cross-Platform

This library runs on **Windows**, **macOS**, and **Linux** via [.NET 8](https://dotnet.microsoft.com/) (LTS), Microsoft's open-source, cross-platform runtime. It is written in [F#](https://fsharp.org/), a functional-first language that is fully open-source under the [F# Software Foundation](https://foundation.fsharp.org/) and the [.NET Foundation](https://dotnetfoundation.org/).

No platform-specific code and no native dependencies beyond the .NET SDK.

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
