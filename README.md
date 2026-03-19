# FockMap

[![CI](https://github.com/johnazariah/encodings/actions/workflows/ci.yml/badge.svg)](https://github.com/johnazariah/encodings/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/johnazariah/encodings/graph/badge.svg)](https://codecov.io/gh/johnazariah/encodings)
[![NuGet](https://img.shields.io/nuget/v/FockMap.svg)](https://www.nuget.org/packages/FockMap)
[![DOI](https://zenodo.org/badge/203530692.svg)](https://zenodo.org/badge/latestdoi/203530692)
[![Docs](https://github.com/johnazariah/encodings/actions/workflows/docs.yml/badge.svg)](https://johnazariah.github.io/encodings/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![.NET 10](https://img.shields.io/badge/.NET-10.0%20GA-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey)

**A practical F# library for symbolic operator algebra on Fock space — fermionic and bosonic — with fermion-to-qubit encodings, qubit tapering, Trotterization, and circuit export.**

> The complete pipeline: molecular integrals → encoding → tapering → Trotter decomposition → OpenQASM / Q# / JSON circuit output.

📖 **The Book:** [*From Molecules to Quantum Circuits*](https://johnazariah.github.io/encodings-book) — 23-chapter guide with interactive labs, computed results (H₂ dissociation curve, H₂O bond angle scan), and companion code.

🍳 **API Cookbook:** [15 progressive chapters](https://johnazariah.github.io/encodings/guides/cookbook/) covering every type and function.

---

## Why FockMap

If you're exploring quantum chemistry on qubits, you usually hit this question quickly: **how do I map fermions to Pauli operators?** And increasingly: **what about phonons and other bosonic modes?**

FockMap gives you one small, consistent API for the complete quantum simulation pipeline:
- **Encode** fermionic or bosonic operators as Pauli strings (5 fermionic + 3 bosonic encodings)
- **Taper** qubits via Z₂ symmetry detection and Clifford rotation (diagonal + general)
- **Trotterize** a Pauli Hamiltonian into gate sequences (first and second order)
- **Export** circuits as OpenQASM 3.0, Q#, or JSON for any quantum platform
- **Define** custom encodings with a few lines of F#
- **Compare** encodings side-by-side — same eigenvalues, different circuit costs

| Feature | OpenFermion | Qiskit Nature | PennyLane | **FockMap** |
|---------|:-----------:|:------------:|:---------:|:-----------:|
| Define a new encoding | ~200 lines Python | Not supported | Not supported | **3–5 lines F#** |
| Tree → encoding pipeline | ❌ | ❌ | ❌ | **✅** |
| Qubit tapering (Z₂ + Clifford) | ❌ | Partial | ❌ | **✅** |
| Trotter decomposition | ❌ | ✅ | ✅ | **✅ (1st + 2nd order)** |
| Circuit export (QASM, Q#, JSON) | ❌ | QASM only | ❌ | **✅ (all three)** |
| Hamiltonian skeleton (PES scans) | ❌ | ❌ | ❌ | **✅** |
| Measurement grouping + shot estimates | ❌ | ✅ | ✅ | **✅** |
| QPE resource estimation | ❌ | ❌ | ❌ | **✅** |
| Bosonic operator algebra (CCR) | ❌ | ❌ | ✅ | **✅** |
| Bosonic-to-qubit encodings | ❌ | ❌ | ❌ | **✅ (Unary, Binary, Gray)** |
| Mixed fermion–boson normal ordering | ❌ | ❌ | ❌ | **✅** |
| Type-safe operator algebra | ❌ | ❌ | ❌ | **✅** |
| Pure functional, zero mutation | ❌ | ❌ | ❌ | **✅** |
| Runtime dependencies | NumPy, SciPy | Many | NumPy, autograd, … | **None** |

Internally, the library uses exact symbolic Pauli algebra (not floating-point matrix multiplication), so encoded operator manipulation stays fast and predictable.

## Available Encodings

### Fermionic Encodings

| Encoding | Worst-Case Pauli Weight | Framework | Function |
|----------|:-----------------------:|:---------:|----------|
| Jordan-Wigner | $O(n)$ | Index-set | `jordanWignerTerms` |
| Bravyi-Kitaev | $O(\log_2 n)$ | Index-set | `bravyiKitaevTerms` |
| Parity | $O(n)$ | Index-set | `parityTerms` |
| Balanced Binary Tree | $O(\log_2 n)$ | Path-based | `balancedBinaryTreeTerms` |
| Balanced Ternary Tree | $O(\log_3 n)$ | Path-based | `ternaryTreeTerms` |
| Vlasov (Complete Ternary) | $O(\log_3 n)$ | Path-based | `vlasovTreeTerms` |

### Bosonic Encodings

| Encoding | Qubits / Mode | Max Weight | Function |
|----------|:---:|:---:|----------|
| Unary (one-hot) | $d$ | 2 | `unaryBosonTerms` |
| Standard Binary | $\lceil\log_2 d\rceil$ | $\lceil\log_2 d\rceil$ | `binaryBosonTerms` |
| Gray Code | $\lceil\log_2 d\rceil$ | $\lceil\log_2 d\rceil$ | `grayCodeBosonTerms` |

Bosonic modes are truncated to $d$ occupation levels. All bosonic encodings share the `BosonicEncoderFn` signature and return `PauliRegisterSequence`, just like their fermionic counterparts.

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

This repository includes a full [dev container](https://containers.dev/) configuration with .NET 10 (GA), F#, Python, LaTeX, and all required tooling pre-installed. To use it:

1. Install [Docker](https://www.docker.com/) and [VS Code](https://code.visualstudio.com/) with the [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers)
2. Clone the repository and open it in VS Code
3. When prompted, click **"Reopen in Container"** (or run `Dev Containers: Reopen in Container` from the command palette)
4. The container builds, restores packages, compiles the project, and runs tests automatically

Everything is pre-configured, so you can start coding immediately.

## Quick Start

```fsharp
open System.Numerics
open Encodings

// 1. Define molecular integrals (H₂ in STO-3G)
let integrals = Map [
    ("0,0", Complex(-1.2563, 0.0)); ("1,1", Complex(-1.2563, 0.0))
    ("2,2", Complex(-0.4719, 0.0)); ("3,3", Complex(-0.4719, 0.0))
    // ... (two-body integrals)
]
let factory key = integrals |> Map.tryFind key

// 2. Encode → 15-term Pauli Hamiltonian
let ham = computeHamiltonianWith jordanWignerTerms factory 4u

// 3. Taper → remove symmetry-redundant qubits
let tapered = taper defaultTaperingOptions ham
// 4 → 2 qubits

// 4. Trotterize → gate sequence
let step = firstOrderTrotter 0.1 tapered.Hamiltonian
let gates = decomposeTrotterStep step

// 5. Export → OpenQASM 3.0
let qasm = toOpenQasm defaultOpenQasmOptions tapered.TaperedQubitCount gates
// Ready to run on IBM Quantum, IonQ, Rigetti, Amazon Braket

// Also available: Q# and JSON export
let qs   = toQSharp defaultQSharpOptions tapered.TaperedQubitCount gates
let json = toCircuitJson tapered.TaperedQubitCount Map.empty gates
```

## Where to Start

- **The Book:** [*From Molecules to Quantum Circuits*](https://johnazariah.github.io/encodings-book) — 23 chapters, from molecular integrals to quantum circuits
- **Interactive Labs:** [10 F# scripts](https://github.com/johnazariah/encodings-book/tree/main/labs) — run with `dotnet fsi`
- **API Cookbook:** [15-chapter tutorial](https://johnazariah.github.io/encodings/guides/cookbook/) — every type and function
- **Architecture:** [How the library works](https://johnazariah.github.io/encodings/guides/architecture.html)
- **API Reference:** [All types and functions](https://johnazariah.github.io/encodings/reference/index.html)

## Documentation

- **Site**: [johnazariah.github.io/encodings](https://johnazariah.github.io/encodings/) — API cookbook, architecture guide, cross-platform notes
- **Book**: [johnazariah.github.io/encodings-book](https://johnazariah.github.io/encodings-book) — full narrative with labs and companion code

## How it Works

FockMap implements the complete quantum simulation pipeline:

1. **Encoding** — Map fermionic/bosonic ladder operators to Pauli strings via index-set schemes (JW, BK, Parity) or path-based tree encodings (binary, ternary, Vlasov, custom)
2. **Hamiltonian Assembly** — Combine encoded operators with molecular integrals to build a qubit Hamiltonian (`PauliRegisterSequence`)
3. **Tapering** — Detect Z₂ symmetries (diagonal or general via Clifford rotation), fix sectors, and remove redundant qubits
4. **Trotterization** — Decompose the Hamiltonian into Pauli rotations (first or second order), then into elementary gates (H, S, CNOT, Rz)
5. **Circuit Output** — Export gate sequences as OpenQASM 3.0, Q#, or JSON
6. **Analysis** — Measurement grouping, shot estimation, QPE resource estimation, cost comparison across encodings

Everything is symbolic and exact — no floating-point matrix multiplication, no approximation until the Trotter step.

For the full API walkthrough, see the [Cookbook](https://johnazariah.github.io/encodings/guides/cookbook/) (18 chapters) or the [Architecture guide](https://johnazariah.github.io/encodings/guides/architecture.html).

## Testing

```bash
dotnet test                                          # all tests
dotnet test --logger "console;verbosity=detailed"    # verbose
dotnet test --collect:"XPlat Code Coverage"          # with coverage
```

The test suite covers encoding correctness, Pauli algebra laws, tapering, Trotterization, circuit output, and cross-encoding consistency.
See the **[Test Register](.project/test-register.md)** for a plain-English catalogue of all 700+ automated tests.

## Cross-Platform

This library runs on **Windows**, **macOS**, and **Linux** via [.NET 10](https://dotnet.microsoft.com/), Microsoft's open-source, cross-platform runtime. It is written in [F#](https://fsharp.org/), a functional-first language that is fully open-source under the [F# Software Foundation](https://foundation.fsharp.org/) and the [.NET Foundation](https://dotnetfoundation.org/).

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
