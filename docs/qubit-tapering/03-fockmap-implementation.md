# Chapter 3: FockMap Implementation

_In this chapter, you'll use FockMap's v1 tapering API to detect symmetries, choose sectors, and taper real Hamiltonians._

## In This Chapter

- **What you'll learn:** The FockMap tapering API: its types, functions, and how to apply them end-to-end
- **Why this matters:** This is your hands-on guide to implementing tapering in your quantum simulation workflow
- **Try this next:** Open the [Qubit Tapering lab](../labs/09-qubit-tapering.html) and experiment with larger systems.

## API Overview

FockMap's qubit tapering lives in the **`Tapering`** module and consists of four main functions:

| Function | Purpose | Returns |
|:---|:---|:---|
| `diagonalZ2SymmetryQubits` | Detect diagonal Z₂ qubits | `int[]` of qubit indices |
| `diagonalZ2Generators` | Construct Z generators for detected qubits | `PauliRegister[]` |
| `taperDiagonalZ2` | Taper with explicit sector choice | `Z2TaperingResult` |
| `taperAllDiagonalZ2WithPositiveSector` | Convenience: taper all detected qubits in +1 sector | `Z2TaperingResult` |

The result type `Z2TaperingResult` packages the tapered Hamiltonian with metadata:

```fsharp
type Z2TaperingResult =
    { OriginalQubitCount : int
      TaperedQubitCount  : int
      RemovedQubits      : int[]
      Sector             : (int * int) list
      Hamiltonian        : PauliRegisterSequence }
```

## Step 1: Create a Hamiltonian

```fsharp
open System.Numerics
open Encodings

let hamiltonian =
    [| PauliRegister("ZIZI", Complex(0.8, 0.0))
       PauliRegister("ZZII", Complex(-0.4, 0.0))
       PauliRegister("IIZZ", Complex(0.3, 0.0))
       PauliRegister("IZIZ", Complex(0.2, 0.0)) |]
    |> PauliRegisterSequence
```

## Step 2: Detect Diagonal Z₂ Qubits

```fsharp
let symQubits = diagonalZ2SymmetryQubits hamiltonian
// → [| 0; 1; 2; 3 |]  (all four are diagonal in this example)
```

If the result is empty, the Hamiltonian has no diagonal Z₂ symmetries and cannot be tapered with v1.

## Step 3a: Automatic Tapering (Positive Sector)

For quick exploration, taper all detected qubits in the $+1$ eigenvalue sector:

```fsharp
let tapered = taperAllDiagonalZ2WithPositiveSector hamiltonian

printfn "Removed qubits: %A" tapered.RemovedQubits
printfn "%d → %d qubits" tapered.OriginalQubitCount tapered.TaperedQubitCount
```

## Step 3b: Manual Sector Choice

For fine control — e.g., to explore different particle-number sectors — specify the sector explicitly:

```fsharp
let sector = [ (1, 1); (3, -1) ]
let tapered2 = taperDiagonalZ2 sector hamiltonian

printfn "Removed: %A" tapered2.RemovedQubits   // [|1; 3|]
printfn "%d → %d qubits" tapered2.OriginalQubitCount tapered2.TaperedQubitCount
// 4 → 2 qubits
```

## Step 4: Inspect Generators

View the single-qubit Z generators for each detected symmetry:

```fsharp
let generators = diagonalZ2Generators hamiltonian
generators |> Array.iter (fun g -> printfn "  %s" (g.ToString()))
```

## Complete Workflow: Sector Sweep

Iterate over all $2^k$ sectors, taper each, and compare:

```fsharp
let allSectors (symQubits: int[]) =
    let k = symQubits.Length
    [ for mask in 0 .. (1 <<< k) - 1 ->
          symQubits
          |> Array.mapi (fun i q ->
              (q, if (mask &&& (1 <<< i)) <> 0 then 1 else -1))
          |> Array.toList ]

let sectors = allSectors (diagonalZ2SymmetryQubits hamiltonian)

for sector in sectors do
    let result = taperDiagonalZ2 sector hamiltonian
    printfn "Sector %A → %d qubits, %d terms"
        sector result.TaperedQubitCount
        result.Hamiltonian.SummandTerms.Length
```

## Validation and Error Handling

The API validates inputs to catch common mistakes:

```fsharp
// Non-diagonal qubit → ArgumentException
try taperDiagonalZ2 [(0, 1)] (PauliRegisterSequence [| PauliRegister("XI", Complex.One) |]) |> ignore
with :? System.ArgumentException as e -> printfn "%s" e.Message

// Invalid eigenvalue → ArgumentException
try taperDiagonalZ2 [(0, 0)] hamiltonian |> ignore
with :? System.ArgumentException as e -> printfn "%s" e.Message
```

## Limitations and v2 Direction

**v1 scope:**
- Diagonal Z₂ symmetries only (generators of the form $Z_i$)
- Single-qubit discovery and removal
- Fixed eigenvalue sectors

**v2 roadmap:**
- Multi-qubit stabiliser generators (e.g., $Z_i Z_j$)
- Clifford tapering — general stabiliser group reduction
- Automatic sector ordering by energy

---

**Previous:** [Chapter 2 — The Diagonal Z₂ Approach](02-diagonal-z2-approach.html)

**Next:** [Chapter 4 — General Z₂ and Clifford Tapering](04-clifford-tapering.html)
