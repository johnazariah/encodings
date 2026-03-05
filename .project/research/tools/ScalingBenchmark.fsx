/// Scaling Benchmark — Fermion-to-Qubit Encoding Comparison
///
/// Measures Pauli weight statistics and gate costs across all four
/// encodings as the number of spin-orbitals increases.
///
/// Key metrics:
///   - Max Pauli weight  (→ circuit depth)
///   - Mean Pauli weight (→ average gate cost)
///   - Operator count    (→ number of circuit terms)
///
/// Expected scaling:
///   Jordan-Wigner  :  O(n)     max weight
///   Parity         :  O(n)     max weight
///   Bravyi-Kitaev  :  O(log n) max weight
///   Binary Tree    :  O(log n) max weight
///   Ternary Tree   :  O(log₃n) max weight — optimal!

#r "Encodings/bin/Debug/net10.0/Encodings.dll"

open System
open System.Diagnostics
open Encodings

// ═══════════════════════════════════════════════════════
//  Measurement helpers
// ═══════════════════════════════════════════════════════

/// Pauli weight = number of non-identity Paulis in a register.
let pauliWeight (reg : PauliRegister) =
    reg.Signature |> Seq.sumBy (fun c -> if c = 'I' then 0 else 1)

/// Max Pauli weight across terms in a PauliRegisterSequence.
let maxPauliWeight (prs : PauliRegisterSequence) =
    prs.SummandTerms |> Array.map pauliWeight |> Array.max

/// Maximum Pauli weight across all creation/annihilation operators
/// for modes 0..n-1.
let maxWeightAllModes (encode : EncoderFn) (n : uint32) =
    [| for j in 0u .. n-1u do
           let c = encode Raise j n
           let a = encode Lower j n
           yield maxPauliWeight c
           yield maxPauliWeight a |]
    |> Array.max

/// Mean Pauli weight across all creation operators.
let meanWeightCreation (encode : EncoderFn) (n : uint32) =
    let weights =
        [| for j in 0u .. n-1u do
               let c = encode Raise j n
               for t in c.SummandTerms do
                   yield pauliWeight t |]
    float (Array.sum weights) / float (Array.length weights)

/// Maximum Pauli weight of a single hopping term a†_i a_j.
let maxWeightHopping (encode : EncoderFn) (n : uint32) =
    let mutable maxW = 0
    for i in 0u .. n-1u do
        for j in 0u .. n-1u do
            if i <> j then
                let hop = (encode Raise i n) * (encode Lower j n)
                let d = hop.DistributeCoefficient
                for t in d.SummandTerms do
                    let w = pauliWeight t
                    if w > maxW then maxW <- w
    maxW

// ═══════════════════════════════════════════════════════
//  Encodings to benchmark
// ═══════════════════════════════════════════════════════

let encodings : (string * EncoderFn) list =
    [ "JW",      jordanWignerTerms
      "BK",      bravyiKitaevTerms
      "Parity",  parityTerms
      "BinTree", balancedBinaryTreeTerms
      "TerTree", ternaryTreeTerms ]

// ═══════════════════════════════════════════════════════
//  Run benchmark
// ═══════════════════════════════════════════════════════

let sizes = [| 4; 8; 12; 16; 20; 24 |]

printfn ""
printfn "═══════════════════════════════════════════════════════════════════════════"
printfn "  Fermion-to-Qubit Encoding Scaling Benchmark"
printfn "═══════════════════════════════════════════════════════════════════════════"

// ─────────────────────────────────────────────
//  Table 1: Max Pauli weight of single operators
// ─────────────────────────────────────────────

printfn ""
printfn "  Table 1: Max Pauli weight (over all a†_j, a_j)"
printfn "  ─────────────────────────────────────────────────"
printfn "  %5s  %6s  %6s  %6s  %6s  %6s" "n" "JW" "BK" "Parity" "BinTr" "TerTr"

for n in sizes do
    let nU = uint32 n
    let row =
        encodings
        |> List.map (fun (_, enc) ->
            let sw = Stopwatch.StartNew()
            let w = maxWeightAllModes enc nU
            sw.Stop()
            w)
    printfn "  %5d  %6d  %6d  %6d  %6d  %6d" n row.[0] row.[1] row.[2] row.[3] row.[4]

// ─────────────────────────────────────────────
//  Table 2: Mean Pauli weight of creation ops
// ─────────────────────────────────────────────

printfn ""
printfn "  Table 2: Mean Pauli weight (creation operators)"
printfn "  ──────────────────────────────────────────────────"
printfn "  %5s  %6s  %6s  %6s  %6s  %6s" "n" "JW" "BK" "Parity" "BinTr" "TerTr"

for n in sizes do
    let nU = uint32 n
    let row =
        encodings
        |> List.map (fun (_, enc) -> meanWeightCreation enc nU)
    printfn "  %5d  %6.2f  %6.2f  %6.2f  %6.2f  %6.2f" n row.[0] row.[1] row.[2] row.[3] row.[4]

// ─────────────────────────────────────────────
//  Table 3: Max hopping term weight (small n)
// ─────────────────────────────────────────────

let smallSizes = [| 4; 8; 12 |]  // O(n²) pairs, keep small

printfn ""
printfn "  Table 3: Max Pauli weight of hopping terms a†_i a_j"
printfn "  ──────────────────────────────────────────────────────"
printfn "  %5s  %6s  %6s  %6s  %6s  %6s" "n" "JW" "BK" "Parity" "BinTr" "TerTr"

for n in smallSizes do
    let nU = uint32 n
    let row =
        encodings
        |> List.map (fun (_, enc) ->
            let sw = Stopwatch.StartNew()
            let w = maxWeightHopping enc nU
            sw.Stop()
            w)
    printfn "  %5d  %6d  %6d  %6d  %6d  %6d" n row.[0] row.[1] row.[2] row.[3] row.[4]

// ─────────────────────────────────────────────
//  Theoretical bounds
// ─────────────────────────────────────────────

printfn ""
printfn "  ─────────────────────────────────────────────────"
printfn "  Theoretical bounds on max single-operator weight:"
printfn ""
printfn "    Jordan-Wigner  :  n      (Z-chain of length n)"
printfn "    Parity         :  n      (X-chain of length n)"
printfn "    Bravyi-Kitaev  :  O(log₂ n)"
printfn "    Binary Tree    :  O(log₂ n)"
printfn "    Ternary Tree   :  O(log₃ n)  — provably optimal"
printfn ""
printfn "  Reference bounds:"
printfn "  %5s  %8s  %8s  %8s" "n" "log₂n" "log₃n" "n/2"
for n in sizes do
    let lg2 = Math.Ceiling(Math.Log2(float n))
    let lg3 = Math.Ceiling(Math.Log(float n) / Math.Log 3.0)
    printfn "  %5d  %8.0f  %8.0f  %8d" n lg2 lg3 (n/2)

printfn ""
printfn "  Done."
