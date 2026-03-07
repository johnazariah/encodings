# Chapter 17: Q# Integration

_Azure Quantum's native language. FockMap generates Q# operations from the same gate sequences._

## In This Chapter

- **What you'll learn:** How to export FockMap gate sequences as Q# operations for Azure Quantum.
- **Why this matters:** Q# is the native language for Microsoft's quantum computing stack. If your target is Azure Quantum hardware or simulators, Q# output saves a translation step.
- **Prerequisites:** Chapter 16 (circuit output concepts).

---

## Q# Output

FockMap generates valid Q# operation code from the same gate sequences used for QASM:

```fsharp
let qsharp = toQSharp defaultQSharpOptions 4 gates
printfn "%s" qsharp
```

Output:

```qsharp
namespace FockMap.Generated {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;

    operation TrotterStep(qs : Qubit[]) : Unit is Adj + Ctl {
        H(qs[0]);
        CNOT(qs[0], qs[1]);
        Rz(0.5, qs[1]);
        CNOT(qs[0], qs[1]);
        H(qs[0]);
    }
}
```

### Gate Mapping

| FockMap Gate | Q# |
|:---|:---|
| `Had i` | `H(qs[i]);` |
| `Sgate i` | `S(qs[i]);` |
| `CNOT (c, t)` | `CNOT(qs[c], qs[t]);` |
| `Rz (i, θ)` | `Rz(θ, qs[i]);` |

### Configuration

```fsharp
type QSharpOptions =
    { Namespace     : string    // default: "FockMap.Generated"
      OperationName : string    // default: "TrotterStep"
      Precision     : int }     // decimal places
```

---

## Using the Generated Q#

The generated operation:
- Has `Adj + Ctl` functors — it supports adjoint (inverse) and controlled variants automatically
- Takes a `Qubit[]` parameter — works with any qubit allocation strategy
- Can be called from QPE or VQE driver code

```qsharp
// Example driver
operation RunVQE() : Double {
    use qs = Qubit[4];
    // Prepare ansatz state...
    TrotterStep(qs);
    // Measure...
}
```

---

## Q# vs QASM: When to Use Which

| Criterion | OpenQASM | Q# |
|:---|:---|:---|
| **Universality** | Accepted everywhere | Azure Quantum only |
| **Type safety** | Minimal | Strong (functors, type checking) |
| **Controlled operations** | Manual | Automatic (`Ctl` functor) |
| **Target hardware** | IBM, IonQ, Rigetti, Braket | Azure Quantum (IonQ, Quantinuum via Azure) |
| **Simulation** | Qiskit Aer, others | QDK full-state simulator |

**Recommendation:** Generate QASM for portability, Q# for Azure Quantum workflows. FockMap can produce both from the same gate sequence.

---

## Key Takeaways

- FockMap generates valid Q# operations with `Adj + Ctl` functor support.
- The same gate sequence used for QASM is used for Q# — no separate compilation.
- Q# output is ideal for Azure Quantum integration.

---

**Previous:** [Chapter 16 — OpenQASM Generation](16-openqasm.html)

**Next:** [Chapter 18 — Python Bridge](18-python-bridge.html)
