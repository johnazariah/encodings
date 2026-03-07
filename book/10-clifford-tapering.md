# Chapter 10: General Clifford Tapering

_When no single qubit is diagonal, multi-qubit Z₂ symmetries may still exist. A Clifford rotation reveals them._

## In This Chapter

- **What you'll learn:** The symplectic representation of Pauli strings, how to find all Z₂ symmetry generators via GF(2) linear algebra, how to synthesize a Clifford circuit that makes them diagonal, and how FockMap's unified `taper` function combines everything.
- **Why this matters:** Many molecular Hamiltonians have Z₂ symmetries that diagonal-only tapering cannot exploit. Clifford tapering finds and uses them all.
- **Prerequisites:** Chapters 8–9 (diagonal tapering concepts and mechanics).

---

## The Limitation of Diagonal Tapering

Diagonal tapering requires a qubit where *every* Hamiltonian term is I or Z. But consider the 2-qubit Heisenberg model:

$$\hat{H} = J(X_0 X_1 + Y_0 Y_1 + Z_0 Z_1)$$

No single qubit is diagonal — both qubits have X, Y, and Z terms. Diagonal tapering finds nothing.

But the operator $Z_0 Z_1$ **commutes** with every term: $[Z_0 Z_1, X_0 X_1] = [Z_0 Z_1, Y_0 Y_1] = [Z_0 Z_1, Z_0 Z_1] = 0$. It is a valid Z₂ symmetry generator — it just involves *two* qubits instead of one.

If we could rotate $Z_0 Z_1$ onto a single-qubit $Z$ — say, $Z_0 I_1$ — then qubit 0 would become diagonally taperable. The Clifford rotation that achieves this is a simple CNOT.

---

## The Symplectic Representation

To find all Z₂ generators systematically, we need a representation that makes commutativity a linear operation. The **symplectic representation** encodes each $n$-qubit Pauli string as a binary vector of length $2n$:

$$\sigma \;\leftrightarrow\; (\underbrace{x_0, x_1, \ldots, x_{n-1}}_{\text{X bits}} \mid \underbrace{z_0, z_1, \ldots, z_{n-1}}_{\text{Z bits}})$$

| Pauli | X bit | Z bit |
|:---:|:---:|:---:|
| I | 0 | 0 |
| X | 1 | 0 |
| Y | 1 | 1 |
| Z | 0 | 1 |

```fsharp
let sv = toSymplectic (PauliRegister("XYZ", Complex.One))
// sv.X = [| true; true; false |]   — X has x=1, Y has x=1, Z has x=0
// sv.Z = [| false; true; true |]   — X has z=0, Y has z=1, Z has z=1
```

### Why symplectic?

Two Pauli strings commute if and only if their **symplectic inner product** is zero (mod 2):

$$\langle a, b \rangle_s = \sum_{i=0}^{n-1} (a_{x_i} \cdot b_{z_i} + a_{z_i} \cdot b_{x_i}) \mod 2$$

If this sum is even → commute. If odd → anti-commute. This reduces commutativity checking to a **dot product over GF(2)** — a single bitwise operation.

```fsharp
let a = toSymplectic (PauliRegister("XX", Complex.One))
let b = toSymplectic (PauliRegister("ZZ", Complex.One))
commutes a b  // true — XX and ZZ commute
```

---

## Finding All Symmetry Generators

A Pauli string $g$ is a Z₂ symmetry of $\hat{H}$ if it commutes with every term and squares to the identity. Since every Pauli string squares to $\pm I$, the squaring condition is automatic.

The commutation condition $\langle g, t_k \rangle_s = 0$ for all terms $t_k$ is a system of linear equations over GF(2). The solution set — the **null space** of the commutation check matrix — gives all Z₂ generators.

```fsharp
let generators = findCommutingGenerators hamiltonian
// Returns SymplecticVector[] — all Pauli strings that commute with every term

let indep = independentGenerators generators
// Selects a maximal linearly independent subset over GF(2)
// The number of independent generators = max qubits taperable
```

The null space computation uses Gaussian elimination over GF(2) — the same algorithm as regular Gaussian elimination, but with XOR instead of subtraction. It runs in $O(n^3)$ time.

---

## Clifford Rotation: Making Generators Diagonal

Once we have independent generators, we need a Clifford circuit $U$ such that:

$$U g_i U^\dagger = Z_{q_i} \quad \text{for each generator } g_i$$

This rotates each multi-qubit generator onto a single-qubit $Z$, making the system diagonally taperable.

FockMap synthesizes this circuit using three elementary gates:

| Gate | Symbol | Effect on Pauli |
|:---|:---:|:---|
| Hadamard | $H_j$ | Swaps X↔Z on qubit $j$ |
| Phase gate | $S_j$ | Maps X→Y on qubit $j$ (Z unchanged) |
| CNOT | $\text{CNOT}_{c,t}$ | Propagates X from control to target; propagates Z from target to control |

The synthesis algorithm:
1. For each generator, find a qubit where it has support (X, Y, or Z bit set).
2. If the support is X, apply H to convert to Z. If Y, apply S then H.
3. Use CNOTs to clear all other qubits' support, leaving Z on exactly one qubit.

```fsharp
let (gates, targets) = synthesizeTaperingClifford independentGens
// gates : CliffordGate list — the rotation circuit
// targets : int[] — which qubit each generator maps to
```

---

## Applying the Clifford to the Hamiltonian

The Clifford circuit is applied **symbolically** — no matrices, no state vectors. Each Pauli term is conjugated: $\sigma_\alpha \to U \sigma_\alpha U^\dagger$. This is a sequence of substitution rules applied to the symplectic vector:

```fsharp
let rotatedH = applyClifford gates hamiltonian
// Every term is now conjugated — generators have become single-qubit Zs
```

After rotation, the target qubits are diagonally taperable, and we apply the v1 diagonal tapering from Chapter 9.

---

## The Unified Pipeline

FockMap's `taper` function combines everything:

```fsharp
// Full Clifford tapering (default)
let result = taper defaultTaperingOptions hamiltonian

// Diagonal only (v1 fallback)
let result = taper { defaultTaperingOptions with Method = DiagonalOnly } h

// Cap removal
let result = taper { defaultTaperingOptions with MaxQubitsToRemove = Some 2 } h

// Explicit sector
let result = taper { defaultTaperingOptions with Sector = [(0,1); (1,-1)] } h
```

The result includes everything you need:

```fsharp
result.OriginalQubitCount  // before tapering
result.TaperedQubitCount   // after tapering
result.RemovedQubits       // which qubits were removed
result.Generators          // the Z₂ generators found
result.CliffordGates       // the rotation circuit applied
result.TargetQubits        // which qubits the generators mapped to
result.Hamiltonian         // the tapered PauliRegisterSequence
```

---

## Worked Example: Heisenberg Model

$$\hat{H} = X_0X_1 + Y_0Y_1 + Z_0Z_1$$

**Step 1: Diagonal tapering finds nothing.** Both qubits have X, Y, and Z.

**Step 2: General Z₂ detection.** $Z_0Z_1$ commutes with all three terms → it is a generator.

**Step 3: Clifford synthesis.** $Z_0Z_1$ has Z on both qubits. A CNOT$(1, 0)$ maps:
- $Z_0Z_1 \to Z_0I_1$ (qubit 0 becomes the target)

**Step 4: Apply Clifford.** The Hamiltonian transforms. After rotation, qubit 0 has only I/Z on every term.

**Step 5: Diagonal taper.** Fix qubit 0 to sector $+1$, remove it. Result: 1-qubit Hamiltonian.

```fsharp
let heis =
    [| PauliRegister("XX", Complex(1.0, 0.0))
       PauliRegister("YY", Complex(1.0, 0.0))
       PauliRegister("ZZ", Complex(1.0, 0.0)) |]
    |> PauliRegisterSequence

let result = taper defaultTaperingOptions heis
// 2 → 1 qubit
```

---

## Key Takeaways

- **Symplectic representation** turns commutativity into GF(2) linear algebra — fast and exact.
- **Null space** of the commutation check matrix gives all Z₂ generators; Gaussian elimination selects independent ones.
- **Clifford synthesis** rotates multi-qubit generators onto single-qubit Zs using H, S, and CNOT — no matrices needed.
- **The unified `taper` function** handles both diagonal and Clifford tapering with one API.
- Everything is symbolic and exact — no approximation, no eigensolvers, no numerical instability.

## Further Reading

- Bravyi, S. et al. "Tapering off qubits to simulate fermionic Hamiltonians." arXiv:1701.08213 (2017). The general Z₂ tapering framework.
- Aaronson, S. and Gottesman, D. "Improved simulation of stabilizer circuits." *Phys. Rev. A* 70, 052328 (2004). The tableau formalism underlying our Clifford implementation.

---

**Previous:** [Chapter 9 — Diagonal Z₂ Symmetries](09-diagonal-z2.html)

**Next:** [Chapter 11 — Tapering Benchmarks](11-tapering-benchmarks.html)
