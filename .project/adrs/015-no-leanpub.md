# ADR-015: Do Not Publish the Book on Leanpub

**Date:** 2026-03-11
**Status:** Accepted
**Source:** PhD advisor feedback

## Context

The companion book *From Molecules to Quantum Circuits* was initially
planned for self-publication on Leanpub. Build infrastructure (Makefile
targets, `Leanpub.yml` metadata, API integration) was in place.

After consultation with the PhD advisor, we decided against Leanpub
publication. The concerns were:

1. **Academic credibility.** Self-published technical books on Leanpub
   carry less weight in academic contexts than peer-reviewed or
   university-press publications. For a PhD candidate, the publication
   venue matters for career progression.

2. **JOSE paper overlap.** The book already has a companion JOSE
   (Journal of Open Source Education) submission at `book/jose/paper.md`.
   A peer-reviewed JOSE publication provides the academic credibility
   that Leanpub cannot.

3. **Open access alternatives.** The manuscript is already freely
   available as a PDF built from the open-source repository. Leanpub
   would add a paywall without adding peer review.

## Decision

**Do not publish on Leanpub.** Remove all Leanpub-specific build
infrastructure from the book repository.

The book will be distributed through:
- The open-source GitHub repository (source + PDF build)
- The JOSE paper (peer-reviewed pedagogical publication)
- The companion website (GitHub Pages)

## Consequences

- Leanpub Makefile targets (`leanpub`, `preview`, `leanpub-check`)
  removed.
- `LEANPUB_SLUG` variable removed from Makefile.
- `Leanpub.yml` retained as general metadata but no longer used for
  Leanpub API integration.
- No revenue stream from book sales — the book is freely available.
- Academic credibility comes from the JOSE submission instead.
