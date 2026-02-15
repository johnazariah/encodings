# Bosonic Operators and Encodings

FockMap supports both fermionic and bosonic ladder-operator normal ordering. Fermions use canonical anti-commutation relations (CAR), while bosons use canonical commutation relations (CCR).

This page introduces the bosonic algebra, how truncation works in practice, and the three bosonic-to-qubit encodings provided by FockMap.

## Bosons vs Fermions

The key difference between bosons and fermions lies in their commutation relations:

**Fermions** (CAR):
$$
\{a_i, a^\dagger_j\} = a_i a^\dagger_j + a^\dagger_j a_i = \delta_{ij}
$$

**Bosons** (CCR):
$$
[b_i, b^\dagger_j] = b_i b^\dagger_j - b^\dagger_j b_i = \delta_{ij}
$$

For fermions, each mode holds at most one particle (occupation $n_j \in \{0, 1\}$). For bosons, there is no intrinsic upper bound, so a single mode can hold $0,1,2,\ldots$ particles.

In qubit simulation, that means bosonic modes require a truncation choice.

## Truncated Bosonic Fock Space

The standard approach is to truncate each bosonic mode at occupancy cutoff $d$:

$$
n_j \in \{0, 1, 2, \ldots, d-1\}
$$

With this truncation, each bosonic mode is finite-dimensional, with local Hilbert space size $d$. For $n$ bosonic modes, the bosonic sector has dimension $d^n$.

Common cutoffs:
- $d = 2$: aggressive truncation (hard-core limit)
- $d = 4$: Two qubits per mode
- $d = 8$: Three qubits per mode

## Normal Ordering in FockMap

Bosonic normal ordering in FockMap is exposed via `BosonicLadderOperatorSumExpression` and helper constructors:

```fsharp
open Encodings

let expr =
    S.Apply(
        P.Apply [|
            IxOp.Apply(0u, Lower)
            IxOp.Apply(0u, Raise)
        |])

let bosonic = constructBosonicNormalOrdered expr
```

The CCR rewrite rule used by `BosonicAlgebra` is:

$$
b_i b_i^\dagger = 1 + b_i^\dagger b_i
$$

For different indices, reordering does not introduce a minus sign:

$$
b_i b_j^\dagger = b_j^\dagger b_i \quad (i \neq j)
$$

This differs from the fermionic CAR case, where reordering contributes a sign flip.

## Mixed Bosonic + Fermionic Models

Many applications (for example, electron-phonon models) include both fermionic and bosonic operators. In FockMap, the practical pattern is:

1. Partition indices into bosonic and fermionic sectors.
2. Build expressions in the shared indexed representation `IxOp<uint32, LadderOperatorUnit>`.
3. Normal-order each sector with the appropriate algebra (`FermionicAlgebra` or `BosonicAlgebra`).
4. Encode fermionic terms to Pauli strings with any encoding scheme.
5. Keep bosonic terms symbolic or apply a separate bosonic truncation/encoding layer.

See the [Mixed Systems chapter](../guides/cookbook/11-mixed-systems.html) in the Library Cookbook for a full workflow.

## Truncation Encodings

FockMap provides three bosonic-to-qubit encodings. Each maps a truncated bosonic mode (d levels) into qubits, then decomposes the ladder operators into Pauli strings.

### Unary (One-Hot) Encoding

Each Fock state $|n\rangle$ is mapped to a one-hot qubit state with exactly one qubit in $|1\rangle$:

$$
|n\rangle_{\text{boson}} \mapsto |0\cdots0\,\underset{n}{1}\,0\cdots0\rangle
$$

**Qubits per mode:** $d$

The creation operator is decomposed algebraically:

$$
b^\dagger = \sum_{n=0}^{d-2} \sqrt{n+1}\, \sigma^+_{n+1}\, \sigma^-_n
$$

Each transition $|n\rangle \to |n+1\rangle$ produces four weight-2 Pauli terms:

$$
\sigma^+_{q_1}\, \sigma^-_{q_0} = \tfrac{1}{4}(X_{q_0}X_{q_1} - iX_{q_0}Y_{q_1} + iY_{q_0}X_{q_1} + Y_{q_0}Y_{q_1})
$$

**Total Pauli terms:** $4(d-1)$.  **Maximum Pauli weight:** 2.

```fsharp
let result = unaryBosonTerms Raise 0u 1u 4u   // d=4, 1 mode
// → 12 Pauli terms, all weight ≤ 2
```

### Standard Binary Encoding

Each Fock state $|n\rangle$ maps to the standard binary representation of $n$:

$$
|n\rangle_{\text{boson}} \mapsto |n_{\text{binary}}\rangle
$$

**Qubits per mode:** $\lceil \log_2 d \rceil$

The $d \times d$ operator matrix is embedded in a $2^q \times 2^q$ space and decomposed via:

$$
O = \sum_{P \in \{I,X,Y,Z\}^{\otimes q}} \frac{\mathrm{Tr}(PO)}{2^q}\, P
$$

**Maximum Pauli weight:** $\lceil \log_2 d \rceil$.

```fsharp
let result = binaryBosonTerms Raise 0u 1u 4u   // d=4, 2 qubits
```

The number operator has an especially compact binary decomposition:

$$
\hat{n} = \sum_{k=0}^{q-1} 2^k \cdot \frac{I - Z_k}{2}
$$

### Gray Code Encoding

Like binary, but maps Fock state $|n\rangle$ to the reflected Gray code $G(n) = n \oplus (n \gg 1)$:

$$
|n\rangle_{\text{boson}} \mapsto |G(n)\rangle
$$

**Qubits per mode:** $\lceil \log_2 d \rceil$ (same as binary)

Consecutive Fock states differ in exactly one qubit, so transition operators $|n\rangle\langle n{+}1|$ have lower average Pauli weight.

```fsharp
let result = grayCodeBosonTerms Raise 0u 1u 4u   // d=4, 2 qubits
```

### Comparison Table

| Encoding | Qubits / mode | Terms for $b^\dagger$ | Max weight | Best for |
|----------|:---:|:---:|:---:|----------|
| Unary | $d$ | $4(d{-}1)$ | 2 | Hardware with nearest-neighbour connectivity |
| Binary | $\lceil\log_2 d\rceil$ | $O(d)$ | $\lceil\log_2 d\rceil$ | Minimum qubit count |
| Gray code | $\lceil\log_2 d\rceil$ | $O(d)$ | $\lceil\log_2 d\rceil$ | Lower average weight than binary |

Reference: Sawaya et al., "Resource-efficient digital quantum simulation of d-level systems" (arXiv:1909.05820).

---

**Previous:** [Beyond Jordan-Wigner](05-beyond-jordan-wigner.html) — BK, trees, and O(log n) encodings

**Next:** [Mixed Systems](07-mixed-systems.html) — canonical ordering for boson+fermion models
