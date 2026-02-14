/// ═══════════════════════════════════════════════════════
///  ParityOperator.fsx — SYMBOLIC VERSION
///  Compute the encoded parity operator P̂ = ∏_j (I - 2n̂_j)
///  entirely via Pauli algebra (no matrices, no 2^n scaling).
///
///  Key insight: PauliRegisterSequence multiplication implements
///  the Pauli group algebra exactly. We never build matrices,
///  so this works for n = 100+ trivially.
///
///  For Paper 3 §4.2 (Symmetry Fractionalization)
///  and Paper 2 (Software — demonstrating symbolic algebra)
/// ═══════════════════════════════════════════════════════

#r "../../src/Encodings/bin/Debug/net10.0/Encodings.dll"

open System
open System.Numerics
open Encodings

// ═══════════════════════════════════════════════════════
//  Symbolic Pauli algebra helpers
// ═══════════════════════════════════════════════════════

/// Build the n-qubit identity as a PauliRegisterSequence.
let pauliIdentity (n : int) : PauliRegisterSequence =
    let ops = List.replicate n Pauli.I
    PauliRegisterSequence [| PauliRegister(ops, Complex.One) |]

/// Build the number operator n̂_j = a†_j a_j symbolically.
let numberOperatorSym (encode : EncoderFn) (j : int) (n : int) : PauliRegisterSequence =
    let adag = encode Raise (uint32 j) (uint32 n)
    let a    = encode Lower (uint32 j) (uint32 n)
    adag * a  // Exact Pauli algebra

/// Build (I - 2·n̂_j) symbolically.
/// This is the parity factor for mode j: eigenvalue +1 if unoccupied, -1 if occupied.
let parityFactor (encode : EncoderFn) (j : int) (n : int) : PauliRegisterSequence =
    let nj = numberOperatorSym encode j n
    // -2 · n̂_j
    let minus2nj = 
        nj.SummandTerms 
        |> Array.map (fun r -> r.ResetPhase (r.Coefficient * Complex(-2.0, 0.0)))
        |> PauliRegisterSequence
    // I + (-2·n̂_j)  — combine via array constructor (merges like terms)
    let id = pauliIdentity n
    PauliRegisterSequence [| id; minus2nj |]

/// Build the full parity operator P̂ = ∏_{j=0}^{n-1} (I - 2n̂_j) symbolically.
let parityOperatorSym (encode : EncoderFn) (n : int) : PauliRegisterSequence =
    let mutable result = pauliIdentity n
    for j in 0 .. n-1 do
        let factor = parityFactor encode j n
        result <- result * factor
    result

/// Pauli weight of a single PauliRegister (count non-I sites).
let pauliWeight (reg : PauliRegister) : int =
    reg.Signature |> Seq.filter (fun c -> c <> 'I') |> Seq.length

/// Max Pauli weight across all terms in a PauliRegisterSequence.
let maxWeight (rs : PauliRegisterSequence) : int =
    rs.SummandTerms |> Array.map pauliWeight |> Array.max

/// Number of terms in a PauliRegisterSequence.
let termCount (rs : PauliRegisterSequence) : int =
    rs.SummandTerms.Length

/// Pretty-print a PauliRegisterSequence.
let formatPRS (rs : PauliRegisterSequence) : string =
    let terms = rs.SummandTerms
    if terms.Length = 0 then "0"
    elif terms.Length = 1 then
        let t = terms.[0]
        let c = t.Coefficient
        if abs(c.Imaginary) < 1e-12 then
            if abs(c.Real - 1.0) < 1e-12 then sprintf "+%s" t.Signature
            elif abs(c.Real + 1.0) < 1e-12 then sprintf "-%s" t.Signature
            else sprintf "%.4f·%s" c.Real t.Signature
        else sprintf "(%.3f%+.3fi)·%s" c.Real c.Imaginary t.Signature
    else
        let shown = terms |> Array.sortByDescending (fun t -> t.Coefficient.Magnitude)
        let strs = shown |> Array.take (min 4 shown.Length) |> Array.map (fun t ->
            let c = t.Coefficient
            if abs(c.Imaginary) < 1e-12 then sprintf "%.2f·%s" c.Real t.Signature
            else sprintf "(%.2f%+.2fi)·%s" c.Real c.Imaginary t.Signature)
        let joined = strs |> String.concat " + "
        if shown.Length > 4 then sprintf "%s + ... (%d terms)" joined shown.Length
        else joined

// ═══════════════════════════════════════════════════════
//  Single-operator weight analysis
// ═══════════════════════════════════════════════════════

/// Weight of the encoded a†_j operator.
let creationWeight (encode : EncoderFn) (j : int) (n : int) : int =
    let adag = encode Raise (uint32 j) (uint32 n)
    adag.SummandTerms |> Array.map pauliWeight |> Array.max

/// Weight of the encoded a†_i a_j hopping operator.
let hoppingWeight (encode : EncoderFn) (i : int) (j : int) (n : int) : int =
    let adag_i = encode Raise (uint32 i) (uint32 n)
    let a_j    = encode Lower (uint32 j) (uint32 n)
    let hop = adag_i * a_j
    hop.SummandTerms |> Array.map pauliWeight |> Array.max

// ═══════════════════════════════════════════════════════
//  Run
// ═══════════════════════════════════════════════════════

printfn ""
printfn "╔═══════════════════════════════════════════════════════╗"
printfn "║  Parity Operator Analysis — SYMBOLIC (exact algebra) ║"
printfn "║  No matrices. No 2^n scaling. Works for n = 100+.    ║"
printfn "╚═══════════════════════════════════════════════════════╝"
printfn ""

let encodings : (string * EncoderFn) list =
    [ "Jordan-Wigner",        (fun op j n -> jordanWignerTerms op j n)
      "Bravyi-Kitaev",        (fun op j n -> bravyiKitaevTerms op j n)
      "Parity",               (fun op j n -> parityTerms op j n)
      "Balanced Binary",      (fun op j n -> balancedBinaryTreeTerms op j n)
      "Balanced Ternary",     (fun op j n -> ternaryTreeTerms op j n) ]

// ─── Parity operator for small n ───
printfn "━━━ Parity operator P̂ = ∏(I - 2n̂_j) ━━━"
printfn ""
for n in [4; 6; 8; 10; 12] do
    printfn "── n = %d ──" n
    for (name, encode) in encodings do
        let parity = parityOperatorSym encode n
        let w = maxWeight parity
        let nTerms = termCount parity
        let repr = formatPRS parity
        printfn "  %-22s  weight=%d  terms=%d  P̂ = %s" name w nTerms repr
    printfn ""

// ─── Weight scaling table ───
printfn "━━━ Parity weight scaling w(P̂) ━━━"
printfn ""
printfn "  n     JW    BK   PAR   BinT  TerT"
printfn "  ──   ───   ───   ───   ────  ────"
for n in [2; 4; 6; 8; 10; 12; 16; 20; 24; 32] do
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let weights =
        encodings |> List.map (fun (_, encode) ->
            let parity = parityOperatorSym encode n
            maxWeight parity)
    sw.Stop()
    let ws = weights |> List.map (sprintf "%4d") |> String.concat "  "
    printfn "  %2d  %s   (%.1f ms)" n ws sw.Elapsed.TotalMilliseconds

printfn ""

// ─── Single-operator weights ───
printfn "━━━ Single-operator weights w(a†_j) for n=8 ━━━"
printfn ""
printfn "  j    JW    BK   PAR   BinT  TerT"
printfn "  ─   ───   ───   ───   ────  ────"
let n8 = 8
for j in 0..n8-1 do
    let weights = encodings |> List.map (fun (_, enc) -> creationWeight enc j n8)
    let ws = weights |> List.map (sprintf "%4d") |> String.concat "  "
    printfn "  %d  %s" j ws
printfn ""

let avgWeights = encodings |> List.map (fun (name, enc) ->
    let avg = [0..n8-1] |> List.averageBy (fun j -> float (creationWeight enc j n8))
    (name, avg))
printfn "  Average single-operator weight (n=%d):" n8
for (name, avg) in avgWeights do
    printfn "    %-22s  %.2f" name avg
printfn ""

// ─── Hopping weights ───
printfn "━━━ Hopping term weights w(a†_i a_j) for n=8 ━━━"
printfn ""
for (name, encode) in encodings do
    printfn "  %s:" name
    printf "  j\\i "
    for i in 0..n8-1 do printf "%3d" i
    printfn ""
    for j in 0..n8-1 do
        printf "   %d  " j
        for i in 0..n8-1 do
            if i = j then printf "  ·"
            else printf "%3d" (hoppingWeight encode i j n8)
        printfn ""
    printfn ""

// ─── Large-n scaling demonstration ───
printfn "━━━ Large-n demonstration (symbolic algebra) ━━━"
printfn ""
for n in [32; 64; 100] do
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let parityJW = parityOperatorSym (fun op j nn -> jordanWignerTerms op j nn) n
    let tJW = sw.Elapsed.TotalMilliseconds
    sw.Restart()
    let parityBK = parityOperatorSym (fun op j nn -> bravyiKitaevTerms op j nn) n
    let tBK = sw.Elapsed.TotalMilliseconds
    sw.Restart()
    let parityPAR = parityOperatorSym (fun op j nn -> parityTerms op j nn) n
    let tPAR = sw.Elapsed.TotalMilliseconds
    printfn "  n=%3d:  JW weight=%d (%4.0f ms)  BK weight=%d (%4.0f ms)  PAR weight=%d (%4.0f ms)" 
        n (maxWeight parityJW) tJW (maxWeight parityBK) tBK (maxWeight parityPAR) tPAR
    printfn "          JW = %s" (formatPRS parityJW)
    printfn "          BK = %s" (formatPRS parityBK)
printfn ""

// ─── Verify P² = I symbolically ───
printfn "━━━ Verify P̂² = I (symbolic, n=4) ━━━"
printfn ""
for (name, encode) in encodings do
    let p = parityOperatorSym encode 4
    let p2 = p * p
    let terms = p2.SummandTerms
    let isIdentity = 
        terms.Length = 1 && 
        terms.[0].Signature = "IIII" && 
        abs(terms.[0].Coefficient.Real - 1.0) < 1e-12 &&
        abs(terms.[0].Coefficient.Imaginary) < 1e-12
    printfn "  %-22s  P̂² = %s  %s" name (formatPRS p2) (if isIdentity then "✅" else "❌")

printfn ""
printfn "Done."
