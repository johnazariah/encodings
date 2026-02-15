# Five Encodings, One Interface

Every encoding function in FockMap has the exact same type signature:

```fsharp
type EncoderFn = LadderOperatorUnit -> uint32 -> uint32 -> PauliRegisterSequence
//                    Raise/Lower       mode     total       result
//                                      index    qubits
```

This makes them drop-in replacements for each other:

```fsharp
let n = 8u
let mode = 2u

let jw  = jordanWignerTerms       Raise mode n   // O(n) weight
let bk  = bravyiKitaevTerms       Raise mode n   // O(log₂ n)
let par = parityTerms             Raise mode n   // O(n)
let bt  = balancedBinaryTreeTerms Raise mode n   // O(log₂ n)
let tt  = ternaryTreeTerms        Raise mode n   // O(log₃ n) — optimal
```

## Side-by-side comparison

```fsharp
let encodings = [
    ("Jordan-Wigner",     jordanWignerTerms)
    ("Bravyi-Kitaev",     bravyiKitaevTerms)
    ("Parity",            parityTerms)
    ("Binary Tree",       balancedBinaryTreeTerms)
    ("Ternary Tree",      ternaryTreeTerms)
]

printfn "%-20s  %s" "Encoding" "Pauli weight of a†₂"
printfn "%-20s  %s" "────────" "───────────────────"

for (name, encode) in encodings do
    let terms = encode Raise 2u 8u
    let weight =
        terms.DistributeCoefficient.SummandTerms
        |> Array.map (fun t ->
            t.Signature |> Seq.filter (fun c -> c <> 'I') |> Seq.length)
        |> Array.max
    printfn "%-20s  %d" name weight
```

## Understanding the output: PauliRegister and PauliRegisterSequence

Every encoding returns a `PauliRegisterSequence` — a sum of `PauliRegister`
terms. Let's unpack these types:

```fsharp
// A PauliRegister is a fixed-width Pauli string with a coefficient:
let reg = PauliRegister("ZZXI", Complex.One)

reg.Signature       // "ZZXI"
reg.Coefficient     // Complex(1.0, 0.0)
reg.[0]             // Some Z — first qubit
reg.[2]             // Some X — third qubit

// Immutable update:
let modified = reg.WithOperatorAt 2 Y       // "ZZYI"

// Multiply registers (qubit-by-qubit):
let product = reg * modified

// A PauliRegisterSequence is a sum of registers:
let seq = PauliRegisterSequence([| reg; modified |])
seq.SummandTerms              // PauliRegister[]
seq.DistributeCoefficient     // folds global coeff into each term

// Look up by signature:
let (found, r) = seq.["ZZXI"]
```

---

**Next:** [Encoding Internals](08-encoding-internals.html) — Majorana decomposition and custom schemes
