# Chapter 14: Trotterization in Practice

_Chapter 13 gave us the theory. Now we compute: from Hamiltonian to rotation list to gate count._

## In This Chapter

- **What you'll learn:** How to apply first-order and second-order Trotter decomposition to a real Hamiltonian, what the rotation list looks like, and how to estimate circuit cost before generating a single gate.
- **Why this matters:** The rotation list is the bridge between the symbolic Hamiltonian and the physical circuit. Getting the angles and ordering right is where theory meets implementation.
- **Prerequisites:** Chapter 13 (you understand the Trotter–Suzuki formula and why it's needed).

---

## From Theory to Concrete Rotations

Chapter 13 established the idea: break $e^{-i\hat{H}t}$ into a product of single-term rotations $e^{-ic_k P_k \Delta t}$. Now let's apply this to the H₂ Hamiltonian we've been carrying since Chapter 6 and see exactly what comes out.

Recall the 15-term Hamiltonian:

| # | Pauli String | Coefficient (Ha) |
|:---:|:---:|:---:|
| 1 | $IIII$ | $-1.0704$ |
| 2–5 | $IIIZ$, $IIZI$, $IZII$, $ZIII$ | $\pm 0.09$ to $\pm 0.30$ |
| 6–11 | $IIZZ$, $IZIZ$, ... | $\pm 0.01$ to $\pm 0.17$ |
| 12–15 | $XXYY$, $XYYX$, $YXXY$, $YYXX$ | $\pm 0.1744$ |

The identity term ($IIII$) contributes a global phase $e^{-i(-1.0704)t}$ which has no observable effect — it shifts all eigenvalues equally. We drop it from the circuit, leaving **14 non-identity terms** to Trotterize.

### First-order Trotter with $\Delta t = 0.1$

Each term produces one Pauli rotation. The angle is $\theta_k = c_k \times \Delta t$:

```fsharp
open Encodings

let step = firstOrderTrotter 0.1 hamiltonian

printfn "Rotations: %d" step.Rotations.Length
for r in step.Rotations do
    printfn "  angle=%+.6f  Pauli=%s  weight=%d"
        r.Angle
        r.Operator.Signature
        (r.Operator.Signature |> Seq.filter (fun c -> c <> 'I') |> Seq.length)
```

The output will show 14 rotations — one per non-identity Hamiltonian term. The angles are small (all less than 0.04 in magnitude) because we chose $\Delta t = 0.1$ and the coefficients are all less than 0.35 Ha.

### What the rotation list tells us

Each entry says: "rotate the quantum state by angle $\theta$ around the axis defined by Pauli string $P$." In physical terms, this evolves the state under the influence of one term of the Hamiltonian for a short time.

The key observation from Chapter 6 carries through: the **diagonal rotations** (weights 1–2, Z-only terms) are cheap and classical. The **off-diagonal rotations** (weight 4, XXYY-type) are expensive and quantum. The Trotter decomposition separates them — each gets its own rotation — and the encoding determines how expensive each one is.

---

## Second-Order Trotter: Symmetry for Accuracy

First-order Trotter applies the rotations in one direction:

$$e^{-ic_1 P_1 \Delta t} \cdot e^{-ic_2 P_2 \Delta t} \cdot \ldots \cdot e^{-ic_L P_L \Delta t}$$

Second-order Trotter applies them forward at half-angle, then backward at half-angle:

$$\underbrace{e^{-ic_1 P_1 \Delta t/2} \cdots e^{-ic_L P_L \Delta t/2}}_{\text{forward, half angle}} \cdot \underbrace{e^{-ic_L P_L \Delta t/2} \cdots e^{-ic_1 P_1 \Delta t/2}}_{\text{reverse, half angle}}$$

```fsharp
let step2 = secondOrderTrotter 0.1 hamiltonian

printfn "Rotations: %d (vs %d for first-order)"
    step2.Rotations.Length
    step.Rotations.Length
// → 28 rotations (2 × 14)
```

The symmetrization cancels the leading-order error terms. Intuitively: the forward pass over-approximates by a small amount, and the reverse pass under-approximates by the same amount. The errors cancel, leaving a much smaller residual.

| Property | First-order | Second-order |
|:---|:---:|:---:|
| Rotations per step | $L$ (14 for H₂) | $2L$ (28 for H₂) |
| Rotation angles | $c_k \Delta t$ | $c_k \Delta t / 2$ |
| Error per step | $O(\Delta t^2)$ | $O(\Delta t^3)$ |
| Total error for $N$ steps | $O(t^2/N)$ | $O(t^3/N^2)$ |

For the same target accuracy, second-order Trotter typically needs $\sqrt{N}$ fewer steps than first-order — which more than compensates for the doubled rotation count per step.

---

## Choosing the Time Step

The time step $\Delta t$ is the key knob in Trotterization. Too large → bad approximation. Too small → too many steps → too deep a circuit.

A practical rule of thumb: $\Delta t \leq 1 / \lVert\hat{H}\rVert_1$, where the 1-norm is $\lVert\hat{H}\rVert_1 = \sum_k |c_k|$.

For H₂: $\lVert\hat{H}\rVert_1 \approx 3.7$ Ha, giving $\Delta t \lesssim 0.27$. Our choice of $\Delta t = 0.1$ is comfortably within this bound.

For H₂O (12 qubits): $\lVert\hat{H}\rVert_1$ is larger (~30 Ha), so $\Delta t$ must be smaller — around $0.03$. This means more Trotter steps per unit time, which means more CNOTs. This is another reason larger molecules are harder to simulate.

---

## Quick CNOT Estimate (Before Gate Decomposition)

We can estimate the CNOT cost without actually building the gate circuit, using the formula from Chapter 4:

$$\text{CNOTs per Trotter step} = \sum_{k=1}^{L} 2(w_k - 1)$$

```fsharp
let cnots = trotterCnotCount step
printfn "Estimated CNOTs per first-order step: %d" cnots
```

For H₂ (14 non-identity terms):

| Term type | Count | Weight | CNOTs each | Subtotal |
|:---:|:---:|:---:|:---:|:---:|
| Single-Z ($IIIZ$, etc.) | 4 | 1 | 0 | 0 |
| Double-Z ($IIZZ$, etc.) | 6 | 2 | 2 | 12 |
| Exchange ($XXYY$, etc.) | 4 | 4 | 6 | 24 |
| **Total** | **14** | — | — | **36** |

36 CNOTs per first-order Trotter step. Second-order: 72. For a 100-step simulation: 3,600 (first-order) or 7,200 (second-order) CNOTs total.

These are small numbers — easily within reach of current hardware for H₂. For H₂O with ~600 terms and higher weights, the numbers grow into the thousands per step — which is where encoding choice and tapering make the difference between feasible and infeasible.

---

## Key Takeaways

- A Trotter step converts a Hamiltonian into a list of **Pauli rotations** — each with a Pauli string and an angle.
- First-order: $L$ rotations. Second-order: $2L$ rotations at half angle, with quadratically better error scaling.
- The time step $\Delta t$ is bounded by $1/\lVert H \rVert_1$ — larger Hamiltonians need smaller steps.
- CNOT cost is estimable from Pauli weights alone: $\sum_k 2(w_k - 1)$.
- For H₂: 36 CNOTs per first-order step. Feasible. For H₂O: thousands. That's where optimization matters.

## Common Mistakes

1. **Including the identity term.** The $IIII$ term contributes only a global phase — include it and you waste one rotation per step on something unobservable.

2. **Using first-order Trotter for production.** First-order is fine for learning but second-order is almost always better in practice — the error improvement outweighs the doubled rotation count.

3. **Choosing $\Delta t$ too large.** If $\Delta t \cdot \lVert H \rVert_1 > 1$, the Trotter approximation breaks down and the circuit does not approximate the correct time evolution.

## Exercises

1. **Rotation count.** For H₂O with 600 non-identity terms, how many rotations does a single second-order Trotter step have?

2. **Time step.** If $\lVert H \rVert_1 = 30$ Ha and you want $\Delta t = 0.01$, how many Trotter steps do you need for total evolution time $t = 1$?

3. **CNOT scaling.** If every term in a Hamiltonian has weight 5 (ternary tree on a 27-orbital system), and there are 200 non-identity terms, what is the CNOT count per first-order step?

---

**Previous:** [Chapter 13 — From Hamiltonian to Time Evolution](13-time-evolution.html)

**Next:** [Chapter 15 — The CNOT Staircase](15-cnot-staircase.html)
