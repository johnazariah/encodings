# Grand Finale: Three Encodings, One Molecule

Let's tie every chapter together. This script encodes H₂ with three
different encodings and compares the results:

```fsharp
open Encodings
open System.Numerics

// ─── Integrals (from the Hamiltonian chapter) ───────────────
let nModes = 4u

let integrals = Map [
    ("00", Complex(-1.2563, 0.0)); ("11", Complex(-1.2563, 0.0))
    ("22", Complex(-0.4719, 0.0)); ("33", Complex(-0.4719, 0.0))
    ("0000", Complex(0.6745, 0.0)); ("1111", Complex(0.6745, 0.0))
    ("2222", Complex(0.6974, 0.0)); ("3333", Complex(0.6974, 0.0))
    ("0011", Complex(0.6745, 0.0)); ("1100", Complex(0.6745, 0.0))
    ("0022", Complex(0.6636, 0.0)); ("2200", Complex(0.6636, 0.0))
    ("0033", Complex(0.6636, 0.0)); ("3300", Complex(0.6636, 0.0))
    ("1122", Complex(0.6636, 0.0)); ("2211", Complex(0.6636, 0.0))
    ("1133", Complex(0.6636, 0.0)); ("3311", Complex(0.6636, 0.0))
    ("2233", Complex(0.6974, 0.0)); ("3322", Complex(0.6974, 0.0))
    ("0220", Complex(0.1809, 0.0)); ("2002", Complex(0.1809, 0.0))
    ("1331", Complex(0.1809, 0.0)); ("3113", Complex(0.1809, 0.0))
]

let lookup key =
    match (key : string).Length with
    | 2 | 4 -> integrals |> Map.tryFind key
    | _ -> None

// ─── Encode and compare ─────────────────────────────────────
let encoders = [
    ("Jordan-Wigner",  jordanWignerTerms)
    ("Bravyi-Kitaev",  bravyiKitaevTerms)
    ("Ternary Tree",   ternaryTreeTerms)
]

for (name, encoder) in encoders do
    let ham = computeHamiltonianWith encoder lookup nModes
    let terms = ham.DistributeCoefficient.SummandTerms

    let avgWeight =
        terms
        |> Array.averageBy (fun t ->
            t.Signature
            |> Seq.filter (fun c -> c <> 'I')
            |> Seq.length
            |> float)

    printfn "═══ %s ═══" name
    printfn "  Terms: %d    Avg Pauli weight: %.2f\n" terms.Length avgWeight
    for t in terms do
        let sign = if t.Coefficient.Real >= 0.0 then "+" else ""
        printfn "    %s%.4f  %s" sign t.Coefficient.Real t.Signature
    printfn ""
```

All three Hamiltonians have the same eigenvalues — they represent
identical physics. The differences in term count and Pauli weight
affect the cost of measurement on real quantum hardware.

---

**Next:** [Bosonic-to-Qubit Encodings](14-bosonic-encodings.html) — Unary, Binary, and Gray code truncation encodings

**Back to:** [Cookbook index](index.html) — quick reference and further reading
