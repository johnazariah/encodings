# Research Investigation Journal

Living record of investigations, discoveries, dead ends, and open questions.  
Entries are reverse-chronological (newest first).

---

## 2026-02-09 — Verification Tools & Three Major Discoveries

### Goal

Build the computational verification tools needed for all three papers.
Four tools completed today: MatrixVerification, AnticommutationTest,
MonotonicityCensus, ParityOperator.

---

### Investigation 1: Eigenspectrum Equivalence (MatrixVerification.fsx)

**Question:** Do all five encodings produce the same energy spectrum for H₂?

**Approach:** Built a full matrix verification pipeline:
- Construct the 2ⁿ × 2ⁿ matrix for each encoding's qubit Hamiltonian
- Compute eigenvalues via complex Hermitian solver (2n×2n real embedding)
- Compare against direct FCI (full configuration interaction) in Fock space

**Bugs encountered:**
1. *DLL path:* Scripts in `.research/tools/` need `#r "../../Encodings/bin/Debug/net8.0/Encodings.dll"` — two levels up, not one.
2. *`reg.Size` is internal:* Had to use `reg.Signature.Length` instead.
3. *FCI operator ordering:* `applyExchange` was applying exchange operators in the wrong order. The two-body term `a†_p a†_q a_s a_r` requires applying operators right-to-left (a_r first, then a_s, then a†_q, then a†_p).
4. *Complex eigenvalue solver:* Balanced ternary encoding produced complex-valued matrix entries. Standard real symmetric eigenvalue routines fail. Fixed by embedding the n×n Hermitian matrix as a 2n×2n real symmetric matrix: `[[Re, -Im], [Im, Re]]`.

**Result:** ✅ All five encodings match to |Δλ| = 4.44e-16 (machine epsilon).
Ground state energy = −1.7621792965 Ha.

**Implication for papers:** Paper 1 (Tutorial) can confidently state that all
encodings are exactly isospectral. Paper 2 (Software) can cite this as a
validation methodology.

---

### Investigation 2: CAR Verification & Monotonicity (AnticommutationTest.fsx)

**Question:** Do all five encodings satisfy the canonical anti-commutation relations? What happens when we use the index-set construction on non-monotonic trees?

**Approach:**
- Compute all Majorana operators c_j, d_j for each encoding
- Verify {c_j, c_k} = 2δ_{jk}I, {d_j, d_k} = 2δ_{jk}I, {c_j, d_k} = 0
- Apply `treeEncodingScheme` (Construction A) to various tree shapes and test CAR

**Key learning — monotonicity definition:**
Went through three iterations of what "monotonic" means for a labelled rooted tree:

1. ❌ First attempt: `child.Index > parent.Index` — wrong direction; Fenwick trees have parent > child.
2. ✅ Correct: `parent.Index > child.Index` on every edge — ancestors always have larger indices.

Fenwick trees satisfy this by construction: parent of 1-based index k is k + lsb(k) > k.
Linear chains do NOT: in JW's chain 0→1→2→...→(n−1), node 0's parent is 1, and 1 > 0 ✓, but this only works for the JW ordering, not general linear chains.

**Result:** ✅ All 15 standard encoding tests pass with zero deviation.
Index-set construction on non-monotonic trees fails as expected.

**Built a Fenwick tree as EncodingTree:** Needed to verify that BK's Fenwick tree can be expressed as an `EncodingTree` and correctly identified as monotonic. Required fixing a `Root` field access issue on the `EncodingTree` type.

---

### Investigation 3: Monotonic Tree Census (MonotonicityCensus.fsx) ⭐

**Question:** How many labelled rooted trees on n nodes are monotonic? What fraction of all trees?

**Approach:**
- Enumerate ALL labelled rooted trees on n nodes via Prüfer sequences
- For each tree, check monotonicity (parent > child on every edge)
- Count and compute fractions

**Discovery 1 — |M(n)| = (n−1)! exactly:**

| n | Total trees (nⁿ⁻¹) | Monotonic |M(n)| | Fraction |
|---|---------------------|-----------|----------|
| 1 | 1 | 1 | 1.000 |
| 2 | 2 | 1 | 0.500 |
| 3 | 9 | 2 | 0.222 |
| 4 | 64 | 6 | 0.094 |
| 5 | 625 | 24 | 0.038 |
| 6 | 7776 | 120 | 0.015 |

|M(n)| = (n−1)! for every n tested. The fraction (n−1)!/nⁿ⁻¹ decays
super-exponentially at rate ~e⁻ⁿ (by Stirling).

This confirms Conjecture 2 from Paper 3's plan. The proof should follow from
the fact that a monotonic labelling is equivalent to a heap ordering on the
tree, and the number of heap-ordered labelled trees rooted at n is (n−1)!
(every permutation of {1,...,n−1} gives a unique heap-ordered tree when
inserted by the rule "attach to largest available ancestor").

**Reference:** This connects to the theory of increasing trees / heap-ordered trees.
See Bergeron, Flajolet, Salvy — "Varieties of Increasing Trees" (1992).

**Bug:** Factorial computation overflowed for n > 12 using `int`. Switched to
log-based comparison: `log((n-1)!) vs log(count)`.

---

### Investigation 4: Star Trees Only ⭐⭐ (MonotonicityCensus.fsx, Part 2)

**Question:** Do ALL monotonic trees produce valid encodings via Construction A (`treeEncodingScheme`)?

**Approach:**
- For every labelled rooted tree on n = 3, 4, 5 nodes, build an `EncodingTree`
- Apply `treeEncodingScheme` to get an encoding
- Test full CAR

**Discovery 2 — Only star trees pass Construction A:**

| n | Monotonic trees | Trees passing CAR | Passing trees |
|---|----------------|-------------------|---------------|
| 3 | 2 | 3 | All 3 are stars (depth 1) |
| 4 | 6 | 4 | All 4 are stars |
| 5 | 24 | 5 | All 5 are stars |

Wait — the count of passing trees equals n, not (n−1)!. And the passing trees
are ALL stars (one root with all other nodes as direct children). The n
variations come from choosing which node is the root (labelled 0, 1, ..., n−1).

**Why this happens:**
The `treeRemainderSet` function in `TreeEncoding.fs` computes the remainder
R(j) using the condition `child.Index < j`. For non-star monotonic trees
(e.g., a Fenwick tree expressed as an EncodingTree), this condition excludes
nodes that should be in the remainder, because nodes on the path from j to root
can have children with indices that interact incorrectly.

**Why BK still works:**
The Bravyi-Kitaev encoding does NOT use `treeEncodingScheme`. It uses
`bravyiKitaevScheme` which delegates to `FenwickTree.fs` — completely separate
code with Fenwick-specific bit-manipulation formulas (`updateSet`, `paritySet`,
`occupationSet`). These formulas are correct for Fenwick trees but are NOT
the same as the generic `treeRemainderSet` computation.

**Impact on Paper 3:**
This significantly sharpens Theorem 3 (the "structural phase boundary").
The paper's plan assumed Construction A works for all monotonic trees.
In fact:
- Construction A (`treeEncodingScheme`) works ONLY for star trees (depth ≤ 1)
- Construction B (path-based, `encodeWithTernaryTree`) works for ALL trees
- BK's construction is a THIRD method, specific to Fenwick trees

This means the "phase boundary" isn't between monotonic and non-monotonic —
it's between star and non-star. The algebraic construction is far more
restrictive than we thought.

**Open question:** Can `treeEncodingScheme` be fixed to work for all monotonic
trees? The `treeRemainderSet` formula needs careful rethinking. Or is the
star-only restriction fundamental to the index-set approach?

---

### Investigation 5: Symbolic Parity Operators (ParityOperator.fsx) ⭐

**Question:** What is the Pauli weight of the parity operator P̂ = (−1)^N̂ under each encoding? How do single-operator and hopping-term weights compare?

**Approach — first attempt (matrix, FAILED):**
Original plan: decompose the parity operator matrix into Pauli strings by
computing Tr(σ_α · P̂)/2ⁿ for all 4ⁿ Pauli strings α.

This requires 4ⁿ Pauli strings × 2ⁿ × 2ⁿ matrix multiplies = O(4ⁿ · 4ⁿ) = O(16ⁿ)
operations. For n = 10 with 5 encodings: ~5 × 10¹² floating-point operations.
The script hung/crashed.

**Key insight — symbolic algebra is polynomial:**
The `PauliRegisterSequence` type already implements exact Pauli string
multiplication (finite group operation). Multiplying two k-term sequences
produces at most k² terms, and `DistributeCoefficient` collects like terms.

The number operator n̂_j = a†_j a_j produces O(1) Pauli terms per mode.
The parity operator P̂ = ∏_j (I − 2n̂_j) is a product of n factors, each
with O(1) terms. Because Pauli strings multiply to single Pauli strings
(the Pauli group is closed under multiplication), the intermediate products
stay compact.

**This is not an approximation.** The Pauli group multiplication table is
exact (it's a finite group). The 303 unit tests + matrix cross-validation
at small n confirm the implementation is correct.

**Approach — second attempt (symbolic, SUCCESS):**
Rewrote entirely using `PauliRegisterSequence` multiplication. No matrices anywhere.

**API learnings along the way:**
- `PauliRegister` constructor accepts `Pauli list`, not `Pauli array` or `Pauli seq` — had to use `List.replicate` instead of `Array.create`
- `reg.Signature` returns `string` (not `Pauli[]`)
- `reg.[i]` returns `Pauli option` (not `Pauli`)
- `.SummandTerms` for accessing terms (not `.Terms`)

**Results:**

Parity operator weight w(P̂):
| Encoding | w(P̂) | Structure |
|----------|-------|-----------|
| Jordan-Wigner | n | Z⊗ⁿ (all-Z string) |
| Parity | 1 | Single Z on last qubit |
| Balanced Binary | 1 | Single Z on root |
| Bravyi-Kitaev | O(log n) | Z on Fenwick path |
| Balanced Ternary | O(log₃ n) | Z on ternary root path |

All parity operators are single Pauli strings (1 term). P² = I verified
symbolically for all 5 encodings. ✅

Performance: n = 100 computed in 5.5 seconds with zero matrix operations.

Single-operator average weights at n = 8:
| Encoding | Mean w(a†_j) |
|----------|-------------|
| Balanced Ternary | 2.88 |
| Balanced Binary | 3.12 |
| Bravyi-Kitaev | 4.00 |
| Jordan-Wigner | 4.50 |
| Parity | 5.38 |

Full 8×8 hopping weight matrices W_{ij} = w(a†_i a_j) computed for all
five encodings.

**Lesson for Paper 2 (Software):** The symbolic algebra approach is the
correct way to present the library. Matrix verification is a cross-check
for small n, but the library's VALUE is that it makes large-n symbolic
computation trivial. This should be a central selling point.

---

### API Gotchas Catalogue

Accumulated through all investigations. These should be documented in Paper 2.

| Issue | Wrong | Right |
|-------|-------|-------|
| PauliRegister size | `reg.Size` | `reg.Signature.Length` (`Size` is `member internal`) |
| PauliRegister indexing | `reg.[i]` returns `Pauli` | Returns `Pauli option` |
| PauliRegister constructor | `PauliRegister(Pauli array, coeff)` | `PauliRegister(Pauli list, coeff)` |
| Signature type | `reg.Signature` is `Pauli[]` | It's `string` |
| Accessing terms | `prs.Terms` | `prs.SummandTerms` |
| DLL path from tools/ | `#r "../Encodings/..."` | `#r "../../Encodings/..."` |

**Design note:** The constructor accepting only `Pauli list` (not `Pauli seq`)
is a friction point. Worth considering a library change.

---

### Tools Status

| Tool | Status | Lines | Key Output |
|------|--------|-------|------------|
| MatrixVerification.fsx | ✅ Complete | ~475 | All 5 encodings isospectral to ε |
| AnticommutationTest.fsx | ✅ Complete | ~320 | 15/15 CAR tests pass |
| MonotonicityCensus.fsx | ✅ Complete | ~425 | \|M(n)\| = (n−1)!, star-only for Construction A |
| ParityOperator.fsx | ✅ Complete | ~200 | Parity weights, symbolic algebra at n=100 |
| IntegralTables.fsx | ✅ Complete | ~458 | Full H₂ Pauli decomposition, all 5 encodings |

---

### Open Questions

1. **Can Construction A be generalised?** The `treeRemainderSet` formula fails
   for non-star monotonic trees. Is this fixable, or is the star restriction
   fundamental to index-set methods?

2. **What is the correct theorem statement for Paper 3?** The original plan
   had "Construction A works for monotonic trees" (Theorem 3). The correct
   statement appears to be: "Construction A works only for stars; Construction B
   (path-based) works for all trees; BK is a third construction specific to
   Fenwick trees."

3. **Bosonic encodings?** The Pauli algebra infrastructure (PauliRegister,
   PauliRegisterSequence) is encoding-agnostic. Could extend to bosonic
   commutation relations with truncated Fock spaces (Gray code, unary, compact
   mappings). Scope question for the library.

4. **Heap-ordered tree proof:** |M(n)| = (n−1)! — need a clean bijective
   proof. Likely connects to the theory of increasing trees (Bergeron,
   Flajolet, Salvy 1992).

5. **PauliRegister constructor:** Should accept `Pauli seq` for ergonomics.
   Breaking change or overload?

---

### References Consulted Today

- Seeley, Richard, Love. "The Bravyi-Kitaev transformation for quantum computation of electronic structure." arXiv:1208.5986 (2012)
- Havlíček, Córcoles, Temme, Harrow, Kandala, Chow, Gambetta. "Operator locality in quantum simulation of fermionic models." arXiv:1701.07072 (2017)
- Jiang, McClean, Babbush, Neven. "Majorana loop stabilizer codes for error mitigation in fermionic quantum simulations." arXiv:1910.10746 (2019)
- Miller, Camps, Bettencourt. "Bonsai: diverse and shallow trees for fermion-to-qubit encodings." arXiv:2212.09731 (2022)
- Bergeron, Flajolet, Salvy. "Varieties of Increasing Trees." CAAP 1992, Lecture Notes in Computer Science vol 581.
