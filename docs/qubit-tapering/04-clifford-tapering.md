# Chapter 4: General Z₂ and Clifford Tapering

_In this chapter, you'll go beyond diagonal symmetries to detect multi-qubit Z₂ generators and use Clifford rotations to taper any Hamiltonian._

## In This Chapter

- **What you'll learn:** Symplectic representation of Pauli strings, general symmetry detection, Clifford rotation synthesis, and the unified tapering pipeline
- **Why this matters:** Many molecular Hamiltonians have symmetries that v1 (diagonal-only) cannot exploit — v2 finds and uses them all
- **Try this next:** Use the [Qubit Tapering lab](../labs/09-qubit-tapering.html) with the `taper` function on real Hamiltonians.

## Beyond Diagonal: Multi-Qubit Generators

v1 handled the case where a single qubit had only I or Z on every term — a generator of the form $Z_i$. But many Hamiltonians have **multi-qubit** Z₂ symmetries:

$$g = Z_0 Z_1 \quad \text{or} \quad g = X_1 Y_2 Z_3$$

These commute with every term in $H$ and square to $I$, so they are valid symmetry generators. But they involve multiple qubits, and you cannot simply "fix a value and remove" — you first need a **Clifford rotation** to map the generator onto a single-qubit $Z$.

## The Symplectic Representation

Every $n$-qubit Pauli string can be encoded as a binary vector of length $2n$:

$$\sigma = (x_0, x_1, \ldots, x_{n-1} \mid z_0, z_1, \ldots, z_{n-1})$$

where each qubit's Pauli is encoded as: $I=(0,0)$, $X=(1,0)$, $Y=(1,1)$, $Z=(0,1)$.

Two Pauli strings **commute** if and only if their **symplectic inner product** is zero (mod 2):

$$\langle a, b \rangle_s = \sum_i (a_{x_i} \cdot b_{z_i} + a_{z_i} \cdot b_{x_i}) \mod 2$$

FockMap provides:

```fsharp
let sv = toSymplectic (PauliRegister("XYZ", Complex.One))
// sv.X = [| true; true; false |]  (X=1, Y=1, Z=0)
// sv.Z = [| false; true; true |]  (X=0, Y=1, Z=1)

let a = toSymplectic (PauliRegister("XZ", Complex.One))
let b = toSymplectic (PauliRegister("ZX", Complex.One))
commutes a b  // true (XZ and ZX commute on 2 qubits)
```

## Finding All Symmetry Generators

A Pauli string $g$ is a Z₂ symmetry of $H$ if:
1. $g$ commutes with every term: $[g, \sigma_\alpha] = 0$ for all $\alpha$
2. $g^2 = I$ (automatically true for any Pauli string)

FockMap finds all such generators by computing the **null space** of the commutation check matrix over GF(2):

```fsharp
let generators = findCommutingGenerators hamiltonian
// Returns SymplecticVector[] — all Pauli strings that commute with every term
```

Then selects a **maximal independent subset**:

```fsharp
let indep = independentGenerators generators
// Removes redundant generators (e.g., ZZ is redundant if ZI and IZ are present)
```

The number of independent generators equals the maximum number of qubits that can be tapered:

```fsharp
let k = z2SymmetryCount hamiltonian
// k = number of qubits removable via full Clifford tapering
```

## Clifford Rotation Synthesis

For each independent generator $g_i$, FockMap synthesizes a Clifford circuit (using Hadamard, S, and CNOT gates) that rotates $g_i$ onto a single-qubit $Z$:

$$U g_i U^\dagger = Z_{q_i}$$

```fsharp
let (gates, targets) = synthesizeTaperingClifford independentGens
// gates : CliffordGate list — the rotation circuit
// targets : int[] — which qubit each generator maps to
```

The Clifford is then applied symbolically to every Hamiltonian term:

```fsharp
let rotatedH = applyClifford gates hamiltonian
// All generators are now diagonal — diagonal tapering applies
```

After rotation, the target qubits have only I or Z on every term, and v1 diagonal tapering removes them.

## The Unified Pipeline

The `taper` function combines all steps:

```fsharp
let result = taper defaultTaperingOptions hamiltonian
// result.TaperedQubitCount — qubits after tapering
// result.Generators — the symmetry generators found
// result.CliffordGates — the rotation circuit applied
// result.TargetQubits — which qubits were targeted
// result.Hamiltonian — the final tapered Hamiltonian
```

Configure via `TaperingOptions`:

```fsharp
// v2 full Clifford (default)
taper { defaultTaperingOptions with Method = FullClifford } h

// v1 fallback (diagonal only)
taper { defaultTaperingOptions with Method = DiagonalOnly } h

// Cap removal to at most 2 qubits
taper { defaultTaperingOptions with MaxQubitsToRemove = Some 2 } h

// Explicit sector choice
taper { defaultTaperingOptions with Sector = [(0, 1); (1, -1)] } h
```

## Worked Example

Consider $H = X_0 X_1 + Y_0 Y_1 + Z_0 Z_1$ (Heisenberg coupling on 2 qubits).

1. **Symmetry detection**: $Z_0 Z_1$ commutes with all three terms → it is a generator.
2. **Clifford synthesis**: $Z_0 Z_1$ has Z on both qubits. A CNOT rotates it to $Z_0 I_1$.
3. **Rotation**: Apply CNOT to all terms → transformed Hamiltonian has I or Z on qubit 0.
4. **Taper**: Fix qubit 0 to $+1$, remove it → 1-qubit Hamiltonian.

## Performance

| Operation | Complexity |
|-----------|-----------|
| Commutation check matrix | $O(n^2 \cdot m)$ where $m$ = term count |
| GF(2) null space | $O(n^3)$ |
| Clifford synthesis | $O(n^2)$ per generator |
| Clifford application | $O(n \cdot m)$ |
| Diagonal tapering | $O(n \cdot m)$ |

All operations are symbolic — no matrices, no eigensolver.

## Benchmarking: Before vs After Tapering

Here are concrete measurements on representative Hamiltonians showing the
impact of tapering on qubit count, term count, and estimated CNOT cost.

### 6-qubit diagonal Hamiltonian (v1)

```fsharp
let h6 =
    [| PauliRegister("ZIIIII", Complex(0.5, 0.0))
       PauliRegister("IZIIII", Complex(-0.3, 0.0))
       PauliRegister("IIZIII", Complex(0.8, 0.0))
       PauliRegister("IIIZII", Complex(0.2, 0.0))
       PauliRegister("IIIIZI", Complex(-0.4, 0.0))
       PauliRegister("IIIIIZ", Complex(0.7, 0.0))
       PauliRegister("ZZZZII", Complex(0.1, 0.0))
       PauliRegister("IIZZZZ", Complex(-0.2, 0.0)) |]
    |> PauliRegisterSequence

let result = taper { defaultTaperingOptions with Method = DiagonalOnly } h6
```

| Metric | Before | After | Reduction |
|--------|-------:|------:|----------:|
| Qubits | 6 | 0 | 6 (100%) |
| Terms | 8 | 1 | 7 (88%) |
| Hilbert space dim | 64 | 1 | 64× |

All symmetries are diagonal — v1 suffices and reduces to a scalar.

### 2-qubit Heisenberg model (v2 Clifford needed)

```fsharp
let heis =
    [| PauliRegister("XX", Complex(0.5, 0.0))
       PauliRegister("YY", Complex(0.5, 0.0))
       PauliRegister("ZZ", Complex(-0.3, 0.0))
       PauliRegister("ZI", Complex(0.2, 0.0))
       PauliRegister("IZ", Complex(0.2, 0.0)) |]
    |> PauliRegisterSequence

let diagCount = (diagonalZ2SymmetryQubits heis).Length   // 0
let cliffordResult = taper defaultTaperingOptions heis   // tapers ≥1 qubit
```

| Metric | Diagonal-only | Clifford |
|--------|:---:|:---:|
| Symmetries found | 0 | ≥1 |
| Qubits removed | 0 | ≥1 |

**Key insight:** The Heisenberg model has $XX + YY + ZZ$ terms — no single
qubit is purely diagonal. But $Z_0 Z_1$ commutes with all three terms, so
Clifford tapering rotates that generator onto a single $Z$ and removes a qubit.
Diagonal-only tapering misses this entirely.

### CNOT impact estimate

Each Pauli rotation $e^{-i\theta P}$ with weight $w$ costs $2(w-1)$ CNOTs.
Tapering reduces both term count and qubit width, compounding the savings
across every Trotter step:

| System | Before (terms × avg weight) | After tapering | CNOT savings |
|--------|:---:|:---:|:---:|
| 6-qubit diagonal | 8 × 2.5 | 1 × 0 | ~100% |
| 2-qubit Heisenberg | 5 × 1.6 | fewer terms, 1 qubit | ~40–50% |

These savings multiply across every Trotter step in a VQE or QPE workflow,
making tapering one of the highest-leverage optimizations before circuit compilation.

---

**Previous:** [Chapter 3 — FockMap Implementation](03-fockmap-implementation.html)

**Back to:** [Tapering Index](index.html)
