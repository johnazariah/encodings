# Bosonic Operators

FockMap supports both fermionic and bosonic ladder-operator normal ordering. Fermions use canonical anti-commutation relations (CAR), while bosons use canonical commutation relations (CCR).

This page introduces the bosonic algebra, how truncation works in practice, and how bosonic and fermionic sectors can be modeled together.

## Bosons vs Fermions

The key difference between bosons and fermions lies in their commutation relations:

**Fermions** (CAR):
$$
\{a_i, a^\dagger_j\} = a_i a^\dagger_j + a^\dagger_j a_i = \delta_{ij}
$$

**Bosons** (CCR):
$$
[b_i, b^\dagger_j] = b_i b^\dagger_j - b^\dagger_j b_i = \delta_{ij}
$$

For fermions, each mode holds at most one particle (occupation $n_j \in \{0, 1\}$). For bosons, there is no intrinsic upper bound, so a single mode can hold $0,1,2,\ldots$ particles.

In qubit simulation, that means bosonic modes require a truncation choice.

## Truncated Bosonic Fock Space

The standard approach is to truncate each bosonic mode at occupancy cutoff $d$:

$$
n_j \in \{0, 1, 2, \ldots, d-1\}
$$

With this truncation, each bosonic mode is finite-dimensional, with local Hilbert space size $d$. For $n$ bosonic modes, the bosonic sector has dimension $d^n$.

Common cutoffs:
- $d = 2$: aggressive truncation (hard-core limit)
- $d = 4$: Two qubits per mode
- $d = 8$: Three qubits per mode

## Normal Ordering in FockMap

Bosonic normal ordering in FockMap is exposed via `BosonicLadderOperatorSumExpression` and helper constructors:

```fsharp
open Encodings

let expr =
    S.Apply(
        P.Apply [|
            IxOp.Apply(0u, Lower)
            IxOp.Apply(0u, Raise)
        |])

let bosonic = constructBosonicNormalOrdered expr
```

The CCR rewrite rule used by `BosonicAlgebra` is:

$$
b_i b_i^\dagger = 1 + b_i^\dagger b_i
$$

For different indices, reordering does not introduce a minus sign:

$$
b_i b_j^\dagger = b_j^\dagger b_i \quad (i \neq j)
$$

This differs from the fermionic CAR case, where reordering contributes a sign flip.

## Mixed Bosonic + Fermionic Models

Many applications (for example, electron-phonon models) include both fermionic and bosonic operators. In FockMap, the practical pattern is:

1. Partition indices into bosonic and fermionic sectors.
2. Build expressions in the shared indexed representation `IxOp<uint32, LadderOperatorUnit>`.
3. Normal-order each sector with the appropriate algebra (`FermionicAlgebra` or `BosonicAlgebra`).
4. Encode fermionic terms to Pauli strings with any encoding scheme.
5. Keep bosonic terms symbolic or apply a separate bosonic truncation/encoding layer.

See the [Mixed Registers guide](../guides/mixed-registers.html) for a full workflow.

## Truncation Encodings (Conceptual)

FockMap's current bosonic support focuses on symbolic CCR normal ordering. If you then map bosonic modes to qubits, common strategies include unary, binary, and Gray-code truncation encodings.

Each strategy trades off qubit count against Pauli weight and circuit complexity. In practice, truncation choices should be made per model and validated against convergence of observables.

---

**Previous:** [Beyond Jordan-Wigner](05-beyond-jordan-wigner.html) — BK, trees, and O(log n) encodings

**Next:** [Mixed Systems](07-mixed-systems.html) — canonical ordering for boson+fermion models
