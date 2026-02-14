# The Jordan-Wigner Transformation

The Jordan-Wigner transformation is the oldest and most intuitive fermion-to-qubit encoding, first introduced by Pascual Jordan and Eugene Wigner in 1928. It maps fermionic operators to qubit Pauli operators by inserting a chain of $Z$ operators to track the parity of preceding modes. This page explains how and why the transformation works.

## Majorana Fermions: A Convenient Basis

Before diving into Jordan-Wigner, it helps to introduce **Majorana operators**—a reformulation of fermionic creation and annihilation operators that simplifies many encoding schemes.

### Definition

For each fermionic mode $j$, define two Majorana operators:

$$
c_j = a^\dagger_j + a_j, \quad d_j = i(a^\dagger_j - a_j)
$$

These can be inverted to recover the original ladder operators:

$$
a_j = \frac{1}{2}(c_j + id_j), \quad a^\dagger_j = \frac{1}{2}(c_j - id_j)
$$

### Why Majoranas Simplify Encodings

Majorana operators have three properties that make them ideal for encoding:

1. **Hermitian**: $c_j^\dagger = c_j$ and $d_j^\dagger = d_j$. This means Majoranas correspond to physical observables and map naturally to Hermitian Pauli strings.

2. **Square to identity**: $c_j^2 = d_j^2 = I$. Like Pauli matrices, Majoranas are their own inverses. This algebraic similarity is not coincidental—it hints that a mapping to Paulis should exist.

3. **Simple anti-commutation**: Majoranas anti-commute with each other except when paired:
   $$
   \{c_j, c_k\} = 2\delta_{jk}, \quad \{d_j, d_k\} = 2\delta_{jk}, \quad \{c_j, d_k\} = 0
   $$
   Any two distinct Majoranas anti-commute, while each Majorana commutes only with itself.

The strategy of most encodings is to map each Majorana to a Pauli string, then use the decomposition $a_j = \frac{1}{2}(c_j + id_j)$ to encode the ladder operators. The challenge is ensuring the Pauli strings anti-commute exactly as the Majoranas do.

## The Jordan-Wigner Mapping

Jordan-Wigner assigns one qubit to each fermionic mode. The mapping for the Majorana operators is:

$$
c_j \mapsto X_j Z_{j-1} Z_{j-2} \cdots Z_1 Z_0 = X_j \prod_{k=0}^{j-1} Z_k
$$

$$
d_j \mapsto Y_j Z_{j-1} Z_{j-2} \cdots Z_1 Z_0 = Y_j \prod_{k=0}^{j-1} Z_k
$$

In other words, apply $X$ (or $Y$) at position $j$, and $Z$ on all preceding qubits $0, 1, \ldots, j-1$. This string of $Z$ operators is called the **Jordan-Wigner string** or **Z-chain**.

### Explicit Form for Ladder Operators

Since $a_j = \frac{1}{2}(c_j + id_j)$ and $a^\dagger_j = \frac{1}{2}(c_j - id_j)$:

$$
a_j \mapsto \frac{1}{2}\left(X_j + iY_j\right) \prod_{k=0}^{j-1} Z_k
$$

$$
a^\dagger_j \mapsto \frac{1}{2}\left(X_j - iY_j\right) \prod_{k=0}^{j-1} Z_k
$$

Using the identities $\frac{1}{2}(X + iY) = \lvert0\rangle\langle 1 \rvert$ (lowering) and $\frac{1}{2}(X - iY) = \lvert1\rangle\langle 0 \rvert$ (raising), we see the local action on qubit $j$ matches the fermionic action on mode $j$—but the Z-chain provides the global context needed for anti-commutation.

## Why the Z-Chain Works: Tracking Parity

The Z-chain is the heart of Jordan-Wigner. To understand why it appears, consider what fermion anti-commutation demands.

### The Fermion Sign Problem

The canonical anti-commutation relation $\{a_i, a^\dagger_j\} = \delta_{ij}$ implies:

$$
a_i a^\dagger_j = -a^\dagger_j a_i \quad \text{for } i \neq j
$$

When operator $a_i$ "passes through" operator $a^\dagger_j$, a minus sign appears. This sign depends on the ordering of modes—it's a global, nonlocal effect.

### Qubits Don't Naturally Track This

Qubits are local objects. The operator $X_i$ (acting on qubit $i$) commutes with $X_j$ (acting on qubit $j$) when $i \neq j$:

$$
X_i X_j = X_j X_i
$$

No minus sign appears. To encode fermions on qubits, we must artificially inject these signs.

### Z Operators Provide the Sign

The Z operator eigenvalues are $\pm 1$, corresponding to $\lvert0\rangle$ and $\lvert1\rangle$. When a Z operator encounters an occupied qubit ($\lvert1\rangle$), it contributes a factor of $-1$.

The Z-chain $Z_{j-1} \cdots Z_0$ computes the **parity** of modes $0$ through $j-1$:

$$
\prod_{k=0}^{j-1} Z_k |n_0, n_1, \ldots\rangle = (-1)^{n_0 + n_1 + \cdots + n_{j-1}} |n_0, n_1, \ldots\rangle
$$

This parity determines whether the fermionic minus sign appears when mode $j$ exchanges order with the occupied modes below it. The Z-chain injects exactly the right sign.

## Verifying Anti-Commutation: A Small Example

Let's verify that Jordan-Wigner preserves anti-commutation for a 3-mode system. Consider the Majorana operators $c_0$, $c_1$, and $c_2$:

$$
c_0 \mapsto X_0, \quad c_1 \mapsto X_1 Z_0, \quad c_2 \mapsto X_2 Z_1 Z_0
$$

### Check: $\{c_0, c_1\} = 0$

Compute $c_0 c_1 + c_1 c_0$ using the Pauli images:

$$
c_0 c_1 \mapsto X_0 \cdot (X_1 Z_0) = X_0 Z_0 \cdot X_1 = (-iY_0) X_1 = -i Y_0 X_1
$$

(Here we used $X_0 Z_0 = -iY_0$ from the Pauli multiplication table.)

$$
c_1 c_0 \mapsto (X_1 Z_0) \cdot X_0 = X_1 \cdot Z_0 X_0 = X_1 \cdot (iY_0) = i X_1 Y_0
$$

Since $Y_0$ and $X_1$ act on different qubits, they commute: $Y_0 X_1 = X_1 Y_0$. Thus:

$$
c_0 c_1 + c_1 c_0 \mapsto -i Y_0 X_1 + i X_1 Y_0 = -i X_1 Y_0 + i X_1 Y_0 = 0 \quad ✓
$$

### Check: $c_0^2 = I$

$$
c_0^2 \mapsto X_0 \cdot X_0 = I \quad ✓
$$

### Check: $\{c_1, c_2\} = 0$

$$
c_1 c_2 \mapsto (X_1 Z_0)(X_2 Z_1 Z_0) = X_1 Z_0 Z_0 Z_1 X_2 = X_1 Z_1 X_2
$$

(Since $Z_0 Z_0 = I$.)

Now use $X_1 Z_1 = -iY_1$:

$$
c_1 c_2 \mapsto -iY_1 X_2
$$

$$
c_2 c_1 \mapsto (X_2 Z_1 Z_0)(X_1 Z_0) = X_2 Z_1 X_1 = X_2 (-iY_1) = -iX_2 Y_1
$$

Since operators on different qubits commute:

$$
c_1 c_2 + c_2 c_1 \mapsto -iY_1 X_2 - iX_2 Y_1 = -iY_1 X_2 - iY_1 X_2 = -2iY_1 X_2
$$

Wait—this isn't zero! Let's recalculate more carefully.

Actually, for $c_2 c_1$:

$$
c_2 c_1 = (X_2 Z_1 Z_0)(X_1 Z_0) = X_2 Z_1 Z_0 X_1 Z_0 = X_2 Z_1 X_1 (Z_0 Z_0) = X_2 Z_1 X_1
$$

Now, $Z_1 X_1 = iY_1$, so:

$$
c_2 c_1 \mapsto X_2 \cdot iY_1 = i X_2 Y_1 = i Y_1 X_2
$$

And:

$$
c_1 c_2 + c_2 c_1 \mapsto -iY_1 X_2 + iY_1 X_2 = 0 \quad ✓
$$

The anti-commutation relation holds. The key was careful attention to which Pauli operators share the same qubit index.

## The Cost: $O(n)$ Weight

The Jordan-Wigner transformation has a significant drawback: the Z-chain grows with the mode index.

For mode $j$ in an $n$-mode system:
- $c_j$ maps to a Pauli string with $j+1$ non-identity terms: one $X$ and $j$ copies of $Z$.
- The weight is $O(j)$, and for the highest modes, this is $O(n)$.

### What Does This Mean for Circuits?

Recall that implementing a Pauli rotation $e^{-i\theta P}$ requires $O(\text{weight}(P))$ CNOT gates. With Jordan-Wigner:

- A hopping term $a^\dagger_i a_j + a^\dagger_j a_i$ produces Pauli strings with weight $O(\lvert i - j\rvert)$.
- For molecular Hamiltonians, terms coupling distant orbitals can have weight approaching $n$.
- The total CNOT count for simulating a single Trotter step scales as $O(n \cdot M)$, where $M$ is the number of Hamiltonian terms.

On near-term quantum devices, where each CNOT adds noise, this overhead is substantial. The $O(\log n)$ encodings (Bravyi-Kitaev, tree encodings) were developed specifically to address this limitation.

### When Jordan-Wigner Is Still Preferred

Despite the $O(n)$ weight, Jordan-Wigner remains popular because:

1. **Simplicity**: The transformation is easy to derive, implement, and debug.
2. **Local terms remain local**: The most important terms in chemistry (on-site energies, nearest-neighbor hopping) often involve consecutive modes, where the Z-chain is short.
3. **Compatibility with VQE**: Variational algorithms sometimes benefit from the structure Jordan-Wigner imposes.

## Common Fermionic Terms Under Jordan-Wigner

Understanding how standard Hamiltonian terms transform helps build intuition for the encoding's behavior.

### Number Operators

The number operator $n_j = a^\dagger_j a_j$ counts the occupation of mode $j$. Under Jordan-Wigner:

$$
a^\dagger_j a_j = \frac{1}{4}(c_j - id_j)(c_j + id_j) = \frac{1}{4}(c_j^2 + id_jc_j - ic_jd_j + d_j^2)
$$

Using $c_j^2 = d_j^2 = I$ and simplifying:

$$
n_j \mapsto \frac{1}{2}(I - Z_j)
$$

The Z-chain cancels completely! Number operators transform to single-qubit $Z$ terms—weight 1, regardless of mode index. This makes sense physically: occupation is a local property of mode $j$.

### Hopping Terms

A hopping term $a^\dagger_i a_j + a^\dagger_j a_i$ (with $i < j$) moves an electron between modes $i$ and $j$. The Jordan-Wigner transformation yields:

$$
a^\dagger_i a_j + a^\dagger_j a_i \mapsto \frac{1}{2}\left(X_i X_j + Y_i Y_j\right) \prod_{k=i+1}^{j-1} Z_k
$$

The weight is $j - i + 1$: the $X$ or $Y$ operators at positions $i$ and $j$, plus a Z-chain connecting them. For nearest-neighbor hopping ($j = i + 1$), the weight is just 2—no Z operators appear. For hopping across the entire system ($j = n-1$, $i = 0$), the weight is $n$.

This pattern explains why Jordan-Wigner is efficient for one-dimensional systems with local interactions but expensive for all-to-all connectivity.

### Coulomb Repulsion Terms

Two-body terms like $a^\dagger_i a^\dagger_j a_k a_l$ (Coulomb repulsion) produce more complex Pauli expressions. The general formula involves products of four Majorana operators, yielding multiple Pauli strings with varying weights.

A simplification occurs when indices coincide. For example, the density-density interaction $n_i n_j = a^\dagger_i a_i a^\dagger_j a_j$ becomes:

$$
n_i n_j \mapsto \frac{1}{4}(I - Z_i)(I - Z_j) = \frac{1}{4}(I - Z_i - Z_j + Z_i Z_j)
$$

This is a sum of weight-0, weight-1, and weight-2 terms—still efficient regardless of how far apart $i$ and $j$ are, because the Z-chains again cancel.

### Encoding Examples Table

Here are explicit encodings for a 4-mode system ($n = 4$):

| Operator | Jordan-Wigner Encoding | Weight |
|----------|------------------------|:------:|
| $a^\dagger_0$ | $\frac{1}{2}(X - iY) \otimes I \otimes I \otimes I$ | 1 |
| $a^\dagger_1$ | $Z \otimes \frac{1}{2}(X - iY) \otimes I \otimes I$ | 2 |
| $a^\dagger_2$ | $Z \otimes Z \otimes \frac{1}{2}(X - iY) \otimes I$ | 3 |
| $a^\dagger_3$ | $Z \otimes Z \otimes Z \otimes \frac{1}{2}(X - iY)$ | 4 |
| $n_0 = a^\dagger_0 a_0$ | $\frac{1}{2}(I - Z) \otimes I \otimes I \otimes I$ | 1 |
| $n_2 = a^\dagger_2 a_2$ | $I \otimes I \otimes \frac{1}{2}(I - Z) \otimes I$ | 1 |

The table illustrates the linear growth of weight with mode index for creation/annihilation operators, contrasted with the constant weight-1 for number operators.

## The Library's Implementation

The `jordanWignerTerms` function encodes a single ladder operator:

```fsharp
/// Compute the Jordan-Wigner encoding of a single ladder operator.
let jordanWignerTerms (op : LadderOperatorUnit) (j : uint32) (n : uint32) : PauliRegisterSequence
```

It returns a `PauliRegisterSequence` containing two terms (the $X$ and $Y$ components with coefficients $\frac{1}{2}$ and $\pm\frac{i}{2}$):

```fsharp
let encoded = jordanWignerTerms Raise 2u 4u
// Returns: ½(ZZXI) − ½i(ZZYI)
```

The implementation constructs the Z-chain as a string of 'Z' characters, places 'X' or 'Y' at position $j$, and fills the remainder with 'I'. This string-based approach makes the encoding transparent and easy to verify.

For encoding full Hamiltonians, use `computeHamiltonian` with the Jordan-Wigner encoder:

```fsharp
let h1 = (* one-body integrals *)
let h2 = (* two-body integrals *)
let pauliHamiltonian = computeHamiltonian jordanWignerTerms h1 h2
```

## Historical Note

The Jordan-Wigner transformation first appeared in:

> P. Jordan and E. Wigner, "Über das Paulische Äquivalenzverbot,"
> *Zeitschrift für Physik* **47**, 631–651 (1928).

The original paper addressed the equivalence between certain one-dimensional spin models and fermionic systems—a duality that has since become foundational in condensed matter physics. The modern application to quantum computing, where the transformation serves as an encoding rather than a duality, is a reinterpretation of the same mathematical structure.

The title translates roughly to "On the Pauli Exclusion Principle"—Jordan and Wigner were exploring how the exclusion principle (fermions cannot share a state) could be reformulated in terms of spin operators. Their insight was that the anti-commutation relations could be enforced by a string of sign-tracking operators, precisely what we now call the Jordan-Wigner string.

## Summary

The Jordan-Wigner transformation:

1. **Introduces Majorana operators** $c_j$, $d_j$ as Hermitian building blocks.
2. **Maps Majoranas to Pauli strings** with a Z-chain on preceding qubits.
3. **Preserves anti-commutation** through careful phase tracking by the Z-chain.
4. **Has $O(n)$ worst-case weight**, leading to circuit depths that scale with system size.
5. **Remains the simplest encoding**, making it a natural starting point and often sufficient for small systems or local Hamiltonians.

The Z-chain's growing length motivates the search for better encodings. The next sections explore Bravyi-Kitaev and tree-based approaches that achieve $O(\log n)$ weight while preserving the algebraic structure.
