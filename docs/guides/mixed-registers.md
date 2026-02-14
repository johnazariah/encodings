# Mixed Bosonic and Fermionic Registers

Many physical models combine fermions and bosons in one Hamiltonian (for example, electrons coupled to vibrational or photonic modes). This guide describes how to model those systems in FockMap using a shared indexed representation and algebra-specific normal ordering.

## Core Idea

FockMap uses one operator container type:

- `IxOp<uint32, LadderOperatorUnit>` for indexed creation/annihilation operators.

The difference between fermionic and bosonic behavior is not in the operator token itself, but in the combining algebra used during normal ordering:

- `FermionicAlgebra` applies CAR rewrites.
- `BosonicAlgebra` applies CCR rewrites.

## Partitioning a Register

A mixed register starts with an index partition. For example:

- fermionic modes: `0u..7u`
- bosonic modes: `100u..103u`

Using disjoint index ranges keeps sector logic explicit and prevents accidental overlap.

## Workflow Pattern

1. Build candidate terms in the shared indexed representation.
2. Split terms into fermionic, bosonic, and coupling contributions.
3. Normal-order each contribution with the correct algebra.
4. Encode fermionic contributions to Pauli strings with your chosen encoding scheme.
5. Keep bosonic contributions symbolic or pass them to a separate truncation/encoding stage.

## Example Skeleton

```fsharp
open Encodings
open System.Numerics

let isFermion i = i < 100u
let isBoson i = i >= 100u

let make op i = C.Apply(IxOp.Apply(i, op))

// Example coupling-like candidate: a†_1 a_2 b†_100 b_100
let couplingCandidate : S<IxOp<uint32, LadderOperatorUnit>> =
    P.Apply [|
        IxOp.Apply(1u, Raise)
        IxOp.Apply(2u, Lower)
        IxOp.Apply(100u, Raise)
        IxOp.Apply(100u, Lower)
    |]
    |> S.Apply

let normalOrderFermionic (s : S<IxOp<uint32, LadderOperatorUnit>>) =
    LadderOperatorSumExpr<FermionicAlgebra>.ConstructNormalOrdered s

let normalOrderBosonic (s : S<IxOp<uint32, LadderOperatorUnit>>) =
    constructBosonicNormalOrdered s
```

In production code, you typically project each product term into sector-specific sub-products before normal ordering.

## Coupling Terms

For couplings like $g\,n_f n_b$:

- write the fermionic number factor using fermionic operators,
- write the bosonic number factor using bosonic operators,
- normalize each sector under its own algebra,
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
