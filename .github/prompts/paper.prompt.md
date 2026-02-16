# Research Paper Writing & Building

You are helping write, edit, and build academic papers for the **FockMap** fermion-to-qubit encoding project.

---

## Paper Portfolio

All papers live under `.project/research/`:

| Directory | Title | Format | Target Venue | Status |
|-----------|-------|--------|--------------|--------|
| `paper-tutorial/` | From Molecules to Qubits: A Complete Guide to Quantum Chemistry Simulation | LaTeX | AJP / Quantum | Draft v0.1 |
| `paper-software/` | Algebraic Encodings: A Typed Functional Framework for Fermion-to-Qubit Mappings | JOSS Markdown | JOSS / SoftwareX | Draft v0.1 |
| `paper-cookbook/` | FockMap Library Cookbook | LaTeX | arXiv / JOSS supplement | Draft v0.1 |
| `paper-emergence/` | Emergent Structure in Fermion-to-Qubit Encodings | LaTeX | PRA / PRResearch | Scaffold (major revision) |

### Format Details

- **JOSS** (`paper-software/`): Markdown front matter + `paper.md`. Built by GitHub Actions via `openjournals/openjournals-draft-action`. No local build needed — push to trigger CI.
- **LaTeX papers**: Each has `paper.tex` as the main file. Built with `latexmk -pdf`.
- All LaTeX papers use BibTeX via `\bibliography{../shared/bibliography/references}`.

---

## Shared Bibliography

**Single shared .bib file**: `shared/bibliography/references.bib`

### Citation Key Convention

Use `firstauthor_year` format:

| Pattern | Example |
|---------|---------|
| `author_year` | `jordan1928` |
| `author_yeartopic` | `seeley2012` |
| `author_yearX` | `bravyi2002`, `bravyi2017` |

### Adding a New Reference

1. Add the entry to `shared/bibliography/references.bib`
2. Use the `firstauthor_year` key convention
3. Always include `doi` when available; include `eprint` + `archiveprefix` for arXiv papers
4. The entry is immediately available to all papers

### JOSS Symlink

`paper-software/paper.bib` is a **symlink** to `../shared/bibliography/references.bib`. Do not replace it with a regular file. The JOSS front matter references `bibliography: paper.bib` which resolves through the symlink.

---

## Building Papers

### Local Build

```bash
cd .project/research

# Build all papers
make all

# Build a specific paper
make tutorial
make cookbook
make emergence

# Clean build artifacts
make clean
```

### JOSS Paper (CI only)

The JOSS paper builds via GitHub Actions (`draft-paper.yml`). Push changes to `paper-software/` or `shared/bibliography/` to trigger a build. The workflow produces a PDF artifact.

### Paper-Specific Makefiles

The emergence paper has its own Makefile at `paper-emergence/Makefile` with a `BIB` variable pointing to the shared bibliography.

---

## Verification Tools

Scripts in `tools/` verify encoding results used in the papers:

- **Matrix-level eigenspectrum validation** — confirms operator algebra is correct
- **Symmetry analysis** — Z₂ stabilizer detection
- **CNOT cost estimation** — circuit complexity metrics
- **Encoding space explorer** — random trees, phase diagram generation

All scripts reference the compiled library. Build it first:

```bash
cd ../../src && dotnet build Encodings/Encodings.fsproj
```

---

## Writing Guidelines

### LaTeX Conventions

- **One sentence per line** — makes diffs cleaner and review easier
- Use standard LaTeX cross-referencing (`\ref{}`, `\eqref{}`)
- Mathematical notation:
  - Ladder operators: `a^\dagger_j`, `a_j`
  - Majorana operators: `c_j`, `d_j` (or `\gamma_{2j}`, `\gamma_{2j+1}`)
  - Pauli operators: `X_j`, `Y_j`, `Z_j` (uppercase, subscripted)
  - Encoding maps: `\mathcal{E}`, `\mathcal{M}`
- Figures: prefer TikZ for tree diagrams; use pgfplots for scaling data

### JOSS Conventions

- Markdown with YAML front matter
- References via `@citekey` syntax (pandoc-style)
- Maximum ~1000 words
- Must include: Summary, Statement of Need, References
- See [JOSS submission guidelines](https://joss.readthedocs.io/en/latest/submitting.html)

### General

- British English spelling (consistent with the rest of the project)
- Define acronyms on first use (JW, BK, CAR, CCR)
- Every claim should be backed by a citation or verification script result
- Cross-reference between papers — they form a coherent set

---

## Paper Relationships

The papers form a complementary set:

```
Tutorial (pedagogical introduction, molecules → qubits)
    ↕ shares foundation with
Software (library design, typed algebra, JOSS)
    → Cookbook (progressive worked examples, companion to Software)
    → Emergence (research contribution: star-tree discovery, locality structure)
```

Key discovery to emphasise: the **star tree** (complete bipartite K_{1,n}) achieves minimal Pauli weight — this is the central result of Paper 3 (Emergence) and should be referenced accurately from other papers.

---

## Investigation Journal

Check `.project/research/JOURNAL.md` before writing — it contains the running log of discoveries, open questions, and design decisions that inform the papers. Update it after completing significant writing or analysis work.

---

## Workflow

1. **Before writing**: Read the paper's existing draft and the JOURNAL for context
2. **Adding citations**: Add to shared bib, use consistent keys
3. **After editing LaTeX**: Build locally (`make <paper>`) to verify compilation
4. **After editing JOSS**: Push to trigger CI build; download the artifact to review
5. **Verification**: Run relevant tool scripts to validate any numerical claims
6. **Figures**: Commit TikZ source; generated PDFs are build artifacts
