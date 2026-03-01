# ADR-003: AutoOpen on All Modules

**Date:** 2026-02-09
**Status:** Accepted
**Source:** Cleanup plan; visible in every source file header

## Context

All source files in `src/Encodings/` are `[<AutoOpen>]` modules inside
`namespace Encodings`. This means `open Encodings` imports every symbol
from every module — approximately 50 public types and hundreds of functions
in a single flat namespace.

The F# style guide recommends `[<AutoOpen>]` sparingly. For a library with
15+ modules, this risks namespace pollution and makes it hard for users to
discover where a symbol is defined.

## Decision

Keep `[<AutoOpen>]` on all modules. Document the rationale. Revisit only if
JOSS reviewers flag it.

## Consequences

- **Scripting UX**: The primary consumption mode is `.fsx` scripts where a
  single `open Encodings` makes everything available — ideal for interactive
  exploration and workshops.
- **Discoverability**: IDE tooling (Ionide) handles go-to-definition. The flat
  namespace is a non-issue with modern tooling.
- **Collision risk**: No current collisions. Type names (`C`, `P`, `S`) are
  intentionally terse but generic — they could collide with user code. This is
  mitigated by the namespace qualifier (`Encodings.C`).
- **Breaking change if removed**: Every script, test, and example would need
  explicit module opens. Estimated ~200 affected files.

## Alternatives Considered

- **Remove `[<AutoOpen>]` from most modules** — require `open Encodings.Terms`,
  `open Encodings.PauliRegister`, etc. Better hygiene, but massive breaking
  change. Deferred post-JOSS.
- **Selective AutoOpen** — only on `Terms.fs` and `PauliRegister.fs`, explicit
  opens for encoding-specific modules. A reasonable middle ground, but adds
  friction to the scripting UX that is the library's main selling point.

## References

- [Encodings.fsproj](../../src/Encodings/Encodings.fsproj) — compilation order
- Every `*.fs` file in `src/Encodings/` — `[<AutoOpen>]` attribute
