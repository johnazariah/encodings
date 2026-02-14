# Mixed Bosonic and Fermionic Registers

Many physical models combine fermions and bosons in one Hamiltonian (for example, electrons coupled to vibrational or photonic modes). This guide describes how to model those systems in FockMap using explicit sector-tagged operators and mixed normal ordering.

## Core Idea

FockMap now provides sector-aware ladder units:

- `ParticleSector = Fermionic | Bosonic`
- `SectorLadderOperatorUnit`
- `IxOp<uint32, SectorLadderOperatorUnit>`

Helper constructors make sector intent explicit:

- `fermion : LadderOperatorUnit -> uint32 -> IxOp<uint32, SectorLadderOperatorUnit>`
- `boson : LadderOperatorUnit -> uint32 -> IxOp<uint32, SectorLadderOperatorUnit>`

Mixed canonicalization is handled by:

- `constructMixedNormalOrdered : S<IxOp<uint32, SectorLadderOperatorUnit>> -> ...`

Internally, the mixed normalizer uses the same combining algebras:

- `FermionicAlgebra` applies CAR rewrites.
- `BosonicAlgebra` applies CCR rewrites.

## Canonical Block Rule

For independent sectors, cross-sector commutators are zero, so fermionic and bosonic operators can be reordered without sign:

$$
[a_i, b_j] = [a_i, b_j^\dagger] = [a_i^\dagger, b_j] = [a_i^\dagger, b_j^\dagger] = 0
$$

FockMap's canonical mixed form is therefore:

1. all fermionic operators on the left,
2. all bosonic operators on the right,
3. normal-order each sector with its own algebra.

## Partitioning a Register

A mixed register starts with an index partition. For example:

- fermionic modes: `0u..7u`
- bosonic modes: `100u..103u`

Using disjoint index ranges keeps sector logic explicit and prevents accidental overlap.

## Workflow Pattern

1. Build candidate terms with sector-tagged operators.
2. Apply `constructMixedNormalOrdered` to each expression.
3. The normalizer block-orders by sector, then applies CAR/CCR per sector.
4. Encode fermionic contributions to Pauli strings with your chosen encoding scheme.
5. Keep bosonic contributions symbolic or pass them to a separate truncation/encoding stage.

## Example Skeleton

```fsharp
open Encodings
open System.Numerics

// Example coupling-like candidate: a†_1 a_2 b†_100 b_100
let couplingCandidate : S<IxOp<uint32, SectorLadderOperatorUnit>> =
    P.Apply [|
        fermion Raise 1u
        fermion Lower 2u
        boson Raise 100u
        boson Lower 100u
    |]
    |> S.Apply

let canonical = constructMixedNormalOrdered couplingCandidate
```

Use `isSectorBlockOrdered` / `toSectorBlockOrder` if you need block-order checks independently of full normal ordering.

## Coupling Terms

For couplings like $g\,n_f n_b$:

- write the fermionic number factor using fermionic operators,
- write the bosonic number factor using bosonic operators,
- normalize in mixed canonical form (fermion block + boson block),
- combine at the coefficient/Hamiltonian-assembly level.

This keeps CAR and CCR semantics explicit and avoids hidden sign mistakes.

## Practical Recommendations

- Use disjoint mode-index ranges per sector.
- Keep normalization functions sector-specific.
- Add tests for same-index rewrites (`a_i a_i^\dagger`, `b_i b_i^\dagger`) and cross-index swaps.
- Verify mixed-model observables under truncation changes for bosonic modes.

## Related Docs

- [Architecture](architecture.html)
- [Type System](type-system.html)
- [Bosonic Operators](../theory/06-bosonic-preview.html)
