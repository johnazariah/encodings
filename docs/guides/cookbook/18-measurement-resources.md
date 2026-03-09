# 18. Measurement, Resources, and the Skeleton API

_Measurement grouping, shot estimates, QPE resources, and efficient PES scans._

> **Prerequisites:** Chapter 16 (Trotterization) and Chapter 17 (Circuit Output).

## Measurement Grouping

For VQE, Pauli terms that qubit-wise commute can be measured simultaneously:

```fsharp
open Encodings

let program = groupCommutingTerms hamiltonian
printfn "Total terms: %d" program.TotalTerms
printfn "Groups: %d" program.GroupCount

for basis in program.Bases do
    printfn "  %d terms, weight %.1f" basis.Terms.Length basis.Weight
```

Fewer groups = fewer distinct circuits = faster VQE iterations.

## Shot Count Estimation

How many measurement shots for a target energy precision?

```fsharp
// Chemical accuracy: ε = 1.6 mHa
let shots = estimateShots 0.0016 hamiltonian
printfn "Shots needed: %d" shots
// H₂: ~5 million shots
```

The formula: $N \geq (\sum_k |c_k|)^2 / \epsilon^2$, where the 1-norm $\sum |c_k|$ is determined by the Hamiltonian coefficients. Tapering reduces the 1-norm, so it saves both gate cost *and* measurement cost.

## QPE Resource Estimation

For fault-tolerant QPE — how many qubits and CNOTs?

```fsharp
let resources = qpeResources 10 hamiltonian 0.1
printfn "System qubits:  %d" resources.SystemQubits
printfn "Ancilla qubits: %d" resources.AncillaQubits
printfn "Total CNOTs:    %d" resources.TotalCnots
printfn "Circuit depth:  %d" resources.CircuitDepth
printfn "Precision bits: %d" resources.PrecisionBits
```

## The Skeleton API (for PES Scans)

When scanning a potential energy surface (e.g., bond lengths or angles), the Pauli string *structure* is the same at every geometry — only the integral *values* change. The skeleton API separates structure from coefficients:

```fsharp
// Precompute the structure once (expensive)
let skeleton = computeHamiltonianSkeleton ternaryTreeTerms 14u

// Apply coefficients at each geometry (cheap)
for angle in [| 60.0 .. 5.0 .. 180.0 |] do
    let factory = integralsAtAngle angle
    let ham = applyCoefficients skeleton factory
    let tapered = taper defaultTaperingOptions ham
    printfn "%.0f°: %d terms" angle (tapered.Hamiltonian.SummandTerms.Length)
```

For a 25-point scan, this is 25× faster than calling `computeHamiltonianWith` at each geometry.

### Skeleton with Pre-filtered Coefficients

If you already have integrals for one geometry:

```fsharp
let skeleton = computeHamiltonianSkeletonFor encoder factory numQubits
// skeleton.OneBody : SkeletonEntry[]
// skeleton.TwoBody : SkeletonEntry[]
// skeleton.NumQubits : uint32
```

## Key Types

| Type | Description |
|------|-------------|
| `MeasurementBasis` | Basis rotation + grouped Pauli terms + weight |
| `MeasurementProgram` | `{ Bases; TotalTerms; GroupCount }` |
| `QPEResourceEstimate` | System/ancilla qubits, CNOT count, depth, precision |
| `HamiltonianSkeleton` | Precomputed one-body + two-body Pauli structure |
| `SkeletonEntry` | `{ Key: string; Terms: SkeletonTerm[] }` |

---

[← Circuit Output](17-circuit-output.html) · [Index](index.html)
