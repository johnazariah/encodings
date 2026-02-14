# Advanced Operations Guide

This is the practical playbook for advanced mixed-statistics workflows in FockMap.

If you are building real pipelines, start here.

## Quick Picks

- **I have mixed bosons + fermions and want a safe default** → `constructMixedNormalOrdered` first.
- **I only need qubit encoding for fermions right now** → hybrid pipeline (encode fermions, keep bosons symbolic).
- **I need to choose between JW/BK/tree for the same mixed model** → compare encodings on extracted fermionic blocks.
- **I am seeing weird signs or unstable term shapes** → check sector block ordering and identity leakage.

## Recipe 1: Canonicalize Mixed Terms

For mixed bosonic+fermionic expressions, always canonicalize first:

- block order by sector (fermions left, bosons right),
- CAR rewrites in fermion blocks,
- CCR rewrites in boson blocks.

```fsharp
let canonical = constructMixedNormalOrdered mixedExpr
```

What this gives you:

- prevents hidden sign mistakes,
- provides deterministic term shape,
- simplifies downstream projection/encoding logic.

Runnable example:

- `examples/Mixed_NormalOrdering.fsx`

## Recipe 2: Hybrid Pipeline (Encode Fermions, Keep Bosons Symbolic)

A common production strategy is:

1. canonicalize mixed terms,
2. project fermionic units,
3. encode fermionic units to Pauli strings,
4. preserve bosonic units symbolically for truncation-specific handling.

Runnable example:

- `examples/Mixed_HybridPipeline.fsx`

Minimal skeleton:

```fsharp
let canonical = constructMixedNormalOrdered mixedExpr

// extract fermionic units from each canonical term
// encode with jordanWignerTerms / bravyiKitaevTerms / tree terms
// keep bosonic units symbolic for a separate truncation layer
```

## Recipe 3: Compare Encodings on Same Fermion Block

To compare cost/structure across encodings in a mixed model:

1. canonicalize mixed terms,
2. extract fermionic units from each term,
3. encode with multiple encoders (e.g., JW and BK),
4. compare Pauli term counts and structures.

Runnable example:

- `examples/Mixed_HybridCompare.fsx`

This is useful for choosing an encoding without changing the bosonic side of the model.

## Decision Guide (Fast)

1. **No bosons in the model?**
	- Use the standard fermionic encoding path.
2. **Bosons present but no bosonic qubit mapping needed yet?**
	- Use hybrid pipeline.
3. **Need to pick fermionic encoding strategy?**
	- Run mixed comparison on extracted fermion blocks.
4. **Bosonic cutoff affects results?**
	- Do cutoff convergence checks before trusting observables.

## Common Failure Modes (and Fixes)

| Symptom | Likely Cause | Fix |
|---|---|---|
| Unexpected sign flips | Mixing cross-sector swaps with fermionic swaps mentally | Canonicalize first, then reason per sector |
| Non-deterministic term shape | No sector block rule before transforms | Apply `constructMixedNormalOrdered` up front |
| Bloated fermion encoding | Identity placeholders flowing into encoder | Drop `Identity` units before encoding |
| Hard-to-debug mode assignment | Implicit sectoring by convention only | Use explicit `fermion` / `boson` constructors |
| Unstable bosonic results vs model size | Cutoff too low | Run cutoff sweep and compare observables |

## Sectoring Rules

- Use explicit sector tags (`fermion`, `boson`) instead of inferred index ranges where possible.
- Keep bosonic and fermionic index ranges disjoint anyway for readability/debugging.
- Drop placeholder identity operators before sector-specific transforms.

## Validation Checklist

For advanced workflows, add tests for:

- sector block ordering invariants,
- CAR/CCR same-index rewrites,
- hybrid projections (fermion extraction + boson preservation),
- encoding-agnostic conservation checks on the fermion block.

## Related Docs

- [Mixed Registers](mixed-registers.html)
- [Architecture](architecture.html)
- [Type System](type-system.html)
