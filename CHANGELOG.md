# Changelog

All notable changes to FockMap will be documented in this file.

## [0.9.0] - 2026-07-24

### 💥 BREAKING

- **The public Hamiltonian API now consumes RAW single-bar physicist integrals.**
  `Hamiltonian.computeHamiltonian`, `computeHamiltonianWith`, the parallel / cached
  builders, the sparse-skeleton discovery (`computeHamiltonianSkeletonFor`),
  `applyCoefficients`, and every `Optimization` entry point that builds through them
  now interpret a two-body key `"p,q,r,s"` as the raw physicist integral `⟨pq|rs⟩`
  (no ½, no index swap). The library applies the two-body ½ and the r↔s annihilator
  order internally, assembling `½·⟨pq|rs⟩·a†_p a†_q a_s a_r`. One-body keys `"p,q"`
  (the coefficient `h_pq`) are unchanged.
- **The `Fcidump` adapters now return raw physicist integrals** (`⟨pq|rs⟩ = (pr|qs)`
  in chemist notation), with no compensating ½ or swap. Because both the factory and
  the builders flipped together, **FCIDUMP → Hamiltonian physics is unchanged**: the
  canonical H₂/STO-3G output is still 15 terms, `IIII = −0.8121706072`, four-body
  `±0.0453026155`, weight 32, 15 rotations, 36 CNOTs, electronic ground
  `−1.8523881736 Ha`, 1-norm `2.6992778241`.
- **Exact before/after mapping** for hand-built factories:
  - **Before (≤ 0.8.0, weighted):** key `(p,q,s,r)` → value `0.5·g` (the full
    weighted prefactor of `a†_p a†_q a_s a_r`, ½ pre-folded).
  - **After (0.9.0, raw):** key `(p,q,r,s)` → value `g` (the raw `⟨pq|rs⟩`).
  - i.e. swap the last two indices and drop the pre-folded ½ (double the value).
- **Package version bumped `0.8.0` → `0.9.0`** to signal the breaking contract change
  (pre-1.0 minor bump). See the [migration guide](docs/guides/migration-0.9.md).

### ✨ Added

- **Legacy weighted migration API** — the previously released weighted semantics are
  preserved verbatim behind clearly named functions: `computeHamiltonianFromWeighted`,
  `computeHamiltonianFromWeightedWith`, `computeHamiltonianFromWeightedParallel`,
  `computeHamiltonianFromWeightedWithParallel`, `computeHamiltonianFromWeightedCached`,
  `computeHamiltonianSkeletonForFromWeighted`, and `applyCoefficientsFromWeighted`.
  Each applies the factory value verbatim to `a†_i a†_j a_k a_l` exactly as before.
- **`Hamiltonian.weightedToRawFactory`** — forward migration bridge that adapts a
  legacy weighted factory to the raw contract (`(p,q,r,s) ↦ 2·w(p,q,s,r)`), so
  pre-adapted data can drive the new raw builders and the `Optimization` entry points.
- **`Hamiltonian.antisymmetrizedToRawFactory`** — adapts an antisymmetrised double-bar
  tensor `⟨pq||rs⟩` (¼ convention) to the raw single-bar factory (scales two-body
  entries by ½). The core/nuclear energy remains a separate, caller-supplied constant.

### ⚠️ Deprecated

- **`Hamiltonian.rawPhysicistToWeightedFactory`** is now `[<Obsolete>]`: redundant
  because the primary builders consume raw integrals directly. Retained only to bridge
  raw data into the legacy `computeHamiltonianFromWeighted…` functions.
- **`Hamiltonian.computeHamiltonianFromPhysicist` / `…FromPhysicistWith`** are now
  `[<Obsolete>]` identity aliases of `computeHamiltonian` / `computeHamiltonianWith`
  (both now consume raw integrals). Use the primary builders directly.

### 📝 Documentation

- Added a standalone [0.9.0 migration guide](docs/guides/migration-0.9.md) explaining
  the old weighted factory behaviour, the new raw keys/values, FCIDUMP behaviour, the
  exact before/after mapping, and how to migrate to the named weighted API.
- Rewrote cookbook 10 (and updated 13) to feed the raw physicist tensor straight to
  the primary builders; README, ADR-014, and the test-register now describe the raw
  contract. The canonical executed outputs (15 terms, `IIII −0.8122`, four-body
  `±0.0453`) are unchanged and remain enforced by the semantic doc harness.
- Added an authoritative acceptance lock: the byte-for-byte vendored audited research
  artifact (`johnazariah/encodings-research` H₂/STO-3G physicist spin integrals),
  pinned to its immutable source object — commit `66ebdfe2…`, git blob `e0477e70…`,
  file SHA-256 `6539afb3…` — with a sidecar provenance record, literal
  per-particle-number sector eigenvalue arrays, and a hardened
  `scripts/check-doc-samples.sh` that executes every README/cookbook snippet with
  exact output assertions.

### 🐛 Fixes

- fix: correct the CNOT Clifford conjugation phase in `Tapering.applyClifford`
  (`cx && tz && (tx = cz)`); general (`FullClifford`) tapering now preserves the spectrum.
- fix: Hamiltonian assembly is now **cancellation-aware** — it removes only exact
  zeros and roundoff residues demonstrably produced by cancellation of multiple
  contributions (canonical H₂ has 15 terms, not 23), while **preserving standalone
  legitimate tiny coefficients** (e.g. 1e-12, 1e-13, 1e-15) on every sequential,
  parallel, cached and skeleton path. The previous absolute `|c| ≤ 1e-12` deletion
  (which could silently drop real physics) has been removed from the core builders
  and the `Fcidump` adapters.
- fix: `Hamiltonian` mode loops iterate `0..n-1` safely for `n = 0` (no `uint32` underflow).
- fix: `TreeEncoding.computeLinks` raises a clear error for nodes with more than 3 children.
- fix: Trotterization rejects non-Hermitian (materially imaginary) Pauli coefficients.

## [0.8.0] - 2026-03-17

### ✨ Features

- feat: add Vlasov complete ternary tree encoding (`vlasovTree`, `vlasovTreeTerms`) — implements the Clifford-algebraic construction of Vlasov (arXiv:1904.09912) via level-order indexed ternary trees, achieving O(log₃ n) Pauli weight

### 📖 Documentation

- docs: update cookbook (five → six encodings), add Vlasov to README, docs site, and JOSS paper
- docs: update JOSS paper for submission (test count 733, documentation references)
- docs: archive theory pages to `.project/archive/theory/`
- docs: add ADRs 015 (no Leanpub) and 016 (qubitization backend)

### 🧪 Tests

- test: 22 new Vlasov tree tests (construction, CAR verification, weight scaling, cross-validation)
- Total: **733 tests** (was 711)

## [0.7.0] - 2026-03-09

### ✨ Features

- feat: add Vlasov complete ternary tree encoding (`vlasovTree`, `vlasovTreeTerms`) — implements the Clifford-algebraic construction of Vlasov (arXiv:1904.09912) via level-order indexed ternary trees, achieving O(log₃ n) Pauli weight
- feat: add Trotterization module — first-order and second-order Trotter decomposition, Pauli rotation sequences, CNOT staircase gate decomposition
- feat: add CircuitOutput module — export gate sequences as OpenQASM 3.0, OpenQASM 2.0 (Quokka-compatible), Q#, and JSON
- feat: add VariationalCircuits module — measurement grouping (qubit-wise commuting), shot count estimation, QPE resource estimation
- feat: add `compareTrotterCosts` for side-by-side encoding cost analysis
- feat: add `trotterStepToOpenQasm` and `trotterStepToQSharp` convenience functions

### 📚 Documentation

- docs: complete README rewrite reflecting full pipeline (encode → taper → Trotter → export)
- docs: add 3 cookbook chapters (16-Trotterization, 17-CircuitOutput, 18-Measurement)
- docs: redesign GitHub Pages site with sidebar navigation
- docs: remove duplicated content (tutorials, labs, examples moved to encodings-book repo)
- docs: add encodings-book as git submodule

### 📖 Book

- book: published "From Molecules to Quantum Circuits" (22 chapters, ~44,000 words, 160 pages)
- book: created separate repo (encodings-book) with devcontainer, CI, Zenodo DOI
- book: computed results — H₂ dissociation curve (18 FCI points), H₂O bond angle scan (min at 99° STO-3G)
- book: reviewer remediation across 14 chapters
- book: Mermaid diagram rendering via pandoc Lua filter + mmdc

### 🔧 Maintenance

- ci: 711 tests passing
- chore: bump version to 0.7.0

## [0.6.0] - 2026-03-05

### ✨ Features

- feat: qubit tapering v1 — diagonal Z₂ symmetry detection, sector selection, qubit removal
- feat: qubit tapering v2 — general Clifford tapering with symplectic representation, binary Gaussian elimination, Clifford rotation synthesis
- feat: unified `taper` function combining diagonal and Clifford methods
- feat: `TaperingOptions` with method selection, sector control, `MaxQubitsToRemove`

### 🔧 Maintenance

- chore: upgrade to .NET 10 GA across entire repo
- chore: bump version to 0.5.1 for Zenodo DOI minting
- chore: separate research content for JOSS submission
- ci: fix release and draft workflows
- ci: set artifact retention to 30 days

## [0.5.0] - 2026-03-01

### ✨ Features

- feat: add parallel + skeleton APIs for Hamiltonian construction
- feat: add H₂O workshop lesson with PySCF integral pipeline
- feat: add bond angle scan (Part 8) to H₂O workshop
- feat: unify bibliography into shared/bibliography/references.bib

### 🐛 Bug Fixes

- fix: use comma-separated keys in Hamiltonian coefficient lookup

### 📚 Documentation

- docs: add 14 Architecture Decision Records
- docs: update copilot-instructions with accurate file descriptions and dev commands

### 🔧 Maintenance

- ci: fix release and draft workflows for 3-paper architecture
- chore: update author to 'John S Azariah' with full affiliation
- chore: standardise author metadata across all papers
- chore: move star-tree discovery artifacts to research/tools/
- chore: align devcontainer, .gitignore, and CI workflows
- chore: add copilot-instructions and AI prompts
- refactor: move .research/ to .project/research/
- research: consolidate to three-paper architecture
- spec: Trotterization module for FockMap
- review: add Trotterization motivation for Pauli weight

## [0.3.1] - 2026-02-15

## [0.4.0] - 2026-02-15

### ✨ Features

- feat: add bosonic-to-qubit encodings (Unary, Binary, Gray)

### 🐛 Bug Fixes

- fix(docs): change .fsx links to .html across all doc pages
- fix(docs): remove stale .fsdocs/cache and pass --clean to fsdocs
- fix(docs): also build Debug so fsdocs can resolve assembly references
- fix(docs): use Release configuration for fsdocs build
- fix(ci): skip duplicate NuGet push, remove nuget ref from cookbook ch01

### 📚 Documentation

- docs(paper): convert JOSS paper from LaTeX to Markdown format
- docs: add bosonic encoding cookbook chapter (ch.14) and release checklist
- docs(paper): update JOSS paper with bosonic-to-qubit encodings (497 tests)
- docs: update test register with 70 bosonic encoding tests (497 total)
- docs: add plain-English test register (.project/test-register.md)



### 🔧 Maintenance

- chore: standardise on .NET 8 LTS, fix API reference generation
- docs: restore [0.1.0] changelog entry, add README check to release prompt
- docs(readme): add cookbook links, remove dead guide references

## [0.3.0] - 2026-02-15

### 📚 Documentation

- docs(cookbook): add 13-chapter progressive tutorial, merge redundant guides
- docs(paper): add cookbook companion paper for arXiv/JOSS
- docs(paper): cross-reference cookbook in JOSS and tutorial papers

### 🔧 Maintenance

- chore(devcontainer): use .NET 10 preview SDK, add .NET 8 side-by-side
- chore(devcontainer): add jq; docs(paper): refresh software metrics
- devcontainer: remove hardcoded test count in post-create message
- ci(release): add cookbook PDF to release pipeline

## [0.2.0] - 2026-02-14

### 📚 Documentation

- docs: fix lab links for native markdown pages
- docs(theory): normalize ket/bra math delimiters
- docs(theory): fix parser-unsafe ket/bra math in chapter 1
- docs: make inline math bar notation pages-safe
- docs(pages): native markdown via Jekyll + fsdocs API-only reference
- docs(index): remove duplicate text heading in favor of logo
- docs(pages): keep markdown raw and limit post-processing to links/assets
- docs(pages): apply branding + fix mermaid runtime + streamline docs build
- docs: simplify onboarding and strengthen tutorial pedagogy

### 🔧 Maintenance

- test: make TypeExtensions reflection test CI-safe
- test: harden sequence sorting and swap-tracking edge cases
- test: harden parser and ordering branches for release readiness
- test: expose internals for branch coverage assertions
- test: raise line and branch coverage with edge-case paths
- test: expand coverage across terms, tree encoding, and helpers



### 🐛 Bug Fixes

- fix phase initialization value

### 🔧 Maintenance

- Refactor complete - all existing tests pass!

## [0.1.0] - 2026-02-14

Initial release.
