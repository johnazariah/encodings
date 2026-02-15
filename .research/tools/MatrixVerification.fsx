/// MatrixVerification.fsx — Eigenspectrum verification for fermion-to-qubit encodings
///
/// Builds the full 2ⁿ × 2ⁿ matrix representation of a qubit Hamiltonian
/// from its Pauli decomposition, diagonalises it, and compares eigenvalues
/// across all five encodings to verify they are isospectral.
///
/// Also verifies against the exact FCI eigenvalues obtained by directly
/// diagonalising the fermionic Hamiltonian in the occupation-number basis.

#r "../../src/Encodings/bin/Debug/net8.0/Encodings.dll"

open System
open System.Numerics
open Encodings

// ═══════════════════════════════════════════════════════
//  Complex matrix type (row-major, dense)
// ═══════════════════════════════════════════════════════

/// A dense complex matrix stored as a flat array (row-major).
type CMatrix =
    { Rows : int; Cols : int; Data : Complex[] }

    member m.Item(r, c) = m.Data.[r * m.Cols + c]

    static member Zero(n) =
        { Rows = n; Cols = n; Data = Array.create (n*n) Complex.Zero }

    static member Identity(n) =
        let d = Array.create (n*n) Complex.Zero
        for i in 0 .. n-1 do d.[i*n + i] <- Complex.One
        { Rows = n; Cols = n; Data = d }

    static member (+) (a : CMatrix, b : CMatrix) =
        let d = Array.init (a.Rows * a.Cols) (fun i -> a.Data.[i] + b.Data.[i])
        { a with Data = d }

    static member (*) (s : Complex, a : CMatrix) =
        { a with Data = a.Data |> Array.map (fun x -> s * x) }

/// Kronecker (tensor) product of two matrices.
let kron (a : CMatrix) (b : CMatrix) : CMatrix =
    let m = a.Rows * b.Rows
    let n = a.Cols * b.Cols
    let d = Array.create (m * n) Complex.Zero
    for ar in 0 .. a.Rows-1 do
        for ac in 0 .. a.Cols-1 do
            let aval = a.[ar, ac]
            if aval <> Complex.Zero then
                for br in 0 .. b.Rows-1 do
                    for bc in 0 .. b.Cols-1 do
                        d.[(ar * b.Rows + br) * n + (ac * b.Cols + bc)] <- aval * b.[br, bc]
    { Rows = m; Cols = n; Data = d }

// ═══════════════════════════════════════════════════════
//  Single-qubit Pauli matrices
// ═══════════════════════════════════════════════════════

let pauliI : CMatrix =
    { Rows = 2; Cols = 2
      Data = [| Complex.One; Complex.Zero; Complex.Zero; Complex.One |] }

let pauliX : CMatrix =
    { Rows = 2; Cols = 2
      Data = [| Complex.Zero; Complex.One; Complex.One; Complex.Zero |] }

let pauliY : CMatrix =
    { Rows = 2; Cols = 2
      Data = [| Complex.Zero; Complex(0.0, -1.0); Complex(0.0, 1.0); Complex.Zero |] }

let pauliZ : CMatrix =
    { Rows = 2; Cols = 2
      Data = [| Complex.One; Complex.Zero; Complex.Zero; Complex.MinusOne |] }

let pauliToMatrix = function
    | Pauli.I -> pauliI
    | Pauli.X -> pauliX
    | Pauli.Y -> pauliY
    | Pauli.Z -> pauliZ

// ═══════════════════════════════════════════════════════
//  Build matrix from Pauli string
// ═══════════════════════════════════════════════════════

/// Convert a PauliRegister (Pauli string + coefficient) to its 2ⁿ × 2ⁿ matrix.
let pauliStringToMatrix (reg : PauliRegister) : CMatrix =
    let n = reg.Signature.Length
    let mutable mat = pauliToMatrix (reg.[0] |> Option.defaultValue Pauli.I)
    for i in 1 .. n-1 do
        let p = reg.[i] |> Option.defaultValue Pauli.I
        mat <- kron mat (pauliToMatrix p)
    reg.Coefficient * mat

/// Convert an entire PauliRegisterSequence (sum of Pauli strings) to its matrix.
let hamiltonianToMatrix (ham : PauliRegisterSequence) (nQubits : int) : CMatrix =
    let dim = 1 <<< nQubits
    let mutable result = CMatrix.Zero dim
    for term in ham.SummandTerms do
        result <- result + pauliStringToMatrix term
    result

// ═══════════════════════════════════════════════════════
//  Eigenvalue solver (complex Hermitian via Jacobi)
// ═══════════════════════════════════════════════════════

/// Extract eigenvalues of a complex Hermitian matrix using Jacobi rotations.
/// We reduce H (n×n Hermitian) to a real (2n×2n) symmetric matrix:
///   M = [[Re(H), -Im(H)], [Im(H), Re(H)]]
/// whose eigenvalues come in degenerate pairs equal to the eigenvalues of H.
let eigenvalues (m : CMatrix) : float[] =
    let n = m.Rows

    // Verify Hermiticity
    let mutable maxNonHerm = 0.0
    for i in 0 .. n-1 do
        for j in 0 .. n-1 do
            let diff = Complex.Abs(m.[i,j] - Complex.Conjugate(m.[j,i]))
            if diff > maxNonHerm then maxNonHerm <- diff
    if maxNonHerm > 1e-10 then
        printfn "  ⚠ WARNING: Matrix is not Hermitian! Max deviation = %e" maxNonHerm

    // Check if matrix is purely real-symmetric
    let mutable maxIm = 0.0
    for i in 0 .. n-1 do
        for j in 0 .. n-1 do
            if abs m.[i,j].Imaginary > maxIm then maxIm <- abs m.[i,j].Imaginary

    let isReal = maxIm < 1e-14

    // Build real symmetric matrix to diagonalise
    let (size, realMat) =
        if isReal then
            // Direct real-symmetric Jacobi
            let a = Array2D.init n n (fun i j -> m.[i,j].Real)
            (n, a)
        else
            // Embed complex Hermitian H into real symmetric 2n×2n:
            //   M = [[Re(H), -Im(H)], [Im(H), Re(H)]]
            let nn = 2 * n
            let a = Array2D.zeroCreate<float> nn nn
            for i in 0 .. n-1 do
                for j in 0 .. n-1 do
                    let re = m.[i,j].Real
                    let im = m.[i,j].Imaginary
                    a.[i, j]     <- re       // top-left: Re(H)
                    a.[i, j+n]   <- -im      // top-right: -Im(H)
                    a.[i+n, j]   <- im       // bottom-left: Im(H)
                    a.[i+n, j+n] <- re       // bottom-right: Re(H)
            (nn, a)

    // Jacobi eigenvalue algorithm for real symmetric matrices
    let a = realMat
    let maxIter = 200 * size * size
    let mutable iter = 0
    let mutable converged = false

    while not converged && iter < maxIter do
        let mutable maxVal = 0.0
        let mutable p = 0
        let mutable q = 1
        for i in 0 .. size-1 do
            for j in i+1 .. size-1 do
                if abs a.[i, j] > maxVal then
                    maxVal <- abs a.[i, j]
                    p <- i
                    q <- j

        if maxVal < 1e-14 then
            converged <- true
        else
            // Classic Jacobi: solve tan(2θ) = 2a_{pq}/(a_{pp} - a_{qq})
            // Use the stable formula: t = sgn(τ) / (|τ| + √(1 + τ²))
            // where τ = (a_{qq} - a_{pp}) / (2 a_{pq})
            let apq = a.[p, q]
            let app = a.[p, p]
            let aqq = a.[q, q]
            let (c, s, t) =
                if abs (app - aqq) < 1e-30 then
                    // a_{pp} ≈ a_{qq}: θ = π/4
                    let sq = 1.0 / sqrt 2.0
                    (sq, (if apq >= 0.0 then sq else -sq), (if apq >= 0.0 then 1.0 else -1.0))
                else
                    let tau = (aqq - app) / (2.0 * apq)
                    let t' =
                        if tau >= 0.0 then
                            1.0 / (tau + sqrt(1.0 + tau * tau))
                        else
                            -1.0 / (-tau + sqrt(1.0 + tau * tau))
                    let c' = 1.0 / sqrt(1.0 + t' * t')
                    (c', t' * c', t')
            a.[p, p] <- app - t * apq
            a.[q, q] <- aqq + t * apq
            a.[p, q] <- 0.0
            a.[q, p] <- 0.0
            for r in 0 .. size-1 do
                if r <> p && r <> q then
                    let arp = a.[r, p]
                    let arq = a.[r, q]
                    a.[r, p] <- c*arp - s*arq
                    a.[p, r] <- a.[r, p]
                    a.[r, q] <- s*arp + c*arq
                    a.[q, r] <- a.[r, q]
            iter <- iter + 1

    if not converged then
        printfn "  ⚠ WARNING: Jacobi did not converge after %d iterations" maxIter

    // Extract eigenvalues from diagonal
    let diag = Array.init size (fun i -> a.[i, i]) |> Array.sort

    if isReal then
        diag
    else
        // The 2n eigenvalues come in degenerate pairs; take every other one
        [| for i in 0 .. 2 .. diag.Length - 1 do yield diag.[i] |]


// ═══════════════════════════════════════════════════════
//  H₂ STO-3G integrals (same as H2Demo.fsx)
// ═══════════════════════════════════════════════════════

let nuclearRepulsion = 0.7151043390810812

let h1_spatial = Array2D.init 2 2 (fun p q ->
    if p = q then
        [| -1.2563390730032498; -0.4718960244306283 |].[p]
    else 0.0)

let h2vals = [| 0.6744887663049631; 0.6973979494693556;
                0.6636340478615040; 0.6975782468828187 |]

let h2_spatial = Array4D.init 2 2 2 2 (fun p q r s ->
    match (p,q,r,s) with
    | (0,0,0,0) -> h2vals.[0]
    | (1,1,1,1) -> h2vals.[1]
    | (0,0,1,1) | (1,1,0,0) -> h2vals.[2]
    | (0,1,1,0) | (1,0,0,1) | (0,1,0,1) | (1,0,1,0) -> h2vals.[3]
    | _ -> 0.0)

let nSpin = 4
let n = uint32 nSpin

let h1_spin =
    [| for p in 0 .. nSpin-1 do
           for q in 0 .. nSpin-1 do
               if p%2 = q%2 then
                   let v = h1_spatial.[p/2, q/2]
                   if abs v > 1e-15 then
                       yield (uint32 p, uint32 q, v) |]

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

let molecularHamiltonian (encode : EncoderFn) =
    [| oneBodyTerms encode; twoBodyTerms encode |]
    |> PauliRegisterSequence

// ═══════════════════════════════════════════════════════
//  Build reference FCI Hamiltonian (direct, in occupation basis)
// ═══════════════════════════════════════════════════════

/// Build the exact FCI Hamiltonian matrix in the occupation-number basis
/// (2ⁿ × 2ⁿ, acting on ALL 16 states including wrong particle numbers).
/// This is the "ground truth" that all encodings must reproduce.
let buildFCIMatrix () : CMatrix =
    let dim = 1 <<< nSpin  // 16
    let data = Array.create (dim * dim) Complex.Zero

    // Helper: is orbital j occupied in state |s⟩?
    let occupied s j = (s >>> j) &&& 1 = 1

    // Helper: parity factor (-1)^{sum of occupations below j}
    let parityBelow s j =
        let mutable count = 0
        for k in 0 .. j-1 do
            if occupied s k then count <- count + 1
        if count % 2 = 0 then 1.0 else -1.0

    // Helper: a†_j |s⟩ = 0 if j occupied, else parity * |s with j set⟩
    let create j s =
        if occupied s j then None
        else Some (s ||| (1 <<< j), parityBelow s j)

    // Helper: a_j |s⟩ = 0 if j not occupied, else parity * |s with j cleared⟩
    let annihilate j s =
        if not (occupied s j) then None
        else Some (s ^^^ (1 <<< j), parityBelow s j)

    // Apply a†_p a_q to |s⟩
    let applyOverlap p q s =
        annihilate q s
        |> Option.bind (fun (s', phase1) ->
            create p s'
            |> Option.map (fun (s'', phase2) -> (s'', phase1 * phase2)))

    // Apply a†_p a†_q a_s a_r to |s⟩
    // Operators act right-to-left: first a_r, then a_s, then a†_q, then a†_p
    let applyExchange p q r_idx s_idx s =
        annihilate r_idx s
        |> Option.bind (fun (s1, ph1) ->
            annihilate s_idx s1
            |> Option.bind (fun (s2, ph2) ->
                create q s2
                |> Option.bind (fun (s3, ph3) ->
                    create p s3
                    |> Option.map (fun (s4, ph4) -> (s4, ph1 * ph2 * ph3 * ph4)))))

    // One-body terms: Σ h[p,q] a†_p a_q
    for (p, q, hpq) in h1_spin do
        let pi = int p
        let qi = int q
        for s in 0 .. dim-1 do
            match applyOverlap pi qi s with
            | Some (s', phase) ->
                data.[s' * dim + s] <- data.[s' * dim + s] + Complex(hpq * phase, 0.0)
            | None -> ()

    // Two-body terms: ½ Σ ⟨pq|rs⟩ a†_p a†_q a_s a_r
    for (p, q, r, s_idx, v) in h2_spin do
        let pi = int p
        let qi = int q
        let ri = int r
        let si = int s_idx
        let coeff = 0.5 * v
        for s in 0 .. dim-1 do
            match applyExchange pi qi ri si s with
            | Some (s', phase) ->
                data.[s' * dim + s] <- data.[s' * dim + s] + Complex(coeff * phase, 0.0)
            | None -> ()

    { Rows = dim; Cols = dim; Data = data }


// ═══════════════════════════════════════════════════════
//  Run verification
// ═══════════════════════════════════════════════════════

printfn ""
printfn "╔═══════════════════════════════════════════════════════╗"
printfn "║  Matrix Verification: Eigenspectrum Equivalence      ║"
printfn "║  H₂ STO-3G, 4 spin-orbitals, 16×16 Hamiltonian      ║"
printfn "╚═══════════════════════════════════════════════════════╝"
printfn ""

// ─── Reference: direct FCI ───
printfn "Building reference FCI matrix (direct second-quantization)..."
let fciMatrix = buildFCIMatrix ()
let fciEigenvalues = eigenvalues fciMatrix
printfn "  FCI eigenvalues (electronic, no V_nn):"
for (i, ev) in fciEigenvalues |> Array.indexed do
    printfn "    λ_%02d = %+.10f Ha" i ev
printfn ""

let fciGroundElectronic = fciEigenvalues.[0]
let fciGroundTotal = fciGroundElectronic + nuclearRepulsion
printfn "  Ground state (electronic):        %+.10f Ha" fciGroundElectronic
printfn "  Ground state (+ V_nn = %.10f): %+.10f Ha" nuclearRepulsion fciGroundTotal
printfn ""

// ─── Encoded Hamiltonians ───
let encodings : (string * EncoderFn) list =
    [ "Jordan-Wigner",    jordanWignerTerms
      "Bravyi-Kitaev",    bravyiKitaevTerms
      "Parity",           parityTerms
      "Balanced Binary",  balancedBinaryTreeTerms
      "Balanced Ternary", ternaryTreeTerms ]

let mutable allMatch = true

for (name, enc) in encodings do
    printfn "─── %s ───" name
    let ham = molecularHamiltonian enc
    let mat = hamiltonianToMatrix ham nSpin
    let evs = eigenvalues mat

    printfn "  Eigenvalues (electronic):"
    for (i, ev) in evs |> Array.indexed do
        printfn "    λ_%02d = %+.10f Ha" i ev

    // Compare with FCI
    let maxDiff =
        Array.map2 (fun a b -> abs (a - b)) evs fciEigenvalues
        |> Array.max

    if maxDiff < 1e-8 then
        printfn "  ✅ MATCH with FCI (max |Δλ| = %.2e)" maxDiff
    else
        printfn "  ❌ MISMATCH with FCI (max |Δλ| = %.2e)" maxDiff
        allMatch <- false

    printfn "  Ground state: %.10f Ha (electronic), %.10f Ha (total)"
            evs.[0] (evs.[0] + nuclearRepulsion)
    printfn ""

// ─── Summary ───
printfn "═══════════════════════════════════════════════════════"
if allMatch then
    printfn "  ✅ ALL 5 ENCODINGS MATCH THE REFERENCE FCI SPECTRUM"
else
    printfn "  ❌ SOME ENCODINGS DO NOT MATCH — SEE ABOVE"
printfn ""
printfn "  Reference ground state energy:"
printfn "    Electronic:  %+.10f Ha" fciGroundElectronic
printfn "    Total (+ Vnn): %+.10f Ha" fciGroundTotal
printfn ""

// ─── Particle-number sectors ───
printfn "─── Eigenvalues by particle-number sector ───"
let particleNumber s =
    let mutable count = 0
    for j in 0 .. nSpin-1 do
        if (s >>> j) &&& 1 = 1 then count <- count + 1
    count

// Group FCI eigenvalues by particle number sector
// For this we need the full eigenvector analysis.
// Instead, just report the total eigenvalue count per sector from occupation basis.
printfn ""
printfn "  Sector sizes: C(%d, k) for k = 0..%d" nSpin nSpin
for k in 0 .. nSpin do
    let sectorSize =
        [| 0 .. (1 <<< nSpin) - 1 |]
        |> Array.filter (fun s -> particleNumber s = k)
        |> Array.length
    printfn "    N_e = %d:  %d states" k sectorSize
printfn ""
printfn "  N_e = 2 sector: C(4,2) = 6 states (the physical sector for H₂)"
printfn ""

// ─── Hartree-Fock comparison ───
// HF determinant for H₂: |1100⟩ = occupy spin-orbitals 0α and 0β (bonding orbital)
// In our bit convention: bit 0 and bit 1 set → state index 3 (binary 0b0011)
// E_HF = ⟨1100| H |1100⟩ = diagonal element of FCI matrix
let hfState = 3  // bits 0 and 1 occupied
let hfEnergy = fciMatrix.[hfState, hfState].Real

// The FCI ground state over ALL sectors is not meaningful for chemistry.
// We need the ground state in the N_e=2 sector specifically.
// Extract the 6×6 block for the N_e=2 sector.
let ne2States =
    [| 0 .. (1 <<< nSpin) - 1 |]
    |> Array.filter (fun s -> particleNumber s = 2)

printfn "─── N_e = 2 sector (physical sector for H₂) ───"
printfn "  States in sector: %A" (ne2States |> Array.map (fun s -> sprintf "|%s⟩" (System.Convert.ToString(s, 2).PadLeft(nSpin, '0') |> Seq.rev |> System.String.Concat)))

let ne2Matrix =
    let m = ne2States.Length
    let d = Array.create (m*m) Complex.Zero
    for i in 0 .. m-1 do
        for j in 0 .. m-1 do
            d.[i*m + j] <- fciMatrix.[ne2States.[i], ne2States.[j]]
    { Rows = m; Cols = m; Data = d }

let ne2Eigenvalues = eigenvalues ne2Matrix
printfn "  Eigenvalues (electronic, N_e=2):"
for (i, ev) in ne2Eigenvalues |> Array.indexed do
    printfn "    λ_%d = %+.10f Ha" i ev

let fciNe2Ground = ne2Eigenvalues.[0]
let correlationEnergy = fciNe2Ground - hfEnergy

printfn ""
printfn "─── Hartree-Fock vs. Full CI (N_e=2 sector) ───"
printfn "  E_HF  (electronic)        = %+.10f Ha" hfEnergy
printfn "  E_FCI (electronic, N_e=2) = %+.10f Ha" fciNe2Ground
printfn "  Correlation energy         = %+.10f Ha" correlationEnergy
printfn "  Correlation energy         = %.4f kcal/mol" (correlationEnergy * 627.509)
printfn ""
printfn "  E_HF  (total)             = %+.10f Ha" (hfEnergy + nuclearRepulsion)
printfn "  E_FCI (total, N_e=2)      = %+.10f Ha" (fciNe2Ground + nuclearRepulsion)
printfn ""

printfn "Done."
