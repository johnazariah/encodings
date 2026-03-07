# Chapter 13: First and Second Order Trotter in FockMap

_From Hamiltonian to rotation list — the FockMap Trotterization API._

## In This Chapter

- **What you'll learn:** How FockMap decomposes a Pauli-sum Hamiltonian into a sequence of Pauli rotations using first-order and second-order Trotter formulas.
- **Why this matters:** The rotation list is the intermediate representation between the symbolic Hamiltonian and the gate-level circuit.
- **Prerequisites:** Chapter 12 (Trotter theory).

---

## The Rotation List

A Trotter step converts a Hamiltonian $\hat{H} = \sum_k c_k P_k$ and a time step $\Delta t$ into a list of **Pauli rotations**:

$$e^{-ic_k P_k \Delta t} \quad \text{for each term } k$$

Each rotation is characterized by two things:
- **The Pauli string** $P_k$ (e.g., $XXYY$)
- **The rotation angle** $\theta_k = c_k \Delta t$

FockMap represents this as a `PauliRotation` record:

```fsharp
type PauliRotation =
    { Operator : PauliRegister    // The Pauli string
      Angle    : float }          // θ = c_k × Δt
```

---

## First-Order Trotter

First-order Trotter simply lists the rotations in order:

$$\prod_{k=1}^{L} e^{-ic_k P_k \Delta t}$$

```fsharp
let step = firstOrderTrotter 0.1 hamiltonian
// step.Rotations : PauliRotation[]
// step.Order : First
// step.TimeStep : 0.1
```

For our 15-term H₂ Hamiltonian with $\Delta t = 0.1$:

```fsharp
printfn "Rotations: %d" step.Rotations.Length
// → 14 (the identity term IIII contributes a global phase, typically dropped)

for r in step.Rotations do
    printfn "  %+.4f  %s" r.Angle r.Operator.Signature
```

Each rotation angle is the product of the Hamiltonian coefficient and the time step. The identity term ($IIII$) contributes only a global phase and is usually omitted from the circuit.

---

## Second-Order Trotter

Second-order Trotter (Suzuki) uses the symmetric decomposition:

$$\prod_{k=1}^{L} e^{-ic_k P_k \Delta t/2} \cdot \prod_{k=L}^{1} e^{-ic_k P_k \Delta t/2}$$

```fsharp
let step2 = secondOrderTrotter 0.1 hamiltonian
printfn "Rotations: %d" step2.Rotations.Length
// → 28 (2 × 14, forward then reverse at half angle)
```

The angles are halved ($\theta_k = c_k \Delta t / 2$), and the reverse pass mirrors the sequence. The total rotation count is $2L$ instead of $L$, but the approximation error decreases from $O(\Delta t^2)$ to $O(\Delta t^3)$ per step.

---

## Choosing a Time Step

The time step $\Delta t$ controls the accuracy–depth trade-off:

| Smaller $\Delta t$ | Larger $\Delta t$ |
|:---|:---|
| Better Trotter approximation | Larger Trotter error |
| More steps needed for same total $t$ | Fewer total steps |
| Deeper total circuit | Shallower total circuit |

For molecular Hamiltonians, a common heuristic: $\Delta t \leq 1 / \lVert\hat{H}\rVert$ where $\lVert\hat{H}\rVert = \sum_k \lvert c_k\rvert$ is the 1-norm. For H₂, $\lVert\hat{H}\rVert \approx 3.7$ Ha, giving $\Delta t \lesssim 0.27$.

---

## Quick Cost Estimate

Without decomposing into gates (that's Chapter 14), we can already count CNOTs:

```fsharp
let cnotEstimate = trotterCnotCount step
// Each rotation of weight w costs 2(w-1) CNOTs
// Sum over all rotations
```

For H₂ (first-order, $L = 14$ non-identity terms):

| Term type | Count | Typical weight | CNOTs per rotation | Total CNOTs |
|:---:|:---:|:---:|:---:|:---:|
| Single-Z | 4 | 1 | 0 | 0 |
| Double-Z | 6 | 2 | 2 | 12 |
| XXYY-type | 4 | 4 | 6 | 24 |
| **Total** | **14** | — | — | **36** |

36 CNOTs per first-order Trotter step. Second-order doubles this to 72, but may need fewer total steps for the same precision.

---

## Key Takeaways

- A Trotter step converts a Hamiltonian into a list of `PauliRotation` records — each with a Pauli string and a rotation angle.
- First-order: $L$ rotations at angle $c_k \Delta t$. Second-order: $2L$ rotations at half angle.
- CNOT cost is estimable from Pauli weights alone: $\sum_k 2(w_k - 1)$.
- The time step $\Delta t$ trades circuit depth for approximation accuracy.

---

**Previous:** [Chapter 12 — From Hamiltonian to Time Evolution](12-time-evolution.html)

**Next:** [Chapter 14 — The CNOT Staircase](14-cnot-staircase.html)
