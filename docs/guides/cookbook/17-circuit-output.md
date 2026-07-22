# 17. Circuit Output

_Exporting gate sequences to OpenQASM, Q#, and JSON._

> **Prerequisites:** Chapter 16 (Trotterization).

## Three Formats, One Gate Array

FockMap's `decomposeTrotterStep` produces a `Gate[]`. The circuit output module serialises it to any major quantum platform:

```fsharp
open System.Numerics
open Encodings
open Encodings.Fcidump
open Encodings.Hamiltonian
open Encodings.JordanWigner
open Encodings.Trotterization
open Encodings.CircuitOutput
open Encodings.Tapering

// Setup carried over from Chapters 15–16: build a tapered H₂ Trotter step.
let step, tapered =
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
    let (factory, _core, _nso) = parseToSpinOrbitalFactory fcidump
    let tapered = taper defaultTaperingOptions (computeHamiltonianWith jordanWignerTerms factory 4u)
    firstOrderTrotter 0.1 tapered.Hamiltonian, tapered

let gates = decomposeTrotterStep step
let n = tapered.TaperedQubitCount

// OpenQASM 3.0 (IBM Quantum, IonQ, Rigetti, Amazon Braket)
let qasm = toOpenQasm defaultOpenQasmOptions n gates
System.IO.File.WriteAllText("circuit.qasm", qasm)

// Q# (Azure Quantum)
let qs = toQSharp defaultQSharpOptions n gates
System.IO.File.WriteAllText("circuit.qs", qs)

// JSON (Python ecosystem — Qiskit, Cirq, Quokka)
let json = toCircuitJson n Map.empty gates
System.IO.File.WriteAllText("circuit.json", json)
```

## OpenQASM Options

```fsharp
type OpenQasmOptions =
    { IncludeHeader : bool      // "OPENQASM 3.0;" + include
      QubitName     : string    // default: "q"
      Precision     : int       // decimal places for angles
      Version       : QasmVersion }  // V2 or V3

// QASM 2.0 for older platforms
let qasm2 = toOpenQasm defaultOpenQasm2Options n gates
```

## Q# Options

```fsharp
type QSharpOptions =
    { Namespace     : string    // default: "FockMap.Generated"
      OperationName : string    // default: "TrotterStep"
      Precision     : int }     // decimal places for angles
```

## JSON Metadata

```fsharp
let metadata = Map [
    ("molecule", "H2"); ("encoding", "ternary-tree")
    ("trotter_order", "1"); ("time_step", "0.1")
]
let jsonWithMeta = toCircuitJson n metadata gates
```

## Convenience Functions

Skip the intermediate `Gate[]` and go directly from `TrotterStep`:

```fsharp
let qasmDirect = trotterStepToOpenQasm defaultOpenQasmOptions step
let qsDirect   = trotterStepToQSharp defaultQSharpOptions step
```

## Key Types

| Type | Description |
|------|-------------|
| `QasmVersion` | `V2` or `V3` |
| `OpenQasmOptions` | Header, qubit name, precision, version |
| `QSharpOptions` | Namespace, operation name, precision |
| `CodeFragment` | Internal: list of code lines with indentation support |

---

[← Trotterization](16-trotterization.html) · [Measurement & Resources →](18-measurement-resources.html) · [Index](index.html)
