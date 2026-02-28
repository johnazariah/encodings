# Building a Real Hamiltonian

Everything so far has been about single operators. Now let's build a
complete molecular Hamiltonian — the ultimate goal of the library.

## The second-quantized Hamiltonian

In quantum chemistry, the electronic Hamiltonian is:

$$H = \sum_{pq} h_{pq}\, a^\dagger_p a_q \;+\; \tfrac{1}{2} \sum_{pqrs} \langle pq|rs\rangle\, a^\dagger_p a^\dagger_q a_s a_r$$

where $h_{pq}$ are one-body integrals (kinetic energy + nuclear attraction)
and $\langle pq|rs\rangle$ are two-body integrals (electron-electron repulsion).

## Step 1 — Define integrals

For H₂ in the STO-3G basis, we have 4 spin-orbitals:

```fsharp
let nModes = 4u

let oneBody = Map [
    ("0,0", Complex(-1.2563, 0.0))    // h₀₀ = ⟨σg↑|h|σg↑⟩
    ("1,1", Complex(-1.2563, 0.0))    // h₁₁ = ⟨σg↓|h|σg↓⟩
    ("2,2", Complex(-0.4719, 0.0))    // h₂₂ = ⟨σu↑|h|σu↑⟩
    ("3,3", Complex(-0.4719, 0.0))    // h₃₃ = ⟨σu↓|h|σu↓⟩
]

let twoBody = Map [
    ("0,0,0,0", Complex(0.6745, 0.0)); ("1,1,1,1", Complex(0.6745, 0.0))
    ("2,2,2,2", Complex(0.6974, 0.0)); ("3,3,3,3", Complex(0.6974, 0.0))
    ("0,0,1,1", Complex(0.6745, 0.0)); ("1,1,0,0", Complex(0.6745, 0.0))
    ("0,0,2,2", Complex(0.6636, 0.0)); ("2,2,0,0", Complex(0.6636, 0.0))
    ("0,0,3,3", Complex(0.6636, 0.0)); ("3,3,0,0", Complex(0.6636, 0.0))
    ("1,1,2,2", Complex(0.6636, 0.0)); ("2,2,1,1", Complex(0.6636, 0.0))
    ("1,1,3,3", Complex(0.6636, 0.0)); ("3,3,1,1", Complex(0.6636, 0.0))
    ("2,2,3,3", Complex(0.6974, 0.0)); ("3,3,2,2", Complex(0.6974, 0.0))
    ("0,2,2,0", Complex(0.1809, 0.0)); ("2,0,0,2", Complex(0.1809, 0.0))
    ("1,3,3,1", Complex(0.1809, 0.0)); ("3,1,1,3", Complex(0.1809, 0.0))
]
```

## Step 2 — Build a coefficient lookup

The Hamiltonian builder needs a function that returns `Some coefficient`
for known integrals and `None` for zero entries:

```fsharp
let lookup (key : string) =
    match key.Split(',').Length with
    | 2 -> oneBody |> Map.tryFind key
    | 4 -> twoBody |> Map.tryFind key
    | _ -> None
```

## Step 3 — Compute the qubit Hamiltonian

One function call does everything — loops over indices, looks up
coefficients, encodes each term, and combines results:

```fsharp
let hamiltonian = computeHamiltonian lookup nModes

printfn "H₂ Hamiltonian: %d Pauli terms\n" hamiltonian.SummandTerms.Length

for term in hamiltonian.DistributeCoefficient.SummandTerms do
    let sign = if term.Coefficient.Real >= 0.0 then "+" else ""
    printfn "  %s%.4f  %s" sign term.Coefficient.Real term.Signature
```

## Step 4 — Swap the encoding

Use `computeHamiltonianWith` to try any encoding:

```fsharp
let hBK = computeHamiltonianWith bravyiKitaevTerms lookup nModes
let hTT = computeHamiltonianWith ternaryTreeTerms  lookup nModes

// Or your custom scheme from the Encoding Internals chapter:
let hCustom = computeHamiltonianWith (encodeOperator myJW) lookup nModes
```

All three Hamiltonians have the same eigenvalues — they represent
identical physics. They differ only in the Pauli weight and number of
terms, which affects measurement cost on real quantum hardware.

---

**Next:** [Mixed Bosonic–Fermionic Systems](11-mixed-systems.html) — sector tags and hybrid workflows
