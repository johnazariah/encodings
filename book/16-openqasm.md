# Chapter 16: OpenQASM Generation

> **DRAFT — This chapter will be finalized after the circuit output module is implemented.**
> **The API examples below reflect the planned design; the code is not yet available.**

_The gate sequence exists in FockMap's type system. Now we export it as executable quantum code._

## In This Chapter

- **What you'll learn:** How to convert FockMap's gate sequences to OpenQASM 3.0 — the universal interchange format for quantum circuits.
- **Why this matters:** OpenQASM is accepted by every major quantum platform (IBM, IonQ, Rigetti, Amazon Braket). A valid QASM file is a runnable quantum experiment.
- **Prerequisites:** Chapters 12–15 (you have a Trotter gate sequence).

---

## What Is OpenQASM?

OpenQASM (Open Quantum Assembly Language) is a text-based circuit description language maintained by IBM. Version 3.0 supports:
- Qubit and classical bit declarations
- Standard gates (H, S, CNOT, Rz, Rx, etc.)
- Measurement and classical control
- Parameterized circuits

A minimal QASM program looks like:

```
OPENQASM 3.0;
include "stdgates.inc";

qubit[4] q;

h q[0];
cx q[0], q[1];
rz(0.5) q[1];
cx q[0], q[1];
h q[0];
```

This is a single Pauli rotation for a $ZX$ operator with angle $\theta = 0.25$.

---

## FockMap to QASM

FockMap's circuit output module converts gate sequences to QASM strings:

```fsharp
let step = firstOrderTrotter 0.1 hamiltonian
let gates = decomposeTrotterStep step
let qasm = toOpenQasm defaultOpenQasmOptions gates
printfn "%s" qasm
```

The output is a valid OpenQASM 3.0 program that implements one Trotter step.

### Gate Mapping

| FockMap Gate | OpenQASM |
|:---|:---|
| `Had i` | `h q[i];` |
| `Sgate i` | `s q[i];` |
| `CNOT (c, t)` | `cx q[c], q[t];` |
| `Rz (i, θ)` | `rz(θ) q[i];` |

### Configuration

```fsharp
type OpenQasmOptions =
    { IncludeHeader : bool      // "OPENQASM 3.0;" header
      QubitName     : string    // default: "q"
      Precision     : int }     // decimal places for angles

let opts = { defaultOpenQasmOptions with Precision = 8 }
let qasm = toOpenQasm opts gates
```

---

## Full Pipeline Example

From molecule to QASM in one script:

```fsharp
// 1. Build Hamiltonian
let ham = computeHamiltonianWith jordanWignerTerms h2Factory 4u

// 2. Taper
let tapered = taper defaultTaperingOptions ham

// 3. Trotterize
let step = firstOrderTrotter 0.1 tapered.Hamiltonian

// 4. Decompose to gates
let gates = decomposeTrotterStep step

// 5. Export to QASM
let qasm = toOpenQasm defaultOpenQasmOptions gates
System.IO.File.WriteAllText("h2_trotter.qasm", qasm)
```

The resulting `.qasm` file can be loaded by any QASM-compatible simulator or hardware backend.

---

## Verifying the Output

You can verify the QASM by:
1. Loading it in IBM's Qiskit: `QuantumCircuit.from_qasm_str(qasm)`
2. Running a statevector simulation
3. Comparing the expectation value $\langle\hat{H}\rangle$ against the exact ground-state energy from Chapter 7

This closes the loop: molecule → integrals → Hamiltonian → taper → Trotter → QASM → simulate → compare.

---

## Key Takeaways

- OpenQASM 3.0 is the universal interchange format for quantum circuits.
- FockMap exports gate sequences as valid QASM with configurable options.
- The full pipeline from Hamiltonian to QASM is a few function calls.
- Verification: load the QASM in Qiskit and compare against known eigenvalues.

---

**Previous:** [Chapter 15 — Cost Analysis](15-cost-analysis.html)

**Next:** [Chapter 17 — Q# Integration](17-qsharp.html)
