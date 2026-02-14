# Pauli Algebra

This page introduces the Pauli matrices—the fundamental building blocks for representing quantum operators on qubits. Understanding Pauli algebra is essential because fermion-to-qubit encodings ultimately express fermionic operators as sums of Pauli strings.

## The Four Pauli Matrices

Every single-qubit operator can be written as a linear combination of four 2×2 matrices: the identity $I$ and the three Pauli matrices $X$, $Y$, $Z$.

### Matrix Representations

$$
I = \begin{pmatrix} 1 & 0 \\ 0 & 1 \end{pmatrix}, \quad
X = \begin{pmatrix} 0 & 1 \\ 1 & 0 \end{pmatrix}, \quad
Y = \begin{pmatrix} 0 & -i \\ i & 0 \end{pmatrix}, \quad
Z = \begin{pmatrix} 1 & 0 \\ 0 & -1 \end{pmatrix}
$$

Each matrix has a physical interpretation:

- **$I$ (Identity)**: Does nothing—leaves the qubit unchanged.
- **$X$ (Bit-flip)**: Swaps $|0\rangle \leftrightarrow |1\rangle$, analogous to a classical NOT gate.
- **$Y$ (Bit and phase flip)**: Combines a bit-flip with a phase flip, exchanging $|0\rangle \leftrightarrow i|1\rangle$ and $|1\rangle \leftrightarrow -i|0\rangle$.
- **$Z$ (Phase-flip)**: Leaves $|0\rangle$ unchanged but maps $|1\rangle \mapsto -|1\rangle$.

All three non-identity Paulis are Hermitian ($P = P^\dagger$) and unitary ($PP^\dagger = I$). Since they equal their own inverse, each Pauli squares to the identity:

$$
X^2 = Y^2 = Z^2 = I
$$

## The Pauli Multiplication Table

When you multiply two Pauli matrices, the result is always another Pauli matrix times a phase factor from $\{+1, -1, +i, -i\}$. The multiplication follows a cyclic pattern:

$$
XY = iZ, \quad YZ = iX, \quad ZX = iY
$$

And in reverse order, the sign flips:

$$
YX = -iZ, \quad ZY = -iX, \quad XZ = -iY
$$

This antisymmetry reflects the fundamental property that distinct non-identity Paulis **anti-commute**:

$$
XY = -YX, \quad YZ = -ZY, \quad ZX = -XZ
$$

The full multiplication table is:

| × | I | X | Y | Z |
|:-:|:-:|:-:|:-:|:-:|
| **I** | I | X | Y | Z |
| **X** | X | I | iZ | −iY |
| **Y** | Y | −iZ | I | iX |
| **Z** | Z | iY | −iX | I |

This table encodes everything needed to multiply arbitrary Pauli operators. Notice the diagonal (products like $XX$, $YY$, $ZZ$) always yields $I$, while off-diagonal products yield the third Pauli with a phase of $\pm i$.

## Pauli Strings as Tensor Products

A **Pauli string** (also called a Pauli word or Pauli monomial) is a tensor product of single-qubit Paulis acting on an $n$-qubit system:

$$
P = P_0 \otimes P_1 \otimes \cdots \otimes P_{n-1}
$$

where each $P_j \in \{I, X, Y, Z\}$.

For example, on a 4-qubit system:
- $XIZI$ means: apply $X$ to qubit 0, $I$ to qubit 1, $Z$ to qubit 2, $I$ to qubit 3.
- $ZZZZ$ applies $Z$ to every qubit.

Pauli strings form a basis for the space of all $n$-qubit operators. Any $2^n \times 2^n$ Hermitian matrix can be written as a real linear combination of the $4^n$ Pauli strings.

### Multiplying Pauli Strings

Multiplication of Pauli strings proceeds qubit-by-qubit. Each position contributes a Pauli and a phase; the overall phase is the product of all per-qubit phases.

For example, consider $(XIZ) \cdot (YZI)$ on 3 qubits:

| Qubit | Left | Right | Product | Phase |
|:-----:|:----:|:-----:|:-------:|:-----:|
| 0 | X | Y | Z | +i |
| 1 | I | Z | Z | +1 |
| 2 | Z | I | Z | +1 |

Result: $(+i)(+1)(+1) \cdot ZZZ = iZZZ$

This is an $O(n)$ operation—one multiplication per qubit—which the library exploits for efficient Pauli algebra.

## Phase Tracking: Why $\pm 1, \pm i$ Matter

When working with Pauli strings, the global phase coefficient $c \in \{+1, -1, +i, -i\}$ is crucial. Two Pauli strings that differ only in phase are **not** the same operator—they contribute differently when summed into a Hamiltonian.

Consider simulating time evolution under a Hamiltonian $H$. If the encoding produces a term $+iXYZ$ but you mistakenly drop the $i$, your simulation computes the wrong dynamics. Phases also matter when:

- **Combining terms**: Like terms (same Pauli string) must have their coefficients summed correctly.
- **Verifying anti-commutation**: Encodings preserve fermionic anti-commutation via precise phase relationships.
- **Circuit synthesis**: The phase affects how Pauli rotations translate to gate sequences.

The library tracks phases exactly using a discriminated union type called `Phase`:

```fsharp
type Phase =
    | P1    // +1
    | M1    // -1
    | Pi    // +i
    | Mi    // -i
```

Phase arithmetic is implemented symbolically (not with floating-point), eliminating numerical errors. Multiplication follows the cyclic group $\mathbb{Z}_4$:

$$
i \cdot i = -1, \quad (-1) \cdot (-1) = 1, \quad i \cdot (-i) = 1
$$

## Weight of a Pauli String

The **weight** of a Pauli string is the number of non-identity terms:

$$
\text{weight}(P_0 \otimes P_1 \otimes \cdots \otimes P_{n-1}) = |\{j : P_j \neq I\}|
$$

For example:
- $XIZI$ has weight 2 (the $X$ at position 0 and $Z$ at position 2).
- $IIII$ has weight 0 (the identity operator).
- $XYZX$ has weight 4.

### Why Weight Determines Circuit Cost

On quantum hardware, implementing a Pauli rotation $e^{-i\theta P / 2}$ requires entangling the qubits where $P$ acts non-trivially. The standard decomposition uses:

1. A ladder of CNOT gates to connect the non-identity qubit positions.
2. A single $R_z(\theta)$ rotation on one qubit.
3. The inverse CNOT ladder to disentangle.

The number of CNOT gates scales with the weight: a weight-$w$ Pauli string needs $O(w)$ CNOTs. Since CNOT gates are expensive on most hardware (higher error rates, longer gate times), minimizing Pauli weight directly reduces circuit cost.

This is why encoding choice matters. Jordan-Wigner produces Pauli strings with weight up to $O(n)$—the Z-chains grow with qubit index. Bravyi-Kitaev and tree-based encodings achieve $O(\log n)$ worst-case weight, translating to shallower circuits and less accumulated error.

## Connection to the Library's `PauliRegister` Type

The `PauliRegister` type represents a Pauli string with a complex coefficient:

```fsharp
type PauliRegister =
    // A tensor product of Paulis with coefficient
    // coefficient · (P₀ ⊗ P₁ ⊗ ... ⊗ Pₙ₋₁)
```

You can create one from a string:

```fsharp
let term = PauliRegister("XZII", Complex(0.5, 0.0))
// Represents: 0.5 · (X ⊗ Z ⊗ I ⊗ I)
```

The `Signature` property returns the string of Pauli characters (without coefficient), useful for identifying like terms. The `Coefficient` property holds the complex phase.

Multiplication is implemented via the `*` operator:

```fsharp
let a = PauliRegister("XY", Complex.One)
let b = PauliRegister("YZ", Complex.One)
let c = a * b  // Result: iZX (with appropriate coefficient)
```

The library uses this to multiply encoded operators, combine terms in Hamiltonians, and verify algebraic identities.

### `PauliRegisterSequence`: Sums of Pauli Strings

A `PauliRegisterSequence` represents a linear combination of Pauli strings:

$$
H = \sum_\alpha c_\alpha \cdot P_\alpha
$$

This is the natural output format for fermion-to-qubit encodings: a single fermionic operator typically maps to a sum of Pauli strings. When encoding a full molecular Hamiltonian, the result is a `PauliRegisterSequence` containing hundreds or thousands of terms.

The sequence automatically combines like terms—if two Pauli strings have the same signature, their coefficients are summed. This keeps the representation canonical and efficient.

## Summary

Pauli algebra provides the language for expressing qubit operators. The key points:

- Four matrices $\{I, X, Y, Z\}$ form a basis for single-qubit operators.
- Distinct Paulis anti-commute, with products yielding phases $\pm i$.
- Multi-qubit operators are tensor products (Pauli strings).
- Weight (number of non-identity terms) determines circuit cost.
- Exact phase tracking is essential for correct quantum simulation.

The `PauliRegister` and `PauliRegisterSequence` types encode this algebra efficiently, enabling the library to manipulate encoded Hamiltonians symbolically before circuit synthesis.
