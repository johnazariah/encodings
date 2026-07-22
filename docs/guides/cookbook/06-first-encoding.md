# Your First Encoding

Now we arrive at FockMap's marquee feature: **fermion-to-qubit encoding**.

The problem: quantum computers have qubits (Pauli operators), but chemistry
uses fermions (ladder operators). We need a mapping between the two worlds.

## Jordan-Wigner: the classic

The simplest encoding is Jordan-Wigner (1928). It maps a ladder operator
to Pauli strings by inserting a chain of Z operators on all preceding qubits:

```fsharp
open Encodings
open Encodings.JordanWigner
// Encode a†₂ (creation on mode 2) for a 4-qubit system:
let result = jordanWignerTerms Raise 2u 4u

// Print the Pauli terms:
for term in result.DistributeCoefficient.SummandTerms do
    printfn "%s %s" term.PhasePrefix term.Signature
//  0.5 ZZXI
// -0.5i ZZYI
```

The result is $a^\dagger_2 = \tfrac{1}{2}(ZZXI) - \tfrac{i}{2}(ZZYI)$.

Notice the two Z operators before the X/Y — they track the **parity** of
electrons in modes 0 and 1. This is how the encoding preserves fermionic
anti-symmetry on a qubit register.

> **Ordering convention.** In a `PauliRegister` string, character position *i*
> is mode/qubit *i*, so **mode 0 is the leftmost character** (`"ZZXI"` = Z₀ Z₁ X₂ I₃).
> Occupation integers weight mode *j* by 2ʲ (mode 0 = least-significant bit), so the
> H₂ Hartree–Fock state |modes 0,1 occupied⟩ is the integer 3 = `0b0011`. To read a
> string in that occupation basis, or to produce a Qiskit-style Pauli-label string,
> **reverse it** (mode 0 becomes rightmost / bit 2⁰).

## The Z-chain problem

Watch what happens as you encode higher modes:

```fsharp
for j in 0u .. 7u do
    let terms = jordanWignerTerms Raise j 8u
    let weight =
        terms.DistributeCoefficient.SummandTerms.[0].Signature
        |> Seq.filter (fun c -> c <> 'I')
        |> Seq.length
    printfn "a†_%d → weight %d" j weight

// a†_0 → weight 1
// a†_1 → weight 2
// a†_2 → weight 3
// ...
// a†_7 → weight 8
```

The Pauli weight grows **linearly** — $O(n)$. For a 100-qubit molecule,
the last operator touches all 100 qubits. This is expensive to measure
on real hardware, and it's why we need better encodings.

---

**Next:** [Six Encodings, One Interface](07-five-encodings.html) — comparing all available encodings
