/// H₂ Molecular Hamiltonian — Encoding Comparison Demo
///
/// Encodes the hydrogen molecule (STO-3G, 4 spin-orbitals) using all four
/// fermion-to-qubit transforms and compares the resulting qubit operators.
///
/// The second-quantized electronic Hamiltonian is:
///
///   H = Σ_{pq} h_{pq} a†_p a_q  +  ½ Σ_{pqrs} ⟨pq|rs⟩ a†_p a†_q a_s a_r
///
/// We use literature values for H₂ at equilibrium bond length (0.7414 Å)
/// in the STO-3G basis set.  There are 2 spatial orbitals → 4 spin-orbitals.
///
/// References:
///   O'Malley et al., Phys. Rev. X 6, 031007 (2016) — Table I
///   Seeley, Richard, Love — arXiv:1208.5986 (integral coefficients)
///   OpenFermion hydrogen_integration_test.py (cross-validation)

#r "Encodings/bin/Debug/net8.0/Encodings.dll"

open System
open System.Numerics
open Encodings

// ═══════════════════════════════════════════════════════
//  H₂ STO-3G integrals at R = 0.7414 Å
// ═══════════════════════════════════════════════════════

let nuclearRepulsion = 0.7151043390810812

// ─────────────────────────────────────────────
//  Spatial-orbital integrals (2×2)
// ─────────────────────────────────────────────
//   orbital 0 = σ_g  (bonding)
//   orbital 1 = σ_u  (anti-bonding)

// One-electron integrals h1[p,q]
let h1_spatial = Array2D.init 2 2 (fun p q ->
    if p = q then
        [| -1.2563390730032498    // σ_g
           -0.4718960244306283 |].[p]   // σ_u
    else 0.0)

// Two-electron integrals h2[p,q,r,s] in chemist's notation [pq|rs]
//   [00|00] = 0.6744887663049631
//   [11|11] = 0.6973979494693556
//   [00|11] = [11|00] = 0.6636340478615040
//   [01|10] = [10|01] = [01|01]* = [10|10]* = 0.6975782468828187
//   (* by 8-fold symmetry of real orbitals)
let h2vals = [| 0.6744887663049631; 0.6973979494693556;
                0.6636340478615040; 0.6975782468828187 |]

let h2_spatial = Array4D.init 2 2 2 2 (fun p q r s ->
    match (p,q,r,s) with
    | (0,0,0,0) -> h2vals.[0]
    | (1,1,1,1) -> h2vals.[1]
    | (0,0,1,1) | (1,1,0,0) -> h2vals.[2]
    | (0,1,1,0) | (1,0,0,1) | (0,1,0,1) | (1,0,1,0) -> h2vals.[3]
    | _ -> 0.0)

// ─────────────────────────────────────────────
//  Expand spatial → spin-orbital integrals
// ─────────────────────────────────────────────
// Spin-orbital indexing:  0=0α  1=0β  2=1α  3=1β
// (spatial orbital p → spin-orbitals 2p (α) and 2p+1 (β))

let nSpin = 4

// One-body:  h1_spin[p,q] = h1_spatial[p/2, q/2] · δ(spin_p, spin_q)
let h1_spin =
    [| for p in 0 .. nSpin-1 do
           for q in 0 .. nSpin-1 do
               if p%2 = q%2 then
                   let v = h1_spatial.[p/2, q/2]
                   if abs v > 1e-15 then
                       yield (uint32 p, uint32 q, v) |]

// Two-body in physicist's notation: ⟨pq|rs⟩ = [pr|qs]  (chemist's)
// Spin constraint: spin_p = spin_r AND spin_q = spin_s
let h2_spin =
    [| for p in 0 .. nSpin-1 do
           for q in 0 .. nSpin-1 do
               for r in 0 .. nSpin-1 do
                   for s in 0 .. nSpin-1 do
                       if p%2 = r%2 && q%2 = s%2 then
                           let v = h2_spatial.[p/2, r/2, q/2, s/2]
                           if abs v > 1e-15 then
                               yield (uint32 p, uint32 q, uint32 r, uint32 s, v) |]

// ═══════════════════════════════════════════════════════
//  Build qubit Hamiltonian from ladder operators
// ═══════════════════════════════════════════════════════

let n = uint32 nSpin

/// Σ_{pq} h_{pq} a†_p a_q
let oneBodyTerms (encode : EncoderFn) =
    [| for (p, q, hpq) in h1_spin do
           let coeff = Complex (hpq, 0.0)
           let product = (encode Raise p n) * (encode Lower q n)
           yield product.DistributeCoefficient
                 |> fun prs ->
                     prs.SummandTerms
                     |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))
                     |> PauliRegisterSequence |]
    |> PauliRegisterSequence

/// ½ Σ_{pqrs} ⟨pq|rs⟩ a†_p a†_q a_s a_r
let twoBodyTerms (encode : EncoderFn) =
    let half = Complex (0.5, 0.0)
    [| for (p, q, r, s, v) in h2_spin do
           let coeff = Complex (v, 0.0) * half
           let product =
               (encode Raise p n) * (encode Raise q n)
               * (encode Lower s n) * (encode Lower r n)
           yield product.DistributeCoefficient
                 |> fun prs ->
                     prs.SummandTerms
                     |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))
                     |> PauliRegisterSequence |]
    |> PauliRegisterSequence

/// Full electronic Hamiltonian.
let molecularHamiltonian (encode : EncoderFn) =
    [| oneBodyTerms encode; twoBodyTerms encode |]
    |> PauliRegisterSequence

// ═══════════════════════════════════════════════════════
//  Analysis helpers
// ═══════════════════════════════════════════════════════

let pauliWeight (reg : PauliRegister) =
    reg.Signature |> Seq.sumBy (fun c -> if c = 'I' then 0 else 1)

let analyze (name : string) (ham : PauliRegisterSequence) =
    let terms = ham.SummandTerms
    let nTerms = terms.Length
    let weights = terms |> Array.map pauliWeight
    let maxW = weights |> Array.max
    let avgW = float (weights |> Array.sum) / float nTerms
    let totalCoeffMag =
        terms |> Array.sumBy (fun r -> Complex.Abs r.Coefficient)
    printfn ""
    printfn "  ┌─ %s ─" name
    printfn "  │  Pauli terms    : %d" nTerms
    printfn "  │  Max weight     : %d" maxW
    printfn "  │  Avg weight     : %.2f" avgW
    printfn "  │  Σ|coeff|       : %.6f" totalCoeffMag
    printfn "  │"
    printfn "  │  Terms:"
    for t in terms |> Array.sortBy (fun r -> r.Signature) do
        let c = t.Coefficient
        let re = if abs c.Real > 1e-10 then sprintf "%+.7f" c.Real else ""
        let im = if abs c.Imaginary > 1e-10 then sprintf "%+.7fi" c.Imaginary else ""
        let coefStr =
            match re, im with
            | "", "" -> " 0"
            | r, ""  -> r
            | "", i  -> i
            | r, i   -> sprintf "%s %s" r i
        printfn "  │    %s  %s" coefStr t.Signature
    printfn "  └─"

// ═══════════════════════════════════════════════════════
//  Run the comparison
// ═══════════════════════════════════════════════════════

printfn ""
printfn "═══════════════════════════════════════════════════════"
printfn "  H₂  Molecular Hamiltonian — Encoding Comparison"
printfn "  STO-3G basis, R = 0.7414 Å,  4 spin-orbitals"
printfn "  Nuclear repulsion energy: %.10f Ha" nuclearRepulsion
printfn "═══════════════════════════════════════════════════════"

let encodings =
    [ "Jordan-Wigner",  (jordanWignerTerms  : EncoderFn)
      "Bravyi-Kitaev",  (bravyiKitaevTerms  : EncoderFn)
      "Parity",         (parityTerms        : EncoderFn)
      "Ternary Tree",   (ternaryTreeTerms   : EncoderFn) ]

let results =
    encodings
    |> List.map (fun (name, enc) ->
        let ham = molecularHamiltonian enc
        analyze name ham
        (name, ham))

// ═══════════════════════════════════════════════════════
//  Summary comparison
// ═══════════════════════════════════════════════════════

printfn ""
printfn "═══════════════════════════════════════════════════════"
printfn "  Summary"
printfn "═══════════════════════════════════════════════════════"
printfn ""
printfn "  %-16s %6s %10s %10s %12s" "Encoding" "Terms" "Max Wt" "Avg Wt" "Σ|coeff|"
printfn "  %-16s %6s %10s %10s %12s" "────────" "─────" "──────" "──────" "────────"

for (name, ham) in results do
    let terms = ham.SummandTerms
    let weights = terms |> Array.map pauliWeight
    let maxW = weights |> Array.max
    let avgW = float (weights |> Array.sum) / float terms.Length
    let norm = terms |> Array.sumBy (fun r -> Complex.Abs r.Coefficient)
    printfn "  %-16s %6d %10d %10.2f %12.6f" name terms.Length maxW avgW norm

printfn ""

// ─────────────────────────────────────────────
//  Cross-validation: identity coefficient
// ─────────────────────────────────────────────

let idSig = "IIII"
printfn "  Identity-term coefficient (energy offset):"
for (name, ham) in results do
    match ham.[idSig] with
    | true, reg ->
        printfn "    %-16s  %.10f Ha  (electronic)" name reg.Coefficient.Real
        printfn "    %-16s  %.10f Ha  (+ nuclear repulsion)" "" (reg.Coefficient.Real + nuclearRepulsion)
    | false, _ ->
        printfn "    %-16s  (no identity term)" name

// ─────────────────────────────────────────────
//  Cross-validation: known JW coefficients
// ─────────────────────────────────────────────

printfn ""
printfn "  Known JW qubit Hamiltonian coefficients (Seeley et al.):"
printfn "    f1 (Z₀, Z₁)    ≈  0.1712   — single-qubit Z terms"
printfn "    f2 (Z₂, Z₃)    ≈ -0.2228   — single-qubit Z terms"
printfn "    f7 (XXYY etc.) ≈  0.0453   — excitation terms"
printfn ""

let jwHam = results |> List.find (fun (n,_) -> n = "Jordan-Wigner") |> snd
let tryCoeff sig' = match jwHam.[sig'] with true, r -> Some r.Coefficient.Real | _ -> None
printfn "  Computed JW coefficients:"
printfn "    IIIZ: %s" (match tryCoeff "IIIZ" with Some v -> sprintf "%.7f" v | None -> "missing")
printfn "    IIZI: %s" (match tryCoeff "IIZI" with Some v -> sprintf "%.7f" v | None -> "missing")
printfn "    IZII: %s" (match tryCoeff "IZII" with Some v -> sprintf "%.7f" v | None -> "missing")
printfn "    ZIII: %s" (match tryCoeff "ZIII" with Some v -> sprintf "%.7f" v | None -> "missing")

// Check for XX/YY excitation terms
let excitations = ["XXYY"; "XYYX"; "YXXY"; "YYXX"]
printfn ""
printfn "  Excitation terms (should be ≈ ±0.0454):"
for s in excitations do
    match tryCoeff s with
    | Some v -> printfn "    %s: %.7f" s v
    | None   -> printfn "    %s: missing" s

printfn ""
printfn "  Done."
