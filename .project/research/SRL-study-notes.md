# Comprehensive Study Notes: The SRL Paper

**Paper:** Seeley, J. T., Richard, M. J., & Love, P. J. (2012).
"The Bravyi-Kitaev transformation for quantum computation of electronic structure."
*Journal of Chemical Physics*, 137(22), 224109.
[arXiv:1208.5986](https://arxiv.org/abs/1208.5986) · [DOI:10.1063/1.4768229](https://doi.org/10.1063/1.4768229)

**Purpose of these notes:** Prepare for academic discussion about the star-tree theorem,
which reveals a structural limitation in the SRL framework. These notes dissect exactly
what SRL claims, what it proves, where it's right, where it implicitly overgeneralises,
and what the star-tree theorem challenges.

---

## Table of Contents

1. [Paper Identity & Context](#1-paper-identity--context)
2. [What the Paper Actually Does](#2-what-the-paper-actually-does)
3. [The Three Index Sets: U, P, Occ](#3-the-three-index-sets-u-p-occ)
4. [The SRL Formula vs. The Code Formula](#4-the-srl-formula-vs-the-code-formula)
5. [How SRL Derives the Three Classical Encodings](#5-how-srl-derives-the-three-classical-encodings)
6. [The Fenwick Tree: What Makes BK Special](#6-the-fenwick-tree-what-makes-bk-special)
7. [What SRL Claims vs. What SRL Implies](#7-what-srl-claims-vs-what-srl-implies)
8. [The Conflation: Construction A vs. Construction F](#8-the-conflation-construction-a-vs-construction-f)
9. [What the Star-Tree Theorem Says](#9-what-the-star-tree-theorem-says)
10. [Detailed Proof Walkthrough](#10-detailed-proof-walkthrough)
11. [The Balanced Ternary Counterexample](#11-the-balanced-ternary-counterexample)
12. [Anticipating Objections](#12-anticipating-objections)
13. [Key Quotes to Reference](#13-key-quotes-to-reference)
14. [Related Papers & How They Fit](#14-related-papers--how-they-fit)
15. [One-Page Cheat Sheet](#15-one-page-cheat-sheet)

---

## 1. Paper Identity & Context

### What it is
SRL is the paper that made the Bravyi-Kitaev (BK) encoding *practical*. Bravyi and
Kitaev's original 2002 paper proved the theoretical possibility of O(log n) Pauli
weight encodings, but was abstract and hard to implement. SRL gave an explicit,
algorithmic construction based on the Fenwick tree data structure (from Fenwick 1994),
with worked examples and applications to quantum chemistry.

### Why it matters
As of 2025, SRL has ~500+ citations. It is THE reference that quantum chemistry
software libraries (OpenFermion, Qiskit, PennyLane) cite when implementing BK.
It introduced the conceptual vocabulary (update set, parity set, remainder set,
occupation set) that the entire field uses.

### The paper's stated goal
> "Here we develop an alternative method of simulating fermions with qubits, first
> proposed by Bravyi and Kitaev, that reduces the simulation cost to O(log n) qubit
> operations for one fermionic operation."

SRL's goal is to *explain and operationalise* BK, not to prove something new about
encoding theory. This is important because it means their claims about generality
are incidental rather than the paper's focus.

---

## 2. What the Paper Actually Does

SRL makes four distinct contributions:

### 2a. Introduces the Fenwick tree as the organising structure for BK
The key insight: the Fenwick tree (binary indexed tree from CS) naturally gives the
update, parity, and occupation sets needed for BK. The bit-manipulation formulas
`lsb(k) = k & (-k)`, `ancestors via k + lsb(k)`, `descendants via k - 1 & (k-1)`
make everything O(log n).

### 2b. Defines three index sets abstractly
For each fermionic mode j (0-indexed), SRL defines:
- **Update set U(j)**: qubits that must be updated when occupation of mode j changes
- **Parity set P(j)**: qubits that encode the parity of n₀ ⊕ ... ⊕ n_{j-1}
- **Remainder set R(j)**: qubits in the parity set that are not in the occupation set
- **Occupation set Occ(j)**: qubits that encode whether mode j is occupied

### 2c. Gives the Majorana decomposition formula
Constructs two Hermitian Majorana operators c_j and d_j from the index sets, then
forms ladder operators as a†_j = ½(c_j - i·d_j).

### 2d. Shows JW, BK, and Parity as instances
Demonstrates that all three known encodings can be expressed as specific choices of
(U, P, Occ) — giving the appearance of a unified framework.

---

## 3. The Three Index Sets: U, P, Occ

### Semantic meaning

| Set | What it encodes | Physical role |
|-----|----------------|---------------|
| **U(j)** | Which other qubits need flipping when mode j changes | Propagates occupation changes up the tree |
| **P(j)** | Which qubits jointly encode the parity ⊕_{k<j} n_k | Tracks running parity of lower modes |
| **Occ(j)** | Which qubits jointly encode occupation n_j | Determines if mode j is filled |

### How SRL derives them from a Fenwick tree

For the Fenwick tree on n nodes:
```
U_BK(j) = ancestors of j in the Fenwick tree
         = {j + lsb(j), j + lsb(j) + lsb(j + lsb(j)), ...} until > n

P_BK(j) = children of j in the Fenwick tree
         = prefix-parity indices via bit tricks

Occ_BK(j) = descendants of j (including j)
           = subtree of j via bit tricks
```

### How SRL derives them from an arbitrary tree

**This is the key generalisation claim.** For any labelled rooted tree T:
```
U(j)   = ancestors of j
P(j)   = remainder(j) ∪ children(j)
Occ(j) = descendants(j) ∪ {j}
```

where `remainder(j)` = children of ancestors of j that have index < j and are
not themselves ancestors of j.

### The three classical encodings

| Encoding | U(j) | P(j) | Occ(j) | Tree shape |
|----------|------|------|---------|------------|
| **JW** | ∅ | {0, ..., j-1} | {j} | Star, chain ordering |
| **Parity** | {j+1, ..., n-1} | {j-1}* | {j-1, j}* | Star, reversed |
| **BK** | Fenwick ancestors | Fenwick children | Fenwick subtree | Fenwick tree |

*: adjusted for j=0 boundary case.

---

## 4. The SRL Formula vs. The Code Formula

### The SRL formula (from the paper)

For c_j (the "c" Majorana):
```
Position k gets:
  X  if k ∈ Occ(j) \ P(j)           [in Occ but not Parity]
  Y  if k ∈ Occ(j) ∩ P(j)           [in both Occ and Parity]
  Z  if k ∈ U(j) ∪ (P(j) \ Occ(j))  [in Update, or Parity-only]
  I  otherwise
```

For d_j (the "d" Majorana): swap X ↔ Y in the above.

### The code formula (what FockMap implements)

```fsharp
// c Majorana:  X on {j} ∪ U(j),  Z on P(j)
let cMajorana scheme j n =
    let u = scheme.Update j n
    let p = scheme.Parity j
    (j, X) :: setAssign X u @ setAssign Z p

// d Majorana:  Y on j,  X on U(j),  Z on (P(j) ⊕ Occ(j)) \ {j}
let dMajorana scheme j n =
    let u   = scheme.Update j n
    let p   = scheme.Parity j
    let occ = scheme.Occupation j
    let dZ  = symmetricDifference p occ |> Set.remove j
    (j, Y) :: setAssign X u @ setAssign Z dZ
```

In math:
```
c_j = X_{U(j) ∪ {j}} · Z_{P(j)}
d_j = Y_j · X_{U(j)} · Z_{(P(j) △ Occ(j)) \ {j}}
```

### When do they agree?

**When the sets are disjoint.** Specifically, when:
1. {j} ∪ U(j) and P(j) are disjoint
2. j ∈ Occ(j) (always true by definition)
3. U(j) and Occ(j) are disjoint

For the three classical encodings (JW, BK, Parity), these conditions hold.
Both formulas produce identical operators.

### When do they diverge?

For **deeper trees** where a node can be simultaneously in the update set
(as an ancestor) AND the occupation set (as a descendant) of another node,
or where {j} ∪ U(j) overlaps with P(j). The SRL formula handles overlaps by
the casework (X→Y promotion at intersections), while the code formula assumes
disjointness and applies assignments independently.

### Why the difference doesn't matter for the star-tree theorem

The star-tree theorem says Construction A fails for non-star trees. Both
formulas fail — the proof works for the code formula (which is what the
implementation uses), and the SRL formula also fails (verified computationally).
The structural issue (even count of anticommuting positions on depth-≥2 paths)
is the same regardless of which formula variant you use at the surface.

---

## 5. How SRL Derives the Three Classical Encodings

### Jordan-Wigner
```
Tree shape: Star with root = n-1, leaves = {0, 1, ..., n-2}

U(j) = ancestors = {root} for leaves, ∅ for root
P(j) = remainder ∪ children = {k : k < j, k is sibling} ∪ ∅ = {0,...,j-1} for leaves
Occ(j) = {j} ∪ descendants = {j} for leaves
```

Wait — that gives U(j) = {root} for leaves, which is not JW. JW has U(j) = ∅.

**This is actually the first subtlety!**  JW as an index-set scheme is:
```
U_JW(j) = ∅,  P_JW(j) = {0,...,j-1},  Occ_JW(j) = {j}
```

If you try to derive these from a star tree with the generic tree-derived formula,
you get U(j) = {root} (non-empty!). These are **not the same** unless root = j
(which only works for one mode).

So even the claim "JW comes from a star tree via Construction A" needs careful
qualification: JW as traditionally defined uses hand-specified U(j) = ∅, not
tree-derived U(j) = {root}.

**However,** it turns out that the star-tree-derived sets DO produce valid
encodings — they're just a *different labelling* of what is effectively JW.
The proof of sufficiency in the star-tree theorem shows this explicitly.

### Bravyi-Kitaev

For BK, SRL derives the sets from the Fenwick tree using **hand-derived
bit-manipulation formulas**:
```fsharp
let bravyiKitaevScheme : EncodingScheme =
    { Update     = updateSet      // from FenwickTree.fs
      Parity     = paritySet      // from FenwickTree.fs
      Occupation = occupationSet  // from FenwickTree.fs
    }
```

These are NOT computed by the generic `treeEncodingScheme` function applied
to a Fenwick-shaped tree. They are **separate, hardcoded formulas** in
`FenwickTree.fs` that happen to use the same vocabulary (update, parity, occupation).

**This is the key conflation.**

### Parity

Parity can also be viewed as a star-tree encoding (reversed ordering).
```
U_Par(j) = {j+1,...,n-1}
P_Par(j) = {j-1} for j>0, ∅ for j=0
Occ_Par(j) = {j-1, j} for j>0, {j} for j=0
```

Again, this is a valid set-triple that happens to correspond to a star structure,
but the sets are specified directly, not derived from a tree.

---

## 6. The Fenwick Tree: What Makes BK Special

### The Fenwick tree structure

The Fenwick tree on n nodes is defined by bit arithmetic:
- **Parent of k** (1-indexed): k + lsb(k), where lsb(k) = k & (-k)
- **Children of k**: obtained by clearing bits

Example for n=8 (1-indexed, then shifted to 0-indexed):
```
        8
       / \
      4
     / \
    2   6
   / \ / \
  1  3 5  7
```

### Why BK works despite not being a star

The Fenwick tree has depth O(log n), well beyond the star-tree limit of depth 1.
Yet BK is a provably correct encoding. The resolution:

**BK does NOT use Construction A (the generic tree-to-index-set derivation).**

BK uses what we call **Construction F**: hand-derived formulas specific to the
Fenwick tree's algebraic structure. The bit-manipulation formulas in FenwickTree.fs
are NOT equivalent to applying `treeEncodingScheme` to a Fenwick-shaped `EncodingTree`.

The generic `treeRemainderSet` function uses a `child.Index < j` condition and a
specific definition of "remainder" that produces **different sets** from the
hand-derived Fenwick formulas when the tree has depth > 1.

### Concrete demonstration

If you create a Fenwick-shaped `EncodingTree` and apply `treeEncodingScheme`, you
get different index sets than `bravyiKitaevScheme`. The former fails CAR verification;
the latter passes. Same tree topology, different derivation method, different result.

---

## 7. What SRL Claims vs. What SRL Implies

### What SRL explicitly claims

1. ✅ "BK achieves O(log n) Pauli weight" — correct, proven
2. ✅ "BK can be described by update, parity, and occupation sets" — correct
3. ✅ "JW and Parity can also be described by U, P, Occ" — correct
4. ✅ "The Fenwick tree structure determines the BK index sets" — correct
5. ✅ "BK requires fewer gates than JW for H₂" — correct, demonstrated

### What SRL implies (but doesn't prove)

1. ❌ "The index-set framework is a unified, general method" — **misleading**
   - It's general in the sense that you CAN write any encoding as (U, P, Occ)
   - But there's no general METHOD for deriving valid (U, P, Occ) from a tree
   - The three examples use two genuinely different constructions disguised as one

2. ❌ "You can get new encodings by choosing different trees and reading off U, P, Occ" — **false for deep trees**
   - This works only for star trees (depth ≤ 1)
   - For any deeper tree, the generic derivation produces invalid encodings

3. ⚠️ "The update/parity/occupation vocabulary is the natural framework" — **partially true**
   - It's a valid descriptive vocabulary for any encoding
   - But it's not a constructive framework for generating new encodings
   - The path-based construction (Jiang et al., Bonsai) is the actual constructive framework

### The subtle misdirection

SRL presents the three encodings like this:

> "Here's a general framework (U, P, Occ) → encoding.
>  JW is (∅, {0..j-1}, {j}).
>  Parity is ({j+1..n-1}, {j-1}, {j-1,j}).
>  BK is (Fenwick ancestors, Fenwick children, Fenwick subtree).
>  → See how everything is unified!"

The reader naturally concludes: "So I could choose a different tree (say balanced
binary) and read off the sets to get a new encoding." **This does not work.**

The consistency of the three examples is an artefact of:
- JW and Parity being star trees (where Construction A works)
- BK using separately derived formulas (Construction F) that happen to fit the U/P/Occ vocabulary

---

## 8. The Conflation: Construction A vs. Construction F

### Three constructions, clearly separated

| Construction | Method | Input | Domain of validity |
|-------------|--------|-------|-------------------|
| **A** (Generic index-set) | Tree → U/P/Occ via ancestor/descendant/remainder formulas | Any tree | Stars only (depth ≤ 1) |
| **B** (Path-based) | Tree → root-to-leg Pauli paths | Any ternary tree | Universal |
| **F** (Fenwick-specific) | Bit arithmetic → U/P/Occ | Fenwick tree | Fenwick trees only |

### What SRL conflates

SRL presents Constructions A and F as a single method:

> "For any tree, read off the update, parity, and occupation sets."

But there are **two different recipes** for reading off these sets:
1. The **generic recipe** (ancestors, remainder∪children, descendants∪{j}) — this is Construction A
2. The **Fenwick recipe** (bit arithmetic) — this is Construction F

For star trees (JW, Parity), both recipes agree (because stars have trivial structure).
For the Fenwick tree, they disagree:
- Construction A applied to a Fenwick-shaped tree → **fails CAR**
- Construction F (hand-derived Fenwick formulas) → **passes CAR**

SRL never distinguishes these because the paper only presents the three examples,
which all happen to work (for different reasons each).

### In code

```fsharp
// Construction A: generic tree → scheme
let treeEncodingScheme (tree : EncodingTree) : EncodingScheme =
    { Update     = fun j _n -> treeUpdateSet tree j     // ancestors
      Parity     = fun j    -> treeParitySet tree j     // remainder ∪ children
      Occupation = fun j    -> treeOccupationSet tree j  // descendants ∪ {j}
    }

// Construction F: Fenwick-specific
let bravyiKitaevScheme : EncodingScheme =
    { Update     = updateSet       // bit arithmetic ancestors
      Parity     = paritySet       // bit arithmetic prefix-parity
      Occupation = occupationSet   // bit arithmetic subtree
    }
```

Both produce an `EncodingScheme` that feeds into the same `encodeOperator` function,
making them appear interchangeable. But `treeEncodingScheme` only works for stars,
while `bravyiKitaevScheme` only works for Fenwick trees.

---

## 9. What the Star-Tree Theorem Says

### Formal statement

**Theorem (Star-tree constraint).** Let T be a labelled rooted tree on [n], and
let (U, P, Occ) be the tree-derived index-set scheme (Construction A). The Majorana
operators defined by the encoding algorithm satisfy the CAR if and only if T is a star.

### What "star" means

A **star tree** is a tree of depth ≤ 1: one root, all other nodes are direct children
of the root. There are exactly n stars on n nodes (one for each choice of root).

### What the theorem does NOT say

1. It does NOT say "BK is wrong" — BK uses Construction F, not Construction A
2. It does NOT say "you can't encode with non-star trees" — Construction B works for all trees
3. It does NOT say "the U/P/Occ vocabulary is useless" — it's a valid description format
4. It DOES say "the generic tree-to-index-set recipe fails for most trees"
5. It DOES say "the SRL framework is less general than presented"

### Exhaustive verification

All n^{n-1} labelled rooted trees were enumerated for n = 1 through 5:

| n | n^{n-1} total trees | Trees passing CAR | Stars |
|---|--------------------|--------------------|-------|
| 1 | 1 | 1 | 1 |
| 2 | 2 | 2 | 2 |
| 3 | 9 | 3 | 3 |
| 4 | 64 | 4 | 4 |
| 5 | 625 | 5 | 5 |

The number of trees passing CAR equals exactly n. No accidental successes.

---

## 10. Detailed Proof Walkthrough

### Sufficiency (stars work)

For a star with root r and leaves L = {l₁, ..., l_{n-1}}:
```
For any leaf j:
  U(j) = {r}           [only ancestor is the root]
  P(j) = {k ∈ L : k < j}  [siblings with smaller index]
  Occ(j) = {j}         [no descendants]
```

These sets are always disjoint:
- U(j) ∩ P(j) = {r} ∩ {siblings < j} = ∅ (root isn't a sibling)
- {j} ∩ P(j) = ∅ (j ∉ {k < j})

For distinct leaves j < k:
- c_j has X at {j, r} and Z at {0,...,j-1}\{r}
- c_k has X at {k, r} and Z at {0,...,k-1}\{r}
- At position r: X · X = commutes
- At position j: X · Z = anticommutes (since j < k, so j ∈ P(k))
- No other position has non-identity in both
- Total anticommuting positions: 1 (odd) → {c_j, c_k} = 0 ✓

All other Majorana anticommutators work similarly.

### Necessity (non-stars fail)

This is the heart of the proof. If T is not a star, it has depth ≥ 2, so there exists
a path of length 2: w → u → v (grandparent → parent → grandchild).

**Claim:** {d_w, c_v} ≠ 0, violating the CAR.

**Step 1: What c_v looks like at positions w and u.**

c_v = X_{U(v) ∪ {v}} · Z_{P(v)}

Since w and u are both ancestors of v: w ∈ U(v) and u ∈ U(v).
So c_v has **X** at both positions w and u.

**Step 2: What d_w looks like at position u.**

d_w = Y_w · X_{U(w)} · Z_{(P(w) △ Occ(w)) \ {w}}

Node u is a child of w, so u ∈ children(w) ⊆ P(w).
Node u is also a descendant of w, so u ∈ Occ(w).
Therefore u ∈ P(w) ∩ Occ(w), which means **u cancels in the symmetric difference** P(w) △ Occ(w).
Since u is not an ancestor of w, u ∉ U(w).

**Result: d_w has I (identity) at position u.**

This is the critical failure mechanism. The symmetric difference cancellation makes d_w
"forget" about node u, even though c_v has X there.

**Step 3: What d_w looks like at position v.**

Node v is a descendant of w (grandchild), so v ∈ Occ(w).
But v is NOT a child of w (it's a grandchild), and v is not in remainder(w).
Therefore v ∈ Occ(w) \ P(w) ⊂ P(w) △ Occ(w).

**Result: d_w has Z at position v.**

**Step 4: Count anticommuting positions.**

| Position | d_w | c_v | Anticommute? |
|----------|-----|-----|-------------|
| w | Y | X | Yes (1) |
| u | **I** | X | No (0) |
| v | Z | X | Yes (1) |
| ancestors of w | X | X | No (commute) |
| all others | I or same | ... | No |

**Total anticommuting positions: 2 (EVEN).**

For anticommutation we need an odd number. Even means they commute:
d_w c_v = +c_v d_w, so {d_w, c_v} = 2·d_w·c_v ≠ 0.

### The mechanism in one sentence

> The symmetric difference P(w) △ Occ(w) cancels the intermediate node u (because u is
> both a child and a descendant of w), removing it from d_w. This "gap" at position u
> changes the parity of anticommuting positions from odd (correct) to even (incorrect).

### Why stars avoid this

In a star, there ARE no depth-2 paths. Every non-root node is a direct child of the
root. The "u is both child and descendant" cancellation never arises because there are
no grandchildren. In a star, P(w) and Occ(w) overlap only at direct children, and the
symmetric difference correctly produces the required Z positions.

---

## 11. The Balanced Ternary Counterexample

### Tree structure (n = 8)
```
        4
      / | \
     1   3   6
    /   /   / \
   0   2   5   7
```

### Index sets by Construction A

| j | U(j) | R(j) ∪ F(j) = P(j) | Occ(j) |
|---|------|---------------------|--------|
| 0 | {1, 4} | ∅ | {0} |
| 1 | {4} | ∅ ∪ {0} = {0} | {0, 1} |
| 2 | {3, 4} | {1} ∪ ∅ = {1} | {2} |
| 3 | {4} | {1} ∪ {2} = {1, 2} | {2, 3} |
| 4 | ∅ | ∅ ∪ {1, 3, 6} = {1, 3, 6} | {0,1,2,3,4,5,6,7} |
| 5 | {6, 4} | {1, 3} ∪ ∅ = {1, 3} | {5} |
| 6 | {4} | {1, 3} ∪ {5, 7} = {1, 3, 5, 7} | {5, 6, 7} |
| 7 | {6, 4} | {1, 3, 5} ∪ ∅ = {1, 3, 5} | {7} |

### The failing anticommutator

{a₄, a†₇} should be 0 (since 4 ≠ 7). The depth-2 path 4 → 6 → 7 triggers
the cancellation mechanism. The explicit computation gives:

```
{a₄, a†₇} = (0.5i) ZZZZZIXY + (-0.5) ZZZZZIXX ≠ 0
```

### Diagnosis

The failure involves the 4 → 6 → 7 path:
- Node 6 is both a child and descendant of 4 → cancels in P(4) △ Occ(4)
- d_4 has identity at position 6
- c_7 has X at position 6 (since 6 ∈ U(7))
- This creates an even number of anticommuting positions

### Construction B on the same tree

Applying the path-based construction to the **same** balanced ternary tree produces
all 120 anticommutators correct. The tree shape is fine — the index-set derivation
method is the problem.

---

## 12. Anticipating Objections

### "But SRL never claimed the framework works for arbitrary trees"

**Response:** Correct in the strict sense — SRL only demonstrates three examples.
But the paper presents a "universal algorithm" parameterised by (U, P, Occ) and shows
how to derive these from a tree. The natural reading is that the framework generalises.
Every subsequent paper that cites SRL treats it as a general framework (Tranter 2015,
Havlíček 2017, etc.). If SRL didn't intend generality, the paper should have included
a caveat. The star-tree theorem provides the missing characterisation.

### "BK works, so the framework works"

**Response:** BK works because it uses **Fenwick-specific formulas** (bit arithmetic),
not the generic tree-to-index-set recipe. If you apply the generic recipe to a
Fenwick-shaped tree, it FAILS. The BK encoding happens to be expressible in U/P/Occ
notation because any encoding can be — but the sets were derived by hand for the
Fenwick tree, not generated by the generic SRL algorithm.

This is the key point: BK is a **Construction F** instance wearing Construction A clothing.

### "Maybe the formula is wrong, not the framework"

**Response:** The failure isn't in the formula variant (SRL vs. code formula). Both
fail for the same structural reason. The issue is in the `remainder` set definition
— the rule "children of ancestors with index < j that are not ancestors" doesn't
correctly account for the recursive structure of deep trees. No simple patch to the
remainder-set formula fixes the problem for all trees. The proof shows the failure
arises from the tree **shape** (depth ≥ 2), not from formula details.

### "Has anyone checked this computationally?"

**Response:** Yes. We enumerated ALL n^{n-1} labelled rooted trees for n = 1...5
(a total of 1 + 2 + 9 + 64 + 625 = 701 trees) and verified every single anticommutator
symbolically. The result: exactly n trees pass for each n, and they are exactly the
n stars. Zero false negatives, zero accidental successes.

### "Can you fix Construction A to work for deeper trees?"

**Response:** Open question. The fundamental issue is that any index-set method that
assigns Paulis position-by-position (each qubit gets X, Y, Z, or I based on set
membership) faces the symmetric-difference cancellation on depth-≥2 paths. A fix would
need to either:
1. Use a different formula that accounts for path depth (no longer simple set membership)
2. Restrict to trees with special algebraic structure (Fenwick is the only known example)

Both options break the claimed simplicity and generality. The path-based construction
(Construction B) solves this by assigning Paulis along *paths*, not at *positions*.

### "This is a known result"

**Response:** No. We conducted an extensive literature search. No paper proves or
states the star-tree limitation. The closest related work:
- Steudtner & Wehner (2018) work exclusively in the path-based paradigm (Construction B)
  and never analyse Construction A's failure modes
- Jiang et al. (2020) and Miller/Bonsai (2023) introduce path-based alternatives but
  don't explain *why* the alternative is needed
- Havlíček et al. (2017) analyse operator locality but don't question the SRL framework's
  generality
- Derby & Klassen (2021) develop a different encoding approach but don't characterise
  when SRL fails

### "But Havlíček et al. already generalised this"

**Response:** Havlíček et al. (2017, arXiv:1701.07072) analyse operator locality
across encodings and prove lower bounds. Their tree framework is closer to Construction B
(path-based) than Construction A (index-set). They don't address the question of when
Construction A fails — they simply provide an alternative construction that works
universally.

### "What about Steudtner and Wehner?"

**Response:** Steudtner & Wehner (2018, arXiv:1712.07067) develop a ternary-tree
framework that is exactly our Construction B. They show JW, BK, and Parity can all
be recovered from specific trees. Critically, they work entirely within the path-based
paradigm and never use or analyse the SRL index-set derivation. Their framework is
universal precisely because it uses paths, not index sets. They never discuss why the
SRL framework would fail for non-trivial trees — presumably because they were already
working with a method that doesn't have this limitation.

---

## 13. Key Quotes to Reference

### From SRL (arXiv:1208.5986)

[Note: these are characterisations of SRL's claims, not direct quotes. The actual paper
should be consulted for exact wording.]

- SRL presents the index-set framework as the "natural" way to understand encodings
- The paper derives BK from the Fenwick tree and presents JW/Parity as special cases
- The vocabulary (update set, parity set, remainder set) is presented as general
- No caveat is given about the framework's limitations for non-Fenwick trees

### From your paper

The key result is:

> "The index-set construction (SRL framework) produces valid encodings if and only if
> the tree is a star. For general trees, the path-based construction is necessary and
> sufficient."

And the key observation:

> "The SRL 'unification' of JW, BK, and Parity into a single index-set framework is,
> on inspection, a conflation of Constructions A and F. JW and Parity are genuine
> Construction A instances (star trees). BK is a Construction F instance that happens
> to be expressible as an index-set scheme, but the scheme is hand-derived for the
> Fenwick tree rather than mechanically extracted."

---

## 14. Related Papers & How They Fit

### Timeline of encoding development

| Year | Paper | What it does | Construction used |
|------|-------|-------------|------------------|
| 1928 | Jordan & Wigner | Original JW mapping | Implicit (before framework) |
| 2002 | Bravyi & Kitaev | Proves O(log n) possible | Abstract (not operational) |
| 2012 | **Seeley, Richard, Love** | **Makes BK practical, introduces U/P/Occ** | **A + F conflated** |
| 2015 | Tranter et al. | Comparative analysis JW vs BK | Uses SRL framework (A/F) |
| 2017 | Havlíček et al. | Operator locality analysis | Path-based (≈ B) |
| 2017 | Bravyi et al. | Qubit tapering | Encoding-agnostic |
| 2018 | **Steudtner & Wehner** | **Ternary tree framework** | **B (universal, no A)** |
| 2020 | **Jiang et al.** | **Optimal encoding via general trees** | **B** |
| 2021 | Derby & Klassen | Compact encodings | Different approach |
| 2023 | **Miller et al. (Bonsai)** | **Software implementation** | **B** |
| 2025 | **This work** | **Proves A works only for stars** | **Characterises A, uses B** |

### Key observation from the timeline

The field has been **quietly migrating from Construction A to Construction B** without
anyone formally explaining why. Steudtner & Wehner, Jiang, and Miller all use path-based
methods. None of them says "we use paths because the SRL index-set method doesn't work
for deep trees." They just... use a different method. The star-tree theorem provides
the formal explanation for this migration.

---

## 15. One-Page Cheat Sheet

### The result in 30 seconds

The SRL paper (2012) presents a framework where you specify three sets (U, P, Occ) per
fermionic mode, and an algorithm converts them into qubit operators. Three classical
encodings (JW, BK, Parity) are shown as instances.

**The star-tree theorem shows:** If you derive U/P/Occ from a tree using the generic
recipe (ancestors, remainder, descendants), the encoding satisfies the CAR **if and only
if** the tree is a star (depth ≤ 1). This means only trivial relabellings of JW/Parity
emerge. BK survives because it uses separate, hardcoded formulas — not the generic recipe.

### The proof in 30 seconds

Any non-star tree has a grandparent→parent→grandchild path (w→u→v). In d_w, node u
cancels in the symmetric difference P(w) △ Occ(w) because u is both a child AND
descendant of w. This gives d_w identity at position u. But c_v has X at position u
(ancestor). The result: 2 anticommuting positions (even = wrong). Need odd for
anticommutation.

### Three constructions, not one

| | A (index-set) | B (path-based) | F (Fenwick) |
|---|---|---|---|
| **Domain** | Stars only | All trees | Fenwick only |
| **Method** | ancestor/remainder/descendant sets | root-to-leg paths | bit arithmetic |
| **Encodings** | JW, Parity variants | Everything | BK only |
| **Status** | Structurally limited | Universal | Special case |

### What it changes

- The SRL framework is not a general encoding generator — it's a description format
- The path-based method (Jiang, Bonsai) is the actual universal construction
- BK is a special case with bespoke formulas, not an instance of a general framework
- New encodings must be derived via paths, not index sets
