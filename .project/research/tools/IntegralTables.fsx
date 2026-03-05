/// IntegralTables.fsx — Generate complete integral tables for Paper 1 Appendix A
///
/// Produces publication-ready tables:
///   1. Spatial one-body integrals h_{pq}  (2×2)
///   2. Spatial two-body integrals [pq|rs]  (unique non-zero elements + symmetry relations)
///   3. Spin-orbital one-body integrals h_{pq}  (4×4)
///   4. Spin-orbital two-body integrals ⟨pq‖rs⟩  (all 24 non-zero)
///   5. Full Pauli decomposition for each encoding (all terms + coefficients)
///   6. Summary statistics (term count, Pauli weight, identity coefficient)
///
/// Reference: O'Malley et al., Phys. Rev. X 6, 031007 (2016) — Table I
///            Seeley, Richard, Love — arXiv:1208.5986

#r "../../src/Encodings/bin/Debug/net10.0/Encodings.dll"

open System
open System.Numerics
open Encodings

// ═══════════════════════════════════════════════════════
//  H₂ STO-3G Molecular Integrals
//  R = 1.401 Bohr (near equilibrium)
// ═══════════════════════════════════════════════════════

/// Nuclear repulsion energy V_nn = Z_A Z_B / R
let nuclearRepulsion = 0.7151043390810812

/// Spatial orbitals: 0 = σ_g (bonding), 1 = σ_u* (antibonding)
let orbitalLabels = [| "σ_g"; "σ_u*" |]

/// One-body spatial integrals h_{pq} in the MO basis
let h1_spatial = Array2D.init 2 2 (fun p q ->
    if p = q then
        [| -1.2563390730032498; -0.4718960244306283 |].[p]
    else 0.0)

/// Two-body spatial integral values (chemist's notation [pq|rs])
let h2_vals = [| 0.6744887663049631   // [00|00]
                 0.6973979494693556   // [11|11]
                 0.6636340478615040   // [00|11] = [11|00]
                 0.6975782468828187 |] // [01|10] = [10|01] = [01|01] = [10|10]

/// Full two-body spatial tensor (chemist's notation)
let h2_spatial = Array4D.init 2 2 2 2 (fun p q r s ->
    match (p,q,r,s) with
    | (0,0,0,0) -> h2_vals.[0]
    | (1,1,1,1) -> h2_vals.[1]
    | (0,0,1,1) | (1,1,0,0) -> h2_vals.[2]
    | (0,1,1,0) | (1,0,0,1) | (0,1,0,1) | (1,0,1,0) -> h2_vals.[3]
    | _ -> 0.0)

// ═══════════════════════════════════════════════════════
//  Spin-orbital expansion
//  Interleaved ordering: 0α, 0β, 1α, 1β
// ═══════════════════════════════════════════════════════

let nSpin = 4
let n = uint32 nSpin

/// Spin label for spin-orbital index
let spinLabel j = if j % 2 = 0 then "α" else "β"

/// Spatial index for spin-orbital index (interleaved convention)
let spatialIndex j = j / 2

/// Full spin-orbital label
let spinOrbitalLabel j =
    sprintf "%d%s" (spatialIndex j) (spinLabel j)

/// One-body spin-orbital integrals: h[p,q] = h_spatial[p/2, q/2] × δ(σ_p, σ_q)
let h1_spin =
    [| for p in 0 .. nSpin-1 do
           for q in 0 .. nSpin-1 do
               if p%2 = q%2 then
                   let v = h1_spatial.[p/2, q/2]
                   if abs v > 1e-15 then
                       yield (uint32 p, uint32 q, v) |]

/// Two-body spin-orbital integrals in physicist's notation:
/// ⟨pq|rs⟩ = [p/2, r/2 | q/2, s/2]_chemist × δ(σ_p, σ_r) × δ(σ_q, σ_s)
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
//  Qubit Hamiltonian construction
// ═══════════════════════════════════════════════════════

/// Build one-body Pauli terms: Σ_{pq} h_{pq} a†_p a_q
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

/// Build two-body Pauli terms: ½ Σ_{pqrs} ⟨pq|rs⟩ a†_p a†_q a_s a_r
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

/// Build the full electronic Hamiltonian (no nuclear repulsion)
let molecularHamiltonian (encode : EncoderFn) =
    [| oneBodyTerms encode; twoBodyTerms encode |]
    |> PauliRegisterSequence

// ═══════════════════════════════════════════════════════
//  Pauli term collection & simplification
// ═══════════════════════════════════════════════════════

/// Collect a PauliRegisterSequence into a dictionary keyed by Pauli string,
/// summing coefficients of like terms. Returns sorted list of (signature, coeff).
let collectTerms (ham : PauliRegisterSequence) =
    let dict = System.Collections.Generic.Dictionary<string, Complex>()
    for term in ham.SummandTerms do
        let sig' = term.Signature
        let c = term.Coefficient
        if dict.ContainsKey(sig') then
            dict.[sig'] <- dict.[sig'] + c
        else
            dict.[sig'] <- c
    dict
    |> Seq.map (fun kv -> (kv.Key, kv.Value))
    |> Seq.filter (fun (_, c) -> Complex.Abs c > 1e-14)
    |> Seq.sortBy (fun (sig', _) ->
        // Sort: Identity first, then by Pauli weight, then alphabetically
        let weight = sig' |> Seq.filter (fun c -> c <> 'I') |> Seq.length
        (weight, sig'))
    |> Seq.toArray

/// Pauli weight of a signature string
let pauliWeight (sig' : string) =
    sig' |> Seq.filter (fun c -> c <> 'I') |> Seq.length

// ═══════════════════════════════════════════════════════
//  OUTPUT — Table 1: Spatial One-Body Integrals
// ═══════════════════════════════════════════════════════

printfn ""
printfn "╔═══════════════════════════════════════════════════════════════════════╗"
printfn "║  IntegralTables — H₂ STO-3G Complete Integral & Pauli Decomposition ║"
printfn "║  For Paper 1 Appendix A                                             ║"
printfn "╚═══════════════════════════════════════════════════════════════════════╝"
printfn ""

printfn "═══════════════════════════════════════════════════════"
printfn "  Table 1: Spatial One-Body Integrals h_{pq} (Ha)"
printfn "  MO basis: p,q ∈ {0=σ_g, 1=σ_u*}"
printfn "═══════════════════════════════════════════════════════"
printfn ""
printfn "  ┌─────────────────────────────────────────┐"
printfn "  │          q = 0 (σ_g)    q = 1 (σ_u*)    │"
printfn "  ├─────────────────────────────────────────┤"
for p in 0 .. 1 do
    let label = if p = 0 then "σ_g " else "σ_u*"
    printfn "  │ p=%d (%s) %+16.13f  %+16.13f │" p label h1_spatial.[p,0] h1_spatial.[p,1]
printfn "  └─────────────────────────────────────────┘"
printfn ""
printfn "  Note: Off-diagonal h_{01} = h_{10} = 0 by MO symmetry."
printfn ""

// ═══════════════════════════════════════════════════════
//  OUTPUT — Table 2: Spatial Two-Body Integrals
// ═══════════════════════════════════════════════════════

printfn "═══════════════════════════════════════════════════════"
printfn "  Table 2: Spatial Two-Body Integrals [pq|rs] (Ha)"
printfn "  Chemist's notation: [pq|rs] = ∫∫ φ_p*(1)φ_q(1) (1/r₁₂) φ_r*(2)φ_s(2)"
printfn "═══════════════════════════════════════════════════════"
printfn ""

// Print all 16 elements (2^4) for completeness, grouped by non-zero families
printfn "  Unique non-zero values:"
printfn "  ──────────────────────────────────────────"
printfn "    [00|00]                     = %+.16f" h2_vals.[0]
printfn "    [11|11]                     = %+.16f" h2_vals.[1]
printfn "    [00|11] = [11|00]           = %+.16f" h2_vals.[2]
printfn "    [01|10] = [10|01]           = %+.16f" h2_vals.[3]
printfn "           = [01|01] = [10|10]"
printfn ""
printfn "  Full tensor (all 16 elements):"
printfn "  ──────────────────────────────────────────"
for p in 0 .. 1 do
    for q in 0 .. 1 do
        for r in 0 .. 1 do
            for s in 0 .. 1 do
                let v = h2_spatial.[p,q,r,s]
                if abs v > 1e-15 then
                    printfn "    [%d%d|%d%d] = %+.16f" p q r s v
printfn ""
printfn "  8-fold symmetry: [pq|rs] = [qp|sr] = [rs|pq] = [sr|qp]"
printfn "                 = [pq|rs]* (real integrals)"
printfn ""

// ═══════════════════════════════════════════════════════
//  OUTPUT — Table 3: Spin-Orbital One-Body Integrals
// ═══════════════════════════════════════════════════════

printfn "═══════════════════════════════════════════════════════"
printfn "  Table 3: Spin-Orbital One-Body Integrals h_{pq} (Ha)"
printfn "  Interleaved ordering: 0=0α, 1=0β, 2=1α, 3=1β"
printfn "  Rule: h_spin[p,q] = h_spatial[p/2, q/2] × δ(σ_p, σ_q)"
printfn "═══════════════════════════════════════════════════════"
printfn ""

printfn "  ┌────────────────────────────────────────────────────────────────────┐"
printfn "  │         0α               0β               1α               1β     │"
printfn "  ├────────────────────────────────────────────────────────────────────┤"
for p in 0 .. nSpin-1 do
    let label = spinOrbitalLabel p
    printf "  │ %s  " (label.PadRight(3))
    for q in 0 .. nSpin-1 do
        let v =
            if p%2 = q%2 then h1_spatial.[p/2, q/2]
            else 0.0
        printf "%+16.13f  " v
    printfn "│"
printfn "  └────────────────────────────────────────────────────────────────────┘"
printfn ""
printfn "  Non-zero entries (same-spin only):"
for (p, q, v) in h1_spin do
    printfn "    h[%s, %s] = %+.16f  (from h_spatial[%d,%d])"
        (spinOrbitalLabel (int p)) (spinOrbitalLabel (int q)) v (int p/2) (int q/2)
printfn "  Total non-zero: %d entries" h1_spin.Length
printfn ""

// ═══════════════════════════════════════════════════════
//  OUTPUT — Table 4: Spin-Orbital Two-Body Integrals
// ═══════════════════════════════════════════════════════

printfn "═══════════════════════════════════════════════════════"
printfn "  Table 4: Spin-Orbital Two-Body Integrals ⟨pq|rs⟩ (Ha)"
printfn "  Physicist's notation: ⟨pq|rs⟩ = [pr|qs]_chemist"
printfn "  Rule: ⟨pq|rs⟩ = [p/2,r/2|q/2,s/2]_chem × δ(σ_p,σ_r) × δ(σ_q,σ_s)"
printfn "═══════════════════════════════════════════════════════"
printfn ""

// Group by spin combination for clarity
let spinCombos =
    [| ("αα-αα", fun p q r s -> p%2=0 && q%2=0 && r%2=0 && s%2=0)
       ("ββ-ββ", fun p q r s -> p%2=1 && q%2=1 && r%2=1 && s%2=1)
       ("αβ-αβ", fun p q r s -> p%2=0 && q%2=1 && r%2=0 && s%2=1)
       ("βα-βα", fun p q r s -> p%2=1 && q%2=0 && r%2=1 && s%2=0) |]

for (label, filter) in spinCombos do
    let matching =
        h2_spin |> Array.filter (fun (p,q,r,s,_) ->
            filter (int p) (int q) (int r) (int s))
    if matching.Length > 0 then
        printfn "  ── %s block (%d terms) ──" label matching.Length
        for (p, q, r, s, v) in matching do
            printfn "    ⟨%s %s|%s %s⟩ = %+.16f  ([%d%d|%d%d]_chem)"
                (spinOrbitalLabel (int p)) (spinOrbitalLabel (int q))
                (spinOrbitalLabel (int r)) (spinOrbitalLabel (int s))
                v (int p/2) (int r/2) (int q/2) (int s/2)
        printfn ""

printfn "  Total non-zero two-body spin-orbital integrals: %d" h2_spin.Length
printfn ""

// ═══════════════════════════════════════════════════════
//  OUTPUT — Table 5: Nuclear Repulsion
// ═══════════════════════════════════════════════════════

printfn "═══════════════════════════════════════════════════════"
printfn "  Nuclear Repulsion Energy"
printfn "═══════════════════════════════════════════════════════"
printfn ""
printfn "  V_nn = %.16f Ha" nuclearRepulsion
printfn "       = %.10f eV" (nuclearRepulsion * 27.2114)
printfn "       = %.6f kcal/mol" (nuclearRepulsion * 627.509)
printfn ""

// ═══════════════════════════════════════════════════════
//  OUTPUT — Tables 6–10: Pauli Decomposition per Encoding
// ═══════════════════════════════════════════════════════

let encodings : (string * EncoderFn) list =
    [ "Jordan-Wigner",    jordanWignerTerms
      "Bravyi-Kitaev",    bravyiKitaevTerms
      "Parity",           parityTerms
      "Balanced Binary",  balancedBinaryTreeTerms
      "Balanced Ternary", ternaryTreeTerms ]

printfn "═══════════════════════════════════════════════════════"
printfn "  Tables 6–10: Complete Pauli Decomposition"
printfn "  Electronic Hamiltonian H_el = H₁ + H₂  (no V_nn)"
printfn "═══════════════════════════════════════════════════════"
printfn ""

/// Store results for summary table
let mutable summaryRows = []

for (name, enc) in encodings do
    printfn "─── %s ───" name
    printfn ""

    let ham = molecularHamiltonian enc
    let terms = collectTerms ham

    // Print all terms
    printfn "  ┌──────┬────────┬──────────────────────┬────────┐"
    printfn "  │  #   │ String │     Coefficient      │ Weight │"
    printfn "  ├──────┼────────┼──────────────────────┼────────┤"
    let mutable termIdx = 1
    let mutable identityCoeff = Complex.Zero
    let mutable maxWeight = 0
    let mutable totalWeight = 0
    for (sig', coeff) in terms do
        let w = pauliWeight sig'
        totalWeight <- totalWeight + w
        if w > maxWeight then maxWeight <- w
        if sig' = "IIII" then identityCoeff <- coeff

        // Format coefficient: show real if imaginary is negligible
        let coeffStr =
            if abs coeff.Imaginary < 1e-14 then
                sprintf "%+.10f" coeff.Real
            else
                sprintf "(%+.6f, %+.6f)" coeff.Real coeff.Imaginary
        printfn "  │ %3d  │  %s  │ %s │   %d    │" termIdx sig' (coeffStr.PadRight(20)) w
        termIdx <- termIdx + 1
    printfn "  └──────┴────────┴──────────────────────┴────────┘"
    printfn ""

    let nTerms = terms.Length
    let avgWeight = if nTerms > 0 then float totalWeight / float nTerms else 0.0
    printfn "  Total terms:        %d" nTerms
    printfn "  Identity coeff:     %+.10f Ha" identityCoeff.Real
    printfn "  Max Pauli weight:   %d" maxWeight
    printfn "  Avg Pauli weight:   %.2f" avgWeight
    printfn ""

    summaryRows <- summaryRows @ [ (name, nTerms, identityCoeff.Real, maxWeight, avgWeight) ]

// ═══════════════════════════════════════════════════════
//  OUTPUT — Summary Comparison Table
// ═══════════════════════════════════════════════════════

printfn "═══════════════════════════════════════════════════════"
printfn "  Summary Comparison Across All Encodings"
printfn "═══════════════════════════════════════════════════════"
printfn ""
printfn "  ┌──────────────────┬───────┬──────────────────┬──────┬──────┐"
printfn "  │    Encoding      │ Terms │ Identity Coeff   │ MaxW │ AvgW │"
printfn "  ├──────────────────┼───────┼──────────────────┼──────┼──────┤"
for (name, nTerms, idCoeff, maxW, avgW) in summaryRows do
    let nameStr = name.PadRight(16)
    printfn "  │ %s │  %3d  │ %+14.10f │  %2d  │ %.2f │" nameStr nTerms idCoeff maxW avgW
printfn "  └──────────────────┴───────┴──────────────────┴──────┴──────┘"
printfn ""

// ═══════════════════════════════════════════════════════
//  OUTPUT — Cross-Encoding Term Equivalence
// ═══════════════════════════════════════════════════════

printfn "═══════════════════════════════════════════════════════"
printfn "  Cross-Encoding Verification"
printfn "═══════════════════════════════════════════════════════"
printfn ""

// All encodings should have the same Identity coefficient
// (since Tr(H) is invariant and the identity coefficient = Tr(H)/2^n)
let identCoeffs =
    summaryRows
    |> List.map (fun (name, _, idCoeff, _, _) -> (name, idCoeff))

let allIdentEqual =
    identCoeffs
    |> List.map snd
    |> List.pairwise
    |> List.forall (fun (a, b) -> abs (a - b) < 1e-10)

if allIdentEqual then
    printfn "  ✅ Identity coefficient is the same across all encodings"
    printfn "     (expected: all unitary transforms preserve Tr(H)/2^n)"
else
    printfn "  ❌ Identity coefficients differ!"
    for (name, coeff) in identCoeffs do
        printfn "     %s: %+.12f" name coeff
printfn ""

// All should have the same number of terms
let termCounts =
    summaryRows |> List.map (fun (_, nTerms, _, _, _) -> nTerms)
let allSameTerms = termCounts |> List.distinct |> List.length = 1

if allSameTerms then
    printfn "  ✅ All encodings produce %d Pauli terms" termCounts.[0]
else
    printfn "  ℹ  Term counts vary across encodings:"
    for (name, nTerms, _, _, _) in summaryRows do
        printfn "     %s: %d terms" name nTerms
printfn ""

// ═══════════════════════════════════════════════════════
//  OUTPUT — One-Body vs Two-Body Breakdown
// ═══════════════════════════════════════════════════════

printfn "═══════════════════════════════════════════════════════"
printfn "  One-Body vs Two-Body Term Breakdown"
printfn "═══════════════════════════════════════════════════════"
printfn ""

for (name, enc) in encodings do
    let h1 = oneBodyTerms enc
    let h2 = twoBodyTerms enc
    let h1terms = collectTerms h1
    let h2terms = collectTerms h2
    printfn "  %s:" name
    printfn "    One-body: %d unique Pauli terms (from %d spin-orbital integrals)" h1terms.Length h1_spin.Length
    printfn "    Two-body: %d unique Pauli terms (from %d spin-orbital integrals)" h2terms.Length h2_spin.Length
    printfn ""

// ═══════════════════════════════════════════════════════
//  OUTPUT — Reference Energy Values
// ═══════════════════════════════════════════════════════

printfn "═══════════════════════════════════════════════════════"
printfn "  Reference Energy Values"
printfn "═══════════════════════════════════════════════════════"
printfn ""
printfn "  Source: O'Malley et al., Phys. Rev. X 6, 031007 (2016)"
printfn "  Basis:  STO-3G minimal basis, R = 1.401 Bohr"
printfn "  System: H₂ (2 electrons, 2 spatial orbitals, 4 spin-orbitals)"
printfn ""
printfn "  V_nn (nuclear repulsion)    = %+.16f Ha" nuclearRepulsion
printfn "  E_HF (Hartree-Fock, total)  = (V_nn + diagonal of |1100⟩)"
printfn "  E_FCI (Full CI, N_e=2)      = exact within STO-3G basis"
printfn ""
printfn "  Qubit overhead:"
printfn "    n_qubits = %d (= number of spin-orbitals)" nSpin
printfn "    Hilbert space dim = 2^%d = %d" nSpin (1 <<< nSpin)
printfn "    Physical sector dim = C(%d,2) = %d (N_e = 2)" nSpin (nSpin * (nSpin - 1) / 2)
printfn ""

printfn "Done."
