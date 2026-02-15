# Test Register

> **497 tests** | 0 skipped | xUnit + FsCheck (property-based)
>
> Last verified: 2026-02-15 against `v0.3.1`

> **Purpose:** Plain-English catalogue of every automated test in FockMap test suite.
> The LLM coding agent is responsible for keeping this register in sync
> with the test code before each commit. It serves as a quick
> business-level reference for code quality and reliability.
> It describes *what* is tested in plain English so that readers can
> quickly judge coverage without reading F# source.

---

## 1. Foundations

### 1.1 Phase Algebra — `Phase.fs` (5 property tests)

The `Phase` type represents the four global phases ±1, ±i that arise
when multiplying Pauli operators.

| # | What is tested | Style |
|---|----------------|-------|
| 1 | Multiplying by P1 (phase +1) leaves any phase unchanged | Property |
| 2 | Multiplying by M1 (phase −1) flips the sign but preserves complex flag | Property |
| 3 | Multiplying by Pi (phase +i) toggles the complex flag and conditionally flips sign | Property |
| 4 | Multiplying by Mi (phase −i) toggles the complex flag and conditionally flips sign | Property |
| 5 | `FoldIntoGlobalPhase` correctly maps each phase variant onto a complex coefficient | Property |

### 1.2 Pauli Algebra — `Pauli.fs` (7 tests)

Single-qubit Pauli operators I, X, Y, Z and their multiplication table.

| # | What is tested | Style |
|---|----------------|-------|
| 1 | Identity I leaves any Pauli unchanged under multiplication | Property |
| 2 | Every Pauli is its own inverse (σ² = I) | Property |
| 3 | Paulis commute with I and themselves, and anti-commute with distinct non-identity Paulis | Property |
| 4 | X × Y = iZ (exact) | Fact |
| 5 | Y × Z = iX (exact) | Fact |
| 6 | Z × X = iY (exact) | Fact |
| 7 | `Apply` parses valid Pauli labels "I", "X", "Y", "Z" and rejects invalid ones | Theory (5 cases) |
| 8 | `FromChar` parses valid Pauli chars and rejects invalid ones | Theory (5 cases) |
| 9 | Full 4×4 multiplication table is closed (result ∈ {I,X,Y,Z}) and phase ∈ {±1,±i} | Fact |
| 10 | `ToString` formats all four operators correctly | Fact |

### 1.3 Complex Number Extensions — `Complex.fs` (1 property test)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `TimesI` correctly rotates a complex number by 90° (swaps Re/Im with sign fix) | Property |

### 1.4 Type Extensions — `TypeExtensions.fs` (11 tests)

Utility extensions on `Complex`, `Map`, and functional combinators.

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `uncurry` correctly converts a curried function to a tupled one | Property |
| 2 | `curry` correctly converts a tupled function to a curried one | Property |
| 3 | `SwapSignMultiple` negates a coefficient on odd swap counts, preserves on even | Property |
| 4 | `IsNonZero` excludes zero and non-finite values (NaN, ∞) | Theory (5 cases) |
| 5 | `IsFinite` identifies finite components | Theory (4 cases) |
| 6 | `IsZero` treats non-finite values as logically zero | Theory (5 cases) |
| 7 | `Reduce` preserves finite values and zeroes out non-finite ones | Theory (3 cases) |
| 8 | `TimesI` matches multiplication by i for all finite inputs | Property |
| 9 | `Map.Keys` and `Map.Values` expose sorted contents | Fact |
| 10 | `ToPhasePrefix` formats common phases (±1, ±i, real, imaginary, mixed) | Fact (7 cases) |
| 11 | `ToPhaseConjunction` formats common phases for summation display | Fact (9 cases) |
| 12 | Generated static extension members are invocable via reflection | Fact |

---

## 2. Pauli Registers

### 2.1 PauliRegister & PauliRegisterSequence — `PauliRegister.fs` (14 tests)

Multi-qubit Pauli strings and their arithmetic.

| # | What is tested | Style |
|---|----------------|-------|
| 1 | Default register of size n is all-identity ("IIII") | Fact |
| 2 | Register uses big-endian bit ordering | Fact |
| 3 | `FromString` creates a round-trippable register | Theory (8 strings) |
| 4 | `FromString` creates a big-endian register (X at expected index) | Theory (4 cases) |
| 5 | Two registers multiply correctly (phase + Pauli string) | Theory (9 product cases) |
| 6 | Out-of-range indexing returns `None` | Fact |
| 7 | `WithOperatorAt` ignores out-of-range indices silently | Fact |
| 8 | Constructor with coefficient sets the phase prefix | Fact |
| 9 | Sequence from array combines like terms (adds coefficients) | Fact |
| 10 | Sequence removes terms that cancel to zero | Fact |
| 11 | Sequence constructor from sub-sequences distributes coefficients | Fact |
| 12 | `DistributeCoefficient` multiplies all summand coefficients | Fact |
| 13 | Sequence lookup returns `false` for a missing signature key | Fact |
| 14 | Empty sequence stays empty after distribution and multiplication | Fact |
| 15 | Register multiplication handles mismatched sizes in both directions | Fact |
| 16 | String constructor ignores invalid Pauli characters | Fact |

---

## 3. Generic Term Algebra (C, P, S)

The library's core algebra is parameterised over an operator type:
- **C** — a *coefficient × unit* pair
- **P** — a *product* of C terms (operator product)
- **S** — a *sum* of P terms (sum of products)

### 3.1 C (Coefficient × Unit) — `Terms_C.fs` (4 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `C.Apply(unit)` creates a term with coefficient 1 | Property |
| 2 | `C.Apply(coeff, unit)` preserves both coefficient and unit | Property |
| 3 | `ToString` formats coefficient-unit pairs with correct prefix notation | Theory (12 cases) |

### 3.2 P (Product Term) — `Terms_P.fs` (10 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | Addition of two C terms: combines like terms or keeps both | Property |
| 2 | Multiplication of two C terms: concatenates units, multiplies coefficients | Property |
| 3 | `P.Apply(unit)` creates a single-unit product in reduced form | Property |
| 4 | `P.Apply(coeff, unit)` normalises overall coefficient | Property |
| 5 | `P.Apply(C)` wraps a coefficient-unit into a product | Property |
| 6 | `P.Apply(coeff, C)` distributes outer coefficient | Property |
| 7 | `P.Apply(C[])` creates a multi-unit product in reduced form | Property |
| 8 | `P.Apply(coeff, C[])` distributes coefficient over units | Property |
| 9 | `TryCreateFromString` returns `None` for null input | Fact |
| 10 | `TryCreateFromString` returns `None` when parser throws | Fact |

### 3.3 S (Sum Expression) — `Terms_S.fs` (14 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `S.Apply(unit)` creates a single-term sum in reduced form | Property |
| 2 | `S.Apply(coeff, unit)` preserves reduction invariant | Property |
| 3 | `S.Apply(C)` wraps coefficient-unit into sum | Property |
| 4 | `S.Apply(C[])` wraps array of terms | Property |
| 5 | `S.Apply(coeff, C)` distributes coefficient | Property |
| 6 | `S.Apply(coeff, C[])` distributes coefficient over units | Property |
| 7 | `S.Apply(coeff, P)` wraps product term with coefficient | Property |
| 8 | `S.Apply(coeff, P[])` wraps product terms with coefficient | Property |
| 9 | Addition of sum expressions keeps last duplicate term | Fact |
| 10 | Multiplication distributes over product terms | Fact |
| 11 | `NormalizeTermCoefficient` pushes top-level coefficient into terms | Fact |
| 12 | `Reduce` removes zero-coefficient product terms | Fact |
| 13 | `IsZero` detects zero-coefficient and empty expressions | Fact |
| 14 | `TryCreateFromString` round-trips S expressions | Fact |
| 15 | `TryCreateFromString` returns `None` for null input | Fact |
| 16 | `TryCreateFromString` drops terms when parser throws | Fact |

---

## 4. Indexed Operators

### 4.1 IxOp (Index × Operator) — `IndexedOperator.fs` (2 property tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `.<=.` comparison: smaller-index IxOp ≤ larger-index IxOp | Property |
| 2 | `.>=.` comparison: larger-index IxOp ≥ smaller-index IxOp | Property |

### 4.2 Indexed Terms (Extra) — `IndexedTerms_Extra.fs` (8 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `isOrdered` detects ascending sequences | Property |
| 2 | `isOrdered` detects descending sequences | Property |
| 3 | `isOrdered` returns false for out-of-order pairs | Fact |
| 4 | `IndicesInOrder` validates ascending and descending index sequences | Fact |
| 5 | `TryCreateFromStringWith` parses valid "(Op, Index)" strings | Fact |
| 6 | `TryCreateFromStringWith` rejects invalid inputs (bad index, bad op, garbage) | Theory (3 cases) |
| 7 | `tryParseIxOpUint32` parses Pauli operator and index | Fact |
| 8 | `PIxOp.TryCreateFromString` and `SIxOp.TryCreateFromString` parse product/sum terms | Fact (2 cases) |

---

## 5. Ladder Operators (Fermionic)

### 5.1 LadderOperatorUnit — `LadderOperatorUnit.fs` (4 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `TryCreateFromString` parses valid ladder ops and rejects invalid formats | Theory (6 cases) |
| 2 | Synthesised ladder units have correct readable representation "(u, N)" / "(d, N)" | Property |
| 3 | `Apply` parses known ladder operator symbols (I, u, d) and rejects unknown | Theory (4 cases) |
| 4 | `FromTuple` preserves operator type and index | Fact |

### 5.2 LadderOperatorProductTerm — `LadderOperatorProductTerm.fs` (8 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `TryCreateFromString` round-trips product terms | Theory (8 input/output cases) |
| 2 | Synthesised product terms have correct string representation | Property |
| 3 | `IsInIndexOrder` detects sorted raise operators (ascending) | Property |
| 4 | `IsInIndexOrder` detects sorted lower operators (descending) | Property |
| 5 | Multiplying two product terms concatenates their units | Property |
| 6 | Parsed product term exposes correct units and coefficient | Fact |
| 7 | `Reduce` keeps canonical form | Fact |
| 8 | `IsInNormalOrder` correctly detects normal and non-normal orderings | Fact (2 cases) |

### 5.3 LadderOperatorSumExpression — `LadderOperatorSumExpression.fs` (12 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `TryCreateFromString` round-trips sum expressions | Theory (8 cases) |
| 2 | Product term multiplication concatenates units | Property |
| 3 | Fermionic normal ordering produces correct canonical form | Theory (6 input/output cases) |
| 4 | Parsed sum expression exposes coefficient and product terms | Fact |
| 5 | `Reduce` preserves valid expression | Fact |
| 6 | Addition and multiplication operators produce correct expressions | Fact |
| 7 | Detects normal-but-not-index-ordered expressions | Fact |
| 8 | Detects out-of-index-order raise operators | Fact |
| 9 | Detects fully normal-and-index-ordered expressions | Fact |
| 10 | `ApplyFromProductTerms` builds sum from wrapped product terms | Fact |
| 11 | `ApplyFromPTerms` builds sum from raw product terms | Fact |

### 5.4 LadderOperatorSequence — `LadderOperatorSequence.fs` (11 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `toIndexOrder` sorts raise and lower indices into canonical order | Property |
| 2 | `toIndexOrder` preserves the operator multiset (no operators lost/gained) | Fact |
| 3 | `ConstructNormalOrdered` normal-orders an unsorted expression | Fact |
| 4 | `ConstructNormalOrdered` returns unchanged expression when already normal | Fact |
| 5 | `ConstructIndexOrdered` enforces canonical index order | Fact |
| 6 | `ConstructIndexOrdered` handles non-normal input (applies both orderings) | Fact |
| 7 | `ConstructIndexOrdered` returns unchanged expression when already index-ordered | Fact |
| 8 | `ConstructNormalOrdered` handles empty expression | Fact |
| 9 | `toIndexOrder` keeps empty product term stable | Fact |
| 10 | `TryCreateFromString` parses valid sum expression | Fact |
| 11 | `TryCreateFromString` maps invalid units to empty expression | Fact |
| 12 | Plus and times operators produce valid combined expressions | Fact |

---

## 6. Combining Algebra (CAR / CCR)

### 6.1 Fermionic Algebra (CAR) — `CombiningAlgebra.fs` (4 tests)

The `FermionicAlgebra` implements canonical anti-commutation relations:
{a†ᵢ, aⱼ} = δᵢⱼ.

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `Combine` swaps Lower+Raise at *different* indices with sign flip (−1) | Fact |
| 2 | `Combine` at *same* index generates identity term + swapped term (delta + reordered) | Fact |
| 3 | `Combine` appends operator when no swap is needed (non-Lower→Raise pair) | Fact |
| 4 | `Combine` with short prefix inserts identity sentinel | Fact |

### 6.2 Bosonic Algebra (CCR) — `Bosonic.fs` (5 tests)

The `BosonicAlgebra` implements canonical commutation relations:
[bᵢ, b†ⱼ] = δᵢⱼ (no sign flip on swap).

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `Combine` swaps Lower+Raise at different indices *without* sign flip (coeff = +1) | Fact |
| 2 | `Combine` at same index generates identity + reordered terms (both coeff = +1) | Fact |
| 3 | Bosonic normal ordering uses CCR signs (no minus on swap) | Theory (2 input/output cases) |
| 4 | `constructBosonicNormalOrdered` helper is callable and returns result | Fact |
| 5 | `constructBosonicIndexOrdered` helper sorts indices within raise/lower groups | Fact |

---

## 7. Swap-Tracking Sort — `SwapTrackingSort.fs` (6 tests)

A selection sort that tracks the number of element displacements
(used internally for fermionic sign tracking).

| # | What is tested | Style |
|---|----------------|-------|
| 1 | Sorts arbitrary int arrays in ascending order | Property |
| 2 | Sorts arbitrary int arrays in descending order | Property |
| 3 | Known input/output pairs for ascending sort (5 cases including empty) | Theory |
| 4 | Known swap-count tracking (7 cases including empty, single, pre-sorted) | Theory |
| 5 | `IsSorted` returns false for unsorted arrays | Fact |
| 6 | Sort preserves initial phase when array is already sorted | Fact |
| 7 | Sort handles edge case: minimum at end of array | Fact |

---

## 8. Fenwick Tree — `FenwickTree.fs` (17 tests)

A binary indexed tree used to compute Bravyi-Kitaev index sets
in O(log n) time.

| # | What is tested | Style |
|---|----------------|-------|
| 1 | Prefix query on integer-sum tree returns correct prefix sums | Fact |
| 2 | Update adjusts all affected prefix sums | Fact |
| 3 | Empty tree has identity prefix (all zeros) | Fact |
| 4 | `size` returns element count | Fact |
| 5 | XOR tree computes prefix XOR | Fact |
| 6 | Set-union tree computes prefix unions (the BK use case) | Fact |
| 7 | `lsb` computes lowest set bit correctly | Theory (6 values) |
| 8 | `ancestors` of node 1 in n = 8 are {2, 4, 8} | Fact |
| 9 | `ancestors` of node 3 in n = 8 are {4, 8} | Fact |
| 10 | `ancestors` of node 8 in n = 8 is empty | Fact |
| 11 | `descendants` of node 4 are {3, 2} | Fact |
| 12 | `descendants` of node 8 are {7, 6, 4} | Fact |
| 13 | `descendants` of node 1 is empty | Fact |
| 14 | `prefixIndices` sequences are correct | Fact (2 cases) |
| 15 | BK `updateSet`, `paritySet`, `occupationSet` match hand-computed values for n = 8 | Theory (3 × 4–5 cases) |
| 16 | `remainderSet` and `symmetricDifference` match expected values | Fact (3 cases) |
| 17 | `pointQuery` on XOR tree recovers original elements | Fact |
| 18 | `build` from function produces same result as `ofArray` | Fact |

---

## 9. Fermion-to-Qubit Encodings

### 9.1 Jordan-Wigner — `FermionicOperator_JordanWigner.fs` (4 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | Creation operator a†ⱼ produces correct Pauli strings for all j in n = 8 | Theory (8 modes) |
| 2 | Annihilation operator aⱼ produces correct Pauli strings for all j in n = 8 | Theory (8 modes) |
| 3 | Number operator nⱼ = a†ⱼ aⱼ produces correct Pauli strings for all j in n = 8 | Theory (8 modes) |
| 4 | Identity operator returns empty sequence | Fact |
| 5 | Out-of-range index returns empty sequence | Fact |

### 9.2 Bravyi-Kitaev — `BravyiKitaev.fs` (12 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | n = 2, j = 0: creation operator has correct signatures and coefficients | Fact |
| 2 | n = 2, j = 0: annihilation operator has correct signatures and coefficients | Fact |
| 3 | n = 2, j = 1: creation operator has correct signatures and coefficients | Fact |
| 4 | n = 2, j = 1: annihilation operator has correct signatures and coefficients | Fact |
| 5 | n = 8: all 8 creation operators have correct c and d Majorana signatures | Theory (8 modes) |
| 6 | n = 8: all 8 annihilation operators have correct Majorana signatures | Theory (8 modes) |
| 7 | n = 2 number operators match hand-computed ½I − ½Z forms | Fact (2 cases) |
| 8 | n = 8 number operators are always two-term (½I + ½Z…) | Theory (8 modes) |
| 9 | BK has strictly lower Pauli weight than JW for n = 16 | Fact |
| 10 | BK and JW number operators have same term count for n = 4 | Theory (4 modes) |
| 11 | BK n = 2 j = 0 number operator matches JW term-for-term | Fact |
| 12 | Out-of-range index and Identity return empty | Fact (2 cases) |
| 13 | BK overlap integral (a†₀ a₁) produces non-trivial result | Fact |
| 14 | JW n = 2 number operators for reference comparison | Fact (2 cases) |

### 9.3 Parity Encoding — `Parity.fs` (13 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | Parity update set U(j,n) = {j+1 … n−1} | Theory (3 cases) |
| 2 | Parity set for j = 0 is empty; for j = 3 is {2} | Fact (2 cases) |
| 3 | Occupation set for j = 0 is {0}; for j = 3 is {2,3} | Fact (2 cases) |
| 4 | n = 2 creation/annihilation operators match hand-verified values | Fact (2 cases) |
| 5 | n = 4 creation operators have correct Majorana signatures | Theory (4 modes) |
| 6 | n = 4 annihilation operators have correct Majorana signatures | Theory (4 modes) |
| 7 | Parity weight for j = 0 equals n (full X chain) | Fact |
| 8 | Parity weight for j = n−1 is small (≤ 2) | Fact |
| 9 | n = 2 number operators match expected ½I − ½Z forms | Fact (2 cases) |
| 10 | n = 4 number operators are always two-term | Theory (4 modes) |
| 11 | Generic JW scheme matches `jordanWignerTerms` for n = 4 | Theory (4 modes) |
| 12 | Generic BK scheme matches `bravyiKitaevTerms` for n = 4 | Theory (4 modes) |
| 13 | Out-of-range index and Identity return empty | Fact (2 cases) |
| 14 | All three encodings (JW, BK, Parity) produce 2-term number operators for n = 4 | Theory (4 modes) |

### 9.4 Tree-Based Encodings — `TreeEncoding.fs` (24 tests)

Balanced binary and ternary tree encodings, plus cross-encoding validation.

**Tree construction:**

| # | What is tested | Style |
|---|----------------|-------|
| 1 | Linear tree on 4 nodes has correct chain structure | Fact |
| 2 | Balanced binary tree on 7 nodes has correct root and branching | Fact |
| 3 | Balanced ternary tree on 4 and 8 nodes has correct root | Fact (2 cases) |
| 4 | Tree constructors reject non-positive size (throws exception) | Fact |

**Index sets:**

| # | What is tested | Style |
|---|----------------|-------|
| 5 | Linear tree update set matches expected ancestor sets | Theory (4 modes) |
| 6 | Binary tree on 7: root has no ancestors | Fact |
| 7 | Binary tree on 7: leaf has O(log n) ancestors | Fact |
| 8 | Binary tree on 7: occupation set of leaf is singleton | Fact |
| 9 | Binary tree on 7: occupation set of root includes all 7 descendants | Fact |
| 10 | Ternary tree on 4: update set of root is empty | Fact |
| 11 | Ternary tree on 8: all 8 nodes are reachable | Fact |
| 12 | Tree helpers expose expected children, descendants, parity, and remainder sets | Fact |
| 13 | `treeEncodingScheme` delegates to tree helpers | Fact |
| 14 | `computeLinks` and `allLegs` produce correct counts | Fact |

**Encoding correctness:**

| # | What is tested | Style |
|---|----------------|-------|
| 15 | Ternary tree n = 2: creation and annihilation produce 2 Pauli terms each | Fact (2 cases) |
| 16 | Ternary tree n = 4: every operator produces exactly 2 terms | Fact (8 operator/mode combinations) |
| 17 | Ternary tree n = 4: number operator has real coefficients | Fact |
| 18 | Ternary tree n = 4: number operator has identity coefficient 0.5 (eigenvalues 0,1) | Fact (4 modes) |

**Pauli weight bounds:**

| # | What is tested | Style |
|---|----------------|-------|
| 19 | Ternary tree: max weight ≤ 2·⌈log₃ n⌉ + 3 for n ∈ {4, 8, 16} | Theory (3 sizes × all modes) |
| 20 | Binary tree: max weight ≤ 2·⌈log₂ n⌉ + 3 for n ∈ {4, 8, 16} | Theory (3 sizes × all modes) |

**Anti-commutation relations (CAR):**

| # | What is tested | Style |
|---|----------------|-------|
| 21 | Ternary n = 4: {aᵢ, a†ⱼ} = δᵢⱼ (4 pairs including i = j and i ≠ j) | Theory (4 pairs) |
| 22 | Ternary n = 4: {aᵢ, aⱼ} = 0 (4 same/different pairs) | Theory (4 pairs) |
| 23 | Ternary n = 8: {aⱼ, a†ⱼ} = 1 for j ∈ {0, 3, 7} | Theory (3 modes) |
| 24 | Ternary n = 8: {aᵢ, a†ⱼ} = 0 for i ≠ j | Theory (3 pairs) |

**Cross-encoding validation:**

| # | What is tested | Style |
|---|----------------|-------|
| 25 | All 4 encodings (JW, BK, Parity, Ternary) produce non-trivial results for the hopping Hamiltonian H = a†₀a₁ + h.c. on n = 4 | Fact |
| 26 | All 4 encodings: total number operator Σⱼ nⱼ has identity coefficient n/2 | Fact |

---

## 10. Hamiltonian Construction — `Hamiltonian.fs` (3 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `computeHamiltonian` produces correct JW Pauli string for n = 2 and n = 4 with unit coefficients | Theory (2 sizes) |
| 2 | `computeHamiltonianWith` using JW matches `computeHamiltonian` for n = 2 | Fact |
| 3 | Missing coefficients (factory returns None) produce empty Pauli sequence | Fact |

---

## 11. Mixed Systems (Fermion + Boson) — `MixedSystems.fs` (4 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `toSectorBlockOrder` moves bosonic operators to the right of fermionic ones without changing coefficient | Fact |
| 2 | `constructMixedNormalOrdered` applies CAR within fermion sector and CCR within boson sector; result is sector-block-ordered | Fact |
| 3 | `isSectorBlockOrdered` detects invalid boson-before-fermion sequences | Fact |
| 4 | Mixed normal ordering handles fermion-only inputs correctly | Fact |
| 5 | Mixed normal ordering handles boson-only inputs correctly | Fact |

---

## 12. Bosonic-to-Qubit Encodings — `BosonicEncoding.fs` (70 tests)

Maps truncated bosonic ladder operators (b†, b) to qubit Pauli strings
via three encoding strategies: Unary (one-hot), Standard Binary, and
Gray Code. Reference: Sawaya et al., arXiv:1909.05820.

### 12.1 Helper Functions (10 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `ceilLog2` returns correct ⌈log₂ d⌉ for d = 1, 2, 3, 4, 5, 8, 9, 16 | Theory (8 cases) |
| 2 | `grayCodeBasisMap` produces correct Gray code for n = 0–7 | Theory (8 cases) |

### 12.2 Matrix Construction (4 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `bosonicCreationMatrix` d=2 has (b†)₁₀ = 1 and all other entries zero | Fact |
| 2 | `bosonicCreationMatrix` d=4 has √1, √2, √3 on the sub-diagonal | Fact |
| 3 | `bosonicAnnihilationMatrix` equals the transpose of creation matrix for d = 2, 3, 4, 8 | Fact |
| 4 | `bosonicNumberMatrix` d=4 is diag(0, 1, 2, 3) with all off-diagonals zero | Fact |

### 12.3 Pauli Decomposition Infrastructure (4 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `allPauliStrings` q=1 enumerates exactly 4 strings | Fact |
| 2 | `allPauliStrings` q=2 enumerates exactly 16 strings | Fact |
| 3 | `decomposeIntoPaulis` of the 2×2 identity gives coefficient-1 I term | Fact |
| 4 | `decomposeIntoPaulis` of σ⁺ gives ½X and −½iY | Fact |

### 12.4 Edge Cases — All Encodings (9 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | Identity operator returns empty sequence (unary, binary, Gray) | Theory (3 cases) |
| 2 | d=1 (trivial Fock space) returns empty sequence (unary, binary, Gray) | Theory (3 cases) |
| 3 | Mode index out of range returns empty sequence (unary, binary, Gray) | Theory (3 cases) |

### 12.5 Unary Encoding (7 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | d=2 creation has exactly 4 weight-2 terms with correct coefficients (XX, YY, XY, YX) | Fact |
| 2 | d=2 annihilation has conjugate signs vs creation | Fact |
| 3 | d=2 creation and annihilation XY coefficients are negatives of each other | Fact |
| 4 | d=3 creation has 8 terms (4 per transition), all weight ≤ 2 | Fact |
| 5 | d=3 second transition (1→2) scaled by √2/4 | Fact |
| 6 | Two modes d=2: mode 0 acts on qubits 0-1, mode 1 on qubits 2-3 (register width = 4) | Fact |
| 7 | Max Pauli weight is ≤ 2 for d = 2, 3, 4, 5 | Theory (4 cases) |

### 12.6 Binary Encoding (8 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | d=2 creation equals σ⁺ = ½X − ½iY | Fact |
| 2 | d=2 annihilation equals σ⁻ = ½X + ½iY | Fact |
| 3 | d=4 uses 2 qubits per mode | Fact |
| 4 | d=4 creation has multiple terms with max weight ≤ 2 | Fact |
| 5 | Max Pauli weight is ≤ ⌈log₂ d⌉ for d = 2, 4, 8 | Theory (3 cases) |
| 6 | d=2 b†b product gives the number operator ½I − ½Z | Fact |
| 7 | Two modes d=2 have disjoint qubit support | Fact |
| 8 | `binaryQubitsPerMode` equals ⌈log₂ d⌉ for d = 2, 3, 4, 8 | Theory (4 cases) |

### 12.7 Gray Code Encoding (5 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | d=2 matches binary (Gray code is trivial at d=2) | Fact |
| 2 | d=4 creation uses same number of qubits as binary | Fact |
| 3 | d=4 differs from binary d=4 (different basis mapping produces different coefficients) | Fact |
| 4 | Max Pauli weight is ≤ ⌈log₂ d⌉ for d = 2, 4, 8 | Theory (3 cases) |

### 12.8 Term Counts and Qubit Counts (6 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | Unary term count is 4(d−1) for d = 2, 3, 4 | Theory (3 cases) |
| 2 | `unaryQubitsPerMode` equals d for d = 2, 4, 8 | Theory (3 cases) |

### 12.9 Embedding and Roundtrip (3 tests)

| # | What is tested | Style |
|---|----------------|-------|
| 1 | `embedMatrix` with identity mapping preserves the matrix | Fact |
| 2 | `embedMatrix` with Gray mapping for d=4 rearranges entries correctly (verified element-by-element) | Fact |

---

## Coverage Summary by Area

| Area | Tests | Technique | Confidence |
|------|------:|-----------|------------|
| Phase algebra | 5 | Property-based (FsCheck) | **High** — exhaustive over a 4-element type |
| Pauli algebra | 10 | Property + exact table checks | **High** — complete multiplication table |
| Complex extensions | 12 | Property + boundary cases | **High** |
| Generic term algebra (C, P, S) | 30 | Property-based | **High** — algebraic laws verified for random inputs |
| Indexed operators | 10 | Property + parser edge cases | **High** |
| Ladder operators | 35 | Property + Theory + Fact | **High** — covers parsing, ordering, normal form |
| Combining algebra (CAR/CCR) | 9 | Fact-based, hand-verified | **High** — critical sign logic tested |
| Swap-tracking sort | 7 | Property + known-answer | **High** |
| Fenwick tree | 18 | Fact + Theory | **High** — sum, XOR, set-union variants; bit-twiddling |
| Jordan-Wigner encoding | 5 | Theory (all 8 modes at n = 8) | **High** — full comparison to textbook formulas |
| Bravyi-Kitaev encoding | 14 | Theory + cross-validation vs JW | **High** |
| Parity encoding | 14 | Theory + index-set verification | **High** |
| Tree-based encodings | 26 | Fact + Theory + anti-commutation + cross-encoding | **High** — includes CAR verification and weight bounds |
| Hamiltonian construction | 3 | Theory + cross-validation | **Medium** — covers core path; could add BK/Parity Hamiltonians |
| Mixed systems (fermion + boson) | 5 | Fact-based | **Medium** — covers canonical paths; larger mixed expressions untested |
| Bosonic-to-qubit encodings | 70 | Theory + Fact + cross-encoding | **High** — matrix construction, Pauli decomposition, weight bounds, multi-mode embedding, number-operator roundtrip |

### What is *not* tested

- **Performance / scaling**: No benchmarks or regression tests for execution time.
- **Large-n anti-commutation for BK and Parity**: Anti-commutation relations are verified for tree encodings but not for Bravyi-Kitaev or Parity at n > 2.
- **Hamiltonian with real integrals**: The Hamiltonian builder is tested with unit coefficients only, not with physically meaningful H₂ integrals.
- **Bosonic anti-commutation verification**: Bosonic encodings are tested for correct Pauli decomposition and weight bounds, but [b, b†] = I is not verified at the Pauli level for all encodings.
- **Large-cutoff bosonic convergence**: Truncation encodings are tested up to d = 8; convergence behaviour at large d is not benchmarked.
- **Error messages / diagnostics**: Invalid-input tests verify `None`/empty returns but do not check specific error text.
- **Serialisation round-trips at encoding level**: String parsing is tested for ladder operators and Pauli registers, but not for full encoding pipelines.
