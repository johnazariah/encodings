# FockMap Architecture Guide

FockMap provides two distinct frameworks for mapping fermionic operators to qubit Pauli strings. This guide explains why both exist, how they work, and when to use each.

## The Two-Framework Design

FockMap implements fermion-to-qubit encodings using two complementary approaches:

1. **Index-Set Framework** — Works via `EncodingScheme` and three index-set functions
2. **Path-Based Framework** — Works via `TreeNode` and ternary edge labels

Both frameworks ultimately produce the same output type: `PauliRegisterSequence`, representing a sum of Pauli strings with complex coefficients. The key difference lies in **which tree structures each framework can correctly encode**.

### Why Two Frameworks?

The fundamental reason is the **monotonicity constraint**. The index-set approach from Havlíček et al. (arXiv:1701.07072) derives Update, Parity, and Occupation sets from tree structure. However, these derivations assume a critical property: **all ancestors of node j have index greater than j**.

This property holds for Fenwick trees (used in Bravyi-Kitaev) and linear chains (Jordan-Wigner), but fails for balanced trees where a parent may have a smaller index than its children. The path-based approach from Jiang et al. (arXiv:1910.10746) and Miller et al. (Bonsai, arXiv:2212.09731) bypasses this limitation entirely.

## Index-Set Framework

The index-set framework parameterizes encodings through three functions that map a mode index to sets of qubit indices:

```fsharp
type EncodingScheme =
    { Update     : int -> int -> Set<int>   // U(j, n)
      Parity     : int -> Set<int>          // P(j)
      Occupation : int -> Set<int> }        // Occ(j)
```

### The Three Index Sets

Every fermion-to-qubit encoding in this family uses the same Majorana decomposition, differing only in how these sets are chosen:

- **Update set U(j, n)**: Qubits (besides j) that must flip when the occupation of mode j changes. These receive X operators in the Majorana strings.

- **Parity set P(j)**: Qubits whose collective parity encodes n₀ ⊕ n₁ ⊕ … ⊕ n_{j−1} (the parity of all modes before j). These receive Z operators.

- **Occupation set Occ(j)**: Qubits whose collective parity encodes whether mode j is occupied. Combined with the parity set via symmetric difference for one of the Majorana operators.

### Majorana Construction

The framework builds two Majorana operators for each fermionic mode:

```
cⱼ = X_{U(j) ∪ {j}} · Z_{P(j)}
dⱼ = Yⱼ · X_{U(j)} · Z_{(P(j) ⊕ Occ(j)) ∖ {j}}
```

The ladder operators then follow from the standard Majorana-to-fermion relation:

```
a†ⱼ = ½(cⱼ − i·dⱼ)
aⱼ  = ½(cⱼ + i·dⱼ)
```

### Concrete Schemes

Different choices of index-set functions yield different encodings:

**Jordan-Wigner:**
```fsharp
let jordanWignerScheme : EncodingScheme =
    { Update     = fun _ _ -> Set.empty
      Parity     = fun j   -> set [ 0 .. j - 1 ]
      Occupation = fun j   -> Set.singleton j }
```
The parity set grows linearly with j, yielding O(n) worst-case Pauli weight.

**Bravyi-Kitaev (via Fenwick tree):**
```fsharp
let treeEncodingScheme (tree : EncodingTree) : EncodingScheme =
    { Update     = fun j _n -> treeUpdateSet tree j
      Parity     = fun j    -> treeParitySet tree j
      Occupation = fun j    -> treeOccupationSet tree j }
```
Sets derived from Fenwick tree structure achieve O(log n) Pauli weight.

## Path-Based Framework

The path-based framework represents encodings as **ternary trees** with labeled edges. Each node has exactly three descending links labeled X, Y, and Z. Some links lead to child nodes (edges), others terminate (legs).

### Tree Structure

```fsharp
type TreeNode =
    { Index    : int              // Fermionic mode index
      Children : TreeNode list    // Child nodes
      Parent   : int option }     // Parent index or None for root
```

The link structure assigns labels to each node's three outgoing connections:

```fsharp
type LinkLabel = LX | LY | LZ

type Link =
    { Label  : LinkLabel
      Target : int option }  // Some(childIndex) or None for a leg
```

### Majorana String Construction

For each fermionic mode j, the algorithm identifies two **legs** that pair to form the even and odd Majorana operators. The Pauli string for each leg is constructed by following the path from the tree root to that leg, collecting the edge labels along the way:

1. Start at the root
2. For each edge traversed, record the Pauli corresponding to its label (X, Y, or Z)
3. At the final node, include the leg's own label
4. The collected operators form a Pauli string

This process yields O(log₃ n) Pauli weight for balanced ternary trees—the theoretically optimal scaling.

### Leg Pairing

The algorithm pairs legs into fermionic modes using the **s_x** and **s_y** mapping (Bonsai Algorithm 1):

For each node u:
- Follow the X-link, then keep taking Z-links until reaching a leg → that's s_x(u)
- Follow the Y-link, then keep taking Z-links until reaching a leg → that's s_y(u)

The two legs (s_x, s_y) correspond to the even and odd Majorana operators for mode u.

### Tree Shapes and Encodings

Different tree topologies yield different encodings:

| Tree Shape | Encoding | Pauli Weight |
|------------|----------|--------------|
| Linear chain | Jordan-Wigner | O(n) |
| Fenwick tree | Bravyi-Kitaev | O(log₂ n) |
| Balanced ternary | Optimal | O(log₃ n) |

## The Shared Output: PauliRegisterSequence

Both frameworks produce `PauliRegisterSequence` as their output—a sum of Pauli strings with complex coefficients:

```fsharp
type PauliRegisterSequence(terms : PauliRegister[]) =
    // Represents: Σₐ cₐ σₐ where σₐ is a Pauli string
```

Each `PauliRegister` is a tensor product of single-qubit Paulis with a global coefficient:

```fsharp
type PauliRegister(operators : Pauli[], phase : Complex) =
    // Represents: phase · (P₀ ⊗ P₁ ⊗ ... ⊗ Pₙ₋₁)
```

This unified output type means encoded operators can be multiplied, added, and combined regardless of which framework produced them.

## Data Flow: From Operators to Hamiltonian

The complete pipeline from fermionic operators to a qubit Hamiltonian follows these steps:

```
┌─────────────────────┐
│  LadderOperatorUnit │  ← Raise (a†) or Lower (a) operator
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ Normal Ordering     │  ← Put a† operators before a operators
│ (FermionicAlgebra)  │    Generate terms from anti-commutation
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ Index Assignment    │  ← Associate each operator with mode j
│ IxOp<uint32, Unit>  │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ Encoding            │  ← Choose framework:
│ ┌─────────────────┐ │    • EncodingScheme (index-set)
│ │ EncodingScheme  │ │    • TreeNode (path-based)
│ │       or        │ │
│ │    TreeNode     │ │
│ └─────────────────┘ │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ Majorana Operators  │  ← cⱼ, dⱼ as Pauli strings
│   c_j, d_j          │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ Ladder Operators    │  ← a†ⱼ = ½(cⱼ − i·dⱼ)
│ PauliRegisterSeq    │    aⱼ  = ½(cⱼ + i·dⱼ)
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ Pauli Products      │  ← Multiply sequences for terms like a†ᵢaⱼ
│ DistributeCoeff     │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ Hamiltonian         │  ← Sum over all terms with integral coefficients
│ H = Σ hᵢⱼ a†ᵢaⱼ + … │    Combine like terms
└─────────────────────┘
```

## Choosing a Framework

Use the **index-set framework** when:
- Working with Jordan-Wigner encoding
- Working with Bravyi-Kitaev on standard Fenwick trees
- You need precise control over the index-set functions
- Implementing custom encodings that satisfy the monotonicity constraint

Use the **path-based framework** when:
- You need optimal O(log₃ n) Pauli weight
- Working with custom tree shapes that don't satisfy monotonicity
- Implementing topologically-aware tree constructions (e.g., Bonsai)
- The specific tree structure matters for hardware connectivity

Both frameworks integrate seamlessly with the `C<'T>`, `P<'T>`, `S<'T>` algebraic types for building and manipulating Hamiltonian expressions.
