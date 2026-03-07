# Qubit Tapering

This chapter introduces FockMap's qubit tapering module — a post-encoding
step that detects Z₂ symmetries in the Pauli Hamiltonian and removes
qubits, shrinking the problem before downstream simulation.

## Why Taper?

After encoding, many molecular Hamiltonians have qubits that are
"spectators" — they commute with every term and can be fixed to
an eigenvalue without losing physics. Removing them gives:
- fewer qubits (smaller circuits)
- fewer or lighter Pauli terms (less measurement overhead)
- reduced Hilbert space (faster classical verification)

## v1: Diagonal Z₂ Tapering

The simplest case: a qubit where every term has only `I` or `Z`.

```fsharp
open System.Numerics
open Encodings

let h =
    [| PauliRegister("ZIZI", Complex(0.8, 0.0))
       PauliRegister("ZZII", Complex(-0.4, 0.0))
       PauliRegister("IIZZ", Complex(0.3, 0.0))
       PauliRegister("IZIZ", Complex(0.2, 0.0)) |]
    |> PauliRegisterSequence

// Which qubits are diagonal Z₂?
let symQubits = diagonalZ2SymmetryQubits h
// → [| 0; 1; 2; 3 |]  — all four!

// Taper qubits 1 and 3 in the (+1, −1) sector
let result = taperDiagonalZ2 [ (1, 1); (3, -1) ] h
printfn "%d → %d qubits" result.OriginalQubitCount result.TaperedQubitCount
// 4 → 2 qubits
```

**Sector choice** matters: fixing qubit $j$ to eigenvalue $+1$ vs $-1$
multiplies any term with $Z_j$ by that eigenvalue. Different sectors
correspond to different quantum numbers (particle number, spin projection).

### Convenience: taper everything in the +1 sector

```fsharp
let auto = taperAllDiagonalZ2WithPositiveSector h
// Detects all diagonal Z₂ qubits, tapers them all in the +1 sector
```

## v2: General Clifford Tapering

Many Hamiltonians have symmetries that involve multiple qubits
(e.g., $Z_0 Z_1$). These can't be tapered diagonally, but a
Clifford rotation can map them onto a single-qubit $Z$.

### The pipeline

```fsharp
// Unified function — handles both diagonal and general symmetries
let result = taper defaultTaperingOptions h

printfn "Generators found: %d" result.Generators.Length
printfn "Clifford gates: %d" result.CliffordGates.Length
printfn "%d → %d qubits" result.OriginalQubitCount result.TaperedQubitCount
```

### Exploring symmetries

```fsharp
// How many qubits can we taper?
let k = z2SymmetryCount h

// What are the generators?
let gens = findCommutingGenerators h
let indep = independentGenerators gens
for g in indep do
    printfn "  %s" (fromSymplectic g).Signature
```

### Configuring the pipeline

```fsharp
// v1 fallback (diagonal only — faster for large systems)
taper { defaultTaperingOptions with Method = DiagonalOnly } h

// Cap removal to 2 qubits
taper { defaultTaperingOptions with MaxQubitsToRemove = Some 2 } h

// Explicit sector
taper { defaultTaperingOptions with Sector = [(0, 1); (1, -1)] } h
```

## Types at a Glance

| Type | Purpose |
|------|---------|
| `Z2TaperingResult` | v1 result: qubit counts, removed qubits, sector, Hamiltonian |
| `TaperingResult` | v2 result: adds generators, Clifford gates, target qubits |
| `TaperingOptions` | Sector, max removal, method (`DiagonalOnly` / `FullClifford`) |
| `SymplecticVector` | Binary (x\|z) representation of a Pauli string |
| `CliffordGate` | `Had` / `Sgate` / `CNOT` — elementary Clifford gates |

## Where Tapering Fits

```
Encode → Hamiltonian → Taper → (Trotter / VQE / QPE)
```

Tapering sits after encoding and before any circuit compilation step.
It is purely symbolic — no matrices, no eigensolver — and composes
with every other FockMap API.

## Further Reading

The tapering technique implemented here originates from:

- **Bravyi, Gambetta, Mezzacapo, Temme (2017)**
  *Tapering off qubits to simulate fermionic Hamiltonians.*
  [arXiv:1701.08213](https://arxiv.org/abs/1701.08213).
  Introduces the general Z₂ symmetry framework: finding Pauli operators
  that commute with every Hamiltonian term, using Clifford unitaries to
  rotate them onto single-qubit Zs, and fixing sector eigenvalues.
  FockMap's v2 `taper` pipeline implements this approach.

- **Yen, Verteletskyi, Izmaylov (2020)**
  *Measuring all compatible operators in one series of single-qubit measurements using unitary transformations.*
  [J. Chem. Theory Comput. 16, 2400](https://doi.org/10.1021/acs.jctc.0c00008).
  Discusses how reduced qubit counts from tapering combine with
  measurement grouping to further reduce total shot counts.

- **Setia, Bravyi, Mezzacapo, Whitfield (2020)**
  *Superfast encodings for fermionic quantum simulation.*
  [Phys. Rev. Research 2, 043180](https://doi.org/10.1103/PhysRevResearch.2.043180).
  Extends the symmetry analysis to encoding-specific structures
  (e.g., BK and ternary tree encodings), showing that encoding choice
  affects which symmetries are diagonal and how many qubits can be
  tapered without Clifford rotation.

For the symplectic representation of Pauli strings used internally:

- **Aaronson, Gottesman (2004)**
  *Improved simulation of stabilizer circuits.*
  [Phys. Rev. A 70, 052328](https://doi.org/10.1103/PhysRevA.70.052328).
  The tableau formalism that underlies FockMap's `SymplecticVector`,
  `CliffordGate`, and `applyClifford` implementation.

---

**Previous:** [Bosonic-to-Qubit Encodings](14-bosonic-encodings.html)

**Back to:** [Cookbook index](index.html)
