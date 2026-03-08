# Chapter 20: Speaking the Hardware's Language

_The circuit exists in FockMap's type system. To run it on real hardware, we need to speak the machine's language._

## In This Chapter

- **What you'll learn:** How to export FockMap gate sequences as OpenQASM (universal), Q# (Azure Quantum), or JSON (Python ecosystem) — and when to use which.
- **Why this matters:** A gate sequence that only exists in memory is a theoretical result. An exported circuit is an experiment you can run.
- **Prerequisites:** Chapter 17 (the complete pipeline).

---

## Three Formats, One Circuit

FockMap's Trotter decomposition (Chapter 15) produces a concrete gate sequence — an array of `Gate` values representing Hadamard, S, CNOT, and Rz operations. That array is a platform-independent description of the circuit. But to execute it, you need to express it in a format that a quantum platform understands.

The three main targets:

| Format | Ecosystem | Platforms |
|:---|:---|:---|
| **OpenQASM 3.0** | Universal standard | IBM Quantum, IonQ, Rigetti, Amazon Braket |
| **Q#** | Microsoft | Azure Quantum, QDK simulator |
| **JSON** | Python SDKs | Qiskit, Cirq, Quokka |

FockMap can export to all three from the same gate array. The structure is always the same: take the Trotter step, decompose it to gates, export.

---

## OpenQASM: The Universal Format

OpenQASM (Open Quantum Assembly Language) is maintained by IBM and accepted by every major quantum platform. Version 3.0 supports qubit declarations, standard gates, parameterized rotations, and classical control.

```fsharp
let step = firstOrderTrotter 0.1 hamiltonian
let gates = decomposeTrotterStep step
let qasm = toOpenQasm defaultOpenQasmOptions numQubits gates
printfn "%s" qasm
```

The output looks like:

```
OPENQASM 3.0;
include "stdgates.inc";

qubit[2] q;

h q[0];
cx q[0], q[1];
rz(0.01234567) q[1];
cx q[0], q[1];
h q[0];
```

Each Pauli rotation from Chapter 15 becomes a CNOT staircase bracketed by basis-change gates. The angle in `rz()` is the rotation parameter $\theta = c_k \Delta t$.

### Gate Mapping

| FockMap Gate | OpenQASM |
|:---|:---|
| `Gate.H i` | `h q[i];` |
| `Gate.S i` | `s q[i];` |
| `Gate.Sdg i` | `sdg q[i];` |
| `Gate.CNOT (c, t)` | `cx q[c], q[t];` |
| `Gate.Rz (i, θ)` | `rz(θ) q[i];` |

### Configuration

```fsharp
type OpenQasmOptions =
    { IncludeHeader : bool      // "OPENQASM 3.0;" + include
      QubitName     : string    // default: "q"
      Precision     : int       // decimal places for angles
      Version       : QasmVersion }  // V2 or V3

// QASM 2.0 for Quokka compatibility
let qasm2 = toOpenQasm defaultOpenQasm2Options numQubits gates
```

QASM 2.0 differs in syntax (e.g., `qreg q[2];` instead of `qubit[2] q;`) but the gate names are identical. Use V2 when the target platform doesn't yet support V3.

---

## Q#: Azure Quantum

Q# is Microsoft's quantum programming language, designed from the ground up for expressing quantum algorithms (Svore et al., "Q#: Enabling Scalable Quantum Computing and Development with a High-level DSL", arXiv:1803.00652). Full disclosure: the author was part of the team at Microsoft that created Q# — so the export format here isn't a reverse-engineering exercise, it's a homecoming. FockMap generates a complete Q# operation — namespace, open statements, qubit allocation, gate calls — that you can compile and run on Azure Quantum or the local QDK simulator.

```fsharp
let qsharp = toQSharp defaultQSharpOptions numQubits gates
printfn "%s" qsharp
```

The output:

```qsharp
namespace FockMap.Generated {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;

    operation TrotterStep() : Unit {
        use q = Qubit[2];
        H(q[0]);
        CNOT(q[0], q[1]);
        Rz(0.01234567, q[1]);
        CNOT(q[0], q[1]);
        H(q[0]);
    }
}
```

### Configuration

```fsharp
type QSharpOptions =
    { Namespace     : string    // default: "FockMap.Generated"
      OperationName : string    // default: "TrotterStep"
      Precision     : int }     // decimal places for angles
```

The generated operation is a pure gate sequence — no measurement, no classical control. The calling code (your VQE or QPE driver) allocates the qubits, calls `TrotterStep()`, and handles measurement.

---

## JSON: The Python Bridge

Most quantum computing platforms have Python SDKs (Qiskit, Cirq, PennyLane, Quokka). FockMap exports circuits as JSON that can be loaded by any Python library:

```fsharp
let metadata = Map [
    ("molecule", "H2")
    ("encoding", "ternary-tree")
    ("trotter_order", "1")
    ("time_step", "0.1")
]
let json = toCircuitJson numQubits metadata gates
System.IO.File.WriteAllText("h2_circuit.json", json)
```

The JSON schema:

```json
{
  "num_qubits": 2,
  "metadata": {
    "molecule": "H2",
    "encoding": "ternary-tree",
    "trotter_order": "1",
    "time_step": "0.1"
  },
  "gates": [
    { "type": "H", "qubit": 0 },
    { "type": "CNOT", "control": 0, "target": 1 },
    { "type": "Rz", "qubit": 1, "angle": 0.01234567 },
    { "type": "CNOT", "control": 0, "target": 1 },
    { "type": "H", "qubit": 0 }
  ]
}
```

On the Python side:

```python
import json
from qiskit import QuantumCircuit

with open("h2_circuit.json") as f:
    data = json.load(f)

qc = QuantumCircuit(data["num_qubits"])
for g in data["gates"]:
    if g["type"] == "H":
        qc.h(g["qubit"])
    elif g["type"] == "CNOT":
        qc.cx(g["control"], g["target"])
    elif g["type"] == "Rz":
        qc.rz(g["angle"], g["qubit"])
    elif g["type"] == "S":
        qc.s(g["qubit"])
    elif g["type"] == "Sdg":
        qc.sdg(g["qubit"])
```

The JSON format is deliberately simple — flat list of gates, no nested structures — so that any Python library can consume it with a few lines of code.

---

## Same Circuit, Three Formats

Here is the full pipeline for H₂, exporting to all three formats:

```fsharp
let ham = computeHamiltonianWith ternaryTreeTerms factory 4u
let tapered = taper defaultTaperingOptions ham
let step = firstOrderTrotter 0.1 tapered.Hamiltonian
let gates = decomposeTrotterStep step
let n = tapered.TaperedQubitCount

// OpenQASM 3.0
let qasm = toOpenQasm defaultOpenQasmOptions n gates
System.IO.File.WriteAllText("h2.qasm", qasm)

// Q#
let qs = toQSharp defaultQSharpOptions n gates
System.IO.File.WriteAllText("h2.qs", qs)

// JSON (for Python/Qiskit)
let json = toCircuitJson n Map.empty gates
System.IO.File.WriteAllText("h2.json", json)

printfn "Exported %d gates to 3 formats" gates.Length
```

The gate array is the same in all three cases. Only the serialisation differs.

---

## When to Use Which

**OpenQASM** is the safe default. Every major platform accepts it, and QASM files are human-readable. Use QASM when:
- You want maximum portability
- You're submitting to IBM Quantum, IonQ, or Amazon Braket
- You need a format that works with transpilation tools (Qiskit, tket)

**Q#** when your target is Azure Quantum. The generated operation integrates directly into the Q# project system, with type checking and resource estimation built in.

**JSON** when your workflow lives in Python. The JSON bridge lets you construct the Hamiltonian and gate sequence in F# (where the algebra is exact) and run the circuit in Python (where the hardware SDKs live). This is the "best of both worlds" approach: algebraic precision in the construction, ecosystem breadth in the execution.

---

## Verification

Whichever format you choose, verify the output by:

1. Loading the circuit in a simulator
2. Running a statevector simulation
3. Computing $\langle\hat{H}\rangle$ from the statevector
4. Comparing against the exact ground-state energy from Chapter 8

For H₂, the exact energy is $-1.8572$ Ha (including nuclear repulsion). The Trotterised circuit should produce an energy within $O(\Delta t^2)$ of this — about $-1.856$ Ha at $\Delta t = 0.1$. If the numbers match, the pipeline is correct from integrals to circuit.

---

## Key Takeaways

- FockMap exports to **three formats** from the same gate array: OpenQASM (universal), Q# (Azure), JSON (Python).
- The gate sequence is platform-independent; only the serialisation differs.
- **OpenQASM** for portability, **Q#** for Azure integration, **JSON** for Python workflows.
- Always verify by comparing simulated expectation values against known eigenvalues.

---

**Previous:** [Chapter 19 — Algorithms: VQE and QPE](19-algorithms.html)

**Next:** [Chapter 21 — Scaling: From H₂ to FeMo-co](21-scaling.html)
