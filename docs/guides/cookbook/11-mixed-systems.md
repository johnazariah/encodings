# Mixed Bosonic–Fermionic Systems

Some physical models combine fermions and bosons in one Hamiltonian — for
example, electrons coupled to vibrational or photonic modes. These obey
different statistics and must be handled carefully.

## Tagging operators with their sector

FockMap distinguishes them with a sector tag:

```fsharp
// Fermionic operators:
let f0_up   = fermion Raise 0u    // f†₀
let f0_down = fermion Lower 0u    // f₀

// Bosonic operators:
let b1_up   = boson Raise 1u     // b†₁
let b1_down = boson Lower 1u     // b₁
```

Use explicit `fermion` / `boson` constructors instead of relying on index
ranges by convention — this eliminates an entire class of hard-to-debug
mode-assignment errors.

## The canonical form: fermions first

In the canonical block order, all fermionic operators appear before
bosonic ones. Cross-sector commutators are zero, so the reorder is free:

$$[a_i, b_j] = [a_i, b_j^\dagger] = [a_i^\dagger, b_j] = [a_i^\dagger, b_j^\dagger] = 0$$

```fsharp
// Build a mixed term with bosons before fermions (wrong order!):
let mixedTerm = P<IxOp<uint32, SectorLadderOperatorUnit>>.Apply [|
    b1_down     // b₁ — bosonic (should be on the right)
    f0_up       // f†₀ — fermionic (should be on the left)
|]

isSectorBlockOrdered mixedTerm    // false ✗

let reordered = toSectorBlockOrder mixedTerm
isSectorBlockOrdered reordered    // true ✓
```

## Full mixed normal ordering

The `constructMixedNormalOrdered` function performs three steps in sequence:

1. **Sector ordering** — fermions left, bosons right (no sign change)
2. **Fermionic normal ordering** — CAR within the fermionic block (sign flips)
3. **Bosonic normal ordering** — CCR within the bosonic block (no sign flips)

```fsharp
// An intentionally disordered mixed expression: b₁ f†₀ b†₁ f₀
let messyExpr =
    P<IxOp<uint32, SectorLadderOperatorUnit>>.Apply [|
        b1_down; f0_up; b1_up; f0_down
    |]
    |> S<IxOp<uint32, SectorLadderOperatorUnit>>.Apply

let result = constructMixedNormalOrdered messyExpr

match result with
| Some ordered ->
    for term in ordered.ProductTerms.Value do
        printfn "%O" term
| None ->
    printfn "Expression is trivially zero"
```

The pipeline applies:
- Sector reorder: `f†₀ f₀ b₁ b†₁`
- Fermionic CAR on `f†₀ f₀` → already normal ordered
- Bosonic CCR on `b₁ b†₁` → `1 + b†₁ b₁`
- Final result: `f†₀ f₀ + f†₀ f₀ b†₁ b₁`

## Same-index identity terms in both sectors

When both sectors have same-index pairs, you get CAR and CCR identity
contributions in one pass:

```fsharp
let sameIndex : S<IxOp<uint32, SectorLadderOperatorUnit>> =
    P.Apply [|
        fermion Lower 5u
        fermion Raise 5u
        boson Lower 200u
        boson Raise 200u
    |] |> S.Apply

let canonical = constructMixedNormalOrdered sameIndex
```

- Fermionic: $a_5 a_5^\dagger = 1 - a_5^\dagger a_5$
- Bosonic: $b_{200} b_{200}^\dagger = 1 + b_{200}^\dagger b_{200}$

## Coupling terms

For couplings like $g\, n_f \, n_b$ (e.g., electron–phonon):

- write the fermionic number factor using fermionic operators,
- write the bosonic number factor using bosonic operators,
- normalise in mixed canonical form,
- combine at the coefficient/Hamiltonian-assembly level.

```fsharp
// g · a†₁ a₂ · b†₁₀₀ b₁₀₀
let coupling : S<IxOp<uint32, SectorLadderOperatorUnit>> =
    P.Apply [|
        fermion Raise 1u
        fermion Lower 2u
        boson Raise 100u
        boson Lower 100u
    |]
    |> S.Apply

let canonical = constructMixedNormalOrdered coupling
```

This keeps CAR and CCR semantics explicit and avoids hidden sign mistakes.

## Hybrid pipeline: encode fermions, keep bosons symbolic

A common production strategy:

1. Canonicalise mixed terms with `constructMixedNormalOrdered`.
2. Extract fermionic units from each canonical term.
3. Encode fermionic units to Pauli strings with your chosen encoding.
4. Keep bosonic units symbolic for a separate truncation/encoding stage.

## Decision guide

1. **No bosons in the model?** → Use the standard fermionic encoding path.
2. **Bosons present but no bosonic qubit mapping needed yet?** → Hybrid pipeline.
3. **Need to pick a fermionic encoding strategy?** → Run comparison on extracted fermion blocks.
4. **Bosonic cutoff affects results?** → Do cutoff convergence checks before trusting observables.

## Common failure modes

| Symptom | Likely cause | Fix |
|---|---|---|
| Unexpected sign flips | Mixing cross-sector swaps with fermionic swaps mentally | Canonicalise first, then reason per sector |
| Non-deterministic term shape | No sector block rule before transforms | Apply `constructMixedNormalOrdered` up front |
| Bloated fermion encoding | Identity placeholders flowing into encoder | Drop `Identity` units before encoding |
| Hard-to-debug mode assignment | Implicit sectoring by convention only | Use explicit `fermion` / `boson` constructors |
| Unstable bosonic results vs model size | Cutoff too low | Run cutoff sweep and compare observables |

## Practical recommendations

- **Disjoint index ranges:** Keep fermionic and bosonic mode indices disjoint (e.g., fermions `0u..7u`, bosons `100u..103u`) for readability and debugging.
- **Drop identities:** Strip placeholder `Identity` operators before sector-specific transforms.
- **Test same-index rewrites:** Add tests for `a_i a_i†`, `b_i b_i†`, and cross-index swaps.
- **Verify under truncation:** Bosonic results may shift when you change the cutoff — run convergence checks.

## Runnable scripts

- `examples/Mixed_NormalOrdering.fsx` — basic mixed canonicalisation
- `examples/Mixed_ElectronPhonon_Toy.fsx` — electron–phonon coupling
- `examples/Mixed_HybridPipeline.fsx` — encode fermions, keep bosons symbolic
- `examples/Mixed_HybridCompare.fsx` — compare encodings on the same fermion block

Run any of them with:

```bash
dotnet fsi examples/Mixed_NormalOrdering.fsx
```

---

**Next:** [The Utility Belt](12-utilities.html) — helper functions and extensions
