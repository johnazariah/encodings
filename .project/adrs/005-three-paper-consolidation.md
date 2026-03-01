# ADR-005: Three-Paper Architecture Consolidation

**Date:** 2026-02-27
**Status:** Accepted
**Source:** Research journal — paper consolidation session

## Context

Paper directories had proliferated to 7:

1. `paper-tutorial/` — AJP-style tutorial
2. `paper-software/` — JOSS submission
3. `paper-cookbook/` — arXiv supplement
4. `paper-algebraic/` — standalone algebraic methods paper
5. `paper-emergence/` — star-tree and encoding analysis
6. `paper-explainer/` — pedagogical companion
7. Various archived fragments

Content overlapped heavily. The star-tree theorem was being written in both
the emergence and algebraic papers. Pedagogical content duplicated the
documentation site. Maintaining 7 parallel paper drafts was unsustainable
for a solo-maintained project.

## Decision

Consolidate to 3 active papers:

1. **paper-review/** — Comprehensive tutorial review (46+ pages, targeting
   arXiv / Quantum). Absorbs star-tree theorem, algebraic methods,
   encoding comparison. This is the "everything" paper.

2. **paper-software/** — JOSS software paper (~6 pages). Focused on the
   library itself: design, API, testing, performance. References paper-review
   for theory.

3. **paper-explainer/** — Teaching companion (22+ pages, private / selective
   sharing). Aimed at university lecturers adopting the library. Not intended
   for journal submission.

Archive the remaining 4 directories.

## Consequences

- **Focus**: Three papers is manageable. Each has a distinct audience and venue.
- **No content loss**: All material is preserved — either absorbed into the
  surviving papers or archived.
- **Documentation boundary**: Pedagogical content that was becoming a "paper"
  now lives in `docs/` as cookbook chapters and labs. Papers contain only
  content that needs peer review.
- **Star-tree theorem**: Settled in paper-review (the comprehensive paper),
  not split across multiple drafts.

## Alternatives Considered

- **Keep all 7 papers** — too much parallel work for one person. Rejected.
- **Two papers** (review + software) — drops the teaching companion, which
  serves a different audience. Rejected.
- **One mega-paper** — JOSS requires a separate short paper. And a 60-page
  paper is unpublishable in most venues. Rejected.

## References

- Research journal, 2026-02-27: consolidation entry
- `.project/research/` — paper directories
