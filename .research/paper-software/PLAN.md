# Paper 2 — Software Paper
## A Composable Functional Framework for Fermion-to-Qubit Encodings

---

### Metadata

| Field | Value |
|-------|-------|
| **Title** | FockMap: A Composable Functional Framework for Fermion-to-Qubit Encodings in F# |
| **Target** | Journal of Open Source Software (JOSS) |
| **Audience** | Quantum computing researchers and software developers |
| **Assumed knowledge** | Fermion-to-qubit encodings (JW, BK), basic quantum computing, functional programming concepts |
| **Length** | ~4 pages (JOSS is short — 1000 words max for the paper itself) + repository documentation |
| **Companion** | Full F# library with 303+ tests, extensive README, API docs |
| **Key contribution** | First library to unify ALL encodings (index-set + path-based) under two composable abstractions, with pure functional ADTs |

---

### JOSS Requirements Checklist

JOSS has strict acceptance criteria. We must satisfy ALL of these:

#### Mandatory for Submission
- [ ] **Open-source license**: Add LICENSE file (MIT or Apache-2.0)
- [ ] **Version-controlled repository** on GitHub/GitLab (currently local)
- [ ] **Archived version** (Zenodo DOI)
- [ ] **README.md** with:
  - [ ] Installation instructions
  - [ ] Minimal usage examples
  - [ ] Dependencies listed
  - [ ] Link to full documentation
- [ ] **Contributing guidelines** (CONTRIBUTING.md)
- [ ] **Statement of Need**: Why does this software exist? (in paper)
- [ ] **Functionality Documentation**: All public API documented
- [ ] **Automated tests**: 303 tests passing ✅ (already done)
- [ ] **Community guidelines**: How to report issues, seek support

#### Mandatory for Review
- [ ] **Installation**: Can a reviewer `dotnet build` and `dotnet test` it?
- [ ] **Example**: At least one runnable example (H2Demo.fsx ✅)
- [ ] **Tests**: All tests pass on reviewer's machine
- [ ] **State of the field**: Comparison with existing tools
- [ ] **Quality of writing**: Concise, clear, free of errors

---

### Why This Paper Exists

Existing quantum chemistry encoding libraries (OpenFermion, Qiskit Nature, PennyLane)
all implement encodings as procedural transforms: functions that take a fermionic
operator and return a qubit operator.  Each encoding is a separate code path with
no shared structure.

Our library takes a fundamentally different approach:

1. **Encodings are DATA, not code.**
   An `EncodingScheme` is a record of three set-valued functions:
   ```
   { Update : int → int → Set<int>
     Parity : int → Set<int>
     Occupation : int → Set<int> }
   ```
   Jordan-Wigner, Bravyi-Kitaev, and Parity are all just different values
   of this type — 3–5 lines each.

2. **Two complementary frameworks.**
   - Index-set (MajoranaEncoding): 3 functions → automatic Majorana construction.
     Works for JW, BK, Parity, and any Fenwick-compatible tree.
   - Path-based (TreeEncoding): any rooted tree → automatic encoding.
     Works for ALL tree encodings, including those that violate index-set
     monotonicity (balanced binary, balanced ternary).

3. **Pure functional data structures.**
   The Fenwick tree is a persistent ADT, not a mutable array.
   Trees are immutable recursive types.  The library has zero mutation.

4. **Composability.**
   Want a new encoding?  Write 3 functions.  Want a new tree?  Build a `TreeNode`.
   The framework handles Majorana decomposition, Pauli string construction,
   anti-commutation, and coefficient tracking automatically.

No existing library offers this level of composability.

---

### Competitive Landscape

| Feature | OpenFermion | Qiskit Nature | PennyLane | **Ours** |
|---------|-----------|-------------|---------|------|
| Language | Python | Python | Python | F# |
| JW encoding | ✅ | ✅ | ✅ | ✅ |
| BK encoding | ✅ | ✅ | ✅ | ✅ |
| Parity encoding | ✅ | ✅ | ✅ | ✅ |
| Tree encodings | ✅ (Steiner trees) | ❌ | ❌ | ✅ (balanced binary, ternary) |
| User-defined encodings | ❌ | ❌ | ❌ | ✅ (EncodingScheme record) |
| User-defined trees | ❌ | ❌ | ❌ | ✅ (any TreeNode) |
| Pure functional ADTs | ❌ | ❌ | ❌ | ✅ |
| Composable framework | ❌ | ❌ | ❌ | ✅ |
| Fenwick tree as ADT | ❌ | ❌ | ❌ | ✅ |
| Automated tests | ✅ | ✅ | ✅ | ✅ (303 tests) |
| Eigenspectrum verification | ❌ | ❌ | ❌ | 🔲 (planned) |

**Key differentiators:**
1. Generic encoding abstraction (no other library has this)
2. Tree → encoding pipeline (build any tree, get an encoding automatically)
3. Purely functional (immutable, composable, testable)
4. Discovered and documented the index-set monotonicity constraint

---

## Detailed Section Plan

### Section 1: Summary (250 words)

JOSS papers start with a short Summary section.

**Content:**
- Fermion-to-qubit encodings are a critical component of quantum chemistry
  simulation on quantum computers.
- Different encodings (JW, BK, Parity, tree-based) trade off Pauli weight,
  circuit depth, and implementability.
- Existing libraries implement encodings as separate, non-composable transforms.
- We present FockMap, an F# library that unifies all known encodings
  under two composable abstractions: index-set schemes and path-based tree
  encodings.
- Users can define new encodings by supplying a record of three set functions
  or by specifying a tree structure.
- The library includes a pure functional Fenwick tree ADT, 5 built-in
  encodings, and 303 unit tests.
- A running example encodes the full H₂ STO-3G Hamiltonian.


### Section 2: Statement of Need (300 words)

**Content:**
- Context: Quantum simulation of chemistry is the killer app for quantum computers.
  Fermion-to-qubit encodings are the critical middleware layer.
- Problem: Current tools (OpenFermion, Qiskit Nature) implement each encoding
  as a monolithic function.  There is no shared abstraction.
  - Adding a new encoding requires writing hundreds of lines of code
  - There is no way to systematically explore the encoding space
  - The relationship between encodings (JW, BK, Parity all share Majorana
    structure) is obscured
- Solution: Our library factors out the common structure.
  - An encoding IS an `EncodingScheme` record: 3 functions determine everything
  - Any tree IS an encoding: build a `TreeNode`, get Pauli strings automatically
  - The framework handles all the algebra (Majorana decomposition, Pauli multiplication,
    phase tracking, anti-commutation bookkeeping)
- Who needs this:
  - Researchers exploring novel encodings (encoding-aware circuit synthesis)
  - Students learning about encodings (the code IS the math)
  - Developers building quantum chemistry simulation pipelines


### Section 3: Functionality (200 words)

**Content:**
Itemised list of capabilities:

1. **Encoding schemes:**
   - Jordan-Wigner (`jordanWignerScheme`)
   - Bravyi-Kitaev (`bravyiKitaevScheme`)
   - Parity (`parityScheme`)
   - Arbitrary user-defined schemes via `EncodingScheme` record

2. **Tree encodings:**
   - Balanced binary tree
   - Balanced ternary tree (O(log₃ n) optimal weight)
   - Arbitrary user-defined trees via `TreeNode` construction
   - Path-based encoding that works for ANY tree (no monotonicity constraint)

3. **Data structures:**
   - Pure functional Fenwick tree (`FenwickTree<'a>`)
   - Immutable tree ADT (`TreeNode`, `EncodingTree`)
   - Pauli register algebra (`PauliRegister`, `PauliRegisterSequence`)

4. **Utilities:**
   - Hamiltonian encoding from second-quantized operators
   - Term combining and simplification
   - H₂ STO-3G running example (`H2Demo.fsx`)
   - Scaling benchmark (`ScalingBenchmark.fsx`)


### Section 4: Design Principles (200 words)

**Content:**
1. **Encodings as data**: An `EncodingScheme` is a value, not a class hierarchy.
   This enables composition, comparison, and algebraic reasoning.

2. **Two complementary frameworks**: Index-set (fast, algebraically elegant) and
   path-based (universal, geometrically intuitive). Both produce the same output
   type (`PauliRegisterSequence`).

3. **Pure functions, no mutation**: All data structures are immutable.
   Trees, Fenwick trees, Pauli registers — everything is persistent.
   Side effects are confined to I/O (printing results).

4. **Test-driven correctness**: 303 tests verify:
   - Anti-commutation relations: {aᵢ, a†ⱼ} = δᵢⱼ
   - Number conservation: a†ⱼ aⱼ is diagonal
   - Cross-encoding agreement: all 5 encodings produce isospectral Hamiltonians
   - Edge cases: boundary modes, n=1, n=2

5. **Discovered constraints**: The index-set monotonicity requirement
   (ancestors must have index > node) was discovered through implementation
   and testing. This constraint rules out balanced trees via the index-set
   framework and motivated the path-based alternative.


### Section 5: Related Software (150 words)

**Content:**
- OpenFermion (Google): comprehensive Python library for quantum chemistry.
  Implements JW and BK as separate transforms. No generic encoding abstraction.
  No tree-based encodings built in (though fermion_to_qubit_mapping supports
  Steiner trees via an extension).
- Qiskit Nature (IBM): provides JW, BK, Parity.  Each is a distinct class.
  No user-extensibility mechanism for custom encodings.
- PennyLane (Xanadu): provides JW only in core; BK through qchem module.
  No tree-based encodings.
- InQuanto (Quantinuum): proprietary; provides various encodings but no
  composable framework.

**Our distinction:** We provide the FRAMEWORK, not just the encodings.
Adding a new encoding is 3–5 lines of code, not hundreds.


### Section 6: Acknowledgements + References

Standard JOSS section.

---

## Repository Preparation Checklist

The JOSS review process examines the repository, not just the paper.
The repository must be publication-ready:

### Files to Create
- [ ] **LICENSE** (MIT recommended for F# ecosystem)
- [ ] **README.md** (comprehensive, with badges)
  - [ ] Project overview
  - [ ] Installation: `dotnet build`
  - [ ] Quick start: 3-line encoding example
  - [ ] Full API reference table
  - [ ] Running tests: `dotnet test`
  - [ ] Running demos: `dotnet fsi H2Demo.fsx`
  - [ ] Architecture diagram (index-set vs. path-based)
  - [ ] Contributing section
  - [ ] Citation section (BibTeX)
- [ ] **CONTRIBUTING.md**
  - [ ] How to report bugs
  - [ ] How to propose features
  - [ ] Code style guide (F# conventions)
  - [ ] PR process
- [ ] **CITATION.cff** (Citation File Format)
- [ ] **paper.md** (JOSS paper in Markdown format)
- [ ] **paper.bib** (BibTeX references)
- [ ] **.github/workflows/ci.yml** (CI pipeline: build + test)
- [ ] **docs/** folder with API documentation
- [ ] **examples/** folder
  - [ ] `H2_Encoding.fsx` (minimal: encode H₂ with JW)
  - [ ] `Custom_Encoding.fsx` (show how to define your own EncodingScheme)
  - [ ] `Custom_Tree.fsx` (show how to build an arbitrary tree encoding)
  - [ ] `Compare_Encodings.fsx` (run all 5 encodings on H₂, compare)

### Code Changes Needed
- [ ] Add XML doc comments to ALL public functions
- [ ] Remove any debug/temporary code
- [ ] Add `[<CompiledName>]` attributes for C# interop (nice-to-have)
- [ ] Ensure consistent naming conventions
- [ ] Add module-level doc comments explaining purpose
- [ ] Verify all 303 tests still pass after cleanup

### Repository Hygiene
- [ ] Push to GitHub (public repository)
- [ ] Set up GitHub Actions CI
- [ ] Create initial GitHub release (v0.1.0)
- [ ] Archive on Zenodo (auto-archives from GitHub releases)
- [ ] Create issue templates (bug report, feature request)
- [ ] Add .editorconfig for consistent formatting

---

## Dependent Components

| Component | Location | Purpose | Status |
|-----------|----------|---------|--------|
| LICENSE file | repo root | MIT license | 🔲 |
| README.md | repo root | Comprehensive documentation | 🔲 |
| CONTRIBUTING.md | repo root | Contribution guidelines | 🔲 |
| CITATION.cff | repo root | Machine-readable citation | 🔲 |
| paper.md | .research/paper-software/ | JOSS paper source | 🔲 |
| paper.bib | .research/paper-software/ | BibTeX references | 🔲 |
| CI workflow | .github/workflows/ | Automated build + test | 🔲 |
| API docs | docs/ | Generated or hand-written | 🔲 |
| Examples | examples/ | 4 runnable F# scripts | 🔲 |
| Feature comparison | .research/paper-software/ | Table comparing with OpenFermion etc. | 🔲 |
| Architecture diagram | .research/paper-software/figures/ | Index-set vs path-based diagram | 🔲 |

---

## Writing Checklist

### Phase 1: Repository Preparation
- [ ] Add LICENSE (MIT)
- [ ] Write comprehensive README.md
- [ ] Add CONTRIBUTING.md
- [ ] Add CITATION.cff
- [ ] Set up GitHub repo + CI
- [ ] Create 4 example scripts

### Phase 2: Paper Writing
- [ ] Draft Summary (250 words)
- [ ] Draft Statement of Need (300 words)
- [ ] Draft Functionality (200 words)
- [ ] Draft Design Principles (200 words)
- [ ] Draft Related Software (150 words)
- [ ] Compile references (paper.bib)

### Phase 3: Verification
- [ ] Run all examples on clean checkout
- [ ] Verify installation instructions work
- [ ] Verify test suite passes on Linux/macOS/Windows
- [ ] Internal review pass
- [ ] Compile paper with JOSS template (`whedon`)

### Phase 4: Submission
- [ ] Create Zenodo archive
- [ ] Submit to JOSS via GitHub issue
- [ ] Respond to reviewer feedback

---

## Key API Surface to Document

### Types
```
EncodingScheme = { Update; Parity; Occupation }
FenwickTree<'a> = { Data; Combine; Identity }
TreeNode = { Index; Children; Parent }
EncodingTree = { Root; Nodes; Size }
LinkLabel = LX | LY | LZ
Link = { Label; Target }
LegId = { Node; Label }
```

### Core Functions
```
encodeOperator : EncodingScheme → LadderOperatorUnit → uint32 → uint32 → PauliRegisterSequence
pauliOfAssignments : int → (int × Pauli) list → Complex → PauliRegister
cMajorana : EncodingScheme → int → int → (int × Pauli) list
dMajorana : EncodingScheme → int → int → (int × Pauli) list
encodeWithTernaryTree : EncodingTree → LadderOperatorUnit → uint32 → uint32 → PauliRegisterSequence
```

### Concrete Schemes
```
jordanWignerScheme : EncodingScheme
bravyiKitaevScheme : EncodingScheme
parityScheme : EncodingScheme
```

### Tree Builders
```
linearTree : int → EncodingTree
balancedBinaryTree : int → EncodingTree
balancedTernaryTree : int → EncodingTree
```

### Convenience Functions
```
jordanWignerTerms / bravyiKitaevTerms / parityTerms : LadderOperatorUnit → uint32 → uint32 → PauliRegisterSequence
ternaryTreeTerms / balancedBinaryTreeTerms : same signature
```

### Fenwick Tree API
```
build / ofArray / empty : construction
prefixQuery / pointQuery : queries
update : functional update
updateSet / paritySet / occupationSet / remainderSet : BK index sets
```

---

## Code Examples to Include in Paper/README

### Example 1: Encode a single operator
```fsharp
open Encodings
let result = encodeOperator jordanWignerScheme Raise 2u 4u
// → ½(ZZXI) − ½i(ZZYI)
```

### Example 2: Define a custom encoding
```fsharp
let myScheme : EncodingScheme =
    { Update     = fun j n -> set [ j + 1 .. n - 1 ]
      Parity     = fun j   -> if j > 0 then Set.singleton (j - 1) else Set.empty
      Occupation  = fun j   -> if j > 0 then set [j-1; j] else Set.singleton j }
// This is actually the Parity encoding!
```

### Example 3: Build a custom tree encoding
```fsharp
let myTree = balancedTernaryTree 8
let result = encodeWithTernaryTree myTree Raise 3u 8u
// Automatically constructs path-based Majorana operators
```

### Example 4: Full H₂ pipeline
```fsharp
// 1. Define integrals
// 2. Build Hamiltonian in second quantization
// 3. Encode with any scheme
// 4. Compare Pauli term counts and weights across all 5 encodings
```
