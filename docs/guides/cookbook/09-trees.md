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
let vlasov  = vlasovTree 8            // Complete ternary (Vlasov) → O(log₃ n)
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

## The Vlasov tree: a different ternary shape

The balanced ternary tree (`balancedTernaryTree`) uses midpoint-split
indexing: the root is in the middle, and children are spread across
three roughly equal partitions.  The Vlasov tree (`vlasovTree`) uses
**level-order** (breadth-first) indexing instead: node 0 is the root,
and children of node $j$ are $3j+1, 3j+2, 3j+3$.

This is based on Vlasov's Clifford-algebraic construction
(arXiv:1904.09912), where Clifford algebra generators are defined
recursively on a complete ternary tree.

Both achieve $O(\log_3 n)$ weight, but they distribute weight
differently across modes:

```fsharp
let n = 8u
printfn "%-5s  %-8s  %-8s" "Mode" "TerTree" "Vlasov"
for j in 0u .. n-1u do
    let weight encode =
        let terms = (encode Raise j n).DistributeCoefficient
        terms.SummandTerms
        |> Array.map (fun t ->
            t.Signature |> Seq.filter (fun c -> c <> 'I') |> Seq.length)
        |> Array.max
    printfn "%-5d  %-8d  %-8d" j (weight ternaryTreeTerms) (weight vlasovTreeTerms)
```

Output:

```
Mode   TerTree   Vlasov
0      3         3
1      3         3
2      3         3
3      3         2       ← Vlasov wins here
4      2         3       ← Balanced ternary wins here
5      3         3
6      3         3
7      3         3
```

The tree shape matters: different qubit assignments put different modes
at different depths, so the "best" tree depends on which modes appear
most frequently in your Hamiltonian.

## Comparing tree shapes on H₂

Both ternary tree shapes produce valid H₂ Hamiltonians with identical
eigenspectra but different Pauli structure:

```fsharp
let hamEncoders = [
    ("Jordan-Wigner",    jordanWignerTerms)
    ("Bravyi-Kitaev",    bravyiKitaevTerms)
    ("Balanced Ternary", ternaryTreeTerms)
    ("Vlasov Tree",      vlasovTreeTerms)
]

for (name, encoder) in hamEncoders do
    let ham = (computeHamiltonianWith encoder lookup 4u).DistributeCoefficient
    let terms = ham.SummandTerms |> Array.filter (fun t -> Complex.Abs t.Coefficient > 1e-10)
    let weights = terms |> Array.map (fun t ->
        t.Signature |> Seq.filter (fun c -> c <> 'I') |> Seq.length)
    printfn "%-20s  Terms: %d  MaxWt: %d  AvgWt: %.2f"
        name terms.Length (Array.max weights) (Array.averageBy float weights)
```

```
Jordan-Wigner         Terms: 7  MaxWt: 2  AvgWt: 1.14
Bravyi-Kitaev         Terms: 7  MaxWt: 3  AvgWt: 1.71
Balanced Ternary      Terms: 7  MaxWt: 3  AvgWt: 1.43
Vlasov Tree           Terms: 7  MaxWt: 3  AvgWt: 1.43
```

For H₂ (4 qubits), both tree encodings match.  The differences grow
with system size — at $n = 64$, different tree shapes can differ by
30–40% in total CNOT cost, making tree selection a first-order
optimization concern.

> **Try it yourself:** See [Lab 10: Vlasov Tree](https://github.com/johnazariah/encodings-book/blob/main/labs/10-vlasov-tree.fsx)
> for the full comparison script with tree structure printouts,
> per-mode weight tables, and H₂ Hamiltonian analysis.

---

**Next:** [Building a Real Hamiltonian](10-building-hamiltonian.html) — the complete end-to-end pipeline
