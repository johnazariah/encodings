# Paper 1 — From Molecules to Qubits
## A Complete, Step-by-Step Guide to Quantum Chemistry on a Quantum Computer

---

### Metadata

| Field | Value |
|-------|-------|
| **Title** | From Molecules to Qubits: A Complete Guide to Quantum Chemistry Simulation |
| **Target** | American Journal of Physics (pedagogical review) |
| **Audience** | 3rd-year undergraduate in chemistry or physics |
| **Assumed knowledge** | Linear algebra, intro quantum mechanics (wavefunctions, Schrödinger equation, hydrogen atom), basic chemistry (orbitals, bonds) |
| **NOT assumed** | Second quantization, Fock space, Pauli algebra, any quantum computing |
| **Length** | ~35–40 pages (AJP allows long pedagogical articles) |
| **Tone** | Patient, explicit, every-step-shown, "here's where students get confused" |
| **Running example** | H₂ molecule in STO-3G basis (simplest non-trivial molecule) |
| **Code companion** | F# repository at ../.. with working implementation |

---

### Why This Paper Exists

There is a gap in the literature.  Textbooks on quantum chemistry (Szabo &
Ostlund, Helgaker et al.) assume you know second quantization.  Textbooks on
quantum computing (Nielsen & Chuang) don't cover chemistry.  Research papers
on quantum simulation (Whitfield et al. 2011, O'Malley et al. 2016) compress
the entire pipeline into 2–3 pages of dense notation.

A 3rd-year student who wants to understand "how do you actually simulate a
molecule on a quantum computer?" currently has to read 4+ sources and
reconcile incompatible notation.  This paper is the ONE source they need.

---

### Pedagogical Principles

1. **Never introduce notation without an example.**
   Every formula is immediately followed by "for H₂, this means..."

2. **Name the traps.**
   Every place where notation is ambiguous, where sign conventions differ,
   where indices get swapped — call it out in a box: "⚠️ Common Error."

3. **Build up, don't top-down.**
   Start with something the student already knows (the hydrogen atom)
   and add complexity one layer at a time.

4. **Compute everything explicitly.**
   For H₂ with 4 spin-orbitals, every matrix element, every integral,
   every Pauli string can be written out by hand.  Do it.

5. **Physical intuition before formalism.**
   "Why do we need second quantization?" comes before "here's the definition
   of a creation operator."

---

## Detailed Section Plan

### Section 1: Introduction (2 pages)

**Goal:** Motivate the entire pipeline.  Make the student want to read on.

**Content:**
- Open with a concrete question: "What is the ground-state energy of H₂?"
  - Classical answer: solve the Schrödinger equation (easy for H₂, impossible
    for large molecules)
  - Quantum computing promise: simulate quantum with quantum
- Feynman's 1982 observation (1 paragraph, accessible)
- The pipeline preview (1-sentence summary of each step):
  1. Choose a molecule and a basis set
  2. Compute molecular integrals (overlap, kinetic, electron-electron)
  3. Write the Hamiltonian in second quantization
  4. Convert from spatial to spin orbitals
  5. Encode fermionic operators as qubit operators
  6. Run a quantum algorithm (VQE, QPE)
- "By the end of this paper, you will be able to do steps 1–5 by hand for H₂."
- Road map of the paper

**Figures:**
- Fig. 1: Pipeline diagram (molecule → basis → integrals → 2nd quant → encoding → qubits → algorithm)

**Dependencies:** None


### Section 2: What Does "Solve a Molecule" Mean? (4 pages)

**Goal:** Make the electronic structure problem concrete for a student who
knows the hydrogen atom but has never thought about multi-electron systems.

#### 2.1 The Schrödinger Equation for Molecules (1 page)

- Full molecular Hamiltonian: kinetic energy of nuclei + electrons +
  Coulomb interactions (electron-electron, electron-nucleus, nucleus-nucleus)
- Write it out explicitly for H₂: two protons (A, B), two electrons (1, 2)
- "This is a 6-dimensional PDE.  For H₂ we can solve it.  For caffeine
  (C₈H₁₀N₄O₂, 102 electrons) we cannot."

#### 2.2 The Born-Oppenheimer Approximation (1 page)

- Physical intuition: nuclei are ~2000× heavier than electrons → move slowly
  → treat nuclear positions as fixed parameters
- Result: the electronic Hamiltonian H_el(R) depends parametrically on R
- The nuclear repulsion energy V_nn is just a constant for fixed R
- "We will set R = 0.7414 Å for H₂ (the equilibrium bond length) and forget
  about the nuclei."
- **Box:** "The Born-Oppenheimer approximation is not an approximation for
  quantum simulation — it's the standard starting point for all electronic
  structure theory."

#### 2.3 Basis Sets: Turning Continuous into Discrete (1 page)

- The problem: H_el acts on functions of 3N_e continuous variables
- The solution: expand the wavefunction in a finite basis
- Analogy: Fourier series truncation.  Keep N terms → N×N matrix eigenvalue problem
- Atomic orbitals as the natural basis (student already knows 1s, 2s, 2p for hydrogen)
- LCAO: molecular orbitals = linear combinations of atomic orbitals
- **STO-3G:** each Slater-type orbital approximated by 3 Gaussians
  - For H₂: two atomic 1s orbitals → two molecular orbitals
  - σ_g = (1s_A + 1s_B)/√2  (bonding — lower energy)
  - σ_u = (1s_A − 1s_B)/√2  (antibonding — higher energy)
- "With 2 molecular orbitals, our entire problem lives in a finite space.
  We just need to figure out how big that space is."

**Figures:**
- Fig. 2a: H₂ molecule geometry (two protons, distance R)
- Fig. 2b: Atomic 1s orbitals on each proton → molecular σ_g and σ_u
- Fig. 2c: Energy level diagram (σ_g below σ_u, two electrons filling σ_g)

#### 2.4 How Many States? The Configuration Space (1 page)

- 2 spatial orbitals × 2 spin states = 4 spin-orbitals
- 2 electrons distributed among 4 spin-orbitals → C(4,2) = 6 configurations
- List all 6 explicitly:
  |1100⟩ (both in σ_g),  |1010⟩, |1001⟩, |0110⟩, |0101⟩, |0011⟩ (both in σ_u)
- "The exact ground state is a *superposition* of these 6 configurations.
  Finding the right superposition is the electronic structure problem."
- Preview: "We'll use a quantum computer to prepare this superposition."

**Dependencies:**
- `H2Demo.fsx` for integral values
- Need a figure-generation script for orbital diagrams


### Section 3: Second Quantization (5 pages)

**Goal:** Teach second quantization from scratch to a student who has never
seen it.  By the end, the student can write H₂'s Hamiltonian in operator form.

#### 3.1 Why Can't We Just Use Wavefunctions? (1 page)

- The antisymmetry problem: electrons are fermions → wavefunction must be
  antisymmetric under exchange: ψ(r₁,r₂) = −ψ(r₂,r₁)
- For 2 electrons: Slater determinant is a 2×2 determinant (manageable)
- For N electrons: N×N determinant with N! terms (catastrophic)
- "Second quantization builds antisymmetry into the *operators* instead of
  the *wavefunction*.  It's the same physics, but the bookkeeping is free."

#### 3.2 Occupation Number Representation (1 page)

- Idea: don't track which electron is where — track which *orbitals* are occupied
- For 4 spin-orbitals: state = |n₀ n₁ n₂ n₃⟩ where nᵢ ∈ {0, 1}
- The vacuum |0000⟩: all orbitals empty
- H₂ ground state (Hartree-Fock): |1100⟩ (σ_g↑ and σ_g↓ occupied)
- "This is exactly like writing a binary number.  4 orbitals → 4 bits → 2⁴ = 16
  possible states.  But only C(4,2) = 6 of them have exactly 2 electrons."

**Box:** "Occupation number vectors look exactly like qubit computational
basis states.  This is not a coincidence — it's why quantum simulation works."

#### 3.3 Creation and Annihilation Operators (1.5 pages)

- a†_j: "create an electron in orbital j"
  - a†_j |...0_j...⟩ = (−1)^{Σ_{k<j} n_k} |...1_j...⟩
  - a†_j |...1_j...⟩ = 0  (Pauli exclusion!)
  - The (−1) factor: counts how many occupied orbitals we "pass over"
    (this is the antisymmetry, baked into the operator)
- a_j: "annihilate an electron in orbital j"
  - a_j |...1_j...⟩ = (−1)^{Σ_{k<j} n_k} |...0_j...⟩
  - a_j |...0_j...⟩ = 0  (can't remove what isn't there)
- **Worked examples:**
  - a†_0 |0000⟩ = |1000⟩  (no sign: zero occupied orbitals before 0)
  - a†_1 |1000⟩ = −|1100⟩  (sign: one occupied orbital before 1)
  - a†_0 |1000⟩ = 0  (orbital 0 already occupied)
- Number operator: n̂_j = a†_j a_j
  - n̂_j |...1_j...⟩ = |...1_j...⟩   (eigenvalue 1)
  - n̂_j |...0_j...⟩ = 0             (eigenvalue 0)

**Box:** "The sign factor (−1)^{Σ n_k} is the source of ALL the complexity
in fermion-to-qubit encoding.  Without it, qubits and fermions would be the
same thing."

#### 3.4 The Anti-Commutation Relations (0.5 pages)

- State them:
  - {a_i, a†_j} ≡ a_i a†_j + a†_j a_i = δ_{ij}
  - {a_i, a_j} = 0
  - {a†_i, a†_j} = 0
- Physical meaning: you can't create two electrons in the same state (Pauli exclusion)
- Contrast with bosons (commutation: [b_i, b†_j] = δ_{ij})
- "These relations are the *definition* of fermions.  Any encoding must preserve them."

#### 3.5 The Hamiltonian in Second Quantization (1 page)

- General form:
  H = Σ_{pq} h_{pq} a†_p a_q  +  ½ Σ_{pqrs} h_{pqrs} a†_p a†_q a_s a_r
- h_{pq}: one-electron integrals (kinetic energy + electron-nucleus attraction)
  - "How much energy does an electron in orbital p have, and how easily can
    it hop to orbital q?"
- h_{pqrs}: two-electron integrals (electron-electron repulsion)
  - "How much do electrons in orbitals p,q repel each other when they scatter
    into orbitals r,s?"
- **Worked example for H₂:**
  - Non-zero one-body integrals: h_{00}, h_{11} (diagonal — orbital energies)
  - "The off-diagonal elements h_{01} = h_{10} = 0 because σ_g and σ_u have
    different symmetry in STO-3G."

**Dependencies:**
- Need explicit h_{pq} values from `H2Demo.fsx`


### Section 4: The Notation Minefield (3 pages)

**Goal:** This section saves the reader weeks of confusion.  There are at
least 3 incompatible notations for two-electron integrals in common use.
We lay them all out, show the conversions, and flag every trap.

#### 4.1 Chemist's Notation [pq|rs] (0.5 pages)

- Definition: [pq|rs] = ∫∫ φ*_p(r₁) φ_q(r₁) (1/r₁₂) φ*_r(r₂) φ_s(r₂) dr₁ dr₂
- "Chemists group by *coordinate*: pq share r₁, rs share r₂"
- Also called "charge-density notation" or "Mulliken notation"

#### 4.2 Physicist's Notation ⟨pq|rs⟩ (0.5 pages)

- Definition: ⟨pq|rs⟩ = ∫∫ φ*_p(r₁) φ*_q(r₂) (1/r₁₂) φ_r(r₁) φ_s(r₂) dr₁ dr₂
- "Physicists group by *particle*: p and r are the same electron,
  q and s are the same electron"
- Also called "Dirac notation" (confusingly, not the same as bra-ket notation)

#### 4.3 The Conversion (0.5 pages)

- **⟨pq|rs⟩_physicist = [pr|qs]_chemist**
- "The indices get SHUFFLED.  This is the #1 source of sign errors in
  quantum chemistry papers."
- Explicit example for H₂:
  - ⟨00|11⟩ = [01|01] (physicist → chemist)
  - [00|11] = ⟨01|01⟩ (chemist → physicist)
  - "These are DIFFERENT integrals!"
- Mnemonics: "In physicist's notation, the bra indices (p,q) are on the LEFT
  of both particles; in chemist's notation, each bracket [pq| has one bra
  and one ket for the SAME particle."

#### 4.4 The Hamiltonian: Which Notation? (0.5 pages)

- The second-quantized Hamiltonian uses physicist's notation:
  H = ... + ½ Σ ⟨pq|rs⟩ a†_p a†_q a_s a_r
- **WARNING:** the a operators are in the order p, q, s, r — NOT p, q, r, s!
  - a_s comes before a_r because of normal ordering
  - Getting this wrong flips signs
- Antisymmetrized integrals: ⟨pq||rs⟩ = ⟨pq|rs⟩ − ⟨pq|sr⟩
  - Some textbooks use these; we don't (to keep things explicit)

#### 4.5 Summary Table (0.5 pages)

| Notation | Convention | Integral | Hamiltonian term |
|----------|-----------|----------|-----------------|
| Chemist [pq\|rs] | Group by coordinate | [pq\|rs] | ½ Σ [pr\|qs] a†_p a†_q a_s a_r |
| Physicist ⟨pq\|rs⟩ | Group by particle | ⟨pq\|rs⟩ | ½ Σ ⟨pq\|rs⟩ a†_p a†_q a_s a_r |
| Antisymmetrized | Exchange-subtracted | ⟨pq\|\|rs⟩ | ½ Σ ⟨pq\|\|rs⟩ a†_p a†_q a_s a_r |

#### 4.6 Common Errors (0.5 pages)

- **Error 1:** Using chemist's integrals with the physicist's Hamiltonian formula
  (or vice versa).  Result: wrong by an index permutation.
- **Error 2:** Forgetting the ½ prefactor on two-body terms.  Result: double-counting.
- **Error 3:** Operator ordering a†_p a†_q a_r a_s instead of a†_p a†_q a_s a_r.
  Result: wrong sign on some terms.

**Figures:**
- Fig. 3: Side-by-side comparison of chemist vs. physicist integral layout

**Dependencies:** None (pure exposition)


### Section 5: From Spatial to Spin Orbitals (3 pages)

**Goal:** Take the spatial-orbital integrals for H₂ and expand them into the
spin-orbital basis.  This is where the 2×2 problem becomes 4×4, and where
most implementations get confused by index conventions.

#### 5.1 Spin-Orbital Indexing (0.5 pages)

- Each spatial orbital p produces two spin-orbitals: p↑ (alpha) and p↓ (beta)
- Indexing convention (interleaved):
  - Spin-orbital 0 = spatial orbital 0, spin α
  - Spin-orbital 1 = spatial orbital 0, spin β
  - Spin-orbital 2 = spatial orbital 1, spin α
  - Spin-orbital 3 = spatial orbital 1, spin β
- "Spatial orbital index = spin-orbital index ÷ 2 (integer division)"
- "Spin = spin-orbital index mod 2  (0 = α, 1 = β)"

#### 5.2 One-Body Expansion (1 page)

- Rule: h_spin[p,q] = h_spatial[p/2, q/2] × δ(spin_p, spin_q)
- "The spin-orbital integral equals the spatial integral IF the spins match,
  and zero otherwise."
- **Full expansion for H₂:**
  - h[0,0] = h_spatial[0,0] = −1.2563 (orbital 0α ↔ orbital 0α, same spin ✓)
  - h[1,1] = h_spatial[0,0] = −1.2563 (orbital 0β ↔ orbital 0β, same spin ✓)
  - h[0,1] = 0 (orbital 0α ↔ orbital 0β — different spin ✗)
  - h[2,2] = h_spatial[1,1] = −0.4719 (orbital 1α ↔ orbital 1α ✓)
  - h[3,3] = h_spatial[1,1] = −0.4719 (orbital 1β ↔ orbital 1β ✓)
  - All off-diagonal spatial terms are zero for H₂ STO-3G
- "We get 4 non-zero one-body spin-orbital integrals, all diagonal."

#### 5.3 Two-Body Expansion (1 page)

- Rule: ⟨pq|rs⟩_spin = [p/2, r/2 | q/2, s/2]_spatial × δ(spin_p, spin_r) × δ(spin_q, spin_s)
- "Both electrons must conserve spin independently.  Electron 1 (indices p,r)
  must have the same spin before and after.  Electron 2 (indices q,s) likewise."
- **Key insight:** This generates MORE non-zero integrals than you'd expect,
  because cross-spin terms like ⟨0α, 1β | 0α, 1β⟩ are allowed
  (spin_p = spin_r = α, spin_q = spin_s = β).
- **Common Error:** "If you only include same-spin terms (αα and ββ),
  you miss the cross-spin exchange.  Your Hamiltonian will have only Z-type
  (diagonal) terms and no XX/YY excitation terms.  The resulting energy
  eigenvalues will be wrong."
  (This was our actual bug in the first implementation.)
- **Explicit list of all non-zero two-body spin-orbital integrals for H₂**
  (there are 24 of them by symmetry)

#### 5.4 The Complete Spin-Orbital Hamiltonian (0.5 pages)

- Write out all non-zero terms of:
  H = Σ h[p,q] a†_p a_q + ½ Σ ⟨pq|rs⟩ a†_p a†_q a_s a_r
  + V_nn (nuclear repulsion constant = 0.7151 Ha)
- Count: how many operator products are there?
  - One-body: 4 terms (diagonal)
  - Two-body: 24 terms (but many will cancel or combine after encoding)

**Figures:**
- Fig. 4: Spatial → spin-orbital expansion diagram (2 orbitals → 4 spin-orbitals)
- Fig. 5: Table of all 24 non-zero two-body integrals

**Dependencies:**
- Values from `H2Demo.fsx` (h1_spatial, h2_spatial, h1_spin, h2_spin)
- Need verification script: manually compute all 24 integrals


### Section 6: Qubits and the Encoding Problem (4 pages)

**Goal:** Explain why fermions and qubits are algebraically different, and
why we need an encoding.  Teach the Jordan-Wigner transform completely.

#### 6.1 Qubits 101 (1 page)

- A qubit: |0⟩, |1⟩, superpositions α|0⟩ + β|1⟩
- Multi-qubit states: tensor products |q₀ q₁ q₂ q₃⟩
- Pauli operators: I, X, Y, Z on a single qubit
  - X = bit flip, Z = phase flip, Y = iXZ
  - Explicit 2×2 matrices
- Multi-qubit Paulis: tensor products like X⊗Z⊗I⊗Y = "XZIY"
  - "A Pauli string is a shorthand for a 2^n × 2^n matrix"
- Pauli algebra: Paulis either commute or anti-commute
  - {X, Y} = 0, {X, Z} = 0, {Y, Z} = 0 (anti-commute on same qubit)
  - [X_i, Z_j] = 0 when i ≠ j (commute on different qubits)

**Box:** "Fermions anti-commute on ALL modes.  Qubits anti-commute on the
SAME qubit but commute on DIFFERENT qubits.  This mismatch is the whole problem."

#### 6.2 The Obvious (Wrong) Mapping (0.5 pages)

- Tempting: set a†_j = (X_j − iY_j)/2, a_j = (X_j + iY_j)/2
  (these are the qubit lowering/raising operators σ±)
- This satisfies {a_j, a†_j} = 1 ✓
- But {a_0, a†_1} = 0?  Let's check:
  a_0 a†_1 + a†_1 a_0 = σ⁺_0 σ⁻_1 + σ⁻_1 σ⁺_0 ≠ 0 in general!
  "The qubit operators on different sites COMMUTE, not anti-commute."
- "We need to add extra structure to enforce anti-commutation across sites."

#### 6.3 The Jordan-Wigner Transform (1.5 pages)

- The fix: insert a "parity string" of Z operators on all lower-index qubits
- Definition:
  a†_j = (1/2)(X_j − iY_j) ⊗ Z_{j-1} ⊗ Z_{j-2} ⊗ ... ⊗ Z_0
  a_j  = (1/2)(X_j + iY_j) ⊗ Z_{j-1} ⊗ Z_{j-2} ⊗ ... ⊗ Z_0
- Equivalently using Majorana decomposition:
  a†_j = ½(c_j − id_j),  a_j = ½(c_j + id_j)
  c_j = X_j Z_{j-1} Z_{j-2} ... Z_0
  d_j = Y_j Z_{j-1} Z_{j-2} ... Z_0
- **Why this works:**
  - Check {a_0, a†_0} = 1: the Z-chains cancel (no Zs for j=0)
  - Check {a_0, a†_1}: the Z_0 on a†_1 anti-commutes with the X_0 or Y_0
    from a_0, giving the needed minus sign to make the sum vanish
  - "The Z-chain is a 'Jordan-Wigner string' that keeps track of the
    fermionic parity"
- **Worked examples for n = 4:**
  - a†_0 = ½(XIIII − iYIII)... wait, only 4 qubits.
  - a†_0: c = XIII, d = YIII → a†_0 = ½(XIII) + ½(-i)(YIII)
  - a†_1: c = ZXII, d = ZYII → a†_1 = ½(ZXII) + ½(-i)(ZYII)
  - a†_2: c = ZZXI, d = ZZYI
  - a†_3: c = ZZZX, d = ZZZY
- **The problem:** a†_3 has Pauli weight 4 (all non-identity).
  For n = 100, a†_{99} would have weight 100.  O(n) scaling.

#### 6.4 Beyond Jordan-Wigner (1 page)

- "Can we do better?  Yes — by choosing a different structure."
- Brief preview of Bravyi-Kitaev: O(log n) weight via Fenwick tree
- Brief preview of the tree-encoding principle: every tree gives a valid encoding
- "We'll use JW for the rest of this paper (it's simplest), but know that
  better encodings exist for larger molecules."
- Pointer to Paper 3 for the full story

**Figures:**
- Fig. 6: Jordan-Wigner Z-chain diagram (qubits as circles, Z-string as a line)
- Fig. 7: Pauli weight scaling: JW vs. BK vs. ternary tree (simple bar chart for n=4,8,16)

**Dependencies:**
- Encoding library for JW examples
- ScalingBenchmark.fsx data for the bar chart


### Section 7: Building the H₂ Qubit Hamiltonian (5 pages)

**Goal:** The payoff.  Take everything from Sections 2–6 and actually build
the qubit Hamiltonian for H₂ step by step.

#### 7.1 The Recipe (0.5 pages)

1. Start with the spin-orbital Hamiltonian (from Section 5)
2. For each term h_{pq} a†_p a_q:
   - Encode a†_p and a_q as Pauli strings (JW from Section 6)
   - Multiply the Pauli strings together
   - Multiply by the coefficient h_{pq}
3. For each term ½⟨pq|rs⟩ a†_p a†_q a_s a_r:
   - Encode all four operators
   - Multiply the four Pauli strings together
   - Multiply by ½⟨pq|rs⟩
4. Add all terms.  Collect like Pauli strings (combine coefficients).
5. Add nuclear repulsion V_nn × I⊗I⊗I⊗I

#### 7.2 One-Body Terms (1.5 pages)

- There are 4 non-zero one-body terms for H₂:
  h[0,0] a†_0 a_0,  h[1,1] a†_1 a_1,  h[2,2] a†_2 a_2,  h[3,3] a†_3 a_3
- These are all number operators n̂_j = a†_j a_j
- JW encoding of n̂_j:
  n̂_j = a†_j a_j = ½(c_j − id_j) · ½(c_j + id_j) = ½(I − Z_j · [parity correction])
  For JW specifically: n̂_j = ½(I - Z_j)
- So the one-body part becomes:
  h[0,0]·½(I − Z_0) + h[1,1]·½(I − Z_1) + h[2,2]·½(I − Z_2) + h[3,3]·½(I − Z_3)
- Expand and collect:
  = ½(h[0,0]+h[1,1]+h[2,2]+h[3,3])·IIII − ½h[0,0]·IIIZ − ½h[1,1]·IIZI
    − ½h[2,2]·IZII − ½h[3,3]·ZIII
- Plug in numbers: coefficient table

#### 7.3 Two-Body Terms (2 pages)

- There are 24 non-zero two-body terms, but by symmetry they group
- Show 2–3 representative calculations in full detail:
  - ½⟨00|00⟩ a†_0 a†_0 a_0 a_0 = 0  (can't create in same orbital twice!)
  - ½⟨01|01⟩ a†_0 a†_1 a_1 a_0: encode all 4 operators, multiply Pauli strings
    step by step, simplify
  - A cross-spin exchange term showing how XX/YY operators arise
- Summarize the pattern: two-body terms produce ZZ, ZI, IZ, and XXYY/XYYX/YXXY/YYXX
- "The ZZ terms represent electron-electron repulsion (classical Coulomb).
  The XX/YY terms represent *quantum exchange* — a fundamentally quantum effect
  with no classical analogue."

#### 7.4 The Final Hamiltonian (1 page)

- Collect all terms, add nuclear repulsion
- **Table:** All 15 non-zero Pauli terms with their coefficients
- Identity coefficient = -1.0704 Ha (electronic) → −0.3553 Ha (with V_nn)
- Verification: this matches Seeley et al. (arXiv:1208.5986) and OpenFermion
- "This 15-term, 4-qubit Hamiltonian is the COMPLETE electronic structure
  of H₂ in the STO-3G basis.  A quantum computer that can measure these
  15 terms can determine the ground-state energy of the hydrogen molecule."

**Figures:**
- Fig. 8: Complete H₂ qubit Hamiltonian term table (signature + coefficient)
- Fig. 9: Categorisation of terms: diagonal (Z-type) vs. off-diagonal (XX/YY)

**Dependencies:**
- H2Demo.fsx output (all 15 terms with coefficients)
- Need: matrix eigenspectrum verification script (from .research/tools/)
  to confirm eigenvalues match classical FCI


### Section 8: Checking Our Answer (2 pages)

**Goal:** Verify the Hamiltonian by computing eigenvalues and comparing
to known results.

#### 8.1 Exact Diagonalisation (1 page)

- Build the 16×16 matrix representation of the Hamiltonian
- Each Pauli string = a known 16×16 matrix (tensor products of 2×2 matrices)
- Sum them with coefficients → full H matrix
- Diagonalise (eigenvalues)
- Ground state energy: E₀ ≈ −1.8573 Ha (total) = −1.1373 Ha (electronic) + V_nn
  (This is the Full Configuration Interaction energy in STO-3G basis)

#### 8.2 Comparison to Experiment and Classical Methods (1 page)

- Hartree-Fock (single determinant): E_HF = −1.1168 Ha (electronic)
- Full CI (our exact answer):        E_FCI = −1.1373 Ha
- Correlation energy = E_FCI − E_HF = −0.0205 Ha ≈ −12.9 kcal/mol
  "This 'missing energy' is what makes quantum simulation valuable —
  it captures electron correlation that mean-field theory misses."
- Experimental dissociation energy: D_e = 4.747 eV ≈ 0.1745 Ha
  Our computed D_e: E(R→∞) − E(R_eq) = ... (compute from two data points)
- "STO-3G is the smallest possible basis set — our answer is approximate
  but qualitatively correct."

**Figures:**
- Fig. 10: Energy eigenvalue spectrum (16 eigenvalues, grouped by particle number sector)
- Fig. 11: H₂ dissociation curve (energy vs. R, with HF and FCI curves)

**Dependencies:**
- MatrixVerification.fsx (to be built in .research/tools/)
- Need eigenvalue data at multiple bond lengths for dissociation curve


### Section 9: What Comes Next (2 pages)

**Goal:** Brief overview of quantum algorithms that use this Hamiltonian.
NOT a tutorial on VQE/QPE — just enough to show the student where the
pipeline leads.

- 9.1 Variational Quantum Eigensolver (VQE): 1 paragraph + diagram
  - Prepare a parameterised state, measure ⟨H⟩, optimise parameters
  - "NISQ-friendly": short circuits, handles noise
- 9.2 Quantum Phase Estimation (QPE): 1 paragraph + diagram
  - Requires fault-tolerant qubits but gives exponential speedup
  - "The long-term goal"
- 9.3 Scaling to larger molecules
  - Basis sets: STO-3G → 6-31G → cc-pVDZ → cc-pVTZ
  - Active space selection: freeze core orbitals, focus on valence
  - Resource estimates: LiH (6 qubits), H₂O (14 qubits), FeMoco (~100 qubits)

### Section 10: Conclusion (1 page)

- Recap the pipeline
- What we accomplished: H₂ from scratch to eigenvalues, by hand
- Where to go next: our code, OpenFermion, Qiskit Nature
- Pointer to Paper 3 (the encoding as emergent structure)

### Appendix A: H₂ STO-3G Integral Tables (2 pages)
- Complete one-body integrals (spatial + spin-orbital)
- Complete two-body integrals (spatial, all symmetries; spin-orbital, all 24 non-zero)
- Nuclear repulsion energy

### Appendix B: Pauli Algebra Reference (1 page)
- Single-qubit: multiplication table, commutation/anti-commutation
- Multi-qubit: tensor product rules, signature notation

### Appendix C: Code Companion (1 page)
- Repository URL, build instructions
- Key functions: `jordanWignerTerms`, `bravyiKitaevTerms`
- "Run `dotnet fsi H2Demo.fsx` to reproduce all results in this paper"

---

## Dependent Components (must build before writing)

| Component | Location | Purpose | Status |
|-----------|----------|---------|--------|
| MatrixVerification.fsx | .research/tools/ | Build 16×16 matrices, diagonalise, eigenspectrum | 🔲 |
| DissociationCurve.fsx | .research/tools/ | H₂ energy at multiple bond lengths | 🔲 |
| IntegralTables.fsx | .research/paper-tutorial/ | Generate all integral tables for appendix | 🔲 |
| Fig. 1: Pipeline | .research/paper-tutorial/figures/ | TikZ or SVG pipeline diagram | 🔲 |
| Fig. 2: Orbitals | .research/paper-tutorial/figures/ | H₂ orbital diagram | 🔲 |
| Fig. 3: Notation | .research/paper-tutorial/figures/ | Chemist vs. physicist comparison | 🔲 |
| Fig. 6: JW string | .research/paper-tutorial/figures/ | Z-chain visualization | 🔲 |
| Fig. 8: Hamiltonian | .research/paper-tutorial/figures/ | 15-term table | 🔲 |
| Fig. 10: Eigenvalues | .research/paper-tutorial/figures/ | Eigenspectrum plot | 🔲 |
| Fig. 11: Dissociation | .research/paper-tutorial/figures/ | E vs. R curve | 🔲 |

---

## Writing Checklist

### Phase 1: Foundations (can be drafted now)
- [ ] Section 1 (Introduction)
- [ ] Section 2 (Electronic structure)
- [ ] Section 3 (Second quantization)
- [ ] Section 4 (Notation minefield)

### Phase 2: The Core (requires integral tables)
- [ ] Section 5 (Spatial → spin-orbital)
- [ ] Appendix A (Integral tables)

### Phase 3: The Encoding (requires library verification)
- [ ] Section 6 (Encoding problem + JW)
- [ ] Appendix B (Pauli algebra)

### Phase 4: The Payoff (requires matrix verification)
- [ ] Section 7 (Building the Hamiltonian)
- [ ] Section 8 (Checking our answer — eigenvalues)
- [ ] MatrixVerification.fsx tool

### Phase 5: Horizon + Polish (requires dissociation curve)
- [ ] Section 9 (What comes next)
- [ ] Section 10 (Conclusion)
- [ ] Appendix C (Code companion)
- [ ] DissociationCurve.fsx tool

### Phase 6: Figures + Final
- [ ] All 11 figures created
- [ ] References compiled
- [ ] Full draft assembled and internally consistent
- [ ] Notation checked for consistency throughout
- [ ] All "worked example" calculations verified against code
- [ ] Internal review pass
- [ ] Format for AJP submission

---

## Key Numbers to Verify Before Submitting

| Quantity | Expected Value | Source |
|----------|---------------|--------|
| h₁[0,0] (spatial, σ_g) | −1.2563390730032498 Ha | O'Malley et al. |
| h₁[1,1] (spatial, σ_u) | −0.4718960244306283 Ha | O'Malley et al. |
| [00\|00] | 0.6744887663049631 | O'Malley et al. |
| [11\|11] | 0.6973979494693556 | O'Malley et al. |
| [00\|11] | 0.6636340478615040 | O'Malley et al. |
| [01\|10] | 0.6975782468828187 | O'Malley et al. |
| V_nn | 0.7151043390810812 Ha | O'Malley et al. |
| # Pauli terms (JW) | 15 | Computed (H2Demo.fsx) |
| Identity coefficient | −1.0704 Ha (electronic) | Computed |
| E₀ (FCI, electronic) | ≈ −1.1373 Ha | To verify via diagonalisation |
| E₀ (total) | ≈ −1.8573 + 0.7151 Ha | To verify |
| Correlation energy | ≈ −0.0205 Ha | FCI − HF |
