# Migrating to FockMap 0.9.0 — the raw physicist Hamiltonian API

FockMap 0.9.0 makes a **deliberate breaking change** to the coefficient contract
of the Hamiltonian builders. This guide explains what changed, why the physics is
unchanged for FCIDUMP users, the exact before/after mapping, and how to migrate a
hand-built factory.

## What changed

The electronic Hamiltonian FockMap assembles is unchanged:

$$H = \sum_{pq} h_{pq}\, a^\dagger_p a_q \;+\; \tfrac{1}{2} \sum_{pqrs} \langle pq|rs\rangle\, a^\dagger_p a^\dagger_q a_s a_r$$

What changed is **who supplies the ½ and the index order**.

| | ≤ 0.8.0 (weighted) | 0.9.0+ (raw physicist) |
|---|---|---|
| Two-body key | `"p,q,s,r"` | `"p,q,r,s"` |
| Two-body value | `0.5 · ⟨pq\|rs⟩` (full weighted prefactor of `a†_p a†_q a_s a_r`, ½ pre-folded) | `⟨pq\|rs⟩` (raw single-bar integral, **no** ½, **no** swap) |
| Who applies the ½ / r↔s order | the **caller** | the **library** |
| One-body key/value | `"p,q"` → `h_pq` | `"p,q"` → `h_pq` (unchanged) |

The affected functions are the primary builders and everything that routes through
them:

- `computeHamiltonian`, `computeHamiltonianWith`
- `computeHamiltonianParallel`, `computeHamiltonianWithParallel`
- `computeHamiltonianCached`
- `computeHamiltonianSkeletonFor`, `applyCoefficients`
- all `Optimization` entry points (`evaluate`, `optimizeOver`, `optimizeStandard`, …)

`computeHamiltonianSkeleton` (the full, structure-only skeleton) is unchanged — it
carries no coefficients and feeds either contract.

## FCIDUMP users: nothing to do

The `Fcidump` adapters (`toCoefficientFactory`, `toSpinOrbitalFactory`,
`parseToFactory`, `parseToSpinOrbitalFactory`) now return **raw** physicist integrals
(`⟨pq|rs⟩ = (pr|qs)` in chemist notation) instead of the pre-weighted `½·(ps|qr)`.
Because the factory and the builders flipped together, the assembled physics is
**identical** to earlier releases. If you build your Hamiltonian from an FCIDUMP file,
your code and results are unchanged:

```fsharp
let (factory, core, nso) = Fcidump.parseToSpinOrbitalFactory content
let ham = computeHamiltonianWith jordanWignerTerms factory (uint32 nso)
// H₂/STO-3G: 15 terms, IIII = -0.8121706072, four-body ±0.0453026155, ground -1.8523881736 Ha
```

## Canonical example (hand-built factory)

If a chemistry code hands you the raw single-bar integral `g = ⟨pq|rs⟩`:

```fsharp
// ── Before (≤ 0.8.0): pre-adapt to the weighted contract yourself ──
//    weighted key (p, q, s, r)  →  value 0.5 * g
let weighted (key:string) =
    // e.g. (0,1,1,0) -> 0.5 * ⟨01|01⟩
    ...

// ── After (0.9.0): feed the raw integral directly ──
//    raw key (p, q, r, s)  →  value g
let raw (key:string) =
    // e.g. (0,1,0,1) -> ⟨01|01⟩
    ...
let ham = computeHamiltonian raw nModes
```

The one-line rule for converting an existing weighted factory to a raw key/value:

> **raw key `(p,q,r,s)` = weighted key `(p,q,s,r)`, and raw value = `2 ×` the weighted value.**

## Three ways to keep the old weighted behaviour

If you already pre-adapt your integrals to the weighted convention (½ folded in, indices
swapped), pick one:

1. **Named legacy functions** — swap each call for its `…FromWeighted…` twin:

   | Raw primary | Legacy weighted |
   |---|---|
   | `computeHamiltonian` | `computeHamiltonianFromWeighted` |
   | `computeHamiltonianWith` | `computeHamiltonianFromWeightedWith` |
   | `computeHamiltonianParallel` | `computeHamiltonianFromWeightedParallel` |
   | `computeHamiltonianWithParallel` | `computeHamiltonianFromWeightedWithParallel` |
   | `computeHamiltonianCached` | `computeHamiltonianFromWeightedCached` |
   | `computeHamiltonianSkeletonFor` | `computeHamiltonianSkeletonForFromWeighted` |
   | `applyCoefficients` | `applyCoefficientsFromWeighted` |

   ```fsharp
   let ham = computeHamiltonianFromWeightedWith jordanWignerTerms myWeightedFactory n
   ```

2. **One adapter, any builder** — wrap the weighted factory once and keep using the raw
   builders (this is the only option for the `Optimization` entry points, which have no
   dedicated weighted overload):

   ```fsharp
   let raw = Hamiltonian.weightedToRawFactory myWeightedFactory
   let ham = computeHamiltonianWith jordanWignerTerms raw n
   let best = Optimization.optimizeStandard Optimization.lambdaNormCost raw n
   ```

3. **Do nothing** if you build from FCIDUMP — see above.

## Antisymmetrised (double-bar) tensors

If your code hands you an antisymmetrised double-bar tensor `⟨pq||rs⟩` (the ¼
convention, `⟨pq||rs⟩ = ⟨pq|rs⟩ − ⟨pq|sr⟩`), wrap it with the dedicated adapter:

```fsharp
let raw = Hamiltonian.antisymmetrizedToRawFactory myDoubleBarFactory
let ham = computeHamiltonian raw n
```

The adapter scales the two-body entries by ½; fermionic anticommutation makes the
resulting `¼·Σ ⟨pq||rs⟩ a†_p a†_q a_s a_r` identical to the single-bar
`½·Σ ⟨pq|rs⟩ a†_p a†_q a_s a_r`. The core/nuclear energy remains a separate constant.

## Deprecated aliases

- `rawPhysicistToWeightedFactory` is now obsolete: the primary builders take raw
  integrals directly, so you no longer need to convert raw → weighted (it is retained
  only to bridge raw data into the legacy `computeHamiltonianFromWeighted…` functions).
- `computeHamiltonianFromPhysicist` / `computeHamiltonianFromPhysicistWith` are now
  obsolete identity aliases of `computeHamiltonian` / `computeHamiltonianWith`. Call the
  primary builders directly.

## Migration hazards to watch for

- **Pre-adapted weighted data fed to the raw builders** applies a *second* ½ and re-swaps
  — the result is materially wrong (e.g. `IIII` no longer `−0.8121706072`). Use a
  `…FromWeighted…` function instead.
- **Raw data fed to a `…FromWeighted…` function** omits the ½ and the swap — also wrong.
  Feed raw data to the primary builders.

Both hazards produce a clearly *different* map (no silent accidental equivalence), and
both are covered by regression tests.
