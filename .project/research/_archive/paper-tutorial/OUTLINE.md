# Paper 1 — From Molecules to Qubits
## A Complete Guide to Quantum Chemistry Simulation

**Target journal:** American Journal of Physics / Quantum (pedagogical review)
**Audience:** Graduate students, physicists entering quantum computing
**Length:** ~30 pages
**Tone:** Textbook-quality, every step shown

---

## Thesis

A self-contained, step-by-step guide that takes the reader from the
Schrödinger equation to a qubit Hamiltonian, exposing every notation
conversion, integral expansion, and encoding choice along the way.
The H₂ molecule in STO-3G serves as a running worked example.

---

## Outline

### 1. Introduction
- The quantum simulation promise (Feynman's vision → NISQ reality)
- Why this guide exists: the gap between textbooks and papers
- What the reader will be able to do by the end

### 2. The Electronic Structure Problem
- 2.1 Born-Oppenheimer approximation
  - Nuclear vs. electronic degrees of freedom
  - The electronic Hamiltonian
- 2.2 Basis sets and spatial orbitals
  - Slater-type vs. Gaussian-type orbitals
  - STO-3G as the minimal example
  - What a "basis set" actually means (LCAO)
- 2.3 Worked example: H₂ spatial orbitals
  - σ_g (bonding) and σ_u (antibonding)
  - Symmetry and physical intuition

### 3. Second Quantization
- 3.1 Why second quantization?
  - Antisymmetry of fermions (Slater determinants are unwieldy)
  - Fock space: variable particle number
- 3.2 Creation and annihilation operators
  - Definition, action on Fock states
  - The canonical anti-commutation relations (CAR)
  - Normal ordering
- 3.3 The molecular Hamiltonian in second quantization
  - One-body: h_{pq} a†_p a_q
  - Two-body: ½ ⟨pq|rs⟩ a†_p a†_q a_s a_r
  - The notation minefield (Section 3.4)
- 3.4 **The Notation Minefield** (key pedagogical contribution)
  - Table: chemist [pq|rs] vs. physicist ⟨pq|rs⟩ vs. Dirac ⟨pq||rs⟩
  - Conversion formulas with explicit index mappings
  - Common errors and how to catch them

### 4. From Spatial to Spin-Orbital Integrals
- 4.1 Spin-orbital indexing conventions
  - Interleaved (0α, 0β, 1α, 1β) vs. blocked (0α, 1α, 0β, 1β)
  - Advantages of each; we use interleaved
- 4.2 One-body expansion
  - h_spin[p,q] = h_spatial[p/2, q/2] · δ(spin_p, spin_q)
  - Step-by-step for H₂: 2×2 → 4×4
- 4.3 Two-body expansion
  - ⟨pq|rs⟩_physicist = [pr|qs]_chemist
  - Spin constraints: spin_p = spin_r AND spin_q = spin_s
  - Step-by-step for H₂: 2222 → 4444
  - **Common error:** forgetting cross-spin exchange terms
    (our actual bug — only diagonal terms, no XX/YY excitations)
- 4.4 The full H₂ spin-orbital Hamiltonian
  - All non-zero integrals listed explicitly

### 5. The Encoding Problem
- 5.1 Fermions live in Fock space, qubits live in Hilbert space
  - Both are 2^n dimensional — but the algebras differ
  - The challenge: preserve anti-commutation
- 5.2 Jordan-Wigner transform
  - Derivation from scratch
  - The Z-chain: why locality is lost
  - Worked example: a†_0 in 4 qubits
- 5.3 Bravyi-Kitaev transform
  - The Fenwick tree idea
  - Update, parity, occupation sets
  - Why weight drops to O(log n)
- 5.4 Other encodings (brief overview)
  - Parity, balanced binary tree, balanced ternary tree
  - The tree-as-encoding principle (preview of Paper 3)

### 6. Building the Qubit Hamiltonian
- 6.1 Recipe: integrals + encoding → Pauli strings
  - One-body terms: Σ h_{pq} · (encode a†_p) · (encode a_q)
  - Two-body terms: ½ Σ ⟨pq|rs⟩ · (encode a†_p)(encode a†_q)(encode a_s)(encode a_r)
  - Collecting like terms
- 6.2 Worked example: H₂ with Jordan-Wigner
  - Every term computed explicitly
  - Final 15-term Hamiltonian
  - Identity coefficient = electronic energy offset
- 6.3 Verification
  - Cross-check: same Hamiltonian from all encodings
  - Eigenvalue computation (exact diagonalization for small systems)
  - Ground state energy → dissociation curve

### 7. What Comes Next
- 7.1 Variational Quantum Eigensolver (VQE) — overview
- 7.2 Quantum Phase Estimation — overview
- 7.3 Scaling to larger molecules
  - Basis set hierarchy: STO-3G → 6-31G → cc-pVDZ → ...
  - Active space selection
  - Resource estimation

### 8. Conclusion
- Summary of the pipeline
- Where to find implementations (our library, OpenFermion, Qiskit Nature)
- Invitation to Paper 3 (the encoding as emergent structure)

### Appendix A: H₂ STO-3G Integral Tables
### Appendix B: Pauli Algebra Reference
### Appendix C: Implementation Notes (link to repository)

---

## Key Figures Needed

1. Pipeline diagram: molecule → basis → integrals → 2nd quant → encoding → qubits
2. Fock space diagram for 2 modes (4 basis states)
3. Spatial → spin-orbital expansion diagram
4. Jordan-Wigner Z-chain visualization
5. Fenwick tree for n = 8
6. H₂ qubit Hamiltonian term table
7. H₂ dissociation curve (energy vs. bond length)

---

## Status

- [ ] Outline finalized
- [ ] Section 2 drafted (electronic structure)
- [ ] Section 3 drafted (second quantization)
- [ ] Section 4 drafted (spin-orbital expansion)
- [ ] Section 5 drafted (encoding problem)
- [ ] Section 6 drafted (building the Hamiltonian)
- [ ] Figures created
- [ ] Verification scripts complete
- [ ] Full draft assembled
- [ ] Internal review
