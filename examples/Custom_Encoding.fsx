// Custom_Encoding.fsx — Defining your own fermion-to-qubit encoding
//
// This script demonstrates how to create a custom EncodingScheme by
// specifying three index-set functions. Any encoding in the Majorana
// decomposition family can be defined this way.

#r "../src/Encodings/bin/Debug/net8.0/Encodings.dll"

open Encodings

// ─────────────────────────────────────────────────────────────────────
// The EncodingScheme interface
// ─────────────────────────────────────────────────────────────────────
//
// An EncodingScheme is a record with three functions:
//
//   type EncodingScheme = {
//       Update     : int -> int -> Set<int>   // U(j, n) — qubits to flip
//       Parity     : int -> Set<int>          // P(j)    — parity qubits
//       Occupation : int -> Set<int>          // Occ(j)  — occupation qubits
//   }
//
// These sets determine how the Majorana operators c_j and d_j are built:
//   c_j = X_{j ∪ U(j)} · Z_{P(j)}
//   d_j = Y_j · X_{U(j)} · Z_{(P(j) ⊕ Occ(j)) \ {j}}
//
// The ladder operators follow: a†_j = ½(c_j − i·d_j), a_j = ½(c_j + i·d_j)

// ─────────────────────────────────────────────────────────────────────
// Built-in schemes: jordanWignerScheme, bravyiKitaevScheme, parityScheme
// ─────────────────────────────────────────────────────────────────────

printfn "=== Built-in Parity Scheme ==="
printfn "parityScheme.Update 2 4 = %A" (parityScheme.Update 2 4)  // {3}
printfn "parityScheme.Parity 2   = %A" (parityScheme.Parity 2)    // {1}
printfn "parityScheme.Occupation 2 = %A" (parityScheme.Occupation 2) // {1;2}

// ─────────────────────────────────────────────────────────────────────
// Defining a custom scheme
// ─────────────────────────────────────────────────────────────────────
// Let's define a "local parity" scheme where each mode j only references
// its immediate neighbor j-1 for parity (similar to parity encoding but
// with empty update set for demonstration).

let localParityScheme : EncodingScheme =
    { Update     = fun _ _ -> Set.empty              // No propagating updates
      Parity     = fun j   -> if j > 0 then Set.singleton (j - 1) else Set.empty
      Occupation = fun j   -> Set.singleton j }      // Each mode is local

printfn "\n=== Custom Local Parity Scheme ==="
printfn "Update 2 4     = %A" (localParityScheme.Update 2 4)
printfn "Parity 2       = %A" (localParityScheme.Parity 2)
printfn "Occupation 2   = %A" (localParityScheme.Occupation 2)

// ─────────────────────────────────────────────────────────────────────
// Encoding operators
// ─────────────────────────────────────────────────────────────────────
// Use encodeOperator to map a ladder operator to Pauli strings.

let n = 4u  // 4 qubits
let j = 2u  // Mode index 2

printfn "\n=== Encoding a†₂ (raising operator at mode 2) ==="
let raising = encodeOperator localParityScheme Raise j n
printfn "Result: %d Pauli terms" raising.SummandTerms.Length
for term in raising.SummandTerms do
    printfn "  %O" term

printfn "\n=== Encoding a₂ (lowering operator at mode 2) ==="
let lowering = encodeOperator localParityScheme Lower j n
for term in lowering.SummandTerms do
    printfn "  %O" term

// ─────────────────────────────────────────────────────────────────────
// Verification: Compare with Jordan-Wigner
// ─────────────────────────────────────────────────────────────────────
printfn "\n=== Jordan-Wigner a†₂ for comparison ==="
let jwRaising = encodeOperator jordanWignerScheme Raise j n
for term in jwRaising.SummandTerms do
    printfn "  %O" term
