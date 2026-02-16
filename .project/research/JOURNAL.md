# Research Investigation Journal

Living record of investigations, discoveries, dead ends, and open questions.
Entries are reverse-chronological (newest first).

---

## 2026-02-16 — Paper 3 (Emergence) Draft: Major Revision Incorporating Discoveries

### Context

The Paper 3 scaffold was committed on 2026-02-15 (commit `18ca6ca`):
8 section drafts, paper.tex, paper.bib, Makefile — a clean LaTeX skeleton
for the PRA-targeted emergence paper. However, the drafts still contained
the **old hypothesis** (Theorem 3: "Construction A works for monotonic trees")
rather than the **star-tree discovery** documented on 2026-02-09.

This session rewrote all 8 draft sections, both appendices, the abstract,
and the bibliography to incorporate the actual computational findings.

### What changed (10 files, +561 / −155 lines, uncommitted)

**Core theorem rewrite (05-phase-boundary.tex)**
- Complete rewrite (~150 lines → ~250 lines)
- New structure: §5.1 Monotonicity → §5.2 Three constructions (A/B/F) →
  §5.3 Star-tree theorem → §5.4 Failure mode → §5.5 Counting → §5.6 Phase diagram
- Theorem 3 now states: "Construction A satisfies CAR iff tree is a star"
- Proof by exhaustive enumeration for n=3,4,5 with exact counts
- Three-region phase diagram: Stars (n trees) / Non-star monotonic ((n−1)!−n) /
  Non-monotonic (n^{n−1}−(n−1)!)
- Proposition: |M(n)| = (n−1)! with computational proof for n≤6
- Two universality classes: algebraic (tree-specific) vs geometric (universal)

**Validation data (06-validation.tex)**
- Complete rewrite with actual computed numbers:
  - Eigenspectrum equivalence: |Δλ| = 4.44×10⁻¹⁶ (machine precision)
  - H₂ ground state: E₀ = −1.7622 Ha (electronic), −1.0471 Ha (total)
  - Scaling table: max Pauli weight for n=4..24, all 5 encodings
  - Mean weight table: n=4..24 confirming TerTree lowest at every size
  - Parity operator weight table: JW=n, Parity/BK/BinTree=1, TerTree=O(log₃n)
  - Monotonicity census: full table n=1..6 with |M(n)|=(n−1)! and Pass CAR=n

**Introduction reframing (01-intro.tex)**
- "Phase boundary" subsection rewritten: SRL generality called "illusory",
  star-only restriction stated, three constructions named, (n−1)! fraction cited

**Tree-encoding correspondence (03-tree-encoding.tex)**
- "Critical restriction" updated: "only for stars" (was "only for monotonic")
- Recovery table: JW/Parity marked as stars (depth 1), BK uses Construction F
  (was "A"), BinTree/TerTree use Construction B
- Closing paragraph: three-construction landscape, forward-reference to §5

**Emergence (04-emergence.tex)**
- Parity weight examples expanded: added BinTree (weight 1) and TerTree
  (weight O(log₃n), n=8→2, n=27→3)
- Gauge structure example: clarified Parity and JW are both stars with
  different orderings
- TODO markers replaced with actual data comments

**Discussion (07-discussion.tex)**
- New paragraph: "The three-construction landscape" — algebraic constructions
  as brittle holographic codes, path-based as robust universal code
- Open question 3: updated from "monotonicity boundary" to star-tree
  restriction and three-region structure

**Conclusion (08-conclusion.tex)**
- Rewritten from "two universality classes" to three-construction narrative
- Added |M(n)|=(n−1)! as a concrete mathematical finding
- Named SRL framework explicitly and cited star-tree theorem

**Abstract (paper.tex)**
- Rewritten to state star-tree restriction, three constructions, and (n−1)!
  result directly

**Appendices (paper.tex)**
- Appendix B: Monotonicity counterexample — full computation for modes 4,7
  on n=8 balanced ternary tree. Shows index sets, spurious anticommutator
  terms (ZZZZZIXY, ZZZZZIXX), diagnostic D(T)=0.707
- Appendix C: Census data table for n=1..8 with columns for total, monotonic,
  stars, non-star monotonic, fraction

**Bibliography (paper.bib)**
- Added `bergeron1992`: Bergeron, Flajolet, Salvy — "Varieties of Increasing
  Trees", CAAP 1992, LNCS 581

### Build status

- Emergence paper excluded from release cycle: `make all` target in
  `.project/research/Makefile` has `#emergence` commented out
- These changes are uncommitted — the emergence paper is a long-term
  working document (estimated 6-month iteration toward viva defence)

### Key editorial decisions

1. **Construction F named explicitly.** The BK encoding's separate Fenwick
   formulas deserve their own label — they are neither Construction A on a
   Fenwick tree nor Construction B. This clarifies a genuine confusion in
   the literature.

2. **Proof by exhaustive enumeration accepted.** For n≤5, the star-tree
   theorem is proved computationally. An analytical proof for all n remains
   an open question (see §5.3 remark). This is honest and appropriate for
   a computational physics paper.

3. **Three-region phase diagram rather than binary boundary.** The original
   plan had a clean "monotonic vs non-monotonic" split. Reality is richer:
   stars / non-star monotonic / non-monotonic, with each region having
   different construction compatibility.

### Open questions for the 6-month iteration

1. **Analytical proof of star-tree theorem for all n** — can we show that
   the `treeRemainderSet` formula necessarily produces incorrect Z-assignments
   when depth ≥ 2?

2. **Bijective proof of |M(n)| = (n−1)!** — the heap-ordering connection
   (Bergeron et al.) is sketched but not formalised in the paper.

3. **Full proof of Proposition 1** (Construction B universality) — currently
   a proof sketch with "induction on tree depth". Needs to be rigorous.

4. **Emergence terminology** — need precise definitions that withstand viva
   examination: what exactly is meant by "emergent" vs "representation-dependent"?

5. **Holographic analogy limits** — the Discussion acknowledges what doesn't
   carry over, but needs sharper articulation.

6. **Figures** — no figures have been created yet. Need: tree diagrams,
   phase diagram, scaling plots, parity operator visualisation.

---

## 2026-02-15 (evening) — Emergence Paper Scaffold (PR #3)

### What was done

Created the full LaTeX skeleton for Paper 3 (Emergence) in
`.project/research/paper-emergence/` and merged as PR #3:

- `paper.tex` — revtex4-2 (PRA format), `\input{}` per section, custom
  commands for operators, sets, weights
- 8 section draft files (`01-intro.tex` through `08-conclusion.tex`) —
  substantive stubs, not just placeholders
- `paper.bib` — 21 references covering core encoding theory,
  emergence/holography, chemistry, software, combinatorics
- `Makefile` — build / watch / clean / wordcount targets
- `README.md` — project overview, claim tracker, figure list
- `figures/`, `supplementary/` — placeholder directories

Updated `.project/research/Makefile` with `emergence` and `watch-emergence` targets.
Paper compiles to 8 pages (two-column PRA format).

### Paper structure at scaffold stage

| Section | Content | Status |
|---------|---------|--------|
| §1 Intro | Motivating question, tree-encoding claim, emergence angle | Solid draft |
| §2 Background | CAR, Pauli algebra, three encodings, Majorana framework | Solid draft |
| §3 Tree-encoding | Trees, Construction A/B, recovery of known encodings | Solid draft |
| §4 Emergence | Locality, symmetry, gauge, renormalization | Conceptual heart, needs data |
| §5 Phase boundary | Monotonicity, theorem statement | **Used old hypothesis** |
| §6 Validation | Eigenspectrum, scaling, census | Stub with TODOs |
| §7 Discussion | Holographic map, adapted trees, open questions | Good framework |
| §8 Conclusion | Summary paragraph | Brief but coherent |

**Critical issue:** §5 still contained the original Theorem 3 ("Construction A
works for monotonic trees") rather than the star-tree discovery from 2026-02-09.
This was fixed in the next session (2026-02-16).

---

## 2026-02-15 (afternoon) — v0.3.1 Hotfix & Project Infrastructure

### v0.3.1 release (tag `v0.3.1`, commit `488c328`)

Quick fix release:
- Standardised on .NET 8 LTS (had drifted to preview SDKs in devcontainer)
- Fixed API reference generation in fsdocs
- Restored missing [0.1.0] changelog entry
- Added README check to release prompt
- Added cookbook links to README, removed dead guide references

### CI fix

- `73a1364` — Skip duplicate NuGet push when package version already exists;
  removed stale `nuget` reference from cookbook chapter 01 that broke builds

### Project infrastructure

Added tooling for maintainability:

- **`.project/test-register.md`** — Plain-English test register listing all
  test files, test counts, and what each test validates. Makes it easy to
  check that every behavior has coverage.
- **`.github/prompts/commit.prompt.md`** — Reusable Copilot prompt for the
  commit workflow (quality gate → group changes → imperative messages).
  Moved from `.project/` to `.github/prompts/` for discoverability.
- **`.github/prompts/release.prompt.md`** — Already existed; commit workflow
  complements it.

---

## 2026-02-15 (morning) — v0.3.0 Release: Cookbook & Devcontainer Overhaul

### Cookbook (13 chapters)

Created a comprehensive progressive tutorial at `docs/guides/cookbook/`:

| Chapter | Topic |
|---------|-------|
| 01 | Hello Qubit — first Pauli expression |
| 02 | Building Expressions — combining terms |
| 03 | Indexed Operators — sites and registers |
| 04 | Creation & Annihilation — fermionic operators |
| 05 | Normal Ordering — canonical form |
| 06 | First Encoding — JW by hand |
| 07 | Five Encodings — side-by-side comparison |
| 08 | Encoding Internals — Majorana framework |
| 09 | Trees — building custom tree encodings |
| 10 | Building a Hamiltonian — H₂ end-to-end |
| 11 | Mixed Systems — bosonic + fermionic |
| 12 | Utilities — helper functions |
| 13 | Grand Finale — putting it all together |

Merged redundant guide pages into cookbook. This replaces scattered docs with
a single learning path.

### Cookbook companion paper

Created `.project/research/paper-cookbook/` — a short arXiv/JOSS companion paper
documenting the cookbook's pedagogical design. Cross-referenced in both the
tutorial paper and the JOSS software paper.

### Devcontainer overhaul

- `fe91429` — Switched to .NET 10 preview SDK with .NET 8 side-by-side
  (needed for latest tooling while keeping production target at net8.0)
- Added `jq` to devcontainer tooling
- Refreshed software metrics in JOSS paper
- Removed hardcoded test count from post-create message (was showing "303
  tests" but count changes with coverage work)

### Release pipeline

- Added cookbook PDF to release pipeline artifacts
- v0.3.0 tagged and released (`1323689`)

---

## 2026-02-14 (afternoon) — v0.2.0 Release: Bosonic Extension & Docs Hardening

### PR #2: Bosonic & mixed-system support (`67298ce`)

Major feature addition (22 files, +1,412 / −270 lines):

**New library modules:**
- `Bosonic.fs` — Bosonic creation/annihilation with CCR (commutation relations)
- `MixedSystems.fs` — Sector-aware mixed normal ordering (fermionic + bosonic)
- `CombiningAlgebra.fs` — Extended combining layer for bosonic terms

**New tests:**
- `Bosonic.fs` — CCR swap/identity verification
- `MixedSystems.fs` — Sector separation, hybrid pipeline tests

**New examples:**
- `Mixed_NormalOrdering.fsx` — Basic mixed-system normal ordering
- `Mixed_ElectronPhonon_Toy.fsx` — Toy electron-phonon model
- `Mixed_HybridCompare.fsx` — Compare fermionic vs bosonic handling
- `Mixed_HybridPipeline.fsx` — End-to-end hybrid workflow

**New documentation:**
- `docs/theory/07-mixed-systems.md` — Theory chapter on mixed systems
- `docs/guides/mixed-registers.md` — Practical mixed-system guide
- `docs/guides/advanced-operations.md` — Practical playbook (replaced old guide)
- Overhauled `docs/guides/architecture.md` for GitHub rendering

### Test coverage push

Six commits hardening test coverage after v0.1.0:

- `4fa9b6a` — Expand coverage: terms, tree encoding, helpers
- `146ad40` — Raise line and branch coverage with edge-case paths
- `569d335` — Expose internals for branch coverage assertions (`InternalsVisibleTo`)
- `0e27df8` — Harden parser and ordering branches
- `dacc33d` — Harden sequence sorting and swap-tracking edge cases
- `dc1408c` — Make TypeExtensions reflection test CI-safe (platform-independent)

### Documentation pages overhaul

Significant effort getting GitHub Pages to render math and diagrams correctly:

- `7374982` — Branding + Mermaid runtime fix + streamlined docs build
- `41ead06` — Keep markdown raw, limit post-processing to links/assets
- `7a3c7ba` — Native markdown via Jekyll + fsdocs API-only reference (split
  strategy: Jekyll renders theory/guides, fsdocs generates API reference only)
- `d8fd2b4`, `311ac00`, `fca2713` — Three passes fixing ket/bra math notation
  for Pages parser safety (MathJax vs fsdocs conflicts)
- `eabc087` — Fix lab links for native markdown pages
- Simplified onboarding docs and strengthened tutorial pedagogy

### v0.2.0 tagged and released (`27b89ec`)

---

## 2026-02-14 (morning) — v0.1.0 Release (PR #1)

### First public release

PR #1 merged (`a8cc64f`, 131 files, +22,091 / −1,221 lines). This was the
full repository cleanup documented in the entry below. Tagged as `v0.1.0`
(`1a6cdb5`).

### CI fixes for LaTeX paper builds

Two quick fixes needed after tagging:
- `ecf6927` — Install `texlive-publishers` for revtex4-2 paper compilation
- `1a6cdb5` — Install `lmodern` font package for LaTeX builds

Both were needed because the CI runner's TeX Live installation was minimal.

---

## 2026-02-14 — Repository Cleanup, Documentation & Release Infrastructure

### Context

Executed the full 9-phase cleanup plan (`cleanup-dotnet-repo.md`) to prepare
the repository for JOSS submission and public release. The library is being
published as **FockMap** (NuGet PackageId), in the repository
`github.com/johnazariah/encodings`.

### Phase 1: Project structure reorganisation

Moved from flat layout to canonical .NET solution structure:

```
src/Encodings/          ← library (15 source files)
test/Test.Encodings/    ← tests (19 test files, 303 tests)
examples/               ← runnable .fsx scripts
docs/                   ← fsdocs site source
scripts/                ← automation
.github/workflows/      ← CI/CD
.github/prompts/        ← Copilot prompt files
```

Added `Encodings.sln` at root. Both projects build and test clean on .NET 8.0.

### Phase 2: XML documentation

Added comprehensive `///` XML doc comments to all 15 source files. Every public
type, module, and function now has structured documentation with `<summary>`,
`<param>`, `<returns>`, `<remarks>`, and `<example>` tags where appropriate.
`GenerateDocumentationFile` enabled in `.fsproj`.

### Phase 3: Dead code removal

- Removed empty `Tests.fs` from test project
- Cleaned commented-out code across source files

### Phase 4: Repository root files

Created:
- **README.md** — badges (CI, codecov, NuGet, license, platform), feature
  overview, quick start, API summary, encoding comparison table, citation
- **LICENSE** — MIT
- **CONTRIBUTING.md** — development setup, coding standards, PR process
- **CITATION.cff** — CFF format with ORCID, version, DOI placeholder
- **.editorconfig** — F# formatting conventions

### Phase 5: Example scripts

Created 4 runnable examples in `examples/`:

| Script | Purpose |
|--------|---------|
| H2_Encoding.fsx | Encode H₂ Hamiltonian with all 5 encodings |
| Compare_Encodings.fsx | Side-by-side Pauli weight comparison |
| Custom_Encoding.fsx | Build a custom Majorana encoding |
| Custom_Tree.fsx | Construct and use a custom tree encoding |

All verified working with `dotnet fsi`.

### Phase 6: GitHub Pages documentation site

Built a full documentation site using `fsdocs` (16 pages):

**Background** (6 pages):
- Second Quantization, Pauli Algebra, Jordan-Wigner, Bravyi-Kitaev,
  Fenwick Trees, Tree Encodings

**Tutorials** (6 literate .fsx scripts):
- Getting Started, Encoding Hamiltonians, Comparing Encodings,
  Custom Encodings, Tree-Based Encodings, Advanced Symbolic Algebra

**Guides** (3 pages):
- API Reference, Performance Tips, Extending the Library

Verified with `dotnet fsdocs build` — all HTML generates cleanly.
Site configured for GitHub Pages at `/encodings/` root.

### Phase 7: NuGet package metadata

Configured `Encodings.fsproj` with full NuGet metadata:
- `PackageId`: FockMap
- `Version`: 0.1.0
- `Authors`: John Azariah
- `PackageLicenseExpression`: MIT
- `RepositoryUrl`: github.com/johnazariah/encodings
- Source Link enabled, symbols included
- `PackageReadmeFile` and `PackageIcon` configured

### Phase 8: CI/CD workflows

Created three GitHub Actions workflows:

**ci.yml** — Continuous Integration:
- Triggers on every push/PR
- Linux-only (cost optimisation — no macOS/Windows on every commit)
- Runs `dotnet build` + `dotnet test` with code coverage via coverlet
- Uploads coverage to Codecov

**release.yml** — Release pipeline:
- Triggers on `v*` tag push
- Multi-platform test matrix (Linux, Windows, macOS) with `fail-fast: true`
- Packs NuGet package, publishes to nuget.org
- Creates GitHub Release with `.nupkg` artifact

**docs.yml** — Documentation:
- Triggers on push to `main`
- Builds fsdocs site, deploys to GitHub Pages

### Phase 9: Test coverage

Added `coverlet.collector` to test project. Current coverage:
- **78% line coverage** (303/303 tests passing)
- **66% branch coverage**
- Codecov badge in README (needs `CODECOV_TOKEN` secret)

### Release infrastructure

Created three levels of release automation:

1. **`scripts/release.sh`** — Interactive bash script:
   - Analyzes conventional commits since last tag
   - Proposes PATCH/MINOR/MAJOR based on commit types
   - Updates `.fsproj` version, generates `CHANGELOG.md`, updates `CITATION.cff`
   - Commits, tags, pushes, monitors CI via `gh run watch`
   - Supports `--dry-run` mode

2. **`.github/workflows/release-dispatch.yml`** — GitHub Actions workflow:
   - `workflow_dispatch` trigger from UI or `gh workflow run`
   - Same logic as local script but runs entirely server-side
   - Accepts bump type input: auto, patch, minor, major
   - Pushes tag to trigger `release.yml` pipeline

3. **`.github/prompts/release.prompt.md`** — Copilot prompt:
   - Reusable prompt file for Copilot Chat (`#release`)
   - Step-by-step instructions for the full release flow
   - Includes confirmation gate before making changes

### Author & metadata corrections

- Family name: Azariah (not Aziz)
- ORCID: 0009-0007-9870-1970
- GitHub: johnazariah
- Repository URL: github.com/johnazariah/encodings (not FockMap)
- Updated across all files: README, CITATION.cff, .fsproj, workflows, docs

### Naming decision

- **Repository**: `encodings` (current, unchanged)
- **NuGet package**: `FockMap` (covers future bosonic modes, not just fermionic)
- **`[<AutoOpen>]`**: Kept as-is; documented but deferred to JOSS reviewer feedback

### Statistics

- 92 files changed, 11,004 insertions, 1,221 deletions
- 303 tests passing
- 32 commits in branch `johnaz/review`
- PR #1 opened against `main`

### What's next

- Merge PR #1 (squash into `main`)
- Set up Codecov token and NuGet API key in GitHub Secrets
- First release: `v0.1.0`
- Continue Paper 1 (Tutorial) and Paper 2 (JOSS) iterations
- Address `[<AutoOpen>]` based on reviewer feedback

---

## 2026-02-09 (night) — Paper Drafts v0.1 Written

### Paper 1: "From Molecules to Qubits" (Tutorial, AJP target)

Draft at `paper-tutorial/drafts/draft-v01.md`.

**What's in it:**
- Full 10-section structure following the PLAN.md
- Complete pipeline: Schrödinger equation → Born-Oppenheimer → basis sets →
  second quantization (Fock space, CAR, Hamiltonian) → notation minefield
  (chemist vs physicist integrals) → spin-orbital expansion → JW encoding
  (worked out with Majorana operators) → full H₂ 15-term Hamiltonian →
  verification by exact diagonalisation → what's next (VQE/QPE)
- All three common errors documented (notation, double-counting, operator ordering)
- Cross-encoding comparison table (JW, BK, Parity, balanced binary, balanced ternary)
- Three appendices: integral tables, Pauli algebra reference, (code companion TBD)
- 11 references

**What's NOT in it yet:**
- Numerical values need verification against IntegralTables.fsx output (some
  are approximate in the draft)
- Figures (none yet — need orbital diagrams, encoding pipeline diagram,
  weight-vs-n plot, eigenvalue comparison)
- Appendix C (code companion) is sketched but not written
- Tone needs a pass — currently too formal in places for AJP audience
- Need to add more "intuition first" passages per PLAN.md pedagogical principles
- The "other encodings" section (6.4) is thin — needs worked BK example

**Estimated completeness:** ~60% of final text. Good structural skeleton.

### Paper 2: "FockMap" (JOSS, ~1000 words)

Draft at `paper-software/drafts/draft-v01.md` + `paper.bib`.

**What's in it:**
- JOSS YAML frontmatter (title, tags, authors — placeholder ORCID)
- Summary (~230 words): two frameworks, 5 encodings, pure functional, 303 tests
- Statement of Need (~280 words): gap analysis vs OpenFermion/Qiskit/PennyLane,
  who needs this and why
- Functionality (~250 words): index-set schemes, tree encodings, data structures,
  verification suite — with code example
- Design Principles (~200 words): encodings-as-data, two frameworks, pure functions,
  discovered constraints (monotonicity)
- Comparison table: 7-feature comparison with 3 competitors
- Related Software narrative (~100 words)
- BibTeX file with 9 references

**What's NOT in it yet:**
- Author ORCID and affiliation are placeholders
- Repository not on GitHub yet (all the JOSS checklist items: LICENSE, README,
  CONTRIBUTING, CITATION.cff, CI, Zenodo DOI)
- No architecture diagram
- References section placeholder (needs pandoc compilation)
- Need to verify word count fits JOSS limits

**Estimated completeness:** ~75% of the paper text. Repository preparation is 0%.

### Decision point
Paper 1 needs months of iteration — teaching intuition, adding figures,
verifying numerics. Paper 2 needs repository preparation before submission.
Neither is close to submission-ready, but both have solid v0.1 skeletons
to iterate on.

---

## 2026-02-09 (evening) — Synthesis: The Star-Tree Discovery Explained

### What we have

There are two ways to build a fermion-to-qubit encoding from a tree:

**Construction A** (index-set method, `treeEncodingScheme` in `TreeEncoding.fs`):
Define three sets for each mode j — Update(j), Parity(j), Occupation(j) — and
plug them into the Seeley-Richard-Love formulas:

    c_j = X_{U(j) ∪ {j}} · Z_{P(j)}
    d_j = Y_j · X_{U(j)} · Z_{(P(j) ⊕ Occ(j)) \ {j}}

This is how JW, BK, and Parity are defined in the original literature
(Seeley et al. arXiv:1208.5986).

**Construction B** (path-based method, `encodeWithTernaryTree` in `TreeEncoding.fs`):
Label each tree edge with a Pauli (X, Y, Z), walk root-to-leaf paths to get
Majorana strings, and pair them via the "follow X then Z" / "follow Y then Z"
rule (Jiang et al. arXiv:1910.10746, Bonsai arXiv:2212.09731). Works for any
tree shape.

### What we expected (Paper 3 plan, Theorem 3)

The original hypothesis was:

- Monotonic trees (parent index > child index on every edge) →
  Construction A works ✅
- Non-monotonic trees → Construction A fails ❌, only Construction B works
- This creates a "structural phase boundary" in the space of labelled trees

A clean story: algebraic methods work on one side, geometric methods needed
on the other. The boundary itself would be a measurable structural transition.

### What we actually found (exhaustive enumeration)

We enumerated ALL labelled rooted trees on n = 3, 4, 5 nodes (9, 64, 625
trees) and tested Construction A on each by checking the full CAR:

    {a_i, a†_j} = δ_{ij}I     for all i, j

Results:

    n=3:  9 total trees,   2 monotonic,  3 pass CAR  → all 3 are stars
    n=4:  64 total trees,  6 monotonic,  4 pass CAR  → all 4 are stars
    n=5:  625 total trees, 24 monotonic, 5 pass CAR  → all 5 are stars

A **star tree** is a tree of depth 1: one root with all other nodes as direct
children. The n passing trees for each n correspond to the n choices of which
node to designate as root.

Key observation: the number of passing trees is LESS than the number of
monotonic trees. Construction A doesn't just fail on non-monotonic trees —
it fails on most monotonic trees too.

### Why does BK work if Fenwick trees aren't stars?

This is the critical subtlety. The Bravyi-Kitaev encoding uses a Fenwick tree,
which is definitely not a star — it has depth O(log n). But BK does NOT go
through `treeEncodingScheme`. It uses `bravyiKitaevScheme` in
`MajoranaEncoding.fs`, which delegates to `FenwickTree.fs` — completely
separate code with hand-derived bit-manipulation formulas:

    updateSet(j)     = ancestors of j in the Fenwick tree
    paritySet(j)     = prefix-parity indices via bit tricks
    occupationSet(j) = subtree-parity indices via bit tricks

These formulas are correct FOR FENWICK TREES. But they are NOT what
`treeEncodingScheme` computes when you hand it a Fenwick-shaped
`EncodingTree`. The generic formula in `treeRemainderSet` uses a
`child.Index < j` condition that is too restrictive for non-star trees: it
misses nodes that should be in the remainder set and includes path nodes
that shouldn't be.

### So there are THREE constructions, not two

1. **Star construction** (`treeEncodingScheme`):
   Works only for depth-1 trees. Essentially JW with qubit relabelling.

2. **Fenwick construction** (`bravyiKitaevScheme`):
   Works only for Fenwick trees. Hand-derived, tree-specific formulas.
   Cannot be extended to other tree shapes without re-deriving everything.

3. **Path-based construction** (`encodeWithTernaryTree`):
   Works for ALL trees. The only universal method. This is what balanced
   binary and balanced ternary encodings use.

### Impact on Paper 3

The "structural phase boundary" story is **sharper** than planned:

The original plan said "Construction A works for monotonic trees."
The correct statement is: "Construction A works only for stars."

This means the algebraic (index-set) framework is far more brittle than
the literature suggests. The Seeley-Richard-Love unification of JW, BK,
and Parity into one framework APPEARS general, but:

- JW is a star (the only non-trivial encoding the formula handles)
- BK uses separate, hardcoded Fenwick formulas (not the generic framework)
- Parity is also a star variant

The SRL framework's generality is an illusion. The path-based construction
from Jiang et al. / Bonsai is the truly general method.

### The monotonic tree count: |M(n)| = (n−1)!

Separately, we proved computationally that the number of monotonic labelled
rooted trees on n nodes is exactly (n−1)! for n = 1, ..., 6.

    n    nⁿ⁻¹ (total)    (n-1)! (monotonic)    fraction
    1    1                1                      1.000
    2    2                1                      0.500
    3    9                2                      0.222
    4    64               6                      0.094
    5    625              24                     0.038
    6    7776             120                    0.015

The fraction (n−1)!/nⁿ⁻¹ decays super-exponentially at rate ~e⁻ⁿ (Stirling).
So even if Construction A worked for all monotonic trees, they'd be a vanishing
minority of the encoding space.

The (n−1)! count likely follows from the bijection between monotonic labellings
and heap orderings. A monotonic labelled rooted tree is equivalent to a
heap-ordered tree (parent > children everywhere), and the number of such
labellings on any fixed unlabelled tree is related to the hook-length formula.
Summing over all unlabelled rooted trees on n nodes gives (n−1)!.

Reference: Bergeron, Flajolet, Salvy — "Varieties of Increasing Trees"
(CAAP 1992, LNCS vol 581).

### Open questions for Paper 3

1. **Can `treeRemainderSet` be fixed?** Is there a correct formula for
   Update/Parity/Occupation that works for arbitrary monotonic trees? Or is
   the star-only restriction fundamental to any index-set method?

2. **Proof of (n−1)! count:** We have computational proof for n ≤ 6. Need
   a clean bijective or generating-function proof for the paper.

3. **What makes BK special?** The Fenwick tree has a unique algebraic
   structure (binary indexed tree, prefix sums via bit manipulation). Is
   there a family of trees for which tree-specific index-set formulas can
   be derived? Or is Fenwick the only one?

4. **Revised Theorem 3:** The statement should be something like:
   "The index-set construction (SRL framework) produces valid encodings if
   and only if the tree is a star. For general trees, the path-based
   construction is necessary and sufficient." Then BK gets a separate
   remark as a historical special case with bespoke formulas.

### Strategic note

These papers will take months. The maths involved spans:
- Finite group theory (Pauli group, CAR algebra)
- Combinatorics (labelled trees, Prüfer sequences, heap orderings)
- Quantum information (encoding maps, Majorana operators)
- Quantum chemistry (second quantization, molecular integrals)

The journal is the memory. Every investigation, every failed attempt, every
"wait, that's not right" gets recorded here. When we come back to write
Section 4 of Paper 3 in three months, we need to reconstruct exactly WHY
we believe what we believe.

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
1. *DLL path:* Scripts in `.project/research/tools/` need `#r "../../Encodings/bin/Debug/net8.0/Encodings.dll"` — two levels up, not one.
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
