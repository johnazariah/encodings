# Paper 3 — Emergent Structure in Fermion-to-Qubit Encodings

> Trees, Locality, and the Geometry of Representation

**Target:** Physical Review A / Physical Review Research / Quantum
**Status:** Scaffolding
**Branch:** `research/paper-emergence`

---

## Thesis

A fermion-to-qubit encoding is a minimal, exactly solvable model of quantum
emergence.  The choice of a classical combinatorial object — a labelled rooted
tree — completely determines every emergent quantum property (locality, symmetry,
gauge structure, complexity) of the resulting qubit representation.

## Repository Layout

```
paper-emergence/
├── README.md           ← this file
├── OUTLINE.md          ← section-level outline with core claims
├── PLAN.md             ← detailed writing guide and proof sketches
├── paper.tex           ← main LaTeX manuscript
├── paper.bib           ← BibTeX references
├── Makefile            ← build / watch targets
├── drafts/             ← per-section drafts (01-intro.tex, …)
├── figures/            ← TikZ sources + generated PDFs
└── supplementary/      ← appendices, data tables, extended proofs
```

## Quick Build

```bash
make              # single build
make watch         # continuous rebuild on save (latexmk -pvc)
make clean         # remove artefacts
```

Requires: `latexmk`, `pdflatex`, TikZ/PGFplots (all present in the dev container).

## Core Claims

| # | Claim | Status |
|---|-------|--------|
| 1 | Tree–Encoding Correspondence | Outline |
| 2 | Emergent Locality (weight bound) | Outline |
| 3 | Emergent Symmetry Fractionalization | Outline |
| 4 | Emergent Gauge Structure | Outline |
| 5 | Structural Phase Boundary (monotonicity) | Outline |
| 6 | Optimal Renormalization (balanced ternary) | Outline |

## Formal Results

| Result | Statement | Status |
|--------|-----------|--------|
| Theorem 1 | $w(a^\dagger_j) \le 2\cdot\text{depth}(j)+1$ | Needs proof |
| Theorem 2 | Parity weight = f(tree leaves, Z-chain) | Needs statement |
| Theorem 3 | Index-set construction ⟺ monotonic tree | Needs proof |
| Conjecture 1 | Optimal tree ↔ optimal renormalization | Open |
| Conjecture 2 | Monotonic fraction → 0 exponentially in n | Needs numerics |

## Computational Dependencies

Scripts live in `../../tools/` (relative to this directory):

| Script | Purpose | Status |
|--------|---------|--------|
| `MatrixVerification.fsx` | Eigenspectrum equivalence | 🔲 |
| `AnticommutationTest.fsx` | Full CAR verification | 🔲 |
| `MonotonicityCensus.fsx` | Tree enumeration + monotonicity test | 🔲 |
| `ParityOperator.fsx` | Parity-operator weight per encoding | 🔲 |
| `ScalingBenchmark.fsx` | Weight vs. n data | ✅ |

## Figures Needed

1. Five encoding trees side-by-side (JW, Parity, BK, BinTree, TerTree)
2. Locality paradox: same hopping term, different weights
3. Symmetry fractionalization: parity operator across encodings
4. Monotonicity counterexample: n=8, modes 4 & 7
5. Phase diagram: monotonic fraction vs. n
6. Scaling: log-log weight vs. n
7. MERA analogy: balanced ternary as RG scheme
8. Holographic diagram: fermion ↔ tree ↔ qubit

## Related Papers

- **Paper 1 (Tutorial):** `../paper-tutorial/` — pedagogical walkthrough
- **Paper 2 (Software):** `../paper-software/` — JOSS-style library paper
- **Paper 4 (Cookbook):** `../paper-cookbook/` — progressive tutorial paper
