/// Bosonic Encoding Example
/// ========================
/// Demonstrates encoding bosonic ladder operators (b†, b) as qubit Pauli
/// strings using three truncation encodings: Unary, Binary, and Gray code.
///
/// Models a simple harmonic oscillator H = ω b†b and a two-mode coupled
/// system H = ω₁ b₁†b₁ + ω₂ b₂†b₂ + g(b₁†b₂ + b₂†b₁).
///
/// Reference: Sawaya et al., "Resource-efficient digital quantum simulation
/// of d-level systems" (arXiv:1909.05820)

#r "../src/Encodings/bin/Debug/net8.0/Encodings.dll"

open System.Numerics
open Encodings

// ══════════════════════════════════════════════
//  Part 1: Single-mode encodings at a glance
// ══════════════════════════════════════════════

printfn "═══════════════════════════════════════════════════"
printfn "  Bosonic-to-Qubit Encodings: Comparison"
printfn "═══════════════════════════════════════════════════\n"

let cutoff = 4u   // truncation: n ∈ {0, 1, 2, 3}
let numModes = 1u

let encodings =
    [ ("Unary",      unaryBosonTerms,   unaryQubitsPerMode)
      ("Binary",     binaryBosonTerms,  binaryQubitsPerMode)
      ("Gray code",  grayCodeBosonTerms, binaryQubitsPerMode) ]

let pauliWeight (reg : PauliRegister) =
    reg.Signature |> Seq.sumBy (fun c -> if c = 'I' then 0 else 1)

printfn "Truncation cutoff d = %d\n" cutoff

printfn "%-12s  %6s  %6s  %9s" "Encoding" "Qubits" "Terms" "MaxWeight"
printfn "%-12s  %6s  %6s  %9s" "────────" "──────" "─────" "─────────"

for (name, encoder, qpm) in encodings do
    let cr = encoder Raise 0u numModes cutoff
    let nTerms = cr.SummandTerms.Length
    let maxW = if nTerms > 0 then cr.SummandTerms |> Array.map pauliWeight |> Array.max else 0
    printfn "%-12s  %6d  %6d  %9d" name (qpm (int cutoff)) nTerms maxW

// ══════════════════════════════════════════════
//  Part 2: Pauli decomposition of b† (binary, d=4)
// ══════════════════════════════════════════════

printfn "\n\n═══════════════════════════════════════════════════"
printfn "  b† Pauli decomposition (Binary, d=4, 2 qubits)"
printfn "═══════════════════════════════════════════════════\n"

let binaryCreation = binaryBosonTerms Raise 0u 1u 4u
for term in binaryCreation.SummandTerms do
    printfn "  %+.4f %+.4fi  %s" term.Coefficient.Real term.Coefficient.Imaginary term.Signature

// ══════════════════════════════════════════════
//  Part 3: Harmonic oscillator H = ω b†b
// ══════════════════════════════════════════════

printfn "\n\n═══════════════════════════════════════════════════"
printfn "  Harmonic Oscillator H = ω b†b"
printfn "═══════════════════════════════════════════════════\n"

let omega = 1.0   // natural frequency

printfn "Computing number operator b†b from Pauli products...\n"

for (name, encoder, _) in encodings do
    let cr = encoder Raise 0u 1u cutoff
    let an = encoder Lower 0u 1u cutoff
    let numberOp = cr * an
    printfn "  %s encoding — b†b has %d Pauli terms:" name numberOp.SummandTerms.Length
    for term in numberOp.SummandTerms do
        let scaled = term.Coefficient * Complex(omega, 0.)
        printfn "    %+.4f %+.4fi  %s" scaled.Real scaled.Imaginary term.Signature
    printfn ""

// ══════════════════════════════════════════════
//  Part 4: Two coupled modes (beam-splitter)
// ══════════════════════════════════════════════

printfn "═══════════════════════════════════════════════════"
printfn "  Two-Mode Coupling: H = ω₁n̂₁ + ω₂n̂₂ + g(b₁†b₂ + b₂†b₁)"
printfn "═══════════════════════════════════════════════════\n"

let omega1 = 1.0
let omega2 = 1.5
let g = 0.3
let twoModes = 2u
let d = 3u  // smaller cutoff for readability

printfn "ω₁ = %.1f,  ω₂ = %.1f,  g = %.1f,  d = %d\n" omega1 omega2 g d

// Using binary encoding for compactness
let qpm = binaryQubitsPerMode (int d)
printfn "Binary encoding: %d qubits/mode × %d modes = %d total qubits\n" qpm (int twoModes) (qpm * int twoModes)

// Number operator terms: n̂_j = b†_j b_j
let cr0 = binaryBosonTerms Raise 0u twoModes d
let an0 = binaryBosonTerms Lower 0u twoModes d
let n0 = cr0 * an0

let cr1 = binaryBosonTerms Raise 1u twoModes d
let an1 = binaryBosonTerms Lower 1u twoModes d
let n1 = cr1 * an1

printfn "n̂₁ (mode 0): %d Pauli terms" n0.SummandTerms.Length
printfn "n̂₂ (mode 1): %d Pauli terms" n1.SummandTerms.Length

// Coupling: b†₁ b₂ + b†₂ b₁
// b†₁ b₂ = (creation on mode 0) × (annihilation on mode 1)
let coupling1 = cr0 * an1   // b†_0 b_1
let coupling2 = cr1 * an0   // b†_1 b_0

printfn "b₁†b₂: %d Pauli terms" coupling1.SummandTerms.Length
printfn "b₂†b₁: %d Pauli terms" coupling2.SummandTerms.Length

// Total Hamiltonian terms (simplistic count — combining would reduce)
let totalTerms =
    n0.SummandTerms.Length
    + n1.SummandTerms.Length
    + coupling1.SummandTerms.Length
    + coupling2.SummandTerms.Length

printfn "\nTotal Hamiltonian Pauli terms (before combining): %d" totalTerms

// ══════════════════════════════════════════════
//  Part 5: Scaling table
// ══════════════════════════════════════════════

printfn "\n\n═══════════════════════════════════════════════════"
printfn "  Scaling: qubits and terms vs. cutoff d"
printfn "═══════════════════════════════════════════════════\n"

printfn "%-4s  %-16s  %-16s  %-16s" "d" "Unary (q,terms)" "Binary (q,terms)" "Gray (q,terms)"
printfn "%-4s  %-16s  %-16s  %-16s" "──" "────────────────" "────────────────" "────────────────"

for d in [2u; 3u; 4u; 6u; 8u] do
    let results =
        encodings |> List.map (fun (_, enc, qpm) ->
            let q = qpm (int d)
            let t = (enc Raise 0u 1u d).SummandTerms.Length
            sprintf "(%d, %d)" q t)
    printfn "%-4d  %-16s  %-16s  %-16s" d results.[0] results.[1] results.[2]

printfn "\nDone."
