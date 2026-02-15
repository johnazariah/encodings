# Trees and Fenwick Trees

## Why trees?

The Z-chain in Jordan-Wigner grows linearly because it uses a **linear**
data structure. The key insight behind better encodings: use a **tree** to
share parity information, cutting depth to $O(\log n)$.

## Fenwick Trees (Binary Indexed Trees)

The Bravyi-Kitaev encoding is built on a Fenwick tree. FockMap provides
a purely functional, immutable implementation:

```fsharp
let occupations = [| 1; 0; 1; 1; 0; 1; 0; 1 |]
let tree = FenwickTree.ofArray (^^^) 0 occupations

// Prefix query: XOR of elements 0..3
FenwickTree.prefixQuery tree 3

// Point query: just element 5
FenwickTree.pointQuery tree 5

// Immutable update (returns a new tree):
let tree' = FenwickTree.update tree 2 0
```

The Fenwick structure defines the BK index sets:

```fsharp
FenwickTree.updateSet 8 3        // U(3) — which qubits to update
FenwickTree.paritySet 3          // P(3) — parity contributors
FenwickTree.occupationSet 3      // Occ(3) — occupation encoding
FenwickTree.remainderSet 3       // R(3) = P(3) \ Occ(3)
```

## Encoding trees: choose your shape

FockMap generalises beyond Fenwick trees to arbitrary tree shapes:

```fsharp
let linear  = linearTree 8            // Chain → recovers Jordan-Wigner
let binary  = balancedBinaryTree 8     // Balanced binary → O(log₂ n)
let ternary = balancedTernaryTree 8    // Balanced ternary → O(log₃ n)
```

Walk any tree:

```fsharp
let tree = balancedBinaryTree 8
treeAncestors tree 5      // path from node 5 toward root
treeDescendants tree 1    // all descendants
treeChildren tree 1       // direct children only
```

## Two frameworks for tree-based encoding

**Framework 1 — Index sets** (Fenwick-compatible trees only):

```fsharp
let scheme = treeEncodingScheme (balancedBinaryTree 8)
encodeOperator scheme Raise 2u 8u
```

**Framework 2 — Path-based ternary tree** (any ternary tree):

Constructs Pauli strings directly from root-to-leg paths using X/Y/Z
link labels. This is the approach from Jiang et al. and the Bonsai paper:

```fsharp
let tree = balancedTernaryTree 8
let links = computeLinks tree          // assign X/Y/Z labels
let legs  = allLegs links              // enumerate all legs
let pairs = pairLegs tree links        // pair legs per mode

// Full encoding in one call:
let result = encodeWithTernaryTree tree Raise 2u 8u
```

---

**Next:** [Building a Real Hamiltonian](10-building-hamiltonian.html) — the complete end-to-end pipeline
