# 16. Trotterization

_Converting a Pauli Hamiltonian into a gate sequence._

> **Prerequisites:** Chapter 15 (tapering) and Chapter 10 (building a Hamiltonian).

## The Problem

You have a `PauliRegisterSequence` — a sum of weighted Pauli strings. A quantum computer needs a sequence of gates. Trotterization bridges the gap: it decomposes the time-evolution operator $e^{-iHt}$ into a product of single-term Pauli rotations.

## Trotter Steps

```fsharp
open Encodings
open System.Numerics
open Encodings.Hamiltonian
open Encodings.JordanWigner
open Encodings.BravyiKitaev
open Encodings.TreeEncoding
open Encodings.Tapering
open Encodings.Trotterization
open Encodings.CircuitOutput
open Encodings.CostAnalysis
open Encodings.Fcidump

// A self-contained H₂/STO-3G coefficient factory (see Chapter 13 for loading
// the same integrals from an FCIDUMP file via Fcidump.parseToSpinOrbitalFactory).
let h2Factory =
    let fcidump = """
 &FCI NORB=   2,NELEC= 2,MS2=0,
  ORBSYM=1,1,
  ISYM=1,
 &END
 0.6747559268144484    1    1    1    1
 0.6637114013508132    1    1    2    2
 0.1812104620151968    2    1    2    1
 0.6637114013508132    2    2    1    1
 0.697651504490461     2    2    2    2
 -1.253309786645977    1    1  0  0
 -0.4750688487721783   2    2  0  0
 0.7151043390810812  0  0  0  0
"""
    let (factory, _core, _nso) = Fcidump.parseToSpinOrbitalFactory fcidump
    factory

let ham = computeHamiltonianWith jordanWignerTerms h2Factory 4u
let tapered = taper defaultTaperingOptions ham

// First-order Trotter: L rotations, O(Δt²) error per step
let step1 = firstOrderTrotter 0.1 tapered.Hamiltonian

// Second-order Trotter: 2L rotations, O(Δt³) error per step
let step2 = secondOrderTrotter 0.1 tapered.Hamiltonian

printfn "First-order:  %d rotations" step1.Rotations.Length
printfn "Second-order: %d rotations" step2.Rotations.Length
```

Each `PauliRotation` has an `Operator` (a `PauliRegister`) and an `Angle` (float).

## Gate Decomposition

Each Pauli rotation becomes a CNOT staircase:

```fsharp
// Decompose into elementary gates (H, S, Sdg, CNOT, Rz)
let gates = decomposeTrotterStep step1

for g in gates do
    match g with
    | Gate.H q       -> printfn "H(%d)" q
    | Gate.S q       -> printfn "S(%d)" q
    | Gate.Sdg q     -> printfn "Sdg(%d)" q
    | Gate.CNOT(c,t) -> printfn "CNOT(%d,%d)" c t
    | Gate.Rz(q,a)   -> printfn "Rz(%d, %.6f)" q a
```

## Cost Analysis

```fsharp
// Quick CNOT count (no gate decomposition needed)
let cnots = trotterCnotCount step1
printfn "CNOTs per step: %d" cnots

// Full statistics
let stats = trotterStepStats step1
printfn "Rotations:     %d" stats.RotationCount
printfn "CNOTs:         %d" stats.CnotCount
printfn "Single-qubit:  %d" stats.SingleQubitCount
printfn "Total gates:   %d" stats.TotalGates
printfn "Max weight:    %d" stats.MaxWeight
printfn "Mean weight:   %.1f" stats.MeanWeight

// Compare across encodings
let jwHam = computeHamiltonianWith jordanWignerTerms h2Factory 4u
let bkHam = computeHamiltonianWith bravyiKitaevTerms h2Factory 4u
let ttHam = computeHamiltonianWith ternaryTreeTerms  h2Factory 4u
let results = compareTrotterCosts [| ("JW", jwHam); ("BK", bkHam); ("TT", ttHam) |] 0.1
for (name, stats) in results do
    printfn "%-5s  CNOTs=%d  Total=%d" name stats.CnotCount stats.TotalGates
```

## Key Types

| Type | Description |
|------|-------------|
| `PauliRotation` | `{ Operator: PauliRegister; Angle: float }` |
| `TrotterOrder` | `First` or `Second` |
| `TrotterStep` | `{ Rotations: PauliRotation[]; Order: TrotterOrder; TimeStep: float }` |
| `Gate` | `H of int \| S of int \| Sdg of int \| Rz of int * float \| CNOT of int * int` |
| `CircuitStats` | Rotation count, CNOT count, gate totals, weight stats |

---

[← Qubit Tapering](15-qubit-tapering.html) · [Circuit Output →](17-circuit-output.html) · [Index](index.html)
