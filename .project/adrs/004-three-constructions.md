# ADR-004: Three Encoding Constructions (A / B / F)

**Date:** 2026-02-09
**Status:** Accepted
**Source:** Research journal — MonotonicityCensus investigation; exhaustive enumeration

## Context

The Seeley-Richard-Love (SRL) framework presents a unified index-set
formulation (Update / Parity / Occupation sets) that supposedly generalises
across tree topologies. The original research hypothesis was: "Construction A
(index-set encoding) works for all monotonic trees."

Exhaustive enumeration for n = 3, 4, 5 falsified this. The `treeRemainderSet`
formula's `child.Index < j` condition means Construction A produces valid
CAR-satisfying encodings **only for star trees** (depth-1 trees where all
leaves connect directly to the root). Out of nⁿ⁻¹ labelled trees, only n are
stars — the generic framework's apparent generality is illusory.

Meanwhile, Bravyi-Kitaev uses hand-derived Fenwick-tree-specific bit
manipulation formulas that are neither Construction A nor the path-based
approach.

## Decision

Recognize three distinct encoding constructions:

- **Construction A** — Index-set (Update/Parity/Occupation). Works only for
  star/depth-1 trees. Covers JW, Parity encoding. Implemented in
  `MajoranaEncoding.fs`.

- **Construction B** — Path-based. Works for arbitrary trees (universal).
  Constructs Majorana strings by collecting Pauli labels along root-to-leg
  paths in a ternary-labelled tree. Implemented in `TreeEncoding.fs`.

- **Construction F** — Fenwick-specific. Hand-derived formulas using bit
  arithmetic for the BK encoding only. Cannot be extended to other tree
  shapes. Implemented in `FenwickTree.fs` + `BravyiKitaev.fs`.

Name "Construction F" explicitly (F for Fenwick) to clarify that BK is **not**
an instance of the generic index-set framework — it uses completely separate
code.

## Consequences

- **Correctness**: `TreeEncoding.fs` carries a `WARNING: Only produces correct
  encodings for Fenwick trees!` on the index-set bridge function. This prevents
  silent misuse.
- **Code duplication**: JW exists in both `JordanWigner.fs` (string-based,
  Construction A style) and could be produced via `TreeEncoding.fs` for a
  linear tree. The duplication is intentional — JW predates the generic
  framework and its string-based implementation is optimally readable.
- **Paper impact**: The star-tree theorem is a key result in Paper 1 (Review),
  proved computationally for n ≤ 5. Analytical proof remains open.
- **Compilation order**: `JordanWigner.fs` comes before `FenwickTree.fs` and
  `MajoranaEncoding.fs` in the `.fsproj`, reflecting that JW is independent
  of the generic framework.

## Alternatives Considered

- **Unified framework for all encodings** — would require fixing the
  `treeRemainderSet` formula for non-star trees. No such fix is known.
- **Drop Construction A entirely** — use path-based universally. Rejected
  because the index-set formulation is pedagogically valuable and efficient
  for star trees.
- **Treat BK as an instance of Construction A** — incorrect. BK's code path
  is entirely separate (`FenwickTree.fs`).

## References

- [MajoranaEncoding.fs](../../src/Encodings/MajoranaEncoding.fs) — Construction A
- [TreeEncoding.fs](../../src/Encodings/TreeEncoding.fs) — Construction B
- [FenwickTree.fs](../../src/Encodings/FenwickTree.fs) — Construction F
- Research journal, 2026-02-09: MonotonicityCensus results
