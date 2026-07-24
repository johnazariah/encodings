# Grand Finale: Three Encodings, One Molecule

Let's tie every chapter together. This script encodes H₂ with three
different encodings and compares the results:

```fsharp
open Encodings.Fcidump
open Encodings.Hamiltonian
open Encodings.JordanWigner
open Encodings.BravyiKitaev
open Encodings.TreeEncoding
open Encodings.MajoranaEncoding
open Encodings.Tapering
open Encodings.Trotterization
open Encodings.CircuitOutput
open Encodings
open System.Numerics

// ─── Integrals: the canonical H₂/STO-3G FCIDUMP (from Chapter 10) ───
let nModes = 4u

// A self-contained H₂/STO-3G coefficient factory. The FCIDUMP adapter
// returns the raw physicist integrals ⟨pq|rs⟩ = (pr|qs), so the factory is
// fed straight to the builders — the library applies the two-body ½ and swap.
let fcidump = """
 &FCI NORB=   2,NELEC= 2,MS2=0,
  ORBSYM=1,1,
  ISYM=1,
 &END
 0.6747559268144484    1    1    1    1
 0.6637114013508132    1    1    2    2
 0.1812104620151968    2    1    2    1
 0.6637114013508132    2    2    1    1
 0.697651504490461     2    2    2    2
 -1.253309786645977    1    1  0  0
 -0.4750688487721783   2    2  0  0
 0.7151043390810812  0  0  0  0
"""

let lookup =
    let (factory, _core, _nso) = parseToSpinOrbitalFactory fcidump
    factory

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
| Jordan–Wigner | 15 | 4 | 2.13 | 36 |
| Bravyi–Kitaev | 15 | 4 | 2.40 | 44 |
| Ternary Tree | 15 | 4 | 2.40 | 44 |

> **Surprise:** For H₂ (4 qubits), Jordan–Wigner has the *lowest* CNOT
> count (36 vs 44)! The $O(n)$ weight scaling only becomes problematic at
> larger $n$. At $n = 32$, JW needs 62 CNOTs per worst-case rotation while the
> ternary tree needs only 8. See [Lab 07](../../labs/07-trotter-cost.html)
> for the full scaling analysis.

---

**Next:** [Bosonic-to-Qubit Encodings](14-bosonic-encodings.html) — Unary, Binary, and Gray code truncation encodings

**Back to:** [Cookbook index](index.html) — quick reference and further reading
