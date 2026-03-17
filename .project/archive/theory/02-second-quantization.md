# Second Quantization

Second quantization is the mathematical language of quantum many-body physics. Rather than tracking individual particles through a maze of antisymmetrized wavefunctions, we describe states by specifying which orbitals are occupied and use operators to create or destroy particles. This formalism is essential for quantum simulation and forms the foundation of the FockMap library.

## First Quantization: The Wavefunction Approach

In introductory quantum mechanics, we describe a single particle by a **wavefunction** $\psi(\mathbf{r})$. For a single electron in a hydrogen atom, this works elegantly—we solve the Schrödinger equation and obtain familiar orbitals: $1s$, $2s$, $2p$, etc.

**The trouble begins with multiple electrons.** Consider two electrons. Their joint wavefunction is $\Psi(\mathbf{r}_1, \mathbf{r}_2)$, where $\mathbf{r}_1$ and $\mathbf{r}_2$ are the coordinates of electrons 1 and 2. But electrons are **identical fermions**: we cannot physically distinguish electron 1 from electron 2. The wavefunction must be **antisymmetric** under particle exchange:

$$\Psi(\mathbf{r}_2, \mathbf{r}_1) = -\Psi(\mathbf{r}_1, \mathbf{r}_2)$$

For two electrons in orbitals $\phi_a$ and $\phi_b$, the properly antisymmetrized wavefunction is a **Slater determinant**:

$$\Psi(\mathbf{r}_1, \mathbf{r}_2) = \frac{1}{\sqrt{2}} \begin{vmatrix} \phi_a(\mathbf{r}_1) & \phi_b(\mathbf{r}_1) \\ \phi_a(\mathbf{r}_2) & \phi_b(\mathbf{r}_2) \end{vmatrix}$$

$$= \frac{1}{\sqrt{2}}\left[\phi_a(\mathbf{r}_1)\phi_b(\mathbf{r}_2) - \phi_b(\mathbf{r}_1)\phi_a(\mathbf{r}_2)\right]$$

### The N! Problem

For $N$ electrons, the Slater determinant is an $N \times N$ determinant with $N!$ terms. Computing expectation values involves integrals over all particle coordinates, with careful tracking of the antisymmetric structure.

Worse, realistic wavefunctions are typically **linear combinations** of many Slater determinants. A molecular wavefunction might require thousands or millions of determinants for chemical accuracy.

The bookkeeping becomes nightmarish:
- Which particles are in which orbitals?
- How do the antisymmetry signs combine?
- How do we systematically construct and manipulate these states?

Second quantization cuts through this complexity by changing perspective.

## The Conceptual Leap: Track Orbitals, Not Particles

The key insight is this: **electrons are identical, but orbitals are distinguishable.** Instead of asking "where is electron 1?", we ask "is orbital $j$ occupied?"

We fix a set of single-particle orbitals $\{\phi_0, \phi_1, \ldots, \phi_{n-1}\}$ (these might be molecular orbitals, atomic orbitals, or plane waves). Then we describe any many-body state by listing which orbitals contain electrons.

## Occupation Number Representation

An **occupation number vector** specifies the occupancy of each orbital:

$$\lvert n_0, n_1, n_2, \ldots, n_{n-1}\rangle$$

where $n_j \in \{0, 1\}$ for fermions (due to the Pauli exclusion principle: at most one fermion per orbital state).

**Examples for a 4-orbital system:**

| State | Interpretation |
|-------|----------------|
| $\|0, 0, 0, 0\rangle$ | All orbitals empty (vacuum) |
| $\|1, 0, 0, 0\rangle$ | One electron in orbital 0 |
| $\|1, 1, 0, 0\rangle$ | Electrons in orbitals 0 and 1 |
| $\|0, 1, 0, 1\rangle$ | Electrons in orbitals 1 and 3 |
| $\|1, 1, 1, 1\rangle$ | All orbitals occupied |

This is radically simpler than tracking which of $N$ labeled electrons sits where—we just list occupancies.

## Fock Space

The **Fock space** $\mathcal{F}$ is the Hilbert space spanned by all possible occupation number vectors. It's a direct sum over particle number sectors:

$$\mathcal{F} = \mathcal{F}^{(0)} \oplus \mathcal{F}^{(1)} \oplus \mathcal{F}^{(2)} \oplus \cdots$$

where $\mathcal{F}^{(N)}$ contains all states with exactly $N$ particles. The dimension of Fock space for $n$ orbitals is $2^n$ (each orbital is occupied or not).

The **vacuum state** $\lvert0\rangle \equiv \lvert0, 0, \ldots, 0\rangle$ has no particles. It's not "nothing"—it's the unique state where all orbitals are empty.

## Creation and Annihilation Operators

The power of second quantization comes from **operators** that add or remove particles from specific orbitals.

### The Creation Operator $a_j^\dagger$

The **creation operator** $a_j^\dagger$ adds a particle to orbital $j$:

$$a_j^\dagger \lvert n_0, \ldots, n_{j-1}, 0, n_{j+1}, \ldots\rangle = (-1)^{\sum_{k<j} n_k} \lvert n_0, \ldots, n_{j-1}, 1, n_{j+1}, \ldots\rangle$$

If orbital $j$ is already occupied, the result is zero (Pauli exclusion):

$$a_j^\dagger \lvert n_0, \ldots, n_{j-1}, 1, n_{j+1}, \ldots\rangle = 0$$

The factor $(-1)^{\sum_{k<j} n_k}$ is the **fermionic sign**: it counts the number of occupied orbitals with index less than $j$. This sign encodes the antisymmetry under particle exchange.

### The Annihilation Operator $a_j$

The **annihilation operator** $a_j$ removes a particle from orbital $j$:

$$a_j \lvert n_0, \ldots, n_{j-1}, 1, n_{j+1}, \ldots\rangle = (-1)^{\sum_{k<j} n_k} \lvert n_0, \ldots, n_{j-1}, 0, n_{j+1}, \ldots\rangle$$

If orbital $j$ is empty, the result is zero:

$$a_j \lvert n_0, \ldots, n_{j-1}, 0, n_{j+1}, \ldots\rangle = 0$$

The annihilation operator is the Hermitian conjugate of the creation operator: $a_j = (a_j^\dagger)^\dagger$.

### Worked Sign Examples

Let's trace through concrete examples with 4 orbitals.

**Example 1: Creating in an empty system**

Start with $\lvert0, 1, 0, 0\rangle$ (one electron in orbital 1). Apply $a_2^\dagger$:

$$a_2^\dagger \lvert0, 1, 0, 0\rangle = (-1)^{n_0 + n_1} \lvert0, 1, 1, 0\rangle = (-1)^{0 + 1} \lvert0, 1, 1, 0\rangle = -\lvert0, 1, 1, 0\rangle$$

The minus sign appears because we pass over one occupied orbital (orbital 1).

**Example 2: Order matters**

Create two particles starting from vacuum:

$$a_1^\dagger a_0^\dagger \lvert0, 0, 0, 0\rangle$$

First: $a_0^\dagger \lvert0, 0, 0, 0\rangle = (-1)^0 \lvert1, 0, 0, 0\rangle = \lvert1, 0, 0, 0\rangle$

Then: $a_1^\dagger \lvert1, 0, 0, 0\rangle = (-1)^1 \lvert1, 1, 0, 0\rangle = -\lvert1, 1, 0, 0\rangle$

Now try the opposite order:

$$a_0^\dagger a_1^\dagger \lvert0, 0, 0, 0\rangle$$

First: $a_1^\dagger \lvert0, 0, 0, 0\rangle = (-1)^0 \lvert0, 1, 0, 0\rangle = \lvert0, 1, 0, 0\rangle$

Then: $a_0^\dagger \lvert0, 1, 0, 0\rangle = (-1)^0 \lvert1, 1, 0, 0\rangle = \lvert1, 1, 0, 0\rangle$

We get: $a_1^\dagger a_0^\dagger \lvert0, 0, 0, 0\rangle = -\lvert1, 1, 0, 0\rangle$ and $a_0^\dagger a_1^\dagger \lvert0, 0, 0, 0\rangle = +\lvert1, 1, 0, 0\rangle$.

Therefore: $a_1^\dagger a_0^\dagger = -a_0^\dagger a_1^\dagger$, or equivalently, $\{a_0^\dagger, a_1^\dagger\} = 0$.

## The Canonical Anticommutation Relations (CARs)

The examples above hint at a general pattern. The **canonical anticommutation relations** are:

### Relation 1: $\{a_i, a_j^\dagger\} = \delta_{ij}$

For $i = j$: Consider any state $\lvert\psi\rangle$. We have:

$$a_j a_j^\dagger \lvert\psi\rangle + a_j^\dagger a_j \lvert\psi\rangle$$

If orbital $j$ is empty in $\lvert\psi\rangle$: $a_j^\dagger a_j \lvert\psi\rangle = 0$ (can't destroy what isn't there), but $a_j a_j^\dagger \lvert\psi\rangle = a_j \lvert\text{state with } j \text{ occupied}\rangle = \lvert\psi\rangle$.

If orbital $j$ is occupied in $\lvert\psi\rangle$: $a_j a_j^\dagger \lvert\psi\rangle = 0$ (can't create where it's full), but $a_j^\dagger a_j \lvert\psi\rangle = a_j^\dagger \lvert\text{state with } j \text{ empty}\rangle = \lvert\psi\rangle$.

Either way: $(a_j a_j^\dagger + a_j^\dagger a_j)\lvert\psi\rangle = \lvert\psi\rangle$, so $\{a_j, a_j^\dagger\} = I$.

For $i \neq j$: The creation and annihilation operators act on different orbitals. The fermionic signs from passing occupied orbitals cause the two terms to cancel. Following the logic of our worked examples, $\{a_i, a_j^\dagger\} = 0$.

### Relation 2: $\{a_i, a_j\} = 0$

Annihilating twice in the same orbital gives zero (can't remove what isn't there after the first removal): $a_j a_j = 0$. For different orbitals, the sign algebra again forces anticommutation: $a_i a_j = -a_j a_i$.

### Relation 3: $\{a_i^\dagger, a_j^\dagger\} = 0$

Creating twice in the same orbital violates Pauli exclusion: $a_j^\dagger a_j^\dagger = 0$. For different orbitals: $a_i^\dagger a_j^\dagger = -a_j^\dagger a_i^\dagger$, as we verified in Example 2.

## The Number Operator

The **number operator** $\hat{n}_j$ counts particles in orbital $j$:

$$\hat{n}_j = a_j^\dagger a_j$$

It has eigenvalue 0 on states where orbital $j$ is empty, and eigenvalue 1 where it's occupied:

$$\hat{n}_j \lvert n_0, \ldots, n_j, \ldots\rangle = n_j \lvert n_0, \ldots, n_j, \ldots\rangle$$

The **total number operator** is $\hat{N} = \sum_j \hat{n}_j$.

Using the CARs, we can derive useful commutation relations:

$$[\hat{n}_j, a_k^\dagger] = \delta_{jk} a_k^\dagger$$
$$[\hat{n}_j, a_k] = -\delta_{jk} a_k$$

These show that $a_k^\dagger$ increases the particle number by 1 (in orbital $k$), while $a_k$ decreases it.

## The Hamiltonian in Second Quantization

Physical Hamiltonians take elegant forms in second quantization. For a system of interacting electrons:

$$H = \sum_{pq} h_{pq} \, a_p^\dagger a_q + \frac{1}{2}\sum_{pqrs} h_{pqrs} \, a_p^\dagger a_q^\dagger a_r a_s$$

The **one-body terms** $h_{pq} \, a_p^\dagger a_q$ represent:
- Kinetic energy
- External potentials (e.g., nuclear attraction)
- Single-particle hopping between orbitals

The coefficient $h_{pq} = \langle \phi_p \mid \hat{h} \mid \phi_q \rangle$ is a matrix element of the one-body Hamiltonian in the orbital basis.

The **two-body terms** $h_{pqrs} \, a_p^\dagger a_q^\dagger a_r a_s$ represent:
- Electron-electron Coulomb repulsion
- Exchange interactions

The coefficient $h_{pqrs}$ is a two-electron integral over the orbitals.

**The beauty of this representation**: the antisymmetry is handled automatically by the operator algebra. We don't need to explicitly antisymmetrize wavefunctions—the CARs do it for us.

## Connection to the FockMap Library

The FockMap library directly represents these concepts in F# types.

### Ladder Operators: `Raise` and `Lower`

The library represents creation and annihilation operators using discriminated union types:

```fsharp
type LadderOperator =
    | Raise   // a†
    | Lower   // a
```

An **indexed ladder operator** pairs the operator type with a mode (orbital) index:

```fsharp
type IndexedLadderOperator = { Operator: LadderOperator; Mode: int }
```

For example, $a_3^\dagger$ is represented as `{ Operator = Raise; Mode = 3 }`.

### Operator Sequences

Products of ladder operators, like $a_2^\dagger a_5^\dagger a_3 a_1$, are represented as sequences. The library tracks:

- The ordered list of operators
- Phase factors from reordering (those fermionic signs!)
- Normal ordering conventions

### Building Hamiltonians

To construct the Hamiltonian $H = \sum_{pq} h_{pq} a_p^\dagger a_q + \ldots$:

1. Create indexed ladder operators for each term
2. Combine them with coefficients
3. The library handles the algebraic simplifications

### Encoding to Qubits

The library then provides encoding transformations:

- **Jordan-Wigner**: Maps each ladder operator to Pauli strings with a $Z$-string parity correction
- **Bravyi-Kitaev**: Uses a tree structure for $O(\log n)$ weight operators

The encoded Hamiltonian is expressed as a sum of Pauli strings, ready for quantum circuit implementation.

## Summary

Second quantization replaces the cumbersome machinery of antisymmetrized wavefunctions with a clean algebraic framework:

| First Quantization | Second Quantization |
|-------------------|---------------------|
| Track $N$ particle coordinates | Track orbital occupancies |
| Slater determinants with $N!$ terms | Occupation number vectors |
| Explicit antisymmetrization | Automatic via CARs |
| Integration over all coordinates | Operator algebra |

The canonical anticommutation relations $\{a_i, a_j^\dagger\} = \delta_{ij}$ are the foundation. They encode fermionic statistics algebraically, enabling systematic construction and manipulation of many-body states and operators.

The FockMap library builds on this foundation, providing tools to construct fermionic operators, combine them into Hamiltonians, and encode them for quantum simulation. Understanding second quantization is essential for using these tools effectively.
