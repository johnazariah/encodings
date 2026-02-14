# Advanced Operations Guide

This guide collects advanced symbolic workflows for FockMap power users. It focuses on mixed-statistics pipelines, selective encoding, and cross-encoding analysis.

## 1) Mixed Canonicalization First

For mixed bosonic+fermionic expressions, always canonicalize first:

- block order by sector (fermions left, bosons right),
- CAR rewrites in fermion blocks,
- CCR rewrites in boson blocks.

```fsharp
let canonical = constructMixedNormalOrdered mixedExpr
```

Why this matters:

- prevents hidden sign mistakes,
- provides deterministic term shape,
- simplifies downstream projection/encoding logic.

## 2) Hybrid Pipeline (Encode Fermions, Keep Bosons Symbolic)

A common production strategy is:

1. canonicalize mixed terms,
2. project fermionic units,
3. encode fermionic units to Pauli strings,
4. preserve bosonic units symbolically for truncation-specific handling.

Runnable example:

- `examples/Mixed_HybridPipeline.fsx`

## 3) Compare Encodings on the Same Fermionic Block

To compare cost/structure across encodings in a mixed model:

1. canonicalize mixed terms,
2. extract fermionic units from each term,
3. encode with multiple encoders (e.g., JW and BK),
4. compare Pauli term counts and structures.

Runnable example:

- `examples/Mixed_HybridCompare.fsx`

This is useful for choosing an encoding without changing the bosonic side of the model.

## 4) Sectoring Rules of Thumb

- Use explicit sector tags (`fermion`, `boson`) instead of inferred index ranges where possible.
- Keep bosonic and fermionic index ranges disjoint anyway for readability/debugging.
- Drop placeholder identity operators before sector-specific transforms.

## 5) Validation Patterns

For advanced workflows, add tests for:

- sector block ordering invariants,
- CAR/CCR same-index rewrites,
- hybrid projections (fermion extraction + boson preservation),
- encoding-agnostic conservation checks on the fermion block.

## 6) Related Documentation

- [Mixed Registers](mixed-registers.html)
- [Architecture](architecture.html)
- [Type System](type-system.html)
