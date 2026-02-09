# Paper 2 — Algebraic Encodings
## A Typed Functional Framework for Fermion-to-Qubit Mappings

**Target journal:** Journal of Open Source Software (JOSS) / SoftwareX
**Audience:** Quantum computing software developers
**Length:** ~10 pages + repository link
**Tone:** Technical, API-focused, reproducibility-oriented

---

## Thesis

Algebraic data types, parametric polymorphism, and pure functions are a
natural fit for the fermion-to-qubit encoding domain.  The `EncodingScheme`
record type captures the mathematical structure directly, enabling
correct-by-construction encodings with compile-time guarantees that
dynamically-typed frameworks cannot provide.

---

## Outline

### 1. Introduction
- The fermion-to-qubit encoding problem (brief)
- Existing tools: OpenFermion (Python), Qiskit Nature (Python), PennyLane
- Gap: no typed/functional implementation; design patterns matter
- Contribution: F# library exposing algebraic structure

### 2. Design Principles
- 2.1 Types as documentation
  - `Pauli`, `LadderOperatorUnit`, `PauliRegister`, `PauliRegisterSequence`
  - Discriminated unions prevent invalid states
- 2.2 Immutability and referential transparency
  - All operations return new values
  - FenwickTree: immutable `update` returns new tree
  - No hidden mutation → easier to reason about correctness
- 2.3 Higher-order encodings
  - `EncodingScheme = { Update; Parity; Occupation }`
  - Swap the three functions → get a different encoding
  - Same verification pipeline for all index-set encodings
- 2.4 Parametric polymorphism
  - `FenwickTree<'a>` works for int, XOR, Set.union, ...
  - BK index sets = special case of prefix/point queries on Set-union tree

### 3. Architecture
- 3.1 Type hierarchy and compilation order
  - Diagram: dependency graph of 15 source files
  - F#'s explicit ordering as enforced modularity
- 3.2 Core types
  - Table of types, their files, and their roles
- 3.3 The two encoding frameworks
  - Index-set (MajoranaEncoding.fs): for JW, BK, Parity
  - Path-based (TreeEncoding.fs): for balanced binary/ternary trees
  - Why two: the monotonicity constraint (preview of Paper 3)
- 3.4 Hamiltonian construction pipeline
  - `EncoderFn` type alias
  - Composing ladder operators → Pauli products → collected terms

### 4. API Reference (condensed)
- 4.1 Encoding a single operator
  ```fsharp
  jordanWignerTerms  Raise 0u 4u  // → PauliRegisterSequence
  bravyiKitaevTerms  Raise 0u 4u
  parityTerms        Raise 0u 4u
  ternaryTreeTerms   Raise 0u 4u
  ```
- 4.2 Building a Hamiltonian
  ```fsharp
  let hop = (encode Raise i n) * (encode Lower j n)
  let ham = hop.DistributeCoefficient
  ```
- 4.3 Defining a custom encoding
  ```fsharp
  let myScheme : EncodingScheme =
      { Update = fun j n -> ...
        Parity = fun j   -> ...
        Occupation = fun j -> ... }
  ```
- 4.4 Working with trees
  ```fsharp
  let tree = balancedTernaryTree 8
  let result = encodeWithTernaryTree tree Raise 3u 8u
  ```

### 5. Verification & Testing
- 5.1 Test architecture: 303 tests, xUnit + FsCheck
  - Unit tests: hand-verified small cases
  - Property-based tests: anti-commutation for random modes
  - Cross-validation: all encodings agree on physical observables
- 5.2 Anti-commutation as the ground truth
  - {a_i, a†_j} = δ_{ij} tested for all (i,j) pairs
  - Catches encoding bugs invisible to single-operator tests
- 5.3 H₂ molecular validation
  - Same eigenspectrum across all encodings (matrix-level)

### 6. Comparison with Existing Tools
- Table: feature comparison vs. OpenFermion, Qiskit Nature, PennyLane
  - Type safety, immutability, encoding coverage, test depth
- What this library adds; what it doesn't (no circuit synthesis, no VQE)

### 7. Conclusion & Availability
- Repository URL, license, installation instructions
- Future: circuit synthesis, larger molecules, Python bindings?

---

## Key Figures Needed

1. Type dependency graph (15 modules)
2. `EncodingScheme` as a functor diagram
3. Code snippet: defining + using a custom encoding
4. Test coverage summary

---

## JOSS Requirements Checklist

- [ ] Statement of need
- [ ] Installation instructions
- [ ] Example usage
- [ ] API documentation
- [ ] Community guidelines (contributing, code of conduct)
- [ ] Tests with CI
- [ ] License (MIT/Apache)
- [ ] Zenodo DOI for archive

---

## Status

- [ ] Outline finalized
- [ ] Statement of need drafted
- [ ] Architecture section drafted
- [ ] API reference drafted
- [ ] Comparison table completed
- [ ] Repository cleaned for public release
- [ ] CI/CD setup (GitHub Actions)
- [ ] JOSS paper.md drafted
