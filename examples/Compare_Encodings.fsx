/// Compare Encodings — Educational Demo
///
/// This script demonstrates the 5 fermion-to-qubit encodings:
///
///   1. Jordan-Wigner   — Simple, O(n) Pauli weight
///   2. Bravyi-Kitaev   — Balanced, O(log n) weight via Fenwick tree
///   3. Parity          — Like JW but tracks parity, O(n) weight
///   4. Binary Tree     — Balanced binary tree, O(log n) weight
///   5. Ternary Tree    — Balanced ternary tree, O(log₃ n) weight (optimal!)
///
/// We encode a single creation operator a†₂ on 8 modes and compare results.

#r "../src/Encodings/bin/Debug/net8.0/Encodings.dll"

open Encodings

// ════════════════════════════════════════════════════════════════════
//  Setup: Encode creation operator a†₂ on 8 modes
// ════════════════════════════════════════════════════════════════════

let modeIndex = 2u      // Mode we're creating a particle in
let totalModes = 8u     // Total number of fermionic modes

let encodings =
    [ "Jordan-Wigner", jordanWignerTerms
      "Bravyi-Kitaev", bravyiKitaevTerms
      "Parity",        parityTerms
      "Binary Tree",   balancedBinaryTreeTerms
      "Ternary Tree",  ternaryTreeTerms ]

// ════════════════════════════════════════════════════════════════════
//  Helper: Calculate Pauli weight (non-identity operators)
// ════════════════════════════════════════════════════════════════════

let pauliWeight (reg: PauliRegister) =
    reg.Signature |> Seq.sumBy (fun c -> if c = 'I' then 0 else 1)

let maxWeight (prs: PauliRegisterSequence) =
    prs.SummandTerms |> Array.map pauliWeight |> Array.max

// ════════════════════════════════════════════════════════════════════
//  Encode and display each encoding
// ════════════════════════════════════════════════════════════════════

printfn ""
printfn "Comparing Fermion-to-Qubit Encodings"
printfn "Operator: a†₂ (creation on mode 2) with %d total modes" totalModes
printfn "════════════════════════════════════════════════════════════════════"

for (name, encode) in encodings do
    let result = encode Raise modeIndex totalModes
    let weight = maxWeight result

    printfn ""
    printfn "▶ %s (max weight = %d)" name weight
    printfn "  Pauli strings:"
    for term in result.SummandTerms do
        printfn "    %s" term.Signature

// ════════════════════════════════════════════════════════════════════
//  Weight comparison summary
// ════════════════════════════════════════════════════════════════════

printfn ""
printfn "════════════════════════════════════════════════════════════════════"
printfn "Weight Scaling Comparison"
printfn "════════════════════════════════════════════════════════════════════"
printfn ""
printfn "  Encoding       Max Weight   Scaling"
printfn "  ─────────────────────────────────────"

for (name, encode) in encodings do
    let result = encode Raise modeIndex totalModes
    let weight = maxWeight result
    let scaling =
        match name with
        | "Jordan-Wigner" | "Parity" -> "O(n)"
        | "Bravyi-Kitaev" | "Binary Tree" -> "O(log n)"
        | _ -> "O(log₃ n)"
    printfn "  %-14s  %5d        %s" name weight scaling

printfn ""
printfn "Key insight: Tree-based encodings reduce circuit depth"
printfn "from O(n) to O(log n), enabling more efficient simulation."
