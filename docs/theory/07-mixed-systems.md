# Mixed Bosonic and Fermionic Systems

Up to now, we treated fermionic and bosonic operators separately. Realistic models often require both in the same Hamiltonian: electrons (fermions) coupled to phonons or photons (bosons), polaritonic models, and vibronic structure problems.

This chapter explains the mixed-statistics structure, the canonical ordering rule, and how it maps to FockMap's sector-aware API.

## Product Structure of the Hilbert Space

For independent fermionic and bosonic sectors:

$$
\mathcal{H} = \mathcal{H}_{\mathrm{f}} \otimes \mathcal{H}_{\mathrm{b}}.
$$

Operators that act on different factors commute:

$$
(A_{\mathrm{f}} \otimes I_{\mathrm{b}})(I_{\mathrm{f}} \otimes B_{\mathrm{b}})
= (I_{\mathrm{f}} \otimes B_{\mathrm{b}})(A_{\mathrm{f}} \otimes I_{\mathrm{b}}).
$$

In ladder-operator notation, this gives the cross-sector commutation identities:

$$
[a_i, b_j] = [a_i, b_j^\dagger] = [a_i^\dagger, b_j] = [a_i^\dagger, b_j^\dagger] = 0.
$$

So a fermion can be swapped past a boson without a minus sign.

## Canonical Mixed Ordering

A practical canonical form for mixed products is:

1. move all fermionic operators to the left,
2. move all bosonic operators to the right,
3. normal-order each block with its own algebra.

Inside each block:

- Fermionic CAR: $a_i a_i^\dagger = 1 - a_i^\dagger a_i$
- Bosonic CCR: $b_i b_i^\dagger = 1 + b_i^\dagger b_i$

This gives deterministic term shape and cleanly separates sign-sensitive (fermionic) and sign-free (cross-sector) swaps.

## Mixed Hamiltonian Form

A common mixed Hamiltonian decomposition is:

$$
H = H_{\mathrm{f}} + H_{\mathrm{b}} + H_{\mathrm{fb}},
$$

with examples:

$$
H_{\mathrm{f}} = \sum_{pq} h_{pq} a_p^\dagger a_q,
\qquad
H_{\mathrm{b}} = \sum_k \omega_k b_k^\dagger b_k,
$$

$$
H_{\mathrm{fb}} = \sum_{p q k} g_{pqk} a_p^\dagger a_q (b_k + b_k^\dagger).
$$

In implementation, this is easiest if every operator carries explicit sector metadata.

## Sector-Aware API in FockMap

FockMap provides a typed mixed representation:

- `ParticleSector = Fermionic | Bosonic`
- `SectorLadderOperatorUnit`
- `IxOp<uint32, SectorLadderOperatorUnit>`

Helpers:

- `fermion Raise 1u`
- `boson Lower 100u`

Canonical mixed normal ordering:

```fsharp
let canonical = constructMixedNormalOrdered mixedExpr
```

Utility helpers:

- `isSectorBlockOrdered`
- `toSectorBlockOrder`

## Encoding Strategy for Mixed Workflows

In many workflows, only the fermionic sector is immediately encoded to qubits.

Typical pipeline:

1. canonicalize mixed expression,
2. extract fermionic units,
3. encode fermions with JW/BK/tree mapping,
4. keep bosons symbolic for truncation-specific handling.

See runnable examples:

- `examples/Mixed_HybridPipeline.fsx`
- `examples/Mixed_HybridCompare.fsx`

## Practical Modeling Notes

- Prefer explicit sector tags over index-range conventions.
- Keep fermion and boson index ranges disjoint for readability.
- Drop placeholder identity operators before sector-specific transforms.
- Validate bosonic cutoff convergence separately from fermionic encoding choices.

---

**Previous:** [Bosonic Operators](06-bosonic-preview.html) — CCR and truncation basics

**Next:** [Advanced Operations Guide](../guides/advanced-operations.html) — hybrid workflows and decision rules
