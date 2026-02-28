# Grand Finale: Three Encodings, One Molecule

Let's tie every chapter together. This script encodes H₂ with three
different encodings and compares the results:

```fsharp
open Encodings
open System.Numerics

// ─── Integrals (from the Hamiltonian chapter) ───────────────
let nModes = 4u

let integrals = Map [
    ("0,0", Complex(-1.2563, 0.0)); ("1,1", Complex(-1.2563, 0.0))
    ("2,2", Complex(-0.4719, 0.0)); ("3,3", Complex(-0.4719, 0.0))
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

let lookup key =
    match (key : string).Split(',').Length with
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

## From Pauli Weight to CNOT Count

Pauli weight isn't just an abstract metric — it directly determines
the number of **CNOT gates** needed on real hardware. To implement a
single Pauli rotation $e^{-i\theta P}$ where $P$ has weight $w$,
the standard CNOT staircase decomposition requires **$2(w-1)$ CNOTs**.

For a first-order Trotter step, you apply one rotation per Hamiltonian
term, so the total CNOT cost is $\sum_k 2(w_k - 1)$ over all terms.

| Encoding | Terms | Max weight | Avg weight | CNOTs / Trotter step |
|:---|:---:|:---:|:---:|:---:|
| Jordan–Wigner | 6 | 2 | 1.3 | 4 |
| Bravyi–Kitaev | 6 | 3 | 2.0 | 12 |
| Ternary Tree | 6 | 3 | 1.7 | 8 |

> **Surprise:** For H₂ (4 qubits), Jordan–Wigner has the *lowest* CNOT
> count! The $O(n)$ weight scaling only becomes problematic at larger $n$.
> At $n = 32$, JW needs 62 CNOTs per worst-case rotation while the
> ternary tree needs only 8. See [Lab 07](../../labs/07-trotter-cost.html)
> for the full scaling analysis.

---

**Next:** [Bosonic-to-Qubit Encodings](14-bosonic-encodings.html) — Unary, Binary, and Gray code truncation encodings

**Back to:** [Cookbook index](index.html) — quick reference and further reading
