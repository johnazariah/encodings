# Paper 3 — Emergent Structure in Fermion-to-Qubit Encodings
## Trees, Locality, and the Geometry of Representation

**Target journal:** Physical Review A / Physical Review Research / Quantum
**Audience:** Quantum information, quantum foundations, emergence community
**Length:** ~20 pages + supplementary material
**Tone:** Research paper with formal results

---

## Thesis

The fermion-to-qubit encoding is a minimal, exactly solvable model of
quantum emergence.  The choice of a classical combinatorial structure
(a labelled rooted tree) completely determines the emergent quantum
properties — locality, symmetry, gauge structure, complexity — of the
resulting qubit representation.  We characterise this emergence precisely,
identify a structural phase boundary separating two construction
universality classes, and show that the encoding exhibits features —
symmetry fractionalization, hierarchical renormalization, emergent gauge
constraints — that mirror emergence in many-body physics and holography.

---

## Core Claims

**Claim 1 (Tree–Encoding Correspondence):**
Every fermion-to-qubit encoding preserving the CAR is uniquely determined
by a labelled rooted tree on n nodes.  Conversely, every such tree defines
a valid encoding.

**Claim 2 (Emergent Locality):**
The Pauli weight of an encoded operator — and hence its physical locality
on a qubit device — is bounded by the tree depth.  The *same* fermionic
interaction has weight O(1) to O(n) depending solely on the tree.  Locality
is not intrinsic; it emerges from the representational structure.

**Claim 3 (Emergent Symmetry Fractionalization):**
The global Z₂ particle-number parity symmetry of the fermionic Hamiltonian
is mapped to a *local* or *non-local* qubit observable depending on the
tree.  Different trees fractionalise the same symmetry differently — a
hallmark of topological order in condensed matter.

**Claim 4 (Emergent Gauge Structure):**
When the physical Hilbert space is restricted (e.g., fixed particle number),
the encoding generates stabiliser constraints that act as emergent gauge
symmetries.  Different trees produce different gauge groups.

**Claim 5 (Structural Phase Boundary):**
The space of labelled rooted trees decomposes into two regions:
  (a) Monotonic trees (all ancestors have index > node) — admit the
      Seeley-Richard-Love index-set construction.
  (b) Non-monotonic trees — require the path-based (Jiang/Bonsai)
      construction; the index-set method silently violates the CAR.
The boundary is sharp and detectable via anti-commutation diagnostics.

**Claim 6 (Optimal Renormalization):**
The balanced ternary tree achieves O(log₃ n) Pauli weight — provably
optimal.  This tree is structurally a MERA-like hierarchical
coarse-graining; its optimality is a statement about the optimal
renormalization scheme for fermionic systems.

---

## Outline

### 1. Introduction
- Quantum simulation: encoding as the bridge between algebras
- The emergence angle: encoding as a lens for studying representation
- Thesis statement: the tree IS the encoding, quantum properties EMERGE
- Connection to emergence in condensed matter, holography, MERA
- Summary of contributions

### 2. Background
- 2.1 Fermionic algebra and the CAR
  - Creation/annihilation operators, Fock space
  - The anti-commutation constraint
- 2.2 Pauli algebra and qubit Hilbert space
  - Pauli group, commutation relations
  - Why fermions and qubits are algebraically incompatible
- 2.3 The Majorana decomposition
  - c_j, d_j operators; their anti-commutation
  - Reconstruction of ladder operators from Majoranas
- 2.4 Review of known encodings
  - Jordan-Wigner, Bravyi-Kitaev, Parity (brief)
  - Existing unification: Steudtner-Wehner (2018), Jiang et al. (2019)

### 3. The Tree–Encoding Correspondence
- 3.1 Labelled rooted trees
  - Definitions, notation
  - Depth, branching factor, index ordering
- 3.2 From tree to Majorana operators: two constructions
  - **Construction A (Index-Set):**
    Update U(j), Parity P(j), Occupation Occ(j) from tree structure
    c_j = X_{U(j)∪{j}} · Z_{P(j)}
    d_j = Y_j · X_{U(j)} · Z_{(P(j)⊕Occ(j))∖{j}}
  - **Construction B (Path-Based):**
    Link labeling (X, Y, Z per descending link)
    Majorana string = product of labels along root-to-leg path
    Leg pairing via follow-X-then-Z / follow-Y-then-Z
- 3.3 Recovering known encodings as special trees
  - JW ↔ linear chain, BK ↔ Fenwick tree, Parity ↔ reverse chain
  - Table: tree shape → encoding → weight scaling
- 3.4 Proof of CAR preservation (path-based construction)
  - Anti-commutation from path orthogonality

### 4. Emergent Properties from Tree Structure
- 4.1 **Emergent Locality**
  - Definition: Pauli weight w(O) of encoded operator O
  - Theorem 1: w(a†_j) ≤ 2·depth(j) + 1
  - Corollary: balanced ternary tree → w = O(log₃ n), optimal
  - The locality paradox: nearest-neighbor hopping a†_i a_j has
    weight O(1) or O(n) depending on tree — same physics, different
    manifest locality
  - Discussion: parallel to emergent locality in holography
    (bulk operator = simple; boundary representation = complex or simple
     depending on the code/encoding)

- 4.2 **Emergent Symmetry Fractionalization**
  - The Z₂ parity symmetry: P = (-1)^N
  - In JW: P = Z₀ Z₁ ... Z_{n-1} (global, weight n)
  - In Parity: P = Z_{n-1} (local, weight 1)
  - In BK: P = Z_{n-1} when n = 2^k (local, weight 1)
  - Theorem 2: The qubit representation of P has weight equal to
    the number of tree leaves not on the Z-chain of the root
  - Fractionalization: same global symmetry → different local structure
  - Connection to symmetry fractionalization in topological phases

- 4.3 **Emergent Gauge Structure**
  - Fixed particle number → stabiliser constraints
  - Example: Parity encoding with N_e even → last qubit frozen to |0⟩
  - The stabiliser group depends on the tree
  - Qubit tapering (Bravyi et al. 2017) = gauge fixing
  - Different trees → different tapered Hamiltonians → different
    effective theories
  - Analogy: gauge redundancy in lattice gauge theory emerges from
    the encoding of local constraints

- 4.4 **The Tree as Renormalization Scheme**
  - Balanced ternary tree as hierarchical coarse-graining
  - Leaf nodes: microscopic (single-mode) information
  - Internal nodes: mesoscopic (parity-of-subtree) information
  - Root: macroscopic (total parity) information
  - Structural parallel to MERA
    (Multi-scale Entanglement Renormalization Ansatz)
  - The optimal encoding = optimal renormalization scheme
  - Conjecture: Hamiltonian-adapted trees correspond to
    Hamiltonian-adapted renormalization flows

### 5. The Structural Phase Boundary
- 5.1 The monotonicity property
  - Definition: a tree is *index-monotonic* if for every node j,
    all ancestors of j in the tree have index strictly greater than j
  - Examples: Fenwick trees are index-monotonic; balanced BSTs are not
- 5.2 Theorem 3 (Monotonicity Constraint)
  - *The index-set construction (Construction A) satisfies the CAR
    if and only if the tree is index-monotonic.*
  - Proof: necessity via explicit counterexample
    (balanced ternary tree n=8, modes 4 and 7)
  - Proof: sufficiency via induction on tree depth
- 5.3 The failure mode
  - When monotonicity fails: the remainder set R(j) contains nodes
    that lie on the root-to-j path, creating contradictions in the
    Majorana decomposition
  - Diagnostic: {a_i, a†_j} ≠ 0 for i ≠ j
  - Explicit computation showing the failure for modes 4, 7
- 5.4 Phase diagram of the encoding space
  - Space of labelled rooted trees on n nodes
  - Partition: monotonic (Construction A valid) vs. non-monotonic
    (only Construction B valid)
  - Counting: fraction of monotonic trees vs. n
  - The boundary as a structural phase transition
- 5.5 Two universality classes
  - Index-set class: compact formulas, bit-twiddling, O(log n) computation
  - Path-based class: universal but requires explicit tree traversal
  - The path-based construction as the "disordered phase" —
    fewer structural constraints, more freedom, same physics

### 6. Empirical Validation
- 6.1 Eigenspectrum equivalence
  - H₂ STO-3G: all 5 encodings → same 16×16 Hamiltonian eigenvalues
  - Ground state energy: -1.1373 Ha (exact FCI)
- 6.2 Anti-commutation verification
  - All {a_i, a†_j} = δ_{ij} verified for n = 4, 8, 16
  - Demonstration of failure for non-monotonic tree + index-set method
- 6.3 Scaling benchmark
  - Max Pauli weight: JW/Parity O(n), BK/BinTree O(log₂ n),
    TerTree O(log₃ n)
  - Mean weight, hopping weight, CNOT cost
  - Table and log-log plot
- 6.4 Monotonicity census
  - Fraction of random labelled trees that are monotonic
  - Scaling with n: monotonic trees become exponentially rare

### 7. Discussion
- 7.1 The encoding as a holographic map
  - Fermion Hilbert space (bulk) ↔ Qubit Hilbert space (boundary)
  - Tree ↔ holographic code / tensor network
  - Locality emergence parallels AdS/CFT
  - Encoding optimisation ↔ finding the "best" holographic code
- 7.2 Hamiltonian-adapted trees
  - The tree should match the interaction structure of H
  - Molecular orbital locality → tree should respect orbital geometry
  - Connection to entanglement structure of the ground state
- 7.3 Noise-aware encodings
  - Hardware topology constrains which qubit interactions are native
  - Optimal tree depends on device connectivity
  - The tree as a compilation target
- 7.4 Open questions
  - Is there an entanglement entropy *of the encoding itself*?
  - Can we define a topological invariant for the encoding space?
  - Does the monotonicity boundary have a field-theoretic interpretation?
  - What is the computational complexity of finding the optimal tree
    for a given Hamiltonian?
  - Connection to error correction: encodings as (non-error-correcting)
    codes — what happens when we add error correction to the tree?

### 8. Conclusion
- The tree is the encoding; the encoding is emergence
- Minimal, exactly solvable model of quantum emergence
- Every property is calculable, every prediction testable
- The space of encodings is itself a rich structure deserving study

---

## Formal Results Summary

| Result | Statement | Status |
|--------|-----------|--------|
| Theorem 1 | w(a†_j) ≤ 2·depth(j) + 1 | Needs formal proof |
| Theorem 2 | Parity weight = f(tree leaves, Z-chain) | Needs formal statement |
| Theorem 3 | Index-set construction ⟺ monotonic tree | Needs formal proof |
| Conjecture 1 | Optimal tree ↔ optimal renormalization | Open |
| Conjecture 2 | Monotonic fraction → 0 exponentially in n | Needs numerical evidence |

---

## Key Figures Needed

1. The five encoding trees side-by-side (JW, Parity, BK, BinTree, TerTree)
2. Emergent locality: same hopping term, different weights across encodings
3. Symmetry fractionalization: parity operator in JW vs. Parity vs. BK
4. The monotonicity counterexample: tree n=8, modes 4 and 7, failure trace
5. Phase diagram: monotonic vs. non-monotonic trees
6. Scaling plot (log-log): weight vs. n for all encodings
7. MERA-like diagram of balanced ternary tree as renormalization scheme
8. Holographic analogy diagram: fermion (bulk) ↔ tree ↔ qubit (boundary)

---

## Supplementary Material

- S1: Complete H₂ integral tables and eigenspectrum verification
- S2: Anti-commutation failure trace for non-monotonic tree
- S3: Proof of Theorem 3 (monotonicity constraint)
- S4: Random tree census data and statistics
- S5: CNOT cost analysis tables

---

## Status

- [ ] Outline finalized
- [ ] Core claims verified computationally
- [ ] Theorem 1 proved
- [ ] Theorem 2 stated and proved
- [ ] Theorem 3 proved (monotonicity constraint)
- [ ] Monotonicity census computed
- [ ] Section 4 drafted (emergent properties)
- [ ] Section 5 drafted (phase boundary)
- [ ] Section 6 drafted (empirical validation)
- [ ] Figures created
- [ ] Full draft assembled
- [ ] Internal review
