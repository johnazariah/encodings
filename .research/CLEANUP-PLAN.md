# Repository Cleanup & Documentation Plan

Prepared 2026-02-09 based on full codebase audit.  
Updated 2026-02-09 to include NuGet packaging, GitHub Pages, and
cross-platform messaging.

Goals:
1. **Publication-ready repository** for JOSS submission (Paper 2)
2. **NuGet package** — pre-built, versioned, ready to `dotnet add package`
3. **GitHub Pages documentation site** — API reference with MathJax,
   tutorials, architecture guide
4. **Cross-platform emphasis** — .NET 8 runs on Windows, macOS, and Linux;
   F# is fully open-source under the F# Software Foundation and .NET Foundation

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
| NuGet packaging | ❌ | No package metadata, no `dotnet pack` support |
| Documentation site | ❌ | No GitHub Pages, no fsdocs, no generated API docs |
| Cross-platform CI | ❌ | No CI at all; needs Windows + macOS + Linux |

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
- Badges: CI status, NuGet version, license, .NET 8, platforms
- **What is this?** — 2-paragraph explanation of fermion-to-qubit encodings
- **Cross-platform** — prominent callout:
  > Fermion2Qubit runs on **Windows**, **macOS**, and **Linux** via
  > [.NET 8](https://dotnet.microsoft.com/), Microsoft's open-source,
  > cross-platform runtime.  The library is written in
  > [F#](https://fsharp.org/), a functional-first language that is
  > fully open-source under the
  > [F# Software Foundation](https://foundation.fsharp.org/) and the
  > [.NET Foundation](https://dotnetfoundation.org/).
- **Installation** — two paths:
  ```bash
  # As a NuGet package (recommended)
  dotnet add package Fermion2Qubit

  # From source
  git clone https://github.com/<org>/Fermion2Qubit.git
  cd Fermion2Qubit && dotnet build && dotnet test
  ```
- **Quick start** — 5-line F# example encoding a single operator
- **Available encodings** — table of all 5 with weight scaling O(·)
- **Architecture** — brief description of the two frameworks
- **API overview** — key types and functions, link to full API docs
- **Documentation** — link to GitHub Pages site
- **Examples** — link to `examples/` folder
- **Running tests** — `dotnet test` on all 3 platforms
- **NuGet package** — link to nuget.org listing
- **Citation** — BibTeX block
- **License** — MIT
- **Related papers** — links to Paper 1 and Paper 2
- **Platform support** — tested on Ubuntu, macOS (ARM + x64), Windows

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

## Phase 6: CI/CD Pipeline

Three GitHub Actions workflows: CI (every push), Docs (on main), Release
(on tag).

### 6.1 CI workflow — `.github/workflows/ci.yml`

Runs on every push and PR.  Tests on all three platforms.

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
      - run: dotnet build --configuration Release
      - run: dotnet test --configuration Release --logger trx
      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results-${{ matrix.os }}
          path: '**/*.trx'
      - name: Pack NuGet (ubuntu only, avoid duplicates)
        if: matrix.os == 'ubuntu-latest'
        run: dotnet pack Encodings/Encodings.fsproj --configuration Release --no-build -o nupkg
      - name: Upload NuGet artifact
        if: matrix.os == 'ubuntu-latest'
        uses: actions/upload-artifact@v4
        with:
          name: nuget-package
          path: nupkg/*.nupkg
```

### 6.2 Docs workflow — `.github/workflows/docs.yml`

Builds GitHub Pages documentation on push to `main`.

```yaml
name: Documentation
on:
  push:
    branches: [main]
permissions:
  pages: write
  id-token: write
jobs:
  build-docs:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet tool restore
      - run: dotnet build --configuration Release
      - run: dotnet fsdocs build --output docs-output --parameters root /Fermion2Qubit/
      - uses: actions/upload-pages-artifact@v3
        with:
          path: docs-output
  deploy:
    needs: build-docs
    runs-on: ubuntu-latest
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    steps:
      - id: deployment
        uses: actions/deploy-pages@v4
```

### 6.3 Release workflow — `.github/workflows/release.yml`

Publishes NuGet package when a version tag is pushed.

```yaml
name: Release
on:
  push:
    tags: ['v*']
jobs:
  publish:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet build --configuration Release
      - run: dotnet test --configuration Release
      - run: dotnet pack Encodings/Encodings.fsproj --configuration Release -o nupkg
      - name: Publish to NuGet
        run: dotnet nuget push nupkg/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
      - name: Create GitHub Release
        uses: softprops/action-gh-release@v2
        with:
          files: nupkg/*.nupkg
          generate_release_notes: true
```

### 6.4 GitHub release + Zenodo

- Create initial release v0.1.0 with tag `v0.1.0`
- Link GitHub repo to Zenodo for automatic DOI minting
- Add DOI badge to README
- Zenodo archives every tagged release automatically

---

## Phase 8: NuGet Package

### 8.1 Package structure

After Phase 7 adds the `.fsproj` metadata, `dotnet pack` will produce:

```
Fermion2Qubit.0.1.0.nupkg
├── lib/net8.0/
│   ├── Encodings.dll          # The compiled library
│   └── Encodings.xml          # XML docs (IntelliSense + fsdocs)
├── README.md                   # Rendered on nuget.org
├── LICENSE.md                  # Embedded license
└── Fermion2Qubit.0.1.0.snupkg  # Symbol package (Source Link)
```

**Zero runtime dependencies.**  The package contains only the F# library
itself.  No transitive `FSharp.Core` pinning issues because we target
`net8.0` (which includes `FSharp.Core` in the runtime).

### 8.2 Local testing before publish

```bash
# Build and pack
dotnet pack Encodings/Encodings.fsproj --configuration Release -o nupkg

# Inspect the package
dotnet tool install --global NuGetPackageExplorer  # or use nuget.info
# Or just unzip the .nupkg (it's a zip file)

# Test install in a fresh project
mkdir /tmp/test-install && cd /tmp/test-install
dotnet new console -lang F#
dotnet add package Fermion2Qubit --source /path/to/nupkg
# Write a 3-line script that encodes an operator
dotnet run
```

### 8.3 Versioning strategy

- `0.1.0` — initial pre-release (before JOSS submission)
- `0.2.0` — after incorporating JOSS reviewer feedback
- `1.0.0` — after JOSS acceptance (stable API)
- Follow [SemVer 2.0](https://semver.org/): breaking changes = major bump

### 8.4 NuGet.org listing

The NuGet.org package page will show:
- **Title:** Fermion2Qubit
- **Description:** From the `<Description>` in `.fsproj`
- **Tags:** quantum, encoding, jordan-wigner, bravyi-kitaev, fsharp
- **README:** Rendered from the embedded `README.md`
- **License:** MIT (clickable)
- **Dependencies:** None
- **Frameworks:** net8.0
- **Source repository:** Link to GitHub (via `<RepositoryUrl>`)

### 8.5 Package name decision

Check availability on nuget.org.  Candidates:
1. `Fermion2Qubit` — matches the paper title, memorable
2. `FSharp.Quantum.Encodings` — follows F# community convention
3. `Encodings.Quantum` — shorter

Recommendation: `Fermion2Qubit` unless already taken.

---

## Phase 9: GitHub Pages Documentation Site

### 9.1 Tooling: FSharp.Formatting (`fsdocs`)

[FSharp.Formatting](https://fsprojects.github.io/FSharp.Formatting/)
is the standard documentation tool for F# libraries.  It:

- Generates **API reference** from `///` XML doc comments
- Renders **literate F# scripts** (`.fsx` files with `(** ... *)` 
  markdown blocks) as tutorial pages
- Supports **MathJax/KaTeX** for mathematical notation out of the box
- Produces a static site ready for GitHub Pages deployment

Major F# libraries use it: FSharp.Data, Plotly.NET, FsToolkit, Saturn.

### 9.2 Setup

```bash
# Add fsdocs as a local tool
dotnet new tool-manifest  # creates .config/dotnet-tools.json
dotnet tool install fsdocs-tool

# Generate docs locally
dotnet build
dotnet fsdocs watch  # live preview at http://localhost:8901

# Build for deployment
dotnet fsdocs build --output docs-output
```

### 9.3 Documentation site structure

```
docs/                          # Source files for fsdocs
├── index.md                   # Landing page
├── tutorial.fsx               # Literate F# tutorial (H₂ walkthrough)
├── architecture.md            # Two-framework architecture diagram
├── encodings.md               # Table of all 5 encodings with formulas
├── custom-encoding.fsx        # Literate: how to define your own
├── custom-tree.fsx            # Literate: how to build a tree encoding
└── cross-platform.md          # .NET/F# ecosystem and platform support

(API reference is auto-generated from XML doc comments — no manual files needed)
```

### 9.4 Mathematical notation in docs

FSharp.Formatting supports LaTeX math via MathJax.  In XML doc comments:

```fsharp
/// The Majorana operator cⱼ maps to:
///
/// $$c_j \mapsto X_j \otimes Z_{j-1} \otimes \cdots \otimes Z_0$$
///
/// with Pauli weight $O(j)$ under Jordan-Wigner.
```

In literate `.fsx` files:
```fsharp
(**
## The Jordan-Wigner Transform

The creation operator $a^\dagger_j$ maps to:

$$a^\dagger_j = \frac{1}{2}(c_j - id_j) \mapsto \frac{1}{2}(X_j - iY_j) \otimes Z_{j-1} \otimes \cdots \otimes Z_0$$
*)
let result = jordanWignerTerms Raise 0u 4u
printfn "%A" result
```

The output of the F# code is captured and rendered inline — so the
reader sees both the formula and the computed Pauli string.

### 9.5 Landing page content (`docs/index.md`)

```markdown
# Fermion2Qubit

A composable functional framework for fermion-to-qubit encodings.

## What is this?

[2-paragraph explanation]

## Cross-Platform

Fermion2Qubit runs on **Windows**, **macOS**, and **Linux**.
Built on [.NET 8](https://dotnet.microsoft.com/) (MIT-licensed,
cross-platform runtime) and written in
[F#](https://fsharp.org/) (open-source, governed by the
[F# Software Foundation](https://foundation.fsharp.org/)).

## Install

    dotnet add package Fermion2Qubit

## Quick Start

    open Encodings
    let h2 = jordanWignerTerms Raise 0u 4u
    printfn "%A" h2

## Available Encodings

| Encoding | Max Weight | Constructor |
|----------|-----------|-------------|
| Jordan-Wigner | O(n) | `jordanWignerTerms` |
| Bravyi-Kitaev | O(log₂ n) | `bravyiKitaevTerms` |
| Parity | O(n) | `parityTerms` |
| Balanced Binary | O(log₂ n) | `balancedBinaryTreeTerms` |
| Balanced Ternary | O(log₃ n) | `ternaryTreeTerms` |

## Documentation

- [Tutorial: H₂ from scratch](tutorial.html)
- [Architecture: two frameworks](architecture.html)
- [Define your own encoding](custom-encoding.html)
- [API Reference](reference/index.html)
```

### 9.6 Deployment

The Docs workflow (Phase 6.2) builds and deploys automatically on push
to `main`.  The site URL will be:

    https://<org>.github.io/Fermion2Qubit/

---

## Phase 7: Project File Cleanup

### 7.1 Encodings.fsproj — Full NuGet Package Metadata

The `.fsproj` file is currently minimal (just `TargetFramework` and
compile order).  It needs full NuGet packaging metadata so that
`dotnet pack` produces a publication-ready `.nupkg`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>

    <!-- Package identity -->
    <PackageId>Fermion2Qubit</PackageId>
    <Version>0.1.0</Version>
    <Authors>John Aziz</Authors>
    <Company />

    <!-- Description and discoverability -->
    <Description>
      A composable functional framework for fermion-to-qubit encodings.
      Implements Jordan-Wigner, Bravyi-Kitaev, Parity, and tree-based
      encodings (balanced binary, balanced ternary) as algebraic data
      types in pure F#.  Define custom encodings in 3-5 lines.
    </Description>
    <PackageTags>quantum;quantum-computing;quantum-chemistry;encoding;jordan-wigner;bravyi-kitaev;fsharp;functional-programming</PackageTags>
    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageProjectUrl>https://github.com/ORG/Fermion2Qubit</PackageProjectUrl>
    <RepositoryUrl>https://github.com/ORG/Fermion2Qubit</RepositoryUrl>
    <RepositoryType>git</RepositoryType>

    <!-- License -->
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    <PackageRequireLicenseAcceptance>false</PackageRequireLicenseAcceptance>

    <!-- Documentation -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <DocumentationFile>bin/$(Configuration)/$(TargetFramework)/Encodings.xml</DocumentationFile>

    <!-- NuGet icon (optional, add later) -->
    <!-- <PackageIcon>icon.png</PackageIcon> -->

    <!-- Source Link for debugger source stepping -->
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
    <IncludeSymbols>true</IncludeSymbols>
    <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  </PropertyGroup>

  <!-- Include README in NuGet package -->
  <ItemGroup>
    <None Include="../README.md" Pack="true" PackagePath="/" />
  </ItemGroup>

  <!-- Source Link -->
  <ItemGroup>
    <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.*" PrivateAssets="All" />
  </ItemGroup>

  <ItemGroup>
    <!-- ... existing Compile items ... -->
  </ItemGroup>

</Project>
```

Key points:
- **`PackageId`** = `Fermion2Qubit` — the name users type in `dotnet add package`
- **`PackageTags`** — discoverable via NuGet search for "quantum", "encoding", etc.
- **`GenerateDocumentationFile`** — produces `Encodings.xml` alongside the DLL;
  this is what `fsdocs` and IDEs consume for IntelliSense tooltips
- **`IncludeSymbols` + Source Link** — enables consumers to step into our source
  in the debugger, directly from the NuGet package
- **`PackageReadmeFile`** — NuGet.org renders the README on the package page

### 7.2 Test.Encodings.fsproj

- [ ] Remove `Tests.fs` from compilation order
- [ ] Verify all test files are listed and in correct order
- [ ] Ensure `IsPackable = false` remains (don't publish test project)

---

## Execution Order

| Order | Phase | Effort | Breaking? | Required for? |
|:-----:|-------|:------:|:---------:|:-------------:|
| 1 | Phase 2: Root files (README, LICENSE, CONTRIBUTING) | 2 hr | No | JOSS + NuGet |
| 2 | Phase 7: .fsproj NuGet metadata + `GenerateDocumentationFile` | 30 min | No | NuGet + Docs |
| 3 | Phase 1: XML docs on all 11 under-documented files | 4 hr | No | JOSS + Docs + NuGet |
| 4 | Phase 3.1: Remove dead code | 15 min | No | Hygiene |
| 5 | Phase 4.1: Remove empty test file | 5 min | No | Hygiene |
| 6 | Phase 5: Organise examples/ | 1 hr | No | JOSS |
| 7 | Phase 8: NuGet packaging (dotnet pack, local test, icon) | 1 hr | No | NuGet |
| 8 | Phase 9: GitHub Pages with fsdocs | 2 hr | No | Docs |
| 9 | Phase 6: CI/CD pipeline (3 workflows) | 2 hr | No | All |
| 10 | Phase 3.2: AutoOpen documentation | 15 min | No | Nice-to-have |
| 11 | Phase 4.2-4.3: Test docs and coverage | 2 hr | No | Nice-to-have |
| 12 | Phase 3.3: Type renaming | 3 hr | **Yes** | Defer |
| 13 | Phase 3.4: Dictionary→Map | 1 hr | No (internal) | Defer |

**Total estimated effort: ~14 hours for core items (phases 1–9).**

Items 12–13 are deferred post-JOSS unless reviewer feedback demands them.

---

## Success Criteria

### Repository & Code Quality
- [ ] `dotnet build` produces zero warnings
- [ ] `dotnet test` passes 303+ tests on Windows, Linux, macOS
- [ ] Every public type and function has `///` XML documentation
- [ ] LICENSE file exists (MIT)
- [ ] CONTRIBUTING.md exists with code style guide
- [ ] At least 4 example scripts in `examples/`

### README & Messaging
- [ ] README has NuGet install command as primary installation method
- [ ] README has from-source build instructions as secondary method
- [ ] README prominently states cross-platform support (Win/macOS/Linux)
- [ ] README links to F# Software Foundation and .NET Foundation
- [ ] README has badges: CI, NuGet version, license, platforms
- [ ] README links to GitHub Pages API documentation

### NuGet Package
- [ ] `dotnet pack` produces a valid `.nupkg` with README, XML docs, symbols
- [ ] Package installs cleanly: `dotnet add package Fermion2Qubit`
- [ ] NuGet.org listing shows description, tags, README, license
- [ ] Source Link works (consumers can step into source in debugger)
- [ ] Package has no unnecessary dependencies (zero runtime deps)

### GitHub Pages Documentation
- [ ] API reference generated from XML docs with mathematical notation
- [ ] Landing page with overview, quick start, and architecture
- [ ] Tutorial page walking through H₂ encoding
- [ ] All pages render correctly on https://<org>.github.io/Fermion2Qubit/

### CI/CD Pipeline
- [ ] CI workflow: build + test on all 3 platforms, on every push/PR
- [ ] Docs workflow: auto-deploy GitHub Pages on merge to `main`
- [ ] Release workflow: auto-publish NuGet on version tag push
- [ ] Zenodo DOI minted and badge in README

### JOSS Submission
- [ ] `paper.md` compiles with Open Journals toolchain
- [ ] All JOSS checklist items satisfied

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

- **NuGet is the primary distribution channel.**  Most .NET developers
  expect `dotnet add package` — not cloning a repo and building from
  source.  The package should be zero-dependency (just the DLL + XML
  docs + README) so it's trivial to add to any project.

- **GitHub Pages via `fsdocs` is the gold standard for F# libraries.**
  [FSharp.Formatting](https://fsprojects.github.io/FSharp.Formatting/)
  (`fsdocs`) generates API reference from XML doc comments and renders
  literate `.fsx` scripts as tutorial pages.  It supports LaTeX math
  via MathJax out of the box.  Major F# libraries (FSharp.Data,
  FsToolkit.ErrorHandling, Plotly.NET) all use it.

- **Cross-platform messaging matters for JOSS.**  JOSS reviewers must
  be able to install and test the software.  If they're on macOS (most
  academics), they need to know .NET 8 works there.  The README should
  make this obvious — not buried in a footnote but in the first
  paragraph, with platform badges.

- **F# open-source lineage is a strength.**  F# is governed by the
  F# Software Foundation (an independent non-profit), the compiler is
  MIT-licensed, and the .NET runtime is MIT-licensed under the .NET
  Foundation.  This matters for reproducibility and long-term
  availability — two things JOSS cares about.  Call it out explicitly.

- **The NuGet package name `Fermion2Qubit` needs to be checked** for
  availability on nuget.org before we commit to it.  Alternative:
  `FSharp.Quantum.Encodings` (follows the `FSharp.*` convention for
  community libraries).
