# Chapter 20: The Complete Pipeline — H₂ End to End

_Every step, one molecule, one script. From geometry to quantum circuit in under 50 lines of code._

## In This Chapter

- **What you'll learn:** The entire pipeline assembled into a single executable workflow, using H₂ as the running example.
- **Why this matters:** This chapter is the capstone of the H₂ story that began in Chapter 1. Every concept from the preceding 18 chapters appears here in its final, integrated form.
- **Prerequisites:** All of Stages 1–5.

---

## The Script

```fsharp
open System.Numerics
open Encodings

// ═══════════════════════════════════════════════════════════════
//  Stage 1: Integrals (from Chapter 3)
// ═══════════════════════════════════════════════════════════════

let Vnn = 0.7151043391  // Nuclear repulsion (Ha)

let integrals = Map [
    // One-body (spin-orbital, physicist's convention)
    ("0,0", Complex(-1.2563390730, 0.0))
    ("1,1", Complex(-1.2563390730, 0.0))
    ("2,2", Complex(-0.4718960244, 0.0))
    ("3,3", Complex(-0.4718960244, 0.0))

    // Two-body (same-spin + cross-spin, physicist's convention)
    ("0,0,0,0", Complex(0.6744887663, 0.0))
    ("1,1,1,1", Complex(0.6744887663, 0.0))
    ("2,2,2,2", Complex(0.6973979495, 0.0))
    ("3,3,3,3", Complex(0.6973979495, 0.0))
    ("0,0,2,2", Complex(0.6636340479, 0.0))
    ("2,2,0,0", Complex(0.6636340479, 0.0))
    ("1,1,3,3", Complex(0.6636340479, 0.0))
    ("3,3,1,1", Complex(0.6636340479, 0.0))
    ("0,2,2,0", Complex(0.1809312700, 0.0))
    ("2,0,0,2", Complex(0.1809312700, 0.0))
    ("1,3,3,1", Complex(0.1809312700, 0.0))
    ("3,1,1,3", Complex(0.1809312700, 0.0))
    // ... (remaining integrals as in Chapter 3)
]

let factory key = integrals |> Map.tryFind key

// ═══════════════════════════════════════════════════════════════
//  Stage 2: Encode (five encodings in a loop)
// ═══════════════════════════════════════════════════════════════

let encoders = [
    ("Jordan-Wigner",       jordanWignerTerms)
    ("Bravyi-Kitaev",       bravyiKitaevTerms)
    ("Parity",              parityTerms)
    ("Binary Tree",         balancedBinaryTreeTerms)
    ("Ternary Tree",        ternaryTreeTerms)
]

for (name, encoder) in encoders do
    let ham = computeHamiltonianWith encoder factory 4u

    // ═══════════════════════════════════════════════════════════
    //  Stage 3: Taper
    // ═══════════════════════════════════════════════════════════
    let tapResult = taper defaultTaperingOptions ham

    // ═══════════════════════════════════════════════════════════
    //  Stage 4: Trotterize
    // ═══════════════════════════════════════════════════════════
    let step = firstOrderTrotter 0.1 tapResult.Hamiltonian
    let cnots = trotterCnotCount step

    // ═══════════════════════════════════════════════════════════
    //  Report
    // ═══════════════════════════════════════════════════════════
    printfn "%-15s  %d qubits → %d tapered  %d terms  %d CNOTs/step"
        name
        tapResult.OriginalQubitCount
        tapResult.TaperedQubitCount
        (tapResult.Hamiltonian.SummandTerms.Length)
        cnots
```

---

## What Just Happened

In under 50 lines, we:

1. Defined the H₂ molecular integrals (Chapter 3)
2. Built the qubit Hamiltonian with 5 different encodings (Chapters 5–6)
3. Tapered each Hamiltonian to remove redundant qubits (Chapters 8–10)
4. Decomposed each into a Trotter step and counted CNOTs (Chapters 12–14)
5. Reported the results

This is the complete pipeline from our cover diagram — molecule to circuit — in a single script that runs in under a second.

---

## The Numbers

| Encoding | Qubits (orig → tapered) | Terms | CNOTs/step |
|:---|:---:|:---:|:---:|
| Jordan–Wigner | 4 → ? | ? | ? |
| Bravyi–Kitaev | 4 → ? | ? | ? |
| Parity | 4 → ? | ? | ? |
| Binary Tree | 4 → ? | ? | ? |
| Ternary Tree | 4 → ? | ? | ? |

*(Run the script to fill in the values — the exercise is the point.)*

---

## From H₂ to H₂O

The same script, with different integrals and $n = 12$, produces the H₂O pipeline. The structure is identical — only the numbers are larger. This is the power of a composable pipeline: adding a bigger molecule doesn't require new code, just new integral tables.

We'll use H₂O for the application sidebars in the next chapter.

---

## Key Takeaways

- The entire pipeline is 5 function calls: `computeHamiltonianWith`, `taper`, `firstOrderTrotter`, `decomposeTrotterStep`, `toOpenQasm`.
- The same code works for any molecule — change the integrals and qubit count.
- Encoding choice, tapering, and Trotter order are all independent knobs.

---

**Previous:** [Chapter 18 — Python Bridge](18-python-bridge.html)

**Next:** [Chapter 20 — Scaling: From H₂ to FeMo-co](20-scaling.html)
