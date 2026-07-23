# Building a Real Hamiltonian

Everything so far has been about single operators. Now let's build a
complete molecular Hamiltonian — the ultimate goal of the library.

## The second-quantized Hamiltonian

In quantum chemistry, the electronic Hamiltonian is:

$$H = \sum_{pq} h_{pq}\, a^\dagger_p a_q \;+\; \tfrac{1}{2} \sum_{pqrs} \langle pq|rs\rangle\, a^\dagger_p a^\dagger_q a_s a_r$$

where $h_{pq}$ are one-body integrals (kinetic energy + nuclear attraction)
and $\langle pq|rs\rangle$ are two-body integrals (electron-electron repulsion).

## Step 1 — Define integrals

For H₂ in the STO-3G basis (bond length 1.3984 bohr) we have 2 spatial
orbitals → 4 spin-orbitals. Chemistry codes report a handful of *spatial*
integrals; every non-zero *spin-orbital* integral is one of these with a
spin-conservation rule applied. We start from the canonical spatial values:

```fsharp
open System.Numerics
open Encodings
open Encodings.Hamiltonian
open Encodings.JordanWigner
open Encodings.BravyiKitaev
open Encodings.TreeEncoding
open Encodings.MajoranaEncoding
let nModes = 4u

// Spatial one-electron integrals h_pq (Hartree).
let hSpatial p q =
    if p = q then (if p = 0 then -1.253309786645977 else -0.4750688487721783)
    else 0.0

// Spatial two-electron integrals in chemist notation (pq|rs), 8-fold symmetric.
let gChem p q r s =
    let a = (min p q, max p q)
    let b = (min r s, max r s)
    let (x, y) = if a <= b then (a, b) else (b, a)
    match x, y with
    | (0,0),(0,0) -> 0.6747559268144484
    | (1,1),(1,1) -> 0.697651504490461
    | (0,0),(1,1) -> 0.6637114013508132
    | (0,1),(0,1) -> 0.1812104620151968   // K₀₁ exchange
    | _ -> 0.0
```

## Step 2 — Build a raw physicist factory

Spin-orbital `i` has spatial index `i/2` and spin `i%2` (interleaved
`0α,0β,1α,1β`). The **raw physicist** integral `⟨pq|rs⟩` equals the chemist
integral `(pr|qs)` when the spins are conserved (`spin p = spin r`,
`spin q = spin s`). The factory returns `Some` for non-zero entries — here 4
one-body and 32 two-body:

```fsharp
let spin i = i % 2
let orb  i = i / 2

// Raw single-bar physicist tensor ⟨pq|rs⟩ for a spin-orbital key.
let rawLookup (key : string) =
    let x = key.Split(',')
    match x.Length with
    | 2 ->
        let p, q = int x.[0], int x.[1]
        if spin p = spin q then
            let v = hSpatial (orb p) (orb q)
            if v <> 0.0 then Some (Complex(v, 0.0)) else None
        else None
    | 4 ->
        let p, q, r, s = int x.[0], int x.[1], int x.[2], int x.[3]
        let v =
            if spin p = spin r && spin q = spin s
            then gChem (orb p) (orb r) (orb q) (orb s)
            else 0.0
        if v <> 0.0 then Some (Complex(v, 0.0)) else None
    | _ -> None
```

> **Coefficient contract (0.9.0+).** The primary builders take the raw tensor
> **directly**:
>
> - **Raw physicist tensor** — what a chemistry code hands you, and what
>   `computeHamiltonian` / `computeHamiltonianWith` now consume: for key
>   `"p,q,r,s"` the single-bar integral `⟨pq|rs⟩`, with **no** ½ and no index
>   swap. The library internally builds `½·⟨pq|rs⟩·a†_p a†_q a_s a_r` (it applies
>   the ½ and the r↔s order). The `Fcidump` adapters produce this raw form
>   (`⟨pq|rs⟩ = (pr|qs)` in chemist notation). An antisymmetrised double-bar
>   `⟨pq||rs⟩` (¼ convention) has its own `antisymmetrizedToRawFactory` adapter.
> - **Legacy weighted factory** — the *full weighted* prefactor of
>   `a†_i a†_j a_k a_l` with the two-body ½ already folded in (the released
>   ≤ 0.8.0 contract). It is still available through the
>   `computeHamiltonianFromWeighted…` functions, or wrap it once with
>   `weightedToRawFactory` to feed the raw builders. See the
>   [0.9.0 migration guide](../migration-0.9.html).

## Step 3 — Compute the qubit Hamiltonian

One call loops over indices, applies the two-body ½ and r↔s order, encodes each
term, and combines like terms:

```fsharp
let hamiltonian = computeHamiltonian rawLookup nModes

printfn "H₂ Hamiltonian: %d Pauli terms\n" hamiltonian.SummandTerms.Length

for term in hamiltonian.DistributeCoefficient.SummandTerms do
    let sign = if term.Coefficient.Real >= 0.0 then "+" else ""
    printfn "  %s%.4f  %s" sign term.Coefficient.Real term.Signature
```

This prints the canonical 15-term Jordan–Wigner H₂ Hamiltonian:

```
H₂ Hamiltonian: 15 Pauli terms

  -0.8122  IIII
  +0.1714  ZIII
  +0.1714  IZII
  +0.1687  ZZII
  -0.2234  IIZI
  +0.1206  ZIZI
  +0.1659  IZZI
  -0.2234  IIIZ
  +0.1659  ZIIZ
  +0.1206  IZIZ
  +0.1744  IIZZ
  -0.0453  XXYY
  +0.0453  XYYX
  +0.0453  YXXY
  -0.0453  YYXX
```

The identity coefficient is −0.8121706072, the four-body exchange terms are
±0.0453026155, and the full 16×16 spectrum has ground state −1.8523881736 Ha
(electronic; add the nuclear repulsion, returned separately by `Fcidump`, for
the total energy).

## Step 4 — Swap the encoding

Use `computeHamiltonianWith` to try any encoding on the same raw integrals:

```fsharp
let hBK = computeHamiltonianWith bravyiKitaevTerms rawLookup nModes
let hTT = computeHamiltonianWith ternaryTreeTerms  rawLookup nModes

// Or your custom scheme from the Encoding Internals chapter:
let myJW : EncodingScheme =
    { Update     = fun _ _ -> Set.empty
      Parity     = fun j   -> set [ for k in 0 .. j - 1 -> k ]
      Occupation = fun j   -> set [ j ] }
let hCustom = computeHamiltonianWith (encodeOperator myJW) rawLookup nModes
```

All three Hamiltonians have the same eigenvalues — they represent
identical physics. They differ only in the Pauli weight and number of
terms, which affects measurement cost when simulating the circuit.

---

**Next:** [Mixed Bosonic–Fermionic Systems](11-mixed-systems.html) — sector tags and hybrid workflows
