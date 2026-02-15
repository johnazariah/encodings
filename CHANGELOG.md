# Changelog

All notable changes to FockMap will be documented in this file.

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
