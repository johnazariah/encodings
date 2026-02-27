# Research Papers — Fermion-to-Qubit Encodings

Three papers supporting the FockMap project: a comprehensive tutorial
review (arXiv), a software paper (JOSS), and a private teaching
companion.

## Papers

| # | Directory | Title | Target | Status |
|---|-----------|-------|--------|--------|
| 1 | `paper-review/` | Fermion-to-Qubit Encodings: A Tutorial Review of the Three-Construction Landscape | arXiv / Quantum | Draft, 46 pages |
| 2 | `paper-software/` | FockMap: A Composable Functional Framework for Symbolic Fock-Space Operator Algebra | JOSS | Draft, ~6 pages |
| 3 | `paper-explainer/` | Why Most Fermion-to-Qubit Encoding Trees Don't Work | Private (teaching/presentations) | Draft, 22 pages |

## Archived Papers

Earlier drafts that were superseded during consolidation live in
`_archive/`.  Their content has been folded into the review paper.

| Directory | What it was | Why archived |
|-----------|-------------|--------------|
| `_archive/paper-algebraic/` | Star-tree theorem paper (PRA target) | Content absorbed into review §7–§8 |
| `_archive/paper-emergence/` | Emergence / phase-boundary paper | Content absorbed into review §7–§8 |
| `_archive/paper-tutorial/` | Pedagogical tutorial (AJP target) | Content absorbed into review §1–§6; also in `docs/from-molecules-to-qubits/` |
| `_archive/paper-cookbook/` | Library cookbook | Not a paper — lives as `docs/guides/cookbook/` in the repo |

## Shared Resources

- `shared/bibliography/` — BibTeX references shared across all papers
- `tools/` — Verification & analysis scripts
  - Matrix-level eigenspectrum validation
  - Symmetry analysis (Z₂ stabilizer detection)
  - CNOT cost estimation
  - Encoding space explorer (random trees, phase diagram)

## Build

```bash
# Build all papers
make all

# Build individually
make review      # arXiv paper (latexmk)
make software    # JOSS paper (builds via GitHub Actions)
make explainer   # Teaching companion (latexmk)

# Clean
make clean
```

## Investigation Journal

See [JOURNAL.md](JOURNAL.md) for the running log of investigations,
discoveries, and open questions.
