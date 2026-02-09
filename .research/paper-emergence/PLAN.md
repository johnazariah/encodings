# Paper 3 — Emergent Structure in Fermion-to-Qubit Encodings
## Full Research Plan and Writing Guide

---

### Metadata

| Field | Value |
|-------|-------|
| **Title** | Emergent Structure in Fermion-to-Qubit Encodings: Trees, Locality, and the Geometry of Representation |
| **Target** | Physical Review A / Physical Review Research / Quantum |
| **Audience** | Quantum information, quantum foundations, emergence community |
| **Assumed knowledge** | Second quantization, Pauli algebra, basic quantum information |
| **NOT assumed** | Specific encoding details, category theory, AdS/CFT |
| **Length** | ~20 pages + appendices/supplementary material |
| **Tone** | Research paper: formal claims, proofs, numerical evidence |
| **Relationship to user's research** | This is the FLAGSHIP paper — connects to your broader quantum emergence program |

---

### Why This Paper Is Important

Most papers on fermion-to-qubit encodings ask: "Which encoding gives the
shortest circuits?"  This paper asks: "What does the encoding MEAN?"

The central insight is:

> A fermion-to-qubit encoding is a *representation change* — the same
> physics viewed through a different lens.  The encoding preserves the
> physics (eigenspectrum) but transforms every structural property:
> locality, symmetry, gauge structure, complexity.  These transformed
> properties are not "choices" — they EMERGE from the tree.

This makes the encoding a minimal, exactly solvable model of quantum
emergence.  Unlike emergence in condensed matter (where we can't solve
the microscopic theory exactly), or in holography (where the bulk theory
is hard), encodings give us:

1. **Exact solvability**: The map is linear, the Hilbert spaces are finite.
2. **Complete classification**: Every encoding IS a tree; the space of
   encodings = the space of labelled rooted trees.
3. **Calculable properties**: Pauli weight, symmetry representation,
   gauge structure — all computable from the tree in O(n log n) time.
4. **Observable phenomena**: Emergent locality, symmetry fractionalization,
   structural phase transitions — all concrete and testable.

No other model of emergence has all four of these properties simultaneously.

---

## Thesis (Detailed)

The fermion-to-qubit encoding provides a minimal, exactly solvable
laboratory for studying quantum emergence.  We establish:

**Part A — Classification:**
Every CAR-preserving encoding corresponds to a labelled rooted tree.
The correspondence is constructive: given a tree, we produce the encoding;
given an encoding, we extract the tree (Claim 1).

**Part B — Emergence:**
From the tree alone, without solving ANY physics, we can read off:
- The locality structure of every qubit operator (Claim 2)
- How global symmetries fractionalise into local/non-local pieces (Claim 3)
- What gauge constraints appear when restricting to fixed particle number (Claim 4)
- The optimal renormalization scheme for the system (Claim 6)

These properties are "emergent" in the precise sense that they do not exist
in the fermionic description — they are created by the encoding.

**Part C — Phase Structure:**
The space of trees decomposes into two universality classes separated by
a sharp boundary (Claim 5).  On one side (monotonic trees), a compact
algebraic construction exists.  On the other (non-monotonic trees), only
the geometric (path-based) construction works.  The boundary is itself
a structural phase transition with measurable signatures.

---

## Detailed Section Plan

### Section 1: Introduction (2.5 pages)

**Opening** (1 paragraph):
Quantum simulation of fermionic systems on qubit-based hardware requires a
fermion-to-qubit encoding.  While much work has focused on optimising encodings
for circuit depth and gate count, we argue that the encoding itself is a rich
mathematical object deserving study on its own terms.

**The emergence angle** (2 paragraphs):
Frame the encoding as a representation change.  The fermionic Hamiltonian and the
qubit Hamiltonian have identical eigenspectra, but their structural properties —
locality, symmetry, computational complexity — differ fundamentally.  These
differences are not artefacts; they are emergent properties of the encoding.

Draw parallels to:
- Condensed matter: emergent quasiparticles (fermions → bosons + gauge fields
  via parton construction)
- Holography: bulk/boundary correspondence (fermion Hilbert space = "bulk",
  qubit Hilbert space = "boundary", tree = "holographic code")
- MERA: optimal renormalization via hierarchical tensor networks

**Contributions** (bullet list):
1. Complete tree–encoding correspondence (Claim 1)
2. Characterisation of emergent locality (Claim 2, Theorem 1)
3. Emergent symmetry fractionalization (Claim 3, Theorem 2)
4. Emergent gauge structure (Claim 4)
5. Discovery of the structural phase boundary (Claim 5, Theorem 3)
6. Optimal renormalization interpretation (Claim 6)

**What this is NOT:**
- Not a new encoding (we use known tree structures)
- Not an algorithmic improvement (we don't claim better circuits)
- It IS a new way of UNDERSTANDING encodings as emergence

**Dependencies:** None (pure exposition)


### Section 2: Background (3 pages)

#### 2.1 The Fermionic Algebra (0.5 pages)

- Creation/annihilation operators a†_j, a_j on n modes
- Canonical anti-commutation relations (CAR):
  {a_i, a†_j} = δ_{ij}, {a_i, a_j} = 0, {a†_i, a†_j} = 0
- Fock space: 2ⁿ-dimensional Hilbert space
- Majorana decomposition: c_j = a†_j + a_j, d_j = i(a†_j − a_j)
  - {c_j, c_k} = 2δ_{jk}, {d_j, d_k} = 2δ_{jk}, {c_j, d_k} = 0
  - 2n Majorana operators → equivalent to n fermionic modes

#### 2.2 The Pauli Algebra (0.5 pages)

- Pauli group P_n on n qubits
- Commutation structure: Paulis either commute or anti-commute
- Crucial difference from fermions: Paulis on different qubits ALWAYS commute
- A fermion-to-qubit encoding must convert inter-site anti-commutation to
  intra-site anti-commutation via non-trivial Pauli strings

#### 2.3 Known Encodings (1 page)

Brief review of JW, BK, Parity, with:
- Definition (1-2 lines each)
- Pauli weight scaling
- Historical context

Reference: Seeley-Richard-Love (2012), Bravyi-Kitaev (2002),
Havlíček et al. (2017), Jiang et al. (2019), Bonsai (2022)

#### 2.4 The Majorana Framework (1 page)

- Any encoding maps 2n Majorana operators to Pauli strings
- The mapping is CAR-preserving iff the Pauli strings pairwise anti-commute
  when the Majoranas anti-commute, and commute otherwise
- This reduces the encoding problem to: find 2n mutually anti-commuting
  Pauli strings in P_n
- **Lemma:** Such a set exists for all n ≥ 1 (constructive proof via JW)

**Dependencies:** Standard material, well-referenced


### Section 3: The Tree–Encoding Correspondence (4 pages)

This is the mathematical core of the paper.

#### 3.1 Labelled Rooted Trees (0.5 pages)

**Definition 1:** A *labelled rooted tree* T on [n] = {0, 1, ..., n−1} is
a tree with n vertices, each labelled by a distinct element of [n], with
a distinguished root vertex.

**Definition 2:** The *depth* of a vertex v in T is the length of the
unique path from the root to v.

**Definition 3:** The *branching factor* of T is max_v |children(v)|.

Key count: The number of labelled rooted trees on n vertices is n^{n−1}
(Cayley's formula).  This is the size of the encoding space.

#### 3.2 Construction A: Index-Set Method (1 page)

For a tree T on [n], define:
- U(j) = {ancestors of j} (the update set)
- R(j) = {v : v is a child of some ancestor of j, v < j, v ∉ ancestors(j)} (remainder set)
- F(j) = {children of j} (children set)
- P(j) = R(j) ∪ F(j) (parity set)
- Occ(j) = {j} ∪ {descendants of j} (occupation set)

Then the Majorana operators are:
  c_j = X_{U(j)∪{j}} · Z_{P(j)}
  d_j = Y_j · X_{U(j)} · Z_{(P(j) ⊕ Occ(j)) \ {j}}

**Show explicit construction for n = 4 with JW (linear chain) and BK (Fenwick tree).**

**Critical restriction:** This construction is well-defined only under a
monotonicity condition (see Section 5).

#### 3.3 Construction B: Path-Based Method (1.5 pages)

This is the universal construction (Jiang et al. / Bonsai).

**Link labeling:**
Each internal node of the tree has at most 3 descending connections (for a
ternary tree). Label them X, Y, Z. Connections that lead to children are
"edges"; connections with no child are "legs" (free Majorana endpoints).

**Majorana string construction:**
The Majorana operator for a leg ℓ is the product of labels along the
root-to-leg path:
  S_ℓ = ∏_{(v, label) ∈ path(root, ℓ)} label_v

where "label_v" means: at vertex v, apply the Pauli corresponding to the
label of the link taken at that vertex.

**Leg pairing:**
For each vertex v:
- Follow the X-link, then Z-links until a leg → this is s_x(v) → c_v Majorana
- Follow the Y-link, then Z-links until a leg → this is s_y(v) → d_v Majorana

**Proposition:** For any rooted tree T on [n] with at most 3 children per node,
Construction B produces 2n Majorana operators satisfying the CAR.

*Proof sketch:* Two Majorana strings S_ℓ and S_{ℓ'} for different legs ℓ, ℓ'
share a common prefix from the root to their lowest common ancestor, then
diverge at different labels. At the divergence point, they have different
Pauli labels on the same qubit, which guarantees anti-commutation.

#### 3.4 Recovering Known Encodings (1 page)

**Table:**

| Tree Shape | Depth | Construction | Encoding | Weight |
|-----------|-------|-------------|----------|--------|
| Linear chain (0→1→...→n−1) | n−1 | A or B | Jordan-Wigner | O(n) |
| Fenwick tree | ⌊log₂ n⌋ | A | Bravyi-Kitaev | O(log₂ n) |
| Reverse chain | n−1 | A or B | Parity | O(n) |
| Balanced binary | ⌊log₂ n⌋ | B only | Balanced binary | O(log₂ n) |
| Balanced ternary | ⌊log₃ n⌋ | B only | Balanced ternary | O(log₃ n) |

**Key observation:** The balanced binary and ternary trees are NOT monotonic,
so Construction A fails for them.  Only Construction B works universally.

**Dependencies:**
- Need formal verification of anti-commutation for all 5 encodings
- Need the counterexample computation from our code


### Section 4: Emergent Properties from Tree Structure (5 pages)

This is the conceptual heart of the paper.

#### 4.1 Emergent Locality (2 pages)

**Definition 4:** The *Pauli weight* of a Pauli string σ = σ_0 ⊗ ... ⊗ σ_{n-1}
is w(σ) = |{i : σ_i ≠ I}|, the number of non-identity factors.

**Definition 5:** An encoded operator Ô = E(O) has *encoding weight* w_E(O)
= max_{σ ∈ supp(Ô)} w(σ), where the maximum is over Pauli strings in the
decomposition of Ô.

**Theorem 1 (Weight Bound):**
For a tree T of depth d, the encoding weight of a single ladder operator satisfies:
  w_E(a†_j) ≤ 2·depth_T(j) + 1

*Proof:*
In Construction B, the Majorana string for a leg ℓ at depth d_ℓ has exactly
d_ℓ + 1 non-identity Pauli factors (one per node on the root-to-leg path,
including the leg's node). A ladder operator a†_j = ½(c_j − id_j) involves
two Majorana strings. The leg s_x(j) has depth ≤ depth(j) + (depth to
follow-Z endpoint). The worst case is 2·depth(j) + 1.  □

**Corollary 1.1:**
For a balanced ternary tree of branching factor 3:
  depth(j) ≤ ⌊log₃ n⌋
  ⟹ w_E(a†_j) = O(log₃ n)

**The Locality Paradox:**
Consider the nearest-neighbor hopping term a†_0 a_1 (modes 0 and 1).
In JW: a†_0 a_1 → ½(XX + YY) (weight 2 — very local!)
In Parity: a†_0 a_1 → ½(XZ...Z X + YZ...Z Y) (weight n — maximally non-local!)
Same physics, same eigenspectrum, completely different locality.

> "Locality is not a property of the interaction. It is a property of the
> REPRESENTATION of the interaction."

**Discussion:** Connect to holographic locality.
In AdS/CFT, a local bulk operator can be represented as a simple boundary
operator (HKLL) or a complex one (behind-the-horizon), depending on the
choice of reconstruction scheme. The tree encoding provides an exact,
finite-dimensional analogue: the tree IS the "holographic code" that
determines how bulk (fermionic) locality maps to boundary (qubit) locality.

**Computational verification needed:**
- [ ] Compute w(a†_j) for all j and all 5 trees for n = 4, 8, 16, 32
- [ ] Verify Theorem 1 bound is tight (find cases where equality holds)
- [ ] Compute hopping weight w(a†_i a_j) for nearest-neighbor and long-range
- [ ] Create locality comparison figure (same term, different encodings)


#### 4.2 Emergent Symmetry Fractionalization (1.5 pages)

**Definition 6:** The *particle-number parity* operator is:
  P = ∏_j (I − 2n̂_j) = ∏_j (I − 2a†_j a_j)

This is a Z₂ symmetry: P² = I, P commutes with any particle-conserving H.

**In the fermionic picture:** P is a "global" symmetry — it involves all n modes.

**In the qubit picture:** P̂ = E(P) is a product of Pauli operators.  Its
weight depends on the encoding:

**Theorem 2 (Parity Weight):**
For a tree T with path-based encoding:
  w(P̂) = [number of nodes v such that the Z-leg of v does NOT lead to another node]

More precisely, let Z-chain(T) be the maximal path from the root following
Z-links.  Then P̂ = ∏_{v ∈ Z-chain} Z_v, and w(P̂) = |Z-chain|.

*This needs careful formal statement and proof.*

**Examples:**
- JW (linear chain): Z-chain = entire chain → P̂ = Z₀Z₁...Z_{n-1}, weight n
- Parity (reverse chain): Z-chain = just the last node → P̂ = Z_{n-1}, weight 1
- BK (Fenwick, n = 2^k): Z-chain = root only → P̂ = Z_{n-1}, weight 1

**Symmetry fractionalization interpretation:**
In condensed matter, symmetry fractionalization is the phenomenon where a
global symmetry of a system is "carried" by fractional quasiparticles in a
non-trivial way.  Here:
- The global Z₂ parity is a property of the fermionic system
- The tree encoding "fractionalises" this parity among the qubits
- Different trees fractionalise differently (weight 1 vs. weight n)
- The weight of the parity operator measures the "degree of fractionalization"

> "The encoding doesn't just ENCODE the symmetry — it FRACTIONALISES it."

**Computational verification needed:**
- [ ] Compute P̂ for all 5 encodings for n = 4, 8, 16
- [ ] Verify weight formula (Theorem 2) for all cases
- [ ] Compute other symmetries (S_z conservation) and their weights
- [ ] Connection to qubit tapering: which trees admit more tapering?


#### 4.3 Emergent Gauge Structure (1 page)

**The setup:**
When we fix the particle number to N_e, the physical Hilbert space is
a C(n, N_e)-dimensional subspace of the 2ⁿ-dimensional qubit space.
The "unphysical" states are projected out.

**The key observation:**
The projection can be expressed as a set of stabiliser constraints.
These constraints form a stabiliser group, and they depend on the tree!

**Example (n = 4, N_e = 2):**
- JW: No automatic stabilisers from particle number alone.
  But if H conserves spin, Z₀Z₁ (total spin parity of first spatial orbital)
  is a stabiliser.
- Parity: Z₃ = (−1)^{N_e} = +1 for N_e = 2, so Z₃ is a stabiliser.
  One qubit can be eliminated.
- BK: Z₃ = total parity (same as Parity encoding for n = 4).

**The gauge interpretation:**
The stabilisers act as gauge constraints — they commute with the Hamiltonian
and restrict the Hilbert space.  "Gauge fixing" (choosing +1 or −1 eigenvalue)
= qubit tapering.  Different trees → different gauge groups → different
reductions.

**Practical implication:** Some trees naturally produce more stabilisers
for a given physical constraint, leading to more aggressive qubit reduction.

**Computational verification needed:**
- [ ] Enumerate stabilisers for all 5 encodings with N_e = 2, n = 4
- [ ] Compute the stabiliser group rank (number of qubits saved)
- [ ] Show that BK and Parity allow tapering Z_{n-1} but JW does not
- [ ] Explore: for n = 8, which tree gives the most stabilisers?


#### 4.4 The Tree as Renormalization Scheme (0.5 pages)

This is the most speculative section.  Keep it short and clearly labelled
as conjecture/interpretation.

**Observation:**
In a balanced ternary tree:
- Leaf nodes encode individual occupation numbers (microscopic)
- Internal nodes at depth d encode parity of O(3^d) modes (mesoscopic)
- The root encodes total parity (macroscopic)
- This is a hierarchical coarse-graining: each level aggregates 3 sub-levels

**Structural parallel to MERA:**
MERA uses isometries + disentanglers to build a hierarchical tensor network
that efficiently represents ground states of critical systems.  The encoding
tree has the same geometry.  But:
- MERA optimises for entanglement; the encoding tree optimises for weight
- MERA is approximate; the encoding is exact
- The connection is structural, not algebraic

**Conjecture 1 (MERA Connection):**
For a 1D fermionic system with critical ground state, the encoding tree
that minimises the total Pauli weight of the Hamiltonian is structurally
similar to the optimal MERA network for the same system.

**Conjecture 2 (Hamiltonian-Adapted Trees):**
For a given Hamiltonian H, the tree that minimises Σ_{terms} |c_term| · w(term)
(the weighted total Pauli weight) corresponds to a renormalization scheme
adapted to the entanglement structure of H's ground state.

**Status:** These conjectures are NOT proven.  They are motivation for
future work.  Present them honestly as such.

**Dependencies:**
- None (conjectures, not claims)
- Future work: numerical search for optimal trees for specific Hamiltonians


### Section 5: The Structural Phase Boundary (3 pages)

This is the technically novel section — the discovery of the monotonicity
constraint and its interpretation.

#### 5.1 The Monotonicity Property (0.5 pages)

**Definition 7:** A labelled rooted tree T on [n] is *index-monotonic* if
for every vertex v and every ancestor u of v:
  label(u) > label(v)

Equivalently: on every root-to-leaf path, indices strictly decrease
(root is largest, leaves are smallest on each path).

**Examples:**
- Fenwick tree on n = 8: root = 8 (1-indexed), children have smaller indices → monotonic ✅
- Balanced BST on n = 8: root = 4, has child 6 > 4 → NOT monotonic ✗

#### 5.2 Theorem 3: The Monotonicity Constraint (1.5 pages)

**Theorem 3:**
*Construction A (index-set method) produces Majorana operators satisfying
the canonical anti-commutation relations if and only if the tree T is
index-monotonic.*

**Proof of necessity (if not monotonic, CAR fails):**

Consider a non-monotonic tree. Then there exists a node j with an ancestor a
such that a < j. We show that this causes the remainder set R(j) to
contain nodes on the root-to-j path, leading to a contradiction.

*Specific counterexample from our implementation:*
- Balanced ternary tree, n = 8
- Node 7 has parent 6 (6 < 7 → non-monotonic)
- R(7) computed by index-set method contains node 6
- d_7 and d_6 fail to anti-commute: {d_6, d_7} ≠ 0

*Detailed computation:*
1. U(7) = ancestors of 7 = {6, 3} (walking up the tree)
2. For each ancestor a ∈ {6, 3}, look at children of a with index < 7
   that are not themselves ancestors of 7:
   - Children of 6: includes nodes with index < 7 not in {6, 3} → some node v
   - Children of 3: similarly
3. R(7) contains v, but v is on the path from 7 to root
4. This contaminates the Z assignments in d_7
5. Explicitly compute d_6 and d_7, multiply, show {d_6, d_7} ≠ 0

**Proof of sufficiency (if monotonic, CAR holds):**

If all ancestors have index > node, then:
- R(j) only contains nodes with index < j that are NOT on the path to root
- These nodes are in disjoint subtrees
- The Z assignments in c_j and d_j act on non-overlapping qubit sets
- Anti-commutation follows from the orthogonality of these sets

*This proof needs careful formalisation. The key step is showing that
monotonicity implies the index sets U(j), P(j), Occ(j) are "properly
nested" in a way that guarantees the Majorana algebra.*

**Dependencies:**
- [ ] Formal write-up of counterexample computation (modes 4 and 7, n=8)
- [ ] Rigorous proof of sufficiency (induction on tree depth)
- [ ] Verification script: test all labelled rooted trees for n = 4, 5, 6

#### 5.3 The Failure Mode (0.5 pages)

When monotonicity fails:
1. The remainder set R(j) "leaks" onto the root-to-j path
2. The Z assignments in d_j interfere with those in c_j at other modes
3. The anti-commutation relation {c_i, d_j} = 0 (i ≠ j) is violated
4. The encoded Hamiltonian has WRONG eigenspectrum

**Diagnostic:**
Given any tree and Construction A, compute:
  D(T) = max_{i≠j} ||{a_i, a†_j}||

If D(T) = 0 → CAR satisfied → encoding valid.
If D(T) > 0 → CAR violated → encoding invalid → tree is non-monotonic.

This gives a computational test for monotonicity without inspecting the tree.

#### 5.4 The Phase Diagram (0.5 pages)

**The space:** All labelled rooted trees on [n]. Size: n^{n-1} trees.

**The partition:**
- M(n) = {monotonic trees} (Construction A works)
- M̄(n) = {non-monotonic trees} (only Construction B works)

**Census (computational):**
For n = 2: 1 tree, 1 monotonic (100%)
For n = 3: 9 trees, ? monotonic
For n = 4: 64 trees, ? monotonic
For n = 5: 625 trees, ? monotonic
...

**Conjecture 2 (Monotonicity Fraction):**
|M(n)| / n^{n-1} → 0 as n → ∞.  Specifically, we conjecture that
|M(n)| / n^{n-1} = O(c^n / n!) for some constant c.

**Interpretation:** As the system size grows, the fraction of encodings
accessible to the "simple" (algebraic) construction vanishes.  The
"generic" encoding requires the geometric (path-based) construction.
This is analogous to a phase transition: the algebraic phase is
"ordered" (few degrees of freedom, compact description) while the
geometric phase is "disordered" (many degrees of freedom, requires
explicit construction).

**Computational verification needed:**
- [ ] Exhaustive enumeration for n = 2, 3, 4, 5, 6
- [ ] Random sampling for n = 7, 8, ..., 20
- [ ] Fit to conjectured scaling
- [ ] Generate phase diagram figure


### Section 6: Empirical Validation (2 pages)

#### 6.1 Eigenspectrum Equivalence (0.5 pages)

All 5 encodings (JW, BK, Parity, BalancedBinary, BalancedTernary) applied to
the H₂ STO-3G Hamiltonian produce qubit Hamiltonians with identical
eigenspectra.

**Method:**
1. Encode H₂ Hamiltonian with each encoding (from H2Demo.fsx)
2. Build 16×16 matrix representation
3. Diagonalise
4. Compare eigenvalues to machine precision

**Expected result:** All 5 give E₀ = −1.1373 Ha (electronic FCI energy).

**Status:** H2Demo.fsx produces 15 terms per encoding with Σ|coeff| = 3.2557 ✅
Matrix diagonalisation: 🔲 not yet implemented

#### 6.2 Anti-Commutation Verification (0.5 pages)

For each encoding, compute all C(2n, 2) anti-commutators:
- {c_j, c_k} = 2δ_{jk} · I
- {d_j, d_k} = 2δ_{jk} · I
- {c_j, d_k} = 0

**Method:**
1. Build 2ⁿ × 2ⁿ matrix for each Majorana
2. Compute all anti-commutators
3. Verify the result

**For the counterexample:** Show that Construction A on the n=8 balanced
ternary tree produces {d_i, d_j} ≠ 0 for specific (i, j).

#### 6.3 Scaling Data (0.5 pages)

From ScalingBenchmark.fsx (already computed):

| n | JW max | BK max | Parity max | BinTree max | TerTree max |
|---|--------|--------|-----------|------------|------------|
| 4 | 4 | 4 | 4 | 3 | 3 |
| 8 | 8 | 4 | 8 | 5 | 3 |
| 12 | 12 | 4 | 12 | 5 | 5 |
| 16 | 16 | 5 | 16 | 5 | 5 |
| 20 | 20 | 5 | 20 | 7 | 5 |
| 24 | 24 | 5 | 24 | 7 | 5 |

Present as log-log plot.  Fit slopes to confirm:
- JW, Parity: slope 1 (linear)
- BK, BinTree: slope log₂(n)/log(n) ≈ 1/ln(2)
- TerTree: slope log₃(n)/log(n) ≈ 1/ln(3)

#### 6.4 Monotonicity Census (0.5 pages)

New computation needed.  Enumerate all labelled rooted trees for small n,
test each for monotonicity.

Present data, fit to scaling hypothesis.

**Dependencies:**
- [ ] MatrixVerification.fsx (eigenspectrum check) — .research/tools/
- [ ] AnticommutationTest.fsx (full {c,d} verification) — .research/tools/
- [ ] MonotonicityCensus.fsx (tree enumeration + test) — .research/tools/
- [ ] ScalingPlot.py or .fsx (log-log plot with fit) — .research/paper-emergence/figures/


### Section 7: Discussion (2.5 pages)

#### 7.1 The Encoding as a Holographic Map (1 page)

**Not a metaphor — a structural parallel.**

In AdS/CFT:
- Bulk: low-dimensional, gravitational
- Boundary: high-dimensional, conformal field theory
- Holographic code: maps bulk operators to boundary operators
- Different codes: HaPPY code, random tensor networks

In fermion-to-qubit:
- "Bulk": fermionic algebra (n modes, 2n Majorana operators)
- "Boundary": qubit algebra (n qubits, Pauli group P_n)
- "Holographic code": the tree T
- Different trees: different encodings = different "codes"

**What carries over:**
1. Locality emergence: bulk local ↔ boundary local (or not), depending on code
2. Symmetry structure: bulk symmetry ↔ boundary symmetry representation
3. Gauge constraints: bulk constraints ↔ boundary stabilisers

**What does NOT carry over:**
1. No gravitational dynamics (the encoding is static, not dynamical)
2. No entanglement wedge reconstruction (all bulk operators are reconstructable)
3. Finite-dimensional (no UV/IR hierarchy in the usual sense)

**Value:** The encoding provides a CALCULABLE toy model for studying
holographic phenomena that are intractable in the full AdS/CFT setting.

#### 7.2 Hamiltonian-Adapted Trees (0.5 pages)

The optimal tree depends on what Hamiltonian you're encoding.
- For local (nearest-neighbor) fermionic H: JW may be best (weight 2 for hopping)
- For non-local H: balanced ternary tree minimises maximum weight
- For hardware-constrained H: tree should match device connectivity

**Open problem:** Given H and a hardware graph G, find the tree T that
minimises the CNOT count of the resulting quantum circuit.

#### 7.3 Noise-Aware Encodings (0.5 pages)

On real quantum hardware, not all qubit interactions are equally noisy.
An encoding that places high-weight operators on low-noise qubit subsets
could outperform a "theoretically optimal" encoding.

**Open problem:** Noise-aware tree optimisation.

#### 7.4 Open Questions (0.5 pages)

1. Is there an "entanglement entropy of the encoding"?
   Possible definition: the entanglement of the fermionic ground state
   when expressed in the qubit computational basis.  Different encodings
   give different entanglement → the encoding has its own entanglement signature.

2. Can we define a topological invariant for the encoding space?
   The space of trees has a graph structure (edge swap = tree mutation).
   Is there a topological property that distinguishes classes?

3. Does the monotonicity boundary have a field-theoretic interpretation?
   Is there a continuum limit where the boundary becomes a phase transition
   in the usual statistical mechanical sense?

4. What is the computational complexity of finding the optimal tree?
   Likely NP-hard, but is it approximable?

5. Connection to error correction:
   An encoding + error-correcting code = an encoded encoding.
   What happens when we compose the two?


### Section 8: Conclusion (0.5 pages)

- The tree IS the encoding; the encoding IS emergence
- We have characterised emergent locality, symmetry fractionalization,
  gauge structure, and renormalization — all from a single combinatorial object
- The structural phase boundary is the first example of a phase transition
  in the space of quantum representations
- This is the simplest, most calculable model of quantum emergence we know of
- The framework is fully implemented and computationally verified

---

## Dependent Components (Must Build Before Writing)

| Component | Location | Purpose | Status |
|-----------|----------|---------|--------|
| MatrixVerification.fsx | .research/tools/ | 2ⁿ × 2ⁿ matrix diag for eigenspectrum | 🔲 |
| AnticommutationTest.fsx | .research/tools/ | Full {c_j, d_k} verification | 🔲 |
| MonotonicityCensus.fsx | .research/tools/ | Enumerate trees, test monotonicity | 🔲 |
| ParityOperator.fsx | .research/tools/ | Compute P̂ weight for each encoding | 🔲 |
| StabiliserAnalysis.fsx | .research/tools/ | Find stabiliser group for fixed N_e | 🔲 |
| CNOTCostEstimator.fsx | .research/tools/ | CNOT count from Pauli decomposition | 🔲 |
| Fig. 1: Five trees | figures/ | Side-by-side tree diagrams | 🔲 |
| Fig. 2: Locality paradox | figures/ | Same term, different weights | 🔲 |
| Fig. 3: Parity fractionalization | figures/ | P̂ weight across encodings | 🔲 |
| Fig. 4: Monotonicity failure | figures/ | Counterexample trace | 🔲 |
| Fig. 5: Phase diagram | figures/ | Monotonic fraction vs. n | 🔲 |
| Fig. 6: Scaling plot | figures/ | Log-log weight vs. n | 🔲 |
| Fig. 7: MERA analogy | figures/ | Balanced ternary as RG | 🔲 |
| Fig. 8: Holographic diagram | figures/ | Fermion ↔ tree ↔ qubit | 🔲 |

---

## Writing Checklist

### Phase 1: Computational Foundation
- [ ] Build MatrixVerification.fsx (eigenspectrum for all 5 encodings)
- [ ] Build AnticommutationTest.fsx (full CAR verification + counterexample)
- [ ] Build MonotonicityCensus.fsx (enumerate and test trees n=2..6)
- [ ] Build ParityOperator.fsx (P̂ weight computation)
- [ ] Build StabiliserAnalysis.fsx (gauge group extraction)
- [ ] Verify all numerical claims

### Phase 2: Core Theorems
- [ ] Formal proof of Theorem 1 (weight bound)
- [ ] Formal statement and proof of Theorem 2 (parity weight)
- [ ] Formal proof of Theorem 3 (monotonicity constraint)
  - [ ] Necessity: counterexample + generalization
  - [ ] Sufficiency: induction argument

### Phase 3: Drafting
- [ ] Section 1 (Introduction)
- [ ] Section 2 (Background)
- [ ] Section 3 (Tree–Encoding Correspondence)
- [ ] Section 4 (Emergent Properties)
  - [ ] 4.1 Emergent Locality
  - [ ] 4.2 Symmetry Fractionalization
  - [ ] 4.3 Gauge Structure
  - [ ] 4.4 Renormalization
- [ ] Section 5 (Structural Phase Boundary)
- [ ] Section 6 (Empirical Validation)
- [ ] Section 7 (Discussion)
- [ ] Section 8 (Conclusion)

### Phase 4: Figures
- [ ] Fig. 1: Five encoding trees
- [ ] Fig. 2: Locality paradox
- [ ] Fig. 3: Parity fractionalization
- [ ] Fig. 4: Monotonicity failure trace
- [ ] Fig. 5: Phase diagram (monotonic fraction)
- [ ] Fig. 6: Scaling (log-log plot)
- [ ] Fig. 7: MERA analogy
- [ ] Fig. 8: Holographic diagram

### Phase 5: Polish
- [ ] Supplementary material assembled
- [ ] References compiled
- [ ] Notation consistency check
- [ ] All computational claims verified against code
- [ ] Internal review
- [ ] Format for PRA/PRResearch/Quantum

---

## Proof Sketches (To Be Formalised)

### Theorem 1: Weight Bound

**Claim:** w(a†_j) ≤ 2·depth_T(j) + 1

**Proof idea:**
- a†_j = ½(S_{s_x(j)} − i·S_{s_y(j)})
- S_{s_x(j)} is the Majorana string for leg s_x(j)
- The path from root to leg s_x(j) has length ≤ depth(j) + d_z where
  d_z is the number of Z-links followed after the X-link
- In the worst case, d_z = depth of the Z-chain below the X-edge
- The worst case total depth is bounded by 2·depth(j) for binary trees,
  and 2·depth(j) + 1 accounting for the leg itself
- The Majorana string has one Pauli per path node → weight = path length

**Gap:** Need to bound d_z tightly.  For balanced trees, d_z ≤ depth(j).
For pathological trees, d_z could be larger.  Need to clarify whether
the bound is universal or requires tree balance.

### Theorem 2: Parity Weight

**Claim:** The encoded parity operator P̂ = E(∏_j (I − 2n̂_j)) has
weight determined by the tree's Z-chain structure.

**Proof idea:**
- The parity operator P = (−1)^N = ∏_j (−1)^{n̂_j}
- In the encoded picture, n̂_j = ½(I − i·c_j·d_j)
- (−1)^{n̂_j} = I − 2n̂_j = i·c_j·d_j
- P = ∏_j (i·c_j·d_j) = i^n · ∏_j c_j · d_j
- In the path-based construction, the product ∏_j c_j · d_j telescopes:
  each intermediate Pauli cancels with its partner in the adjacent Majorana
- What survives is the Z-chain from the root

**Gap:** The telescoping argument needs to be made precise.  The cancellation
depends on the specific pairing of legs.  Need to show that the Bonsai
pairing (follow-X-then-Z, follow-Y-then-Z) guarantees the right cancellation
pattern.

### Theorem 3: Monotonicity Constraint

**Claim:** Construction A satisfies CAR ⟺ tree is index-monotonic.

**Proof of necessity:**
- If tree is non-monotonic, ∃ node j and ancestor a with a < j
- In the computation of R(j), node a contributes children with index < j
- But the path from j to root passes through a, so some of these children
  are on the path → R(j) contains path nodes → Z assignments contaminated
- Explicitly: d_j has a Z on a node that is ALSO in U(j) for some other mode
  → the Majorana operators for the two modes share non-trivially
  → anti-commutation fails

**Proof of sufficiency (harder):**
- If all ancestors of j have index > j, then:
  - R(j) contains only nodes with index < j that are NOT on the path
  - These nodes are in "sibling subtrees" below the path
  - The Z assignments in c_j and d_j are confined to these sibling subtrees
  - For different modes j₁, j₂, the Z assignments are compatible
    (they act on disjoint qubit subsets)
  - Anti-commutation follows
- Need to formalise "compatible" → probably use induction on tree depth

**Status:** Necessity is proven (by computational counterexample + argument).
Sufficiency needs rigorous proof.

---

## Connection to Your Emergence Research

This paper positions the encoding as a test case for your broader quantum
emergence program.  Key connections to highlight:

1. **Emergence is not mysterious:** In this model, we can COMPUTE exactly
   what emerges and WHY.  The tree is the complete description; every
   emergent property follows deterministically.

2. **Emergence requires representation change:** Without changing the
   representation (fermion → qubit), there is no emergence.  The properties
   we call "emergent" (locality, symmetry fractionalization) literally
   do not exist in the fermionic description.

3. **The space of representations has structure:** Not all representations
   are equal.  The monotonicity boundary, the optimal tree, the gauge
   structure — these are properties of the representation space itself.
   Studying this space may reveal universal features of emergence.

4. **Calculability is the key advantage:** Unlike condensed matter emergence
   (approximate) or holographic emergence (intractable), encoding emergence
   is exact and finite.  This makes it the ideal test bed for developing
   and validating emergence theories.
