# Repository Cleanup & Documentation Plan

Prepared 2026-02-09 based on full codebase audit.  
Goal: publication-ready repository for JOSS submission (Paper 2) and
general open-source quality.

---

## Current State Summary

| Category | Status | Notes |
|----------|:------:|-------|
| Build health | ✅ | Pure F#, net8.0, zero external runtime deps |
| Tests | ✅ | 303 tests, 20 test files, FsCheck property tests |
| Debug artifacts | ✅ | Zero `printfn`, zero TODO/FIXME |
| Mutable state | ⚠ | `Dictionary` in `PauliRegisterSequence` (contained) |
| XML doc coverage | ⚠ | 4/15 source files well-documented; 11/15 lacking |
| Root repo files | ❌ | No README, LICENSE, CONTRIBUTING, CITATION.cff |
| `[<AutoOpen>]` | ⚠ | All 15 modules — namespace pollution |
| Terse type names | ⚠ | `C`, `P`, `S` are collision-prone |
| Dead code | ⚠ | Tests.fs is empty; commented code in Hamiltonian.fs |

---

## Phase 1: Documentation — XML Doc Comments with MathJax

**Priority: HIGH — this is the most impactful single improvement.**

Every public type, function, and discriminated union case needs `///`
XML documentation with mathematical context.  The newer files
(FenwickTree, MajoranaEncoding, TreeEncoding, BravyiKitaev) already
have excellent docs — use them as the gold standard.

### 1.1 TypeExtensions.fs (~63 lines)

Currently: Utility functions (`half`, `curry`, `uncurry`, etc.) with
zero documentation.

Add:
```fsharp
/// <summary>
/// General-purpose utility functions used throughout the encoding library.
/// </summary>
/// <remarks>
/// These are small combinators (currying, uncurrying, integer halving)
/// that simplify the implementation of encoding logic.  They are
/// <c>[&lt;AutoOpen&gt;]</c>'d into the <c>Encodings</c> namespace.
/// </remarks>
module TypeExtensions

/// Integer division by 2 (floor).  Used to convert spin-orbital
/// indices to spatial-orbital indices: <c>spatialIndex = half j</c>.
let half x = ...

/// <summary>Uncurry a two-argument function into a function on tuples.</summary>
let uncurry f (a, b) = ...
```

### 1.2 Terms.fs (~176 lines)

Currently: Core algebraic types `C<'unit>`, `P<'unit>`, `S<'unit>` with
zero documentation.  These are the most important types in the library
and the least documented.

Add:
```fsharp
/// <summary>
/// Core algebraic types for representing quantum operator expressions.
/// </summary>
/// <remarks>
/// The library models operator algebra at three levels of structure:
///
/// <list type="bullet">
///   <item>
///     <term><see cref="C{T}"/></term>
///     <description>
///       A <b>coefficient–operator pair</b>: a complex scalar times a
///       single operator unit.  Represents terms like
///       <c>0.5 × a†₂</c> or <c>-i × (XZYI)</c>.
///     </description>
///   </item>
///   <item>
///     <term><see cref="P{T}"/></term>
///     <description>
///       A <b>product</b> (ordered sequence) of coefficient–operator
///       pairs.  Represents operator products like
///       <c>a†₀ a₁</c> — a hopping term.
///     </description>
///   </item>
///   <item>
///     <term><see cref="S{T}"/></term>
///     <description>
///       A <b>sum</b> of products.  Represents full Hamiltonian
///       expressions: <c>H = Σᵢ hᵢ Pᵢ</c>.
///     </description>
///   </item>
/// </list>
///
/// The type parameter <typeparamref name="T"/> is the operator unit
/// type — either <see cref="LadderOperatorUnit"/> (for fermionic
/// second-quantized operators) or <see cref="PauliRegister"/> (for
/// qubit Pauli strings).
/// </remarks>
```

Each type (`C`, `P`, `S`) and each method needs docs:
```fsharp
/// <summary>
/// A single term: coefficient × operator.
/// </summary>
/// <typeparam name="T">The operator unit type.</typeparam>
/// <example>
/// <code>
/// let term = C(Complex(0.5, 0.0), Raise 2u)
/// // Represents: 0.5 × a†₂
/// </code>
/// </example>
type C<'unit> = ...
```

### 1.3 IndexedTerms.fs (~83 lines)

Currently: `IxOp`, `PIxOp`, `SIxOp` — partially documented.

Add module-level doc explaining indexed vs non-indexed terms:
```fsharp
/// <summary>
/// Indexed operator types for tracking qubit/mode assignments.
/// </summary>
/// <remarks>
/// Each <see cref="IxOp{T}"/> wraps an operator unit with an integer
/// index identifying which qubit (or fermionic mode) it acts on.
/// This is essential for multi-qubit Pauli strings where the
/// <i>position</i> of each Pauli matters:
/// <c>X₀ Z₁ I₂ Y₃ = XZIY</c>.
/// </remarks>
```

### 1.4 SwapTrackingSort.fs (~51 lines)

Currently: Zero documentation.  This is the sorting algorithm that
tracks the parity of swaps — critical for fermionic normal ordering.

Add:
```fsharp
/// <summary>
/// Swap-tracking selection sort for computing fermionic phase factors.
/// </summary>
/// <remarks>
/// When reordering fermionic operators to normal order
/// (all creation operators left, annihilation right), each swap of
/// adjacent operators introduces a factor of −1.  This module
/// implements selection sort that counts swaps, so the total
/// fermionic sign <c>(-1)^(number of swaps)</c> can be computed.
///
/// The sort is O(n²) but n is always small (number of operators
/// in a single product term, typically 2–4).
/// </remarks>
```

### 1.5 IndexedPauli.fs (~79 lines)

Currently: `Phase` and `Pauli` types, partially documented.

Add mathematical definitions:
```fsharp
/// <summary>
/// The global phase factor attached to a Pauli product.
/// </summary>
/// <remarks>
/// When multiplying Pauli matrices, the result is another Pauli
/// matrix times a phase ∈ {+1, −1, +i, −i}.  For example:
/// <c>X · Y = iZ</c> (phase = +i).
///
/// Phases are tracked as a discriminated union rather than complex
/// numbers, enabling exact (no floating-point error) phase arithmetic.
///
/// <list type="bullet">
///   <item><term>P1</term><description>+1</description></item>
///   <item><term>Pn1</term><description>−1</description></item>
///   <item><term>Pi</term><description>+i</description></item>
///   <item><term>Pni</term><description>−i</description></item>
/// </list>
/// </remarks>
type Phase = P1 | Pn1 | Pi | Pni

/// <summary>
/// Single-qubit Pauli operators.
/// </summary>
/// <remarks>
/// The four single-qubit Pauli matrices form a basis for all
/// 2×2 Hermitian matrices:
///
/// <c>I = [[1,0],[0,1]]</c>,  <c>X = [[0,1],[1,0]]</c>,
/// <c>Y = [[0,-i],[i,0]]</c>, <c>Z = [[1,0],[0,-1]]</c>
///
/// The multiplication table is cyclic:
/// <c>XY = iZ</c>, <c>YZ = iX</c>, <c>ZX = iY</c> (and reversal
/// gives −i).  Two distinct non-identity Paulis always anti-commute:
/// <c>{X,Y} = XY + YX = iZ − iZ = 0</c>.
/// </remarks>
type Pauli = I | X | Y | Z
```

### 1.6 PauliRegister.fs (~126 lines)

Currently: Partially documented.

Add:
```fsharp
/// <summary>
/// Multi-qubit Pauli strings with phase tracking and symbolic multiplication.
/// </summary>
/// <remarks>
/// A <see cref="PauliRegister"/> represents a tensor product of
/// single-qubit Pauli operators with a global phase:
/// <c>φ · P₀ ⊗ P₁ ⊗ ··· ⊗ Pₙ₋₁</c>
///
/// For example, <c>+i · X ⊗ Z ⊗ Y ⊗ I</c> is a weight-3 Pauli
/// string on 4 qubits.
///
/// <b>Multiplication</b> of two Pauli registers is exact and O(n):
/// each qubit position is multiplied independently using the Pauli
/// multiplication table, and phases accumulate.  The result is always
/// another Pauli register (the Pauli group is closed under multiplication).
///
/// <see cref="PauliRegisterSequence"/> represents a sum of Pauli
/// registers with complex coefficients — the standard representation
/// of a qubit Hamiltonian: <c>H = Σₐ cₐ σₐ</c>.
/// </remarks>
```

### 1.7 IndexedLadderOperator.fs (~123 lines)

Currently: Partially documented (normal ordering predicates).

Add:
```fsharp
/// <summary>
/// Fermionic ladder operators with index tracking and ordering predicates.
/// </summary>
/// <remarks>
/// A <see cref="LadderOperatorUnit"/> is either <c>Raise j</c> (= a†ⱼ,
/// creation) or <c>Lower j</c> (= aⱼ, annihilation), where j is the
/// spin-orbital index.
///
/// Products of ladder operators must respect <b>normal ordering</b>
/// (all creation operators to the left of all annihilation operators)
/// and <b>index ordering</b> (within each group, indices are sorted).
/// The functions <see cref="isInNormalOrder"/> and
/// <see cref="isInIndexOrder"/> test these conditions.
///
/// Normal ordering is required before encoding: the Jordan-Wigner
/// (and other) transforms assume that the operator product is in
/// a canonical form.
/// </remarks>
```

### 1.8 CombiningAlgebra.fs (~54 lines)

Currently: `ICombiningAlgebra` and `FermionicAlgebra` documented.

Add richer mathematical context:
```fsharp
/// <summary>
/// Algebra interface for combining operator terms during normal ordering.
/// </summary>
/// <remarks>
/// When normal-ordering a product of fermionic operators, each swap
/// of adjacent operators may generate new terms via the canonical
/// anti-commutation relations (CAR):
///
///   {aᵢ, a†ⱼ} = aᵢ a†ⱼ + a†ⱼ aᵢ = δᵢⱼ
///
/// The <see cref="ICombiningAlgebra{T}"/> interface abstracts this:
/// <see cref="Combine"/> takes two operators and returns the terms
/// generated by swapping them, including the δᵢⱼ contribution.
///
/// <see cref="FermionicAlgebra"/> implements the fermionic CAR.
/// Other algebras (bosonic, anyonic) could implement the same
/// interface with different commutation relations.
/// </remarks>
```

### 1.9 LadderOperatorSequence.fs (~102 lines)

Currently: `ConstructNormalOrdered` and related functions documented.

Add more context on what normal ordering IS:
```fsharp
/// <summary>
/// Normal ordering of fermionic operator products.
/// </summary>
/// <remarks>
/// A product of ladder operators <c>a†₂ a₀ a†₁ a₃</c> is in
/// <b>normal order</b> when all creation operators (a†) are to the
/// left of all annihilation operators (a):
///   <c>a†₁ a†₂ a₀ a₃</c>  (normal ordered)
///
/// Reordering requires swapping adjacent operators.  Each swap of
/// two fermionic operators introduces a factor of −1 (from the CAR),
/// and swapping a†ᵢ past aᵢ (same index) generates an additional
/// δᵢⱼ term.  The <see cref="ConstructNormalOrdered"/> function
/// performs this reordering, tracking all signs and generated terms,
/// producing a <see cref="S{T}"/> (sum of products).
/// </remarks>
```

### 1.10 JordanWigner.fs (~32 lines)

Currently: `jordanWignerTerms` has a brief doc.

Add the encoding formula:
```fsharp
/// <summary>
/// The Jordan-Wigner fermion-to-qubit encoding (1928).
/// </summary>
/// <remarks>
/// The Jordan-Wigner transform maps fermionic creation and annihilation
/// operators to qubit (Pauli) operators by inserting a chain of Z
/// operators to track the parity of all preceding modes:
///
///   cⱼ → Xⱼ ⊗ Zⱼ₋₁ ⊗ ··· ⊗ Z₀
///   dⱼ → Yⱼ ⊗ Zⱼ₋₁ ⊗ ··· ⊗ Z₀
///
/// where cⱼ = a†ⱼ + aⱼ and dⱼ = i(a†ⱼ − aⱼ) are Majorana operators.
///
/// The Z-chain grows linearly with mode index j, giving O(n) worst-case
/// Pauli weight.  For alternatives with O(log n) weight, see
/// <see cref="BravyiKitaev"/> and <see cref="TreeEncoding"/>.
///
/// <b>Reference:</b> P. Jordan and E. Wigner, "Über das Paulische
/// Äquivalenzverbot," Z. Phys. 47, 631 (1928).
/// </remarks>
```

### 1.11 Hamiltonian.fs (~83 lines)

Currently: `EncoderFn` and `computeHamiltonian` partially documented.

Add:
```fsharp
/// <summary>
/// Hamiltonian construction from one-body and two-body integrals.
/// </summary>
/// <remarks>
/// Assembles the second-quantized electronic Hamiltonian:
///
///   H = Σ_{pq} h_{pq} a†_p a_q  +  ½ Σ_{pqrs} ⟨pq|rs⟩ a†_p a†_q a_s a_r
///
/// and encodes it as a sum of Pauli strings using any provided encoding
/// function (<see cref="EncoderFn"/>).  The encoding function is a
/// parameter, so the same Hamiltonian can be encoded with Jordan-Wigner,
/// Bravyi-Kitaev, or any custom encoding by swapping one argument.
///
/// The function handles:
/// <list type="bullet">
///   <item>One-body terms: h_{pq} a†_p a_q</item>
///   <item>Two-body terms: ½ ⟨pq|rs⟩ a†_p a†_q a_s a_r  (note reversed r,s)</item>
///   <item>Term collection: combining Pauli strings with equal signatures</item>
///   <item>Coefficient simplification: dropping near-zero terms</item>
/// </list>
/// </remarks>
```

### 1.12 FenwickTree.fs, MajoranaEncoding.fs, BravyiKitaev.fs, TreeEncoding.fs

Already well-documented.  Minor additions:
- Add `<example>` blocks with runnable code snippets
- Add `<seealso>` cross-references between related modules
- Add mathematical notation in `<remarks>` where currently only prose

---

## Phase 2: Repository Root Files

### 2.1 README.md

Create comprehensive README with:
- Project title and one-line description
- Badges (build status, license, .NET version)
- **What is this?** — 2-paragraph explanation of fermion-to-qubit encodings
- **Quick start** — 3-line F# example
- **Installation** — `dotnet build`, `dotnet test`
- **Available encodings** — table of all 5 with weight scaling
- **Architecture** — brief description of the two frameworks
- **API overview** — key types and functions
- **Examples** — link to H2Demo.fsx, ScalingBenchmark.fsx
- **Running tests** — `dotnet test`
- **Citation** — BibTeX block
- **License** — MIT
- **Related papers** — links to Paper 1 and Paper 2

### 2.2 LICENSE

MIT license.  Standard text with author name and year.

### 2.3 CONTRIBUTING.md

- How to report bugs (GitHub issues)
- How to propose features
- Code style guide (F# conventions: camelCase functions, PascalCase types,
  `///` XML docs on all public APIs)
- PR process
- Running tests locally

### 2.4 CITATION.cff

Machine-readable citation metadata (Citation File Format).

### 2.5 .editorconfig

Consistent formatting: 4-space indentation, UTF-8, LF line endings,
trim trailing whitespace.

---

## Phase 3: Code Quality Improvements

### 3.1 Remove dead code

- [ ] **Tests.fs** — empty file (5 lines, no tests).  Remove from project
      and delete.
- [ ] **Hamiltonian.fs** — check for and remove any commented-out code.

### 3.2 Address `[<AutoOpen>]` pollution

**Option A (minimal, recommended for now):** Keep `[<AutoOpen>]` but add
a top-level `Encodings` module doc explaining that all types are
intentionally flattened into the `Encodings` namespace for ergonomic
scripting use.

**Option B (thorough, do later):** Remove `[<AutoOpen>]` from most
modules, keeping it only for `TypeExtensions`.  Require explicit
`open Encodings.Terms`, `open Encodings.PauliAlgebra`, etc.  This is a
breaking change for all .fsx scripts and tests.

**Recommendation:** Option A now, Option B after JOSS acceptance if
reviewers flag it.

### 3.3 Rename terse types (DEFERRED)

The types `C<'unit>`, `P<'unit>`, `S<'unit>` are terse but deeply
embedded in the codebase (all 303 tests use them).  Renaming to
`CoefficientTerm`, `ProductTerm`, `SumExpression` would be clearer but
is a massive breaking change.

**Recommendation:** Document them thoroughly (Phase 1.2) but defer
renaming.  Add type aliases in a future version:
```fsharp
type CoefficientTerm<'T> = C<'T>
type ProductTerm<'T> = P<'T>
type SumExpression<'T> = S<'T>
```

### 3.4 Dictionary → Map in PauliRegisterSequence (DEFERRED)

The internal `Dictionary` in `PauliRegisterSequence` is a performance
optimisation for coefficient accumulation.  It's fully contained (the
type exposes only immutable APIs).

**Recommendation:** Document the design choice.  Don't change it unless
a reviewer flags it — `Map` would be 5-10× slower for large
Hamiltonians and the current approach is idiomatic F# (mutable
collections inside immutable wrappers).

---

## Phase 4: Test Improvements

### 4.1 Remove empty test file

Delete `Tests.fs` from `Test.Encodings/` and remove from .fsproj.

### 4.2 Add test documentation

Each test file should have a module-level `///` comment explaining what
aspect of the library it validates and what the testing strategy is
(unit vs property-based).

### 4.3 Add missing test coverage

| Gap | Description | Priority |
|-----|-------------|----------|
| Hamiltonian end-to-end | Only 2 tests (n=2, n=4); add n=8 | Medium |
| Custom encoding scheme | Test a user-defined `EncodingScheme` | High (JOSS demo) |
| Custom tree | Test a user-built `TreeNode` encoding | High (JOSS demo) |
| Error cases | What happens with n=0? mode > n? | Low |
| PauliRegisterSequence | Coefficient accumulation, zero-term dropping | Medium |

---

## Phase 5: Scripts & Examples Cleanup

### 5.1 Organise examples/

Create an `examples/` folder at the repo root with curated scripts:

| Script | Source | Purpose |
|--------|--------|---------|
| `H2_Encoding.fsx` | Adapt from `H2Demo.fsx` | Minimal: encode H₂ with JW, print terms |
| `Custom_Encoding.fsx` | New | Define a custom `EncodingScheme`, verify CAR |
| `Custom_Tree.fsx` | New | Build an arbitrary tree, encode an operator |
| `Compare_Encodings.fsx` | Adapt from `ScalingBenchmark.fsx` | All 5 encodings on H₂, weight comparison |

### 5.2 Research tools stay in .research/

The `.research/tools/` scripts (MatrixVerification, AnticommutationTest,
MonotonicityCensus, etc.) are research artifacts, not user-facing
examples.  Keep them in `.research/` and don't reference them in README.

### 5.3 Review .fsx scripts for hardcoded paths

All scripts use `#r` directives pointing to relative DLL paths.
Verify these work from the expected working directory and document the
expected invocation:
```bash
cd Encodings && dotnet build
cd .. && dotnet fsi examples/H2_Encoding.fsx
```

---

## Phase 6: CI/CD Setup

### 6.1 GitHub Actions workflow

Create `.github/workflows/ci.yml`:
```yaml
name: CI
on: [push, pull_request]
jobs:
  build-and-test:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet build
      - run: dotnet test
```

### 6.2 GitHub release + Zenodo

- Create initial release v0.1.0
- Link GitHub repo to Zenodo for automatic DOI minting
- Add DOI badge to README

---

## Phase 7: Project File Cleanup

### 7.1 Encodings.fsproj

- [ ] Add `<Description>` — one-line package description
- [ ] Add `<Authors>`, `<PackageLicenseExpression>`, `<RepositoryUrl>`
- [ ] Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>`
      (enables XML doc generation for API docs)
- [ ] Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` (optional,
      catches missing docs)

### 7.2 Test.Encodings.fsproj

- [ ] Remove `Tests.fs` from compilation order
- [ ] Verify all test files are listed and in correct order

---

## Execution Order

| Order | Phase | Effort | Breaking? | JOSS-required? |
|:-----:|-------|:------:|:---------:|:--------------:|
| 1 | Phase 2: Root files (README, LICENSE, CONTRIBUTING) | 2 hr | No | **Yes** |
| 2 | Phase 1: XML docs on all 11 under-documented files | 4 hr | No | **Yes** |
| 3 | Phase 3.1: Remove dead code | 15 min | No | No |
| 4 | Phase 4.1: Remove empty test file | 5 min | No | No |
| 5 | Phase 5: Organise examples/ | 1 hr | No | **Yes** |
| 6 | Phase 7: Project file metadata | 30 min | No | **Yes** |
| 7 | Phase 6: CI setup | 1 hr | No | **Yes** |
| 8 | Phase 3.2: AutoOpen documentation | 15 min | No | No |
| 9 | Phase 4.2-4.3: Test docs and coverage | 2 hr | No | Nice-to-have |
| 10 | Phase 3.3: Type renaming | 3 hr | **Yes** | No (defer) |
| 11 | Phase 3.4: Dictionary→Map | 1 hr | No (internal) | No (defer) |

**Total estimated effort: ~12 hours for JOSS-required items (phases 1–7).**

Items 10–11 are deferred post-JOSS unless reviewer feedback demands them.

---

## Success Criteria

- [ ] `dotnet build` produces zero warnings
- [ ] `dotnet test` passes 303+ tests on Windows, Linux, macOS
- [ ] Every public type and function has `///` XML documentation
- [ ] README has installation, quick start, API overview, citation
- [ ] LICENSE file exists (MIT)
- [ ] CONTRIBUTING.md exists
- [ ] At least 4 example scripts in `examples/`
- [ ] CI passes on all three platforms
- [ ] JOSS `paper.md` compiles with `whedon` or Open Journals toolchain
- [ ] Zenodo DOI minted

---

## Notes

- **Don't rename `C`/`P`/`S` yet.** Document them thoroughly instead.
  If JOSS reviewers object, rename then.  The types are internally
  consistent and the short names are actually idiomatic for algebraic
  types in ML-family languages.

- **Keep `[<AutoOpen>]` for now.**  F# scripting (`.fsx`) is the primary
  consumption mode, and `open Encodings` getting everything is the right
  UX for scripts.  Document the design choice.

- **The `Dictionary` in `PauliRegisterSequence` is fine.**  This is a
  well-known F# pattern (mutable builder inside immutable wrapper).
  Document it as a conscious design choice.

- **Phase 1 is the highest-value work.**  Rich XML docs with mathematical
  notation will transform the library from "research code" to "reference
  implementation."  This is what JOSS reviewers look at first.
