namespace Tests

/// <summary>
/// Coefficient-level acceptance tests for Hamiltonian assembly. Signature-only tests
/// pass even when the integral/factory convention is wrong (all encodings agree on the
/// signatures but could share a coefficient error), so these tests pin exact Pauli
/// coefficients, cross-check against a first-principles dense fermionic matrix built by
/// an independent raw second-quantized oracle, and lock the 0.9.0 factory contracts:
///
///   • RAW primary contract (0.9.0+): <c>computeHamiltonian</c> /
///     <c>computeHamiltonianWith</c> and the FCIDUMP adapters consume a raw single-bar
///     physicist tensor ⟨pq|rs⟩ for key "p,q,r,s". The library applies the two-body ½
///     and the r↔s annihilator order internally (½·⟨pq|rs⟩·a†_p a†_q a_s a_r).
///   • LEGACY weighted contract: <c>computeHamiltonianFromWeightedWith</c> applies the
///     value verbatim to a†_i a†_j a_k a_l (FULL WEIGHTED prefactor, two-body ½
///     pre-folded). <c>weightedToRawFactory</c> bridges weighted data to the raw API.
/// </summary>
module HamiltonianCoefficients =
    open System.Numerics
    open Encodings
    open Encodings.Hamiltonian
    open Encodings.JordanWigner
    open Encodings.BravyiKitaev
    open Encodings.MajoranaEncoding
    open Encodings.TreeEncoding
    open Xunit

    // Exact H2/STO-3G integrals (2 spatial orbitals -> 4 spin-orbitals),
    // interleaved spin-orbitals 0α,0β,1α,1β. Canonical source: encodings-research,
    // R = 1.3983973 bohr, chemist ERIs. Frozen fixture — see Phys below for the
    // equivalent raw physicist tensor used by the independent oracle.
    let private h2Fcidump = """
 &FCI NORB=   2,NELEC= 2,MS2=0,
  ORBSYM=1,1,
  ISYM=1,
 &END
 0.6747559268144484    1    1    1    1
 0.6637114013508132    1    1    2    2
 0.1812104620151968    2    1    2    1
 0.6637114013508132    2    2    1    1
 0.697651504490461     2    2    2    2
 -1.253309786645977    1    1  0  0
 -0.4750688487721783   2    2  0  0
 0.7151043390810812  0  0  0  0
"""

    let private h2Factory () =
        let (factory, _core, nso) = Fcidump.parseToSpinOrbitalFactory h2Fcidump
        factory, nso

    let private nonZero (h : PauliRegisterSequence) =
        h.DistributeCoefficient.SummandTerms
        |> Array.filter (fun t -> Complex.Abs t.Coefficient > 1e-10)

    let private coeffOf (h : PauliRegisterSequence) (sign : string) =
        match h.DistributeCoefficient.[sign] with
        | true, reg -> reg.Coefficient.Real
        | false, _ -> 0.0

    let private oneNorm (h : PauliRegisterSequence) =
        h.DistributeCoefficient.SummandTerms |> Array.sumBy (fun t -> Complex.Abs t.Coefficient)

    // ══════════════════════════════════════════════════════════════════
    //  Legacy weighted contract: factory value applied VERBATIM.
    // ══════════════════════════════════════════════════════════════════

    [<Fact>]
    let ``one-body coefficient is applied verbatim (h -> h/2 I - h/2 Z from JW)`` () =
        // n_0 = a†_0 a_0 = (I - Z_0)/2, so factory("0,0")=h gives IIII=h/2, ZIII=-h/2.
        // (The ½ here is the JW encoding of a†a, NOT a library-applied Hamiltonian ½.)
        // One-body terms are convention-invariant, so the raw primary is used directly.
        let factory (key : string) = if key = "0,0" then Some (Complex(1.7, 0.0)) else None
        let ham = computeHamiltonianWith jordanWignerTerms factory 2u
        Assert.Equal( 0.85, coeffOf ham "II", 10)
        Assert.Equal(-0.85, coeffOf ham "ZI", 10)

    [<Fact>]
    let ``two-body: the weighted factory value is applied verbatim to a†_i a†_j a_k a_l`` () =
        // Legacy weighted API. factory("0,1,0,1")=V is the FULL WEIGHTED coefficient of
        // a†_0 a†_1 a_0 a_1 (verbatim; the caller has already folded any ½). JW gives
        //   V·a†_0 a†_1 a_0 a_1 = V·(−II + IZ + ZI − ZZ)/4.
        let mk v = computeHamiltonianFromWeightedWith jordanWignerTerms
                    (fun key -> if key = "0,1,0,1" then Some (Complex(v, 0.0)) else None) 2u
        let h1 = mk 1.0
        let h2 = mk 2.0
        // linearity: h2 == 2 * h1 term-by-term
        for t in nonZero h2 do
            Assert.Equal(2.0 * coeffOf h1 t.Signature, t.Coefficient.Real, 10)
        Assert.Equal(-0.25, coeffOf h1 "II", 10)
        Assert.Equal( 0.25, coeffOf h1 "IZ", 10)
        Assert.Equal( 0.25, coeffOf h1 "ZI", 10)
        Assert.Equal(-0.25, coeffOf h1 "ZZ", 10)

    [<Fact>]
    let ``two-body: the weighted a_k a_l annihilator order is applied verbatim`` () =
        // factory("0,1,1,0")=1 → a†_0 a†_1 a_1 a_0 = +¼(II − IZ − ZI + ZZ), the
        // negation of the a_0 a_1 order above: proves the legacy weighted API keeps the
        // caller's k,l order (it does NOT swap to a_l a_k — the raw API does).
        let h = computeHamiltonianFromWeightedWith jordanWignerTerms
                  (fun key -> if key = "0,1,1,0" then Some (Complex(1.0, 0.0)) else None) 2u
        Assert.Equal( 0.25, coeffOf h "II", 10)
        Assert.Equal(-0.25, coeffOf h "IZ", 10)
        Assert.Equal(-0.25, coeffOf h "ZI", 10)
        Assert.Equal( 0.25, coeffOf h "ZZ", 10)

    [<Fact>]
    let ``two-body: the raw factory applies the internal half and r-s swap`` () =
        // RAW primary. raw("0,1,0,1")=V (⟨01|01⟩) → the library builds
        //   ½·V·a†_0 a†_1 a_1 a_0  (½ folded in; annihilators swapped to a_s a_r).
        // Since a†_0 a†_1 a_1 a_0 = ¼(II − IZ − ZI + ZZ), the result is
        //   (V/8)(II − IZ − ZI + ZZ). Equivalently: raw ⟨01|01⟩ ≡ weighted key (0,1,1,0)=½V.
        let h = computeHamiltonianWith jordanWignerTerms
                  (fun key -> if key = "0,1,0,1" then Some (Complex(1.0, 0.0)) else None) 2u
        Assert.Equal( 0.125, coeffOf h "II", 10)
        Assert.Equal(-0.125, coeffOf h "IZ", 10)
        Assert.Equal(-0.125, coeffOf h "ZI", 10)
        Assert.Equal( 0.125, coeffOf h "ZZ", 10)
        // Cross-check: same map as the legacy weighted key (0,1,1,0) with value ½.
        let w = computeHamiltonianFromWeightedWith jordanWignerTerms
                  (fun key -> if key = "0,1,1,0" then Some (Complex(0.5, 0.0)) else None) 2u
        Assert.Equal(w.DistributeCoefficient.ToString(), h.DistributeCoefficient.ToString())

    [<Fact>]
    let ``H2 JW Hamiltonian has exact IIII and four-body coefficients (FCIDUMP)`` () =
        let factory, nso = h2Factory ()
        let ham = computeHamiltonianWith jordanWignerTerms factory (uint32 nso)
        // Exactly 15 stored terms — cancellation residues are dropped at assembly.
        Assert.Equal(15, ham.DistributeCoefficient.SummandTerms.Length)
        Assert.Equal(15, (nonZero ham).Length)
        // Independently reproduced by the direct raw oracle below.
        Assert.Equal(-0.8121706072, coeffOf ham "IIII", 8)
        Assert.Equal(-0.2234315369, coeffOf ham "IIIZ", 8)
        Assert.Equal( 0.1744128761, coeffOf ham "IIZZ", 8)
        // The four-body exchange coefficient — 0.0453026155, NOT 0.0906 (which a
        // dropped ½ / raw-integral factory fed to the weighted API would produce).
        Assert.Equal(-0.0453026155, coeffOf ham "XXYY", 8)
        Assert.Equal( 0.0453026155, coeffOf ham "XYYX", 8)
        Assert.Equal( 0.0453026155, coeffOf ham "YXXY", 8)
        Assert.Equal(-0.0453026155, coeffOf ham "YYXX", 8)

    // ══════════════════════════════════════════════════════════════════
    //  Independent first-principles dense fermionic oracle (16×16).
    // ══════════════════════════════════════════════════════════════════
    module private Fermion =
        let dim = 16
        let private parity (state : int) (p : int) =
            let mutable s = 1
            for q in 0 .. p - 1 do if (state >>> q) &&& 1 = 1 then s <- -s
            float s
        let annihilate p =
            Array2D.init dim dim (fun r c ->
                if (c >>> p) &&& 1 = 1 && r = (c ^^^ (1 <<< p)) then parity c p else 0.0)
        let create p =
            Array2D.init dim dim (fun r c ->
                if (r >>> p) &&& 1 = 1 && c = (r ^^^ (1 <<< p)) then parity r p else 0.0)
        let matmul (a : float[,]) (b : float[,]) =
            Array2D.init dim dim (fun i j ->
                let mutable s = 0.0
                for k in 0 .. dim - 1 do s <- s + a.[i, k] * b.[k, j]
                s)
        let addScaled (acc : float[,]) (s : float) (m : float[,]) =
            for i in 0 .. dim - 1 do
                for j in 0 .. dim - 1 do acc.[i, j] <- acc.[i, j] + s * m.[i, j]
        // Real-symmetric Jacobi eigenvalues.
        let eigenvalues (a0 : float[,]) =
            let a = Array2D.copy a0
            let mutable off = 1.0
            let mutable pass = 0
            while off > 1e-12 && pass < 400 do
                pass <- pass + 1
                for p in 0 .. dim - 2 do
                    for q in p + 1 .. dim - 1 do
                        if abs a.[p, q] > 1e-300 then
                            let phi = (a.[q, q] - a.[p, p]) / (2.0 * a.[p, q])
                            let t = if phi = 0.0 then 1.0 else float (sign phi) / (abs phi + sqrt (phi * phi + 1.0))
                            let c = 1.0 / sqrt (t * t + 1.0)
                            let s = t * c
                            for k in 0 .. dim - 1 do
                                let akp, akq = a.[k, p], a.[k, q]
                                a.[k, p] <- c * akp - s * akq
                                a.[k, q] <- s * akp + c * akq
                            for k in 0 .. dim - 1 do
                                let apk, aqk = a.[p, k], a.[q, k]
                                a.[p, k] <- c * apk - s * aqk
                                a.[q, k] <- s * apk + c * aqk
                off <- 0.0
                for p in 0 .. dim - 2 do
                    for q in p + 1 .. dim - 1 do off <- off + a.[p, q] * a.[p, q]
            [ for i in 0 .. dim - 1 -> a.[i, i] ] |> List.sort

        // Real-symmetric Jacobi for an arbitrary dimension.
        let private jacobiN (n : int) (a0 : float[,]) =
            let a = Array2D.copy a0
            let mutable off = 1.0
            let mutable pass = 0
            while off > 1e-11 && pass < 2000 do
                pass <- pass + 1
                for p in 0 .. n - 2 do
                    for q in p + 1 .. n - 1 do
                        if abs a.[p, q] > 1e-300 then
                            let phi = (a.[q, q] - a.[p, p]) / (2.0 * a.[p, q])
                            let t = if phi = 0.0 then 1.0 else float (sign phi) / (abs phi + sqrt (phi * phi + 1.0))
                            let c = 1.0 / sqrt (t * t + 1.0)
                            let s = t * c
                            for k in 0 .. n - 1 do
                                let akp, akq = a.[k, p], a.[k, q]
                                a.[k, p] <- c * akp - s * akq
                                a.[k, q] <- s * akp + c * akq
                            for k in 0 .. n - 1 do
                                let apk, aqk = a.[p, k], a.[q, k]
                                a.[p, k] <- c * apk - s * aqk
                                a.[q, k] <- s * apk + c * aqk
                off <- 0.0
                for p in 0 .. n - 2 do
                    for q in p + 1 .. n - 1 do off <- off + a.[p, q] * a.[p, q]
            [ for i in 0 .. n - 1 -> a.[i, i] ] |> List.sort

        /// Eigenvalues of a complex-Hermitian matrix via the 2n×2n real embedding
        /// [[Re,−Im],[Im,Re]] (each eigenvalue appears twice). Required for tree
        /// encodings, whose Y–Y couplings give genuinely imaginary off-diagonals —
        /// taking `.Real` of the dense matrix would silently corrupt the spectrum.
        let hermEigenvalues (h : Complex[,]) =
            let n = Array2D.length1 h
            let big = Array2D.zeroCreate (2 * n) (2 * n)
            for i in 0 .. n - 1 do
                for j in 0 .. n - 1 do
                    big.[i, j] <- h.[i, j].Real
                    big.[i + n, j + n] <- h.[i, j].Real
                    big.[i, j + n] <- -h.[i, j].Imaginary
                    big.[i + n, j] <- h.[i, j].Imaginary
            jacobiN (2 * n) big
            |> List.mapi (fun i v -> (i, v))
            |> List.filter (fun (i, _) -> i % 2 = 0)
            |> List.map snd

        /// Direct dense H with a configurable two-body prefactor (`half`) and
        /// annihilator order (`swap` = a_l a_k when true).
        let matrixOfWith (half : float) (swap : bool) (factory : string -> Complex option) n =
            let h = Array2D.zeroCreate dim dim
            for i in 0 .. n - 1 do
                for j in 0 .. n - 1 do
                    match factory (sprintf "%d,%d" i j) with
                    | Some c -> addScaled h c.Real (matmul (create i) (annihilate j))
                    | None -> ()
            for i in 0 .. n - 1 do
                for j in 0 .. n - 1 do
                    for k in 0 .. n - 1 do
                        for l in 0 .. n - 1 do
                            match factory (sprintf "%d,%d,%d,%d" i j k l) with
                            | Some c ->
                                let ann =
                                    if swap then matmul (annihilate l) (annihilate k)
                                    else matmul (annihilate k) (annihilate l)
                                addScaled h (half * c.Real)
                                    (matmul (matmul (create i) (create j)) ann)
                            | None -> ()
            h

        /// RAW second-quantized oracle: consumes a raw physicist tensor ⟨ij|kl⟩ and
        /// assembles ½·Σ ⟨ij|kl⟩ a†_i a†_j a_l a_k directly, bypassing the library
        /// factory / FCIDUMP adaptation.
        let matrixOfRaw (rawFactory : string -> Complex option) n = matrixOfWith 0.5 true rawFactory n

        /// WEIGHTED oracle: consumes the library's weighted factory verbatim,
        /// assembling Σ f(i,j,k,l) a†_i a†_j a_k a_l (no ½, no swap).
        let matrixOfWeighted (weightedFactory : string -> Complex option) n = matrixOfWith 1.0 false weightedFactory n

    // Encoded Pauli sum -> dense matrix.
    module private Enc =
        let private cI = [| [| Complex.One; Complex.Zero |]; [| Complex.Zero; Complex.One |] |]
        let private cX = [| [| Complex.Zero; Complex.One |]; [| Complex.One; Complex.Zero |] |]
        let private cY = [| [| Complex.Zero; Complex(0.0, -1.0) |]; [| Complex(0.0, 1.0); Complex.Zero |] |]
        let private cZ = [| [| Complex.One; Complex.Zero |]; [| Complex.Zero; Complex(-1.0, 0.0) |] |]
        let private pm c = match c with 'I' -> cI | 'X' -> cX | 'Y' -> cY | 'Z' -> cZ | _ -> failwith "?"
        let private kron (a: Complex[][]) (b: Complex[][]) =
            let rb, cb = b.Length, b.[0].Length
            Array.init (a.Length * rb) (fun i -> Array.init (a.[0].Length * cb) (fun j -> a.[i / rb].[j / cb] * b.[i % rb].[j % cb]))
        let matrixOf (h : PauliRegisterSequence) =
            let terms = h.DistributeCoefficient.SummandTerms
            let n = terms.[0].Signature.Length
            let dim = 1 <<< n
            let acc = Array2D.zeroCreate dim dim
            for t in terms do
                let m = t.Signature |> Seq.map pm |> Seq.reduce kron
                for i in 0 .. dim - 1 do
                    for j in 0 .. dim - 1 do acc.[i, j] <- acc.[i, j] + (t.Coefficient * m.[i].[j]).Real
            acc

        /// Full complex-Hermitian dense matrix (no `.Real` truncation). Needed for
        /// encodings that produce imaginary off-diagonal entries (e.g. tree Y–Y terms).
        let matrixOfC (h : PauliRegisterSequence) =
            let terms = h.DistributeCoefficient.SummandTerms
            let n = terms.[0].Signature.Length
            let dim = 1 <<< n
            let acc = Array2D.zeroCreate<Complex> dim dim
            for t in terms do
                let m = t.Signature |> Seq.map pm |> Seq.reduce kron
                for i in 0 .. dim - 1 do
                    for j in 0 .. dim - 1 do acc.[i, j] <- acc.[i, j] + t.Coefficient * m.[i].[j]
            acc

        /// Encoded matrix in the OCCUPATION basis (mode j → bit 2ʲ): the FockMap
        /// string is qubit-0-leftmost, so the signature is reversed before the
        /// Kronecker product. Diagonal entry k then reads occupation integer k
        /// (e.g. k = 3 = 0b0011 is the H₂ Hartree–Fock state, modes 0,1 occupied),
        /// matching the direct fermionic oracle's basis.
        let matrixOfCOcc (h : PauliRegisterSequence) =
            let terms = h.DistributeCoefficient.SummandTerms
            let n = terms.[0].Signature.Length
            let dim = 1 <<< n
            let acc = Array2D.zeroCreate<Complex> dim dim
            for t in terms do
                let m = t.Signature |> Seq.rev |> Seq.map pm |> Seq.reduce kron
                for i in 0 .. dim - 1 do
                    for j in 0 .. dim - 1 do acc.[i, j] <- acc.[i, j] + t.Coefficient * m.[i].[j]
            acc

    // ══════════════════════════════════════════════════════════════════
    //  Canonical RAW physicist tensor (independent of the library factory).
    //  4 one-body + 32 raw two-body nonzero entries (interleaved 0α,0β,1α,1β).
    // ══════════════════════════════════════════════════════════════════
    module private Phys =
        // Canonical spatial chemist ERIs [pq|rs] (0-indexed, 8-fold symmetry).
        let gChem p q r s =
            let a = (min p q, max p q)
            let b = (min r s, max r s)
            let (x, y) = if a <= b then (a, b) else (b, a)
            match x, y with
            | (0,0),(0,0) -> 0.6747559268144484
            | (1,1),(1,1) -> 0.697651504490461
            | (0,0),(1,1) -> 0.6637114013508132
            | (0,1),(0,1) -> 0.1812104620151968   // K01 exchange
            | _ -> 0.0
        let hSpatial p q = if p = q then (if p = 0 then -1.253309786645977 else -0.4750688487721783) else 0.0
        let spinOf i = i % 2
        let spatialOf i = i / 2
        // Physicist spin-orbital tensor ⟨ab|cd⟩ = [ac|bd]_chem, δ_spin(a,c) δ_spin(b,d).
        let tPhys a b c d =
            if spinOf a = spinOf c && spinOf b = spinOf d then gChem (spatialOf a) (spatialOf c) (spatialOf b) (spatialOf d) else 0.0
        let private oneBody (key : string) =
            let p, q = let x = key.Split(',') in int x.[0], int x.[1]
            if spinOf p = spinOf q then
                let v = hSpatial (spatialOf p) (spatialOf q)
                if v <> 0.0 then Some (Complex(v, 0.0)) else None
            else None
        /// RAW physicist factory F("p,q,r,s") = ⟨pq|rs⟩. Fed DIRECTLY to the raw primary
        /// API (or the raw oracle); a caller error if fed to the legacy weighted API.
        let raw (key : string) =
            let x = key.Split(',')
            match x.Length with
            | 2 -> oneBody key
            | 4 ->
                let p, q, r, s = int x.[0], int x.[1], int x.[2], int x.[3]
                let v = tPhys p q r s
                if v <> 0.0 then Some (Complex(v, 0.0)) else None
            | _ -> None
        /// PRE-ADAPTED weighted factory F("p,q,r,s") = ½·⟨pq|sr⟩ = ½·T[p,q,s,r].
        /// This is the CORRECT legacy weighted input (fed to
        /// computeHamiltonianFromWeighted*); a migration hazard if fed to the raw API.
        let preAdapted (key : string) =
            let x = key.Split(',')
            match x.Length with
            | 2 -> oneBody key
            | 4 ->
                let p, q, r, s = int x.[0], int x.[1], int x.[2], int x.[3]
                let v = 0.5 * tPhys p q s r
                if v <> 0.0 then Some (Complex(v, 0.0)) else None
            | _ -> None

    // ── Named raw adapter and legacy weighted factory agree ──────────────

    [<Fact>]
    let ``raw factory has 4 one-body and 32 raw two-body nonzero entries`` () =
        let nso = 4
        let mutable one = 0
        let mutable two = 0
        for i in 0 .. nso - 1 do
            for j in 0 .. nso - 1 do
                if (Phys.raw (sprintf "%d,%d" i j)).IsSome then one <- one + 1
                for k in 0 .. nso - 1 do
                    for l in 0 .. nso - 1 do
                        if (Phys.raw (sprintf "%d,%d,%d,%d" i j k l)).IsSome then two <- two + 1
        Assert.Equal(4, one)
        Assert.Equal(32, two)

    [<Fact>]
    let ``raw primary produces all 15 canonical H2 coefficient entries`` () =
        // computeHamiltonian consumes the raw physicist tensor DIRECTLY → the full
        // 15-term canonical H₂, not a truncated 7.
        let raw = computeHamiltonian Phys.raw 4u
        Assert.Equal(15, raw.DistributeCoefficient.SummandTerms.Length)
        Assert.Equal(-0.8121706072, coeffOf raw "IIII", 8)
        Assert.Equal(-0.2234315369, coeffOf raw "IIIZ", 8)
        Assert.Equal( 0.1744128761, coeffOf raw "IIZZ", 8)
        Assert.Equal(-0.0453026155, coeffOf raw "XXYY", 8)
        Assert.Equal(2.6992778241, oneNorm raw, 8)

    [<Fact>]
    let ``legacy weighted factory and raw primary produce the same map`` () =
        // Pre-adapted weighted data through the legacy API == raw data through the raw
        // primary == FCIDUMP (raw) through the raw primary. All three routes agree.
        let factory, nso = h2Factory ()
        let viaFcidump = computeHamiltonianWith jordanWignerTerms factory (uint32 nso)
        let viaLegacy  = computeHamiltonianFromWeightedWith jordanWignerTerms Phys.preAdapted 4u
        let viaRaw     = computeHamiltonian Phys.raw 4u
        let s (h : PauliRegisterSequence) = h.DistributeCoefficient.ToString()
        Assert.Equal(s viaFcidump, s viaLegacy)
        Assert.Equal(s viaFcidump, s viaRaw)

    // ── Direct raw oracle acceptance (bypasses the library entirely) ─────

    [<Fact>]
    let ``JW dense matrix matches the direct raw fermionic oracle entrywise`` () =
        // The raw oracle assembles ½·Σ ⟨ij|kl⟩ a†_i a†_j a_l a_k directly from the raw
        // physicist tensor; the library builds the same H from the FCIDUMP raw factory
        // through the raw primary. In the occupation basis they must agree entry-for-entry.
        let factory, nso = h2Factory ()
        let lib = Enc.matrixOfCOcc (computeHamiltonianWith jordanWignerTerms factory (uint32 nso))
        let oracle = Fermion.matrixOfRaw Phys.raw nso
        for i in 0 .. Fermion.dim - 1 do
            for j in 0 .. Fermion.dim - 1 do
                Assert.Equal(oracle.[i, j], lib.[i, j].Real, 8)
                Assert.Equal(0.0, lib.[i, j].Imaginary, 8)

    [<Fact>]
    let ``raw oracle: HF diagonal, particle sectors, ground state, IIII trace`` () =
        let factory, nso = h2Factory ()
        let oracle = Fermion.matrixOfRaw Phys.raw nso
        // HF state = integer 3 (0b0011, modes 0 and 1 occupied): electronic HF energy.
        Assert.Equal(-1.8318636465, oracle.[3, 3], 8)
        // Particle-number sectors: no coupling between different occupation numbers.
        let popcount (x : int) = System.Numerics.BitOperations.PopCount(uint32 x)
        for i in 0 .. Fermion.dim - 1 do
            for j in 0 .. Fermion.dim - 1 do
                if popcount i <> popcount j then Assert.Equal(0.0, oracle.[i, j], 10)
        // Ground state of the full 16×16 spectrum.
        let spec = Fermion.eigenvalues oracle
        Assert.Equal(-1.8523881736, List.head spec, 8)
        // Trace/16 = the identity coefficient IIII (basis-invariant).
        let trace = [ for i in 0 .. Fermion.dim - 1 -> oracle.[i, i] ] |> List.sum
        Assert.Equal(-0.8121706072, trace / float Fermion.dim, 8)

    [<Fact>]
    let ``encoded H2 spectrum matches the direct fermionic matrix (ground -1.8523881736)`` () =
        let factory, nso = h2Factory ()
        let encoded = Enc.matrixOf (computeHamiltonianWith jordanWignerTerms factory (uint32 nso))
        let fermionic = Fermion.matrixOfRaw Phys.raw nso
        let sa = Fermion.eigenvalues encoded
        let sb = Fermion.eigenvalues fermionic
        Assert.Equal(sa.Length, sb.Length)
        List.iter2 (fun (a: float) b -> Assert.Equal(a, b, 8)) sa sb
        Assert.Equal(-1.8523881736, List.head sb, 8)

    [<Fact>]
    let ``FCIDUMP (raw) independently matches the coefficient and raw dense oracles`` () =
        // The FCIDUMP raw factory, consumed by the raw primary, matches (a) the encoded
        // coefficient oracle and (b) a raw dense oracle that consumes the same factory
        // with ½ + a_l a_k — locking the FCIDUMP raw ⟨pq|rs⟩ = (pr|qs) adapter.
        let factory, nso = h2Factory ()
        let lib = Enc.matrixOfCOcc (computeHamiltonianWith jordanWignerTerms factory (uint32 nso))
        let fcidumpOracle = Fermion.matrixOfRaw factory nso
        let rawOracle = Fermion.matrixOfRaw Phys.raw nso
        for i in 0 .. Fermion.dim - 1 do
            for j in 0 .. Fermion.dim - 1 do
                Assert.Equal(fcidumpOracle.[i, j], lib.[i, j].Real, 8)
                Assert.Equal(rawOracle.[i, j], fcidumpOracle.[i, j], 8)

    // ── Migration hazard / caller error ─────────────────────────────────

    [<Fact>]
    let ``pre-adapted weighted data fed to the raw primary double-adapts (migration hazard)`` () =
        // Already-weighted (½-folded) data pushed through the raw primary applies a
        // second ½ and re-swaps → wrong. Migrate such factories to the legacy weighted
        // API (or strip their ½/swap) rather than the raw primary.
        let hazard = computeHamiltonian Phys.preAdapted 4u
        Assert.True(abs (coeffOf hazard "IIII" - (-0.8121706072)) > 0.01,
            sprintf "double-adapted IIII=%f should differ materially from -0.8121706072"
                (coeffOf hazard "IIII"))

    [<Fact>]
    let ``raw data fed straight to the legacy weighted API is a caller error`` () =
        // Raw physicist integrals passed to the legacy weighted factory omit the ½ and
        // the index swap → wrong. Wrap them with weightedToRawFactory's inverse — i.e.
        // feed them to the raw primary instead.
        let wrong = computeHamiltonianFromWeightedWith jordanWignerTerms Phys.raw 4u
        Assert.True(abs (coeffOf wrong "IIII" - (-0.8121706072)) > 0.01,
            sprintf "unadapted-raw IIII=%f should differ materially from -0.8121706072"
                (coeffOf wrong "IIII"))

    // ── All five builders agree ──────────────────────────────────────────

    [<Fact>]
    let ``all five Hamiltonian builders agree on H2`` () =
        let factory, nso = h2Factory ()
        let n = uint32 nso
        let s (h : PauliRegisterSequence) = h.DistributeCoefficient.ToString()
        let seqH     = computeHamiltonianWith jordanWignerTerms factory n
        let parH     = computeHamiltonianWithParallel jordanWignerTerms factory n
        let cacheH   = computeHamiltonianCached jordanWignerTerms factory n
        let fullSk   = applyCoefficients (computeHamiltonianSkeleton jordanWignerTerms n) factory
        let sparseSk = applyCoefficients (computeHamiltonianSkeletonFor jordanWignerTerms factory n) factory
        Assert.Equal(s seqH, s parH)
        Assert.Equal(s seqH, s cacheH)
        Assert.Equal(s seqH, s fullSk)
        Assert.Equal(s seqH, s sparseSk)

    [<Fact>]
    let ``all five legacy weighted builders agree on H2 and match the raw path`` () =
        // Pre-adapted weighted H₂ data through every FromWeighted surface
        // (sequential/parallel/cached/full-skeleton/sparse-skeleton) must all reproduce
        // the identical map AND equal the raw primary on the same physics (item 12).
        let n = 4u
        let s (h : PauliRegisterSequence) = h.DistributeCoefficient.ToString()
        let seqW     = computeHamiltonianFromWeightedWith jordanWignerTerms Phys.preAdapted n
        let parW     = computeHamiltonianFromWeightedWithParallel jordanWignerTerms Phys.preAdapted n
        let cacheW   = computeHamiltonianFromWeightedCached jordanWignerTerms Phys.preAdapted n
        let fullSkW  = applyCoefficientsFromWeighted (computeHamiltonianSkeleton jordanWignerTerms n) Phys.preAdapted
        let sparseSkW = applyCoefficientsFromWeighted (computeHamiltonianSkeletonForFromWeighted jordanWignerTerms Phys.preAdapted n) Phys.preAdapted
        Assert.Equal(s seqW, s parW)
        Assert.Equal(s seqW, s cacheW)
        Assert.Equal(s seqW, s fullSkW)
        Assert.Equal(s seqW, s sparseSkW)
        // …and the whole legacy-weighted path equals the raw primary path.
        let viaRaw = computeHamiltonian Phys.raw n
        Assert.Equal(s viaRaw, s seqW)

    // ── Canonical H2 resource metrics: 15 / 32 / 15 / 36 ─────────────────

    [<Fact>]
    let ``canonical H2 metrics: 15 terms, weight 32, 15 rotations, 36 CNOTs`` () =
        let factory, nso = h2Factory ()
        let ham = computeHamiltonianWith jordanWignerTerms factory (uint32 nso)
        let costs = CostAnalysis.hamiltonianCosts ham
        Assert.Equal(15, costs.TermCount)
        Assert.Equal(32, costs.TotalPauliWeight)
        let step = Trotterization.firstOrderTrotter 1.0 ham
        Assert.Equal(15, step.Rotations.Length)
        Assert.Equal(36, Trotterization.trotterCnotCount step)

    // ══════════════════════════════════════════════════════════════════
    //  Cancellation-aware zero reduction + tiny-term survival.
    // ══════════════════════════════════════════════════════════════════

    [<Fact>]
    let ``H2 stores no numerical-zero terms (cancellation residues removed)`` () =
        // Fermionic cancellations previously left 8 float-noise zero terms
        // (XXXY, XXYX, …) inflating CostAnalysis to 23 terms / weight 64.
        let factory, nso = h2Factory ()
        let ham = computeHamiltonianWith jordanWignerTerms factory (uint32 nso)
        for t in ham.DistributeCoefficient.SummandTerms do
            Assert.True(t.Coefficient.Magnitude > 1e-12,
                sprintf "term %s has a numerical-zero coefficient %A" t.Signature t.Coefficient)

    [<Fact>]
    let ``exact cancellation of two contributions drops the term`` () =
        // n_0 = (I − Z_0)/2 and n_1 = (I − Z_1)/2 each contribute to II. With
        // factory("0,0")=+0.1 and factory("1,1")=−0.1 the two II contributions
        // (+0.05 and −0.05) cancel exactly → II is dropped (count = 2, exact zero),
        // while ZI = −0.05 and IZ = +0.05 survive.
        let factory (key : string) =
            if   key = "0,0" then Some (Complex(0.1, 0.0))
            elif key = "1,1" then Some (Complex(-0.1, 0.0))
            else None
        let ham = computeHamiltonianWith jordanWignerTerms factory 2u
        Assert.Equal(0.0, coeffOf ham "II", 15)
        Assert.Equal(-0.05, coeffOf ham "ZI", 12)
        Assert.Equal(0.05, coeffOf ham "IZ", 12)
        Assert.Equal(2, ham.DistributeCoefficient.SummandTerms.Length)

    [<Fact>]
    let ``floating cancellation residue is removed but nearby real term survives`` () =
        // n_0 = (I - Z_0)/2. factory("0,0")=h1 and factory("1,1")=h2 populate II with
        // h1/2 + h2/2. Choose h1 = 0.1, h2 = -0.1 + 3e-16: the II coefficient is a
        // ~1.5e-16 residue (≈ eps·scale, scale≈0.1) → dropped as cancellation, while the
        // ZI (h1/2 = 0.05) and IZ (h2/2 ≈ -0.05) terms survive.
        let factory (key : string) =
            if   key = "0,0" then Some (Complex(0.1, 0.0))
            elif key = "1,1" then Some (Complex(-0.1 + 3e-16, 0.0))
            else None
        let ham = computeHamiltonianWith jordanWignerTerms factory 2u
        Assert.Equal(0.0, coeffOf ham "II", 12)                 // II dropped
        Assert.Equal(-0.05, coeffOf ham "ZI", 12)               // survives
        Assert.Equal(0.05, coeffOf ham "IZ", 12)                // survives

    [<Fact>]
    let ``a legitimate small residue from two contributions is NOT dropped`` () =
        // Same shape as above but the II residue is 1e-9 (far above eps·scale), a real
        // physical value that must survive even though it arose from two contributions.
        let factory (key : string) =
            if   key = "0,0" then Some (Complex(0.1, 0.0))
            elif key = "1,1" then Some (Complex(-0.1 + 2e-9, 0.0))
            else None
        let ham = computeHamiltonianWith jordanWignerTerms factory 2u
        Assert.Equal(1e-9, coeffOf ham "II", 15)

    [<Fact>]
    let ``standalone tiny coefficients survive on every builder path`` () =
        // A single one-body coefficient far below any legacy absolute threshold must
        // survive verbatim through sequential, parallel, cached and both skeletons.
        for v in [1e-12; 1e-13; 1e-15] do
            let factory (key : string) = if key = "0,0" then Some (Complex(2.0 * v, 0.0)) else None
            let build (h : PauliRegisterSequence) =
                Assert.Equal(2, h.DistributeCoefficient.SummandTerms.Length)
                Assert.Equal(v, coeffOf h "II", 15)
                Assert.Equal(-v, coeffOf h "ZI", 15)
            build (computeHamiltonianWith jordanWignerTerms factory 2u)
            build (computeHamiltonianWithParallel jordanWignerTerms factory 2u)
            build (computeHamiltonianCached jordanWignerTerms factory 2u)
            build (applyCoefficients (computeHamiltonianSkeleton jordanWignerTerms 2u) factory)
            build (applyCoefficients (computeHamiltonianSkeletonFor jordanWignerTerms factory 2u) factory)

    [<Fact>]
    let ``standalone tiny coefficients survive through the JW aliases`` () =
        // computeHamiltonian / computeHamiltonianParallel (the Jordan-Wigner aliases)
        // must preserve a tiny standalone coefficient just like the ...With forms.
        for v in [1e-12; 1e-13; 1e-15] do
            let factory (key : string) = if key = "0,0" then Some (Complex(2.0 * v, 0.0)) else None
            for h in [ computeHamiltonian factory 2u; computeHamiltonianParallel factory 2u ] do
                Assert.Equal(2, h.DistributeCoefficient.SummandTerms.Length)
                Assert.Equal(v, coeffOf h "II", 15)
                Assert.Equal(-v, coeffOf h "ZI", 15)

    [<Fact>]
    let ``standalone tiny two-body coefficient survives verbatim (legacy weighted)`` () =
        // A single two-body weighted coefficient of 2e-13 → four JW terms at ±5e-14,
        // each a standalone contribution (count = 1) that must not be pruned.
        let factory (key : string) = if key = "0,1,0,1" then Some (Complex(2e-13, 0.0)) else None
        let ham = computeHamiltonianFromWeightedWith jordanWignerTerms factory 2u
        Assert.Equal(4, ham.DistributeCoefficient.SummandTerms.Length)
        let close a b = Assert.True(abs (a - b) < 1e-20, sprintf "%g vs %g" a b)
        close (-5e-14) (coeffOf ham "II")
        close ( 5e-14) (coeffOf ham "IZ")
        close ( 5e-14) (coeffOf ham "ZI")
        close (-5e-14) (coeffOf ham "ZZ")

    [<Fact>]
    let ``standalone tiny two-body coefficient survives verbatim (raw primary)`` () =
        // A single raw two-body integral of 2e-13 → ½·2e-13 = 1e-13 on a†_0 a†_1 a_1 a_0
        // → four JW terms at ±2.5e-14, each a standalone contribution that must survive.
        let factory (key : string) = if key = "0,1,0,1" then Some (Complex(2e-13, 0.0)) else None
        let ham = computeHamiltonianWith jordanWignerTerms factory 2u
        Assert.Equal(4, ham.DistributeCoefficient.SummandTerms.Length)
        let close a b = Assert.True(abs (a - b) < 1e-20, sprintf "%g vs %g" a b)
        close ( 2.5e-14) (coeffOf ham "II")
        close (-2.5e-14) (coeffOf ham "IZ")
        close (-2.5e-14) (coeffOf ham "ZI")
        close ( 2.5e-14) (coeffOf ham "ZZ")

    [<Fact>]
    let ``standalone tiny coefficient survives through the raw primary entry points`` () =
        // A raw one-body integral of 2e-13 passes through unchanged and must survive on
        // both raw entry points (…With and the JW alias).
        for v in [1e-12; 1e-13; 1e-15] do
            let raw (key : string) = if key = "0,0" then Some (Complex(2.0 * v, 0.0)) else None
            for h in [ computeHamiltonian raw 2u
                       computeHamiltonianWith jordanWignerTerms raw 2u ] do
                Assert.Equal(2, h.DistributeCoefficient.SummandTerms.Length)
                Assert.Equal(v, coeffOf h "II", 15)
                Assert.Equal(-v, coeffOf h "ZI", 15)

    // ══════════════════════════════════════════════════════════════════
    //  Six-encoding complex-Hermitian spectrum (retained; requirement 9).
    // ══════════════════════════════════════════════════════════════════

    [<Fact>]
    let ``H2 spectrum agrees across all six fermion-to-qubit encodings`` () =
        // Jordan-Wigner, Bravyi-Kitaev, Parity, and the three tree encodings
        // (balanced binary, ternary, Vlasov) must all reproduce the full
        // 16-eigenvalue H₂ spectrum (ground −1.852388 Ha). The tree encodings
        // emit Y–Y couplings, so their dense matrices are genuinely complex-
        // Hermitian; the spectrum must be taken with `hermEigenvalues`, never by
        // truncating to the real part.
        let factory, nso = h2Factory ()
        let n = uint32 nso
        let specOf enc = Fermion.hermEigenvalues (Enc.matrixOfC (computeHamiltonianWith enc factory n))
        let jw = specOf jordanWignerTerms
        Assert.Equal(16, jw.Length)
        Assert.Equal(-1.852388, List.head jw, 5)
        for (name, enc) in
            [ "BK", bravyiKitaevTerms
              "Parity", parityTerms
              "BinTree", balancedBinaryTreeTerms
              "TerTree", ternaryTreeTerms
              "Vlasov", vlasovTreeTerms ] do
            let s = specOf enc
            Assert.True(List.forall2 (fun (a : float) b -> abs (a - b) < 1e-8) jw s,
                sprintf "%s spectrum differs from Jordan-Wigner" name)

    [<Fact>]
    let ``H2 assembly is wrong if the caller's half or index order is corrupted`` () =
        // Proves the ½ and the annihilator order each change the physics independently
        // (compared via spectra). The library now consumes the raw factory and applies
        // the ½ + a_l a_k itself; the raw oracle applies the same ½ + a_l a_k. The
        // defect variants must disagree.
        let factory, nso = h2Factory ()
        let libSpec = Fermion.eigenvalues (Enc.matrixOf (computeHamiltonianWith jordanWignerTerms factory (uint32 nso)))
        let specOfOracle half swap = Fermion.eigenvalues (Fermion.matrixOfWith half swap Phys.raw nso)
        let correct = specOfOracle 0.5 true
        let noHalf  = specOfOracle 1.0 true    // missing ½
        let noSwap  = specOfOracle 0.5 false   // unswapped annihilators
        let eq a b = List.forall2 (fun (x : float) y -> abs (x - y) < 1e-8) a b
        Assert.True(eq libSpec correct, "library must match the ½+swap raw oracle spectrum")
        Assert.False(eq libSpec noHalf, "dropping the ½ must change the spectrum")
        Assert.False(eq libSpec noSwap, "omitting the r↔s swap must change the spectrum")
