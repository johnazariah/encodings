# Changelog

All notable changes to FockMap will be documented in this file.

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
