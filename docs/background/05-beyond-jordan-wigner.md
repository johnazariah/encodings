# Beyond Jordan-Wigner

The Jordan-Wigner transform is elegant and historically important, but it has a fundamental limitation: worst-case Pauli weight grows *linearly* with system size. For a creation operator $a^\dagger_j$ acting on mode $j$ in a system of $n$ modes, the encoded Pauli string contains $j$ Z operators—an $O(n)$ overhead that becomes prohibitive for large quantum simulations.

This chapter explores how we can do better. The key insight: by choosing *different ways to represent parity information*, we can reduce worst-case weight from $O(n)$ to $O(\log n)$. We'll see that Jordan-Wigner, Bravyi-Kitaev, Parity, and tree-based encodings are all instances of a single, elegant framework.

## The $O(n)$ Problem

Recall how Jordan-Wigner encodes a ladder operator:

$$
a^\dagger_j \mapsto \frac{1}{2}(X_j - iY_j) \otimes Z_{j-1} \otimes Z_{j-2} \otimes \cdots \otimes Z_0
$$

The Z-chain ensures fermionic anti-commutation: when we create a fermion at mode $j$, we need to know the parity of all modes below $j$ to get the phase right. The problem is that this chain grows linearly with $j$.

For molecular simulation on quantum computers, Hamiltonians often contain products of many ladder operators. When we multiply two encoded operators and simplify, the cost compounds. A typical molecular Hamiltonian with $n$ spin-orbitals can generate terms with $O(n^4)$ Pauli products, and if each product has $O(n)$ weight, circuit depth scales poorly.

**The question:** Can we encode fermions using only $O(\log n)$ Pauli operators per mode?

The answer is yes—and the key is to rethink how we store parity information.

## Rethinking Parity Storage

In Jordan-Wigner, qubit $k$ stores the *occupation* of mode $k$ directly:

$$
|n_0, n_1, \ldots, n_{n-1}\rangle \mapsto |n_0\rangle \otimes |n_1\rangle \otimes \cdots \otimes |n_{n-1}\rangle
$$

To recover the parity $\bigoplus_{k=0}^{j-1} n_k$, we must query all qubits below $j$—hence the Z-chain.

But what if we stored partial parities *directly* in the qubits? Then we could query parity by reading $O(\log n)$ qubits instead of $O(n)$.

This is precisely what the Bravyi-Kitaev encoding does, using a data structure from computer science: the Fenwick tree.

## Fenwick Trees and Prefix Sums

A **Fenwick tree** (also called a binary indexed tree) is a data structure that supports two operations in $O(\log n)$ time:

1. **Point update:** Increment element $j$
2. **Prefix query:** Compute $\sum_{k=0}^{j-1} a_k$

The key insight is that for fermion encoding, we don't actually need arbitrary sums—we need *parity* sums (addition mod 2). And the Fenwick tree structure tells us exactly which elements contribute to each prefix sum.

### Tree Structure

Consider a Fenwick tree for $n = 8$ elements:

```
Index (1-based):    8
                   / \
                  4   .
                 / \
                2   6
               / \ / \
              1  3 5  7
```

Each node stores a partial aggregate:
- Node 1 stores element 0
- Node 2 stores elements 0–1
- Node 3 stores element 2
- Node 4 stores elements 0–3
- Node 5 stores element 4
- Node 6 stores elements 4–5
- Node 7 stores element 6
- Node 8 stores elements 0–7

The pattern follows from bit manipulation: node $k$ (1-indexed) stores elements in the range $[k - \text{LSB}(k), k-1]$ where $\text{LSB}(k)$ is the lowest set bit of $k$.

### The Three Index Sets

For the Bravyi-Kitaev encoding, we need three sets for each mode $j$:

**Update Set $U(j, n)$:** Which qubits must flip when occupation of mode $j$ changes?

These are the *ancestors* of $j$ in the Fenwick tree—the nodes that include $j$ in their aggregated range.

```fsharp
let updateSet (j : int) (n : int) : Set<int> =
    ancestors n (j + 1)
    |> Seq.map (fun i -> i - 1)
    |> Set.ofSeq
```

**Parity Set $P(j)$:** Which qubits encode the prefix sum $n_0 \oplus n_1 \oplus \cdots \oplus n_{j-1}$?

These are the nodes whose ranges completely cover $[0, j-1]$ without overlap.

```fsharp
let paritySet (j : int) : Set<int> =
    prefixIndices j
    |> Seq.map (fun i -> i - 1)
    |> Set.ofSeq
```

**Occupation Set $\text{Occ}(j)$:** Which qubits together determine whether mode $j$ is occupied?

This includes $j$ itself plus its descendants in the Fenwick tree.

```fsharp
let occupationSet (j : int) : Set<int> =
    let k = j + 1
    descendants k
    |> Seq.map (fun i -> i - 1)
    |> Set.ofSeq
    |> Set.add j
```

### Example: 8-Mode System

For $n = 8$ modes (0-indexed), here are the index sets:

| Mode $j$ | Update $U(j)$ | Parity $P(j)$ | Occupation $\text{Occ}(j)$ |
|:--------:|:-------------:|:-------------:|:--------------------------:|
| 0 | {1, 3, 7} | {} | {0} |
| 1 | {3, 7} | {0} | {0, 1} |
| 2 | {3, 7} | {1} | {2} |
| 3 | {7} | {1, 2} | {0, 1, 2, 3} |
| 4 | {5, 7} | {3} | {4} |
| 5 | {7} | {3, 4} | {4, 5} |
| 6 | {7} | {5} | {6} |
| 7 | {} | {5, 6} | {0, 1, 2, 3, 4, 5, 6, 7} |

Notice that $|U(j)| \leq \lfloor \log_2 n \rfloor$ and $|P(j)| \leq \lfloor \log_2 n \rfloor$. This is why Bravyi-Kitaev achieves $O(\log n)$ weight.

## The Majorana Framework

Rather than treating Jordan-Wigner and Bravyi-Kitaev as separate algorithms, FockMap unifies them under a single framework based on Majorana operators.

### Majorana Operators

Every fermionic mode has two associated Majorana operators:

$$
c_j = a^\dagger_j + a_j \qquad d_j = i(a^\dagger_j - a_j)
$$

These are Hermitian and satisfy $\{c_j, c_k\} = \{d_j, d_k\} = 2\delta_{jk}$ and $\{c_j, d_k\} = 0$.

Conversely, the ladder operators are:

$$
a^\dagger_j = \frac{1}{2}(c_j - id_j) \qquad a_j = \frac{1}{2}(c_j + id_j)
$$

### Index-Set Encoding

Given the three index sets, the Majorana operators map to:

$$
c_j \mapsto X_j \cdot X_{U(j)} \cdot Z_{P(j)}
$$

$$
d_j \mapsto Y_j \cdot X_{U(j)} \cdot Z_{(P(j) \oplus \text{Occ}(j)) \setminus \{j\}}
$$

where $X_S$ means $\bigotimes_{k \in S} X_k$ (and similarly for $Z_S$), and $\oplus$ denotes symmetric difference.

The ladder operators then follow:

$$
a^\dagger_j = \frac{1}{2}c_j - \frac{i}{2}d_j \qquad a_j = \frac{1}{2}c_j + \frac{i}{2}d_j
$$

### The `EncodingScheme` Type

In FockMap, this entire framework is captured by a single type:

```fsharp
type EncodingScheme =
    { Update     : int -> int -> Set<int>  // U(j, n)
      Parity     : int -> Set<int>         // P(j)
      Occupation : int -> Set<int> }       // Occ(j)
```

An encoding is nothing more than three functions. To create a new encoding, you supply these three functions—that's it.

## Jordan-Wigner as an Encoding Scheme

Jordan-Wigner fits naturally into this framework:

```fsharp
let jordanWignerScheme : EncodingScheme =
    { Update     = fun _ _ -> Set.empty
      Parity     = fun j   -> set [ 0 .. j - 1 ]
      Occupation = fun j   -> Set.singleton j }
```

- **Update:** Empty—flipping mode $j$ only affects qubit $j$
- **Parity:** All modes $0, 1, \ldots, j-1$—hence the Z-chain
- **Occupation:** Just $\{j\}$—qubit $j$ directly encodes mode $j$

The Z-chain is $|P(j)| = j$, which grows linearly—confirming our earlier analysis.

## Bravyi-Kitaev as an Encoding Scheme

Bravyi-Kitaev uses the Fenwick tree index sets:

```fsharp
let bravyiKitaevScheme : EncodingScheme =
    { Update     = updateSet
      Parity     = paritySet
      Occupation = occupationSet }
```

The Fenwick tree structure guarantees that $|U(j)| \leq \log_2 n$ and $|P(j)| \leq \log_2 n$, giving $O(\log n)$ Pauli weight.

## The Parity Encoding

There's a third encoding that's the "dual" of Jordan-Wigner:

```fsharp
let parityScheme : EncodingScheme =
    { Update     = fun j n -> set [ j + 1 .. n - 1 ]
      Parity     = fun j   -> if j > 0 then Set.singleton (j - 1) else Set.empty
      Occupation = fun j   -> if j > 0 then set [ j - 1; j ] else Set.singleton j }
```

In the Parity encoding:
- Qubit $k$ stores the *cumulative parity* $n_0 \oplus n_1 \oplus \cdots \oplus n_k$
- Occupation of mode $j$ is determined by $q_{j-1} \oplus q_j$
- When mode $j$ flips, all qubits $j, j+1, \ldots, n-1$ must update

Where Jordan-Wigner has an $O(n)$ parity set (all modes below $j$), Parity has an $O(1)$ parity set but an $O(n)$ update set. They're mirror images—both have $O(n)$ worst-case weight, but the weight appears in different places.

## Comparing the Three Encodings

For a creation operator $a^\dagger_2$ in a 4-mode system:

**Jordan-Wigner:**
$$
a^\dagger_2 \mapsto \frac{1}{2}(ZZXI) - \frac{i}{2}(ZZYI)
$$

Weight = 3 (two Zs plus X/Y)

**Bravyi-Kitaev:**
$$
a^\dagger_2 \mapsto \frac{1}{2}(IXXI) - \frac{i}{2}(XZYI)
$$

Weight = 2–3 (varies by term)

**Parity:**
$$
a^\dagger_2 \mapsto \frac{1}{2}(IXXX) - \frac{i}{2}(ZYYX)
$$

Weight = 3–4 (update set includes modes 3)

## Tree-Based Encodings

The Fenwick tree is just one choice of tree structure. In fact, *any* rooted tree on $n$ nodes defines an encoding!

### The Tree-Encoding Map

Given a rooted tree $T$ with nodes $0, 1, \ldots, n-1$:

1. **Assign a fermionic mode to each node** (the node index)
2. **Label each descending link** with X, Y, or Z
3. **Pair the "legs"** (terminal links) into Majorana operators
4. **Read off Pauli strings** by following root-to-leg paths

This is the approach of Jiang et al. (arXiv:1910.10746) and the Bonsai algorithm (arXiv:2212.09731).

### Why Trees Work

The tree structure encodes locality. In a linear chain (the tree underlying Jordan-Wigner), mode $j$'s ancestors are all modes $0, 1, \ldots, j-1$—hence the long Z-chain.

In a *balanced* tree, every path from root to leaf has length $O(\log n)$. Since Pauli weight equals path length, balanced trees achieve $O(\log n)$ weight.

### The Path-Based Construction

The formal construction works as follows. Each node in a ternary tree has three descending links, labeled X, Y, and Z. If a link connects to a child node, it's an *edge*. If a link terminates without a child, it's a *leg*.

For $n$ fermionic modes, we need $2n$ Majorana operators (two per mode: $c_j$ and $d_j$). A tree on $n$ nodes has exactly $2n$ legs (each node contributes 3 links, and $n-1$ are consumed by edges connecting nodes, leaving $3n - 2(n-1) = n + 2$ ... actually the exact count depends on tree structure).

The key algorithm:

1. **For each node $j$**, find two designated legs: $s_x(j)$ and $s_y(j)$
2. **Follow the X-link from node $j$**, then keep taking Z-links until you hit a leg → that's $s_x(j)$
3. **Follow the Y-link from node $j$**, then keep taking Z-links until you hit a leg → that's $s_y(j)$
4. **The Majorana string** for leg $\ell$ is computed by walking from the root to leg $\ell$'s node, collecting the Pauli label (X, Y, or Z) at each step

This gives us:

$$
c_j \mapsto S_{s_x(j)} \qquad d_j \mapsto S_{s_y(j)}
$$

where $S_\ell$ is the Pauli string for leg $\ell$.

### Worked Example: 4-Mode Binary Tree

Consider a balanced binary tree on 4 modes:

```
        1
       / \
      0   2
           \
            3
```

The root is node 1. Node 0 is the left child; node 2 is the right child; node 3 is the right child of node 2.

For mode 0:
- Walk from root (1) to node 0: take the left (X) edge
- Majorana $c_0$: Follow X from node 0 → leg (no child) → collect path: X at node 1, X at node 0 → $X_1 X_0$
- Majorana $d_0$: Follow Y from node 0 → leg → $X_1 Y_0$

The resulting Pauli weight is 2, compared to Jordan-Wigner's weight of 1 for mode 0 (no Z-chain needed).

For mode 3 (the deepest node):
- Path from root: 1 → 2 (Z-edge) → 3 (Z-edge)
- But actually the labeling depends on ordering; let's say 1 → 2 uses Y, 2 → 3 uses X
- Majorana strings follow the path with appropriate labels

The key point: path length from root to any node is $O(\log n)$ in a balanced tree.

### Ternary Trees and Optimal Weight

A **ternary tree** is optimal for fermion encoding: each node has up to 3 children, labeled X, Y, Z. A balanced ternary tree achieves:

$$
\text{Pauli weight} = O(\log_3 n)
$$

This is provably optimal—you cannot do better than $\log_3 n$ for general fermionic operators.

```fsharp
// Build a balanced ternary tree and encode
let ternaryTreeTerms (op : LadderOperatorUnit) (j : uint32) (n : uint32) =
    let tree = balancedTernaryTree (int n)
    encodeWithTernaryTree tree op j n
```

### Building Custom Trees

FockMap provides tree constructors for common cases:

```fsharp
// Linear chain → Jordan-Wigner equivalent
let jwTree = linearTree n

// Balanced binary tree → O(log₂ n) weight
let binTree = balancedBinaryTree n

// Balanced ternary tree → O(log₃ n) weight (optimal)
let terTree = balancedTernaryTree n
```

You can also construct arbitrary trees by defining nodes and parent relationships:

```fsharp
type TreeNode =
    { Index    : int
      Children : TreeNode list
      Parent   : int option }
```

## Weight Comparison Table

For an $n$-mode system, worst-case Pauli weight per ladder operator:

| Encoding | Weight | Notes |
|----------|:------:|-------|
| Jordan-Wigner | $O(n)$ | Simple, local operators are cheap |
| Parity | $O(n)$ | Dual of JW, global updates |
| Bravyi-Kitaev | $O(\log_2 n)$ | Fenwick tree structure |
| Balanced Binary Tree | $O(\log_2 n)$ | Same asymptotic as BK |
| Balanced Ternary Tree | $O(\log_3 n)$ | **Optimal** |

### Why Does Weight Matter?

Lower Pauli weight translates directly to:

1. **Shallower circuits:** Fewer Pauli terms → fewer quantum gates
2. **Less noise:** Shorter circuits accumulate less error
3. **Faster classical simulation:** Smaller Pauli strings multiply faster

For near-term quantum computers (NISQ devices), keeping circuit depth low is critical. The jump from $O(n)$ to $O(\log n)$ can make the difference between a runnable and unrunnable simulation.

### Concrete Numbers

To see the practical impact, consider encoding a molecular Hamiltonian with $n$ spin-orbitals:

| $n$ | JW worst-case | BK worst-case | Ternary worst-case |
|:---:|:-------------:|:-------------:|:------------------:|
| 8 | 8 | 3 | 2 |
| 16 | 16 | 4 | 3 |
| 32 | 32 | 5 | 4 |
| 64 | 64 | 6 | 4 |
| 128 | 128 | 7 | 5 |
| 256 | 256 | 8 | 5 |

At 256 qubits:
- Jordan-Wigner might place up to 256 Pauli operators on a single term
- Bravyi-Kitaev needs at most 8
- A balanced ternary tree needs at most 5

This is a **50× reduction** in worst-case weight!

### Average-Case Considerations

The worst-case analysis tells part of the story, but *average-case* behavior matters too. In many molecular systems:

- **Local excitations** (modes $j$ and $j+1$) dominate near the Fermi level
- **Long-range terms** ($i$ and $j$ far apart) have smaller coefficients

Jordan-Wigner actually has an advantage for local operators: $a^\dagger_0$ has weight 1, not $\log n$. If your Hamiltonian is dominated by nearest-neighbor terms, JW's average weight may beat BK's.

However, for **non-local Hamiltonians** (e.g., chemistry with many electron-electron interactions), the $O(\log n)$ scaling wins decisively.

### Choosing the Right Encoding

| Use Case | Recommended Encoding |
|----------|---------------------|
| Small systems ($n < 20$) | Jordan-Wigner (simplicity) |
| Nearest-neighbor interactions | Jordan-Wigner (locality) |
| General chemistry | Bravyi-Kitaev (balanced) |
| Large systems ($n > 50$) | Ternary tree (optimal) |
| Custom locality structure | Custom tree |

## The Unified View

The power of FockMap's design is that all these encodings are instances of the *same type*:

```fsharp
// All three are EncodingScheme values
jordanWignerScheme : EncodingScheme
bravyiKitaevScheme : EncodingScheme
parityScheme       : EncodingScheme

// And they all use the same encoding function
let encode (scheme : EncodingScheme) (op : LadderOperatorUnit) (j : uint32) (n : uint32) =
    encodeOperator scheme op j n
```

This uniformity means:
- **Switching encodings is trivial:** Change one line of code
- **Comparing encodings is easy:** Same types, same interface
- **New encodings integrate seamlessly:** Implement three functions, done

## Summary

We've seen how the Jordan-Wigner encoding's $O(n)$ weight limitation can be overcome:

1. **Fenwick trees** reorganize parity information for $O(\log n)$ access
2. **Three index sets** (Update, Parity, Occupation) characterize any encoding
3. **The `EncodingScheme` type** unifies JW, BK, and Parity as instances of one pattern
4. **Tree-based encodings** generalize further: every tree defines an encoding
5. **Balanced ternary trees** achieve the optimal $O(\log_3 n)$ weight

The next step is implementing these encodings for your own systems. See the [labs](../labs/) for hands-on examples, or explore the [API reference](../reference/) for all available functions.

---

**Next:** [Bosonic Operators](06-bosonic-preview.html) — Preview of boson encodings in FockMap v0.2
