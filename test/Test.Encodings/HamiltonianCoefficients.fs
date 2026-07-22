namespace Tests

/// <summary>
/// Coefficient-level tests for Hamiltonian assembly. Signature-only tests pass
/// even when the integral/factory convention is wrong (all encodings agree on the
/// signatures but share the coefficient error), so these tests pin exact Pauli
/// coefficients, cross-check against a first-principles dense fermionic matrix,
/// and lock the factory coefficient contract (the two-body ½ is folded in by the
/// caller; the library applies the factory value verbatim).
/// </summary>
module HamiltonianCoefficients =
    open System.Numerics
    open Encodings
    open Encodings.Hamiltonian
    open Encodings.JordanWigner
    open Encodings.BravyiKitaev
    open Encodings.MajoranaEncoding
    open Encodings.TreeEncoding
    open Encodings.Trotterization
    open Xunit

    // Exact H2/STO-3G integrals (2 spatial orbitals -> 4 spin-orbitals).
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

    // ── (a)/(d) Exact Pauli coefficients — the anti-regression that fails if the
    //    two-body ½ is dropped or double-applied (signatures would still match). ──
    [<Fact>]
    let ``H2 JW Hamiltonian has exact IIII and four-body coefficients (FCIDUMP)`` () =
        let factory, nso = h2Factory ()
        let ham = computeHamiltonianWith jordanWignerTerms factory (uint32 nso)
        // Exactly 15 terms — numerical-zero residues are dropped at assembly.
        Assert.Equal(15, ham.DistributeCoefficient.SummandTerms.Length)
        Assert.Equal(15, (nonZero ham).Length)
        // Independently reproduced by a direct second-quantized/JW oracle:
        Assert.Equal(-0.8121706072, coeffOf ham "IIII", 8)
        Assert.Equal(-0.2234315369, coeffOf ham "IIIZ", 8)
        Assert.Equal( 0.1744128761, coeffOf ham "IIZZ", 8)
        // The four-body exchange coefficient — 0.0453026155, NOT 0.0906 (which is
        // what a dropped ½ / raw-integral factory would produce).
        Assert.Equal(-0.0453026155, coeffOf ham "XXYY", 8)
        Assert.Equal( 0.0453026155, coeffOf ham "XYYX", 8)
        Assert.Equal( 0.0453026155, coeffOf ham "YXXY", 8)
        Assert.Equal(-0.0453026155, coeffOf ham "YYXX", 8)

    // ── Minimal fixture: the factory value is applied verbatim (no hidden factor). ──
    [<Fact>]
    let ``one-body coefficient is applied verbatim (h -> h/2 I - h/2 Z from JW)`` () =
        // n_0 = a†_0 a_0 = (I - Z_0)/2, so factory("0,0")=h gives IIII=h/2, ZIII=-h/2.
        // (The ½ here is the JW encoding of a†a, NOT a library-applied Hamiltonian ½.)
        let factory (key : string) = if key = "0,0" then Some (Complex(1.7, 0.0)) else None
        let ham = computeHamiltonianWith jordanWignerTerms factory 2u
        Assert.Equal( 0.85, coeffOf ham "II", 10)
        Assert.Equal(-0.85, coeffOf ham "ZI", 10)

    [<Fact>]
    let ``two-body: library applies the half and a_s a_r order for raw physicist input`` () =
        // factory("0,1,0,1")=V is the RAW ⟨01|01⟩. The library builds
        // ½·V·a†_0 a†_1 a_1 a_0 = ½·V·n_0 n_1 = (V/8)(II − ZI − IZ + ZZ).
        // (A spurious extra ½, or omitting it, changes these magnitudes.)
        let mk v = computeHamiltonianWith jordanWignerTerms
                    (fun key -> if key = "0,1,0,1" then Some (Complex(v, 0.0)) else None) 2u
        let h1 = mk 1.0
        let h2 = mk 2.0
        // linearity: h2 == 2 * h1 term-by-term
        for t in nonZero h2 do
            Assert.Equal(2.0 * coeffOf h1 t.Signature, t.Coefficient.Real, 10)
        Assert.Equal(0.125, coeffOf h1 "II", 10)
        Assert.Equal(0.125, coeffOf h1 "ZZ", 10)
        Assert.Equal(-0.125, coeffOf h1 "ZI", 10)
        Assert.Equal(-0.125, coeffOf h1 "IZ", 10)

    // ── (b) vs (c): the encoded Hamiltonian matches a first-principles dense
    //    fermionic construction (same factory), so the coefficients are physically
    //    correct, not merely self-consistent across encodings. ──
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
            let mutable it = 0
            while off > 1e-12 && it < 400 do
                it <- it + 1
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
            let mutable it = 0
            while off > 1e-11 && it < 2000 do
                it <- it + 1
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
        /// annihilator order (`swap` = a_l a_k when true). The library uses
        /// half = 0.5 and swap = true; the defect variants (no ½, unswapped) are
        /// used to prove each defect changes the result independently.
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

        /// Correct dense H = Σ h_ij a†_i a_j + ½ Σ ⟨ij|kl⟩ a†_i a†_j a_l a_k.
        let matrixOf (factory : string -> Complex option) n = matrixOfWith 0.5 true factory n

    // Encoded Pauli sum -> dense matrix (qubit 0 = leftmost; convention-agnostic for eigenvalues).
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

    [<Fact>]
    let ``encoded H2 spectrum matches the direct dense fermionic matrix`` () =
        let factory, nso = h2Factory ()
        let encoded = Enc.matrixOf (computeHamiltonianWith jordanWignerTerms factory (uint32 nso))
        let fermionic = Fermion.matrixOf factory nso
        let sa = Fermion.eigenvalues encoded
        let sb = Fermion.eigenvalues fermionic
        Assert.Equal(sa.Length, sb.Length)
        List.iter2 (fun (a: float) b -> Assert.Equal(a, b, 8)) sa sb
        // And the physical ground state is preserved.
        Assert.Equal(-1.852388, List.head sb, 5)

    // ══════════════════════════════════════════════════════════════════
    //  Raw physicist tensor input (new contract)
    //  ──────────────────────────────────────────────────────────────────
    //  FockMap's factory takes, for key "p,q,r,s", the RAW physicist integral
    //  ⟨pq|rs⟩ under the unrestricted sum; the library applies the ½ and the
    //  a_s a_r order. So a raw physicist tensor T[p,q,r,s]=⟨pq|rs⟩ (Phys.raw)
    //  is fed DIRECTLY — the book's original input now works. A factory that
    //  pre-folds ½ and swaps r↔s (Phys.preAdapted, the old convention) now
    //  double-counts and must be migrated. Canonical source: encodings-research,
    //  R=1.3983973 bohr, interleaved 0α,0β,1α,1β, chemist ERIs.
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
            | (0,1),(0,1) -> 0.18121046201519672   // K01 exchange
            | _ -> 0.0
        let hSpatial p q = if p = q then (if p = 0 then -1.2533097866 else -0.4750688488) else 0.0
        let spin i = i % 2
        let sp i = i / 2
        // Physicist spin-orbital tensor ⟨ab|cd⟩ = [ac|bd]_chem, δ_spin(a,c) δ_spin(b,d).
        let tPhys a b c d =
            if spin a = spin c && spin b = spin d then gChem (sp a) (sp c) (sp b) (sp d) else 0.0
        let private oneBody (key : string) =
            let p, q = let x = key.Split(',') in int x.[0], int x.[1]
            if spin p = spin q then
                let v = hSpatial (sp p) (sp q)
                if abs v > 1e-15 then Some (Complex(v, 0.0)) else None
            else None
        /// RAW physicist tensor F("p,q,r,s") = ⟨pq|rs⟩ = T[p,q,r,s].
        /// Under the new contract this is the CORRECT, directly-usable input.
        let raw (key : string) =
            let x = key.Split(',')
            match x.Length with
            | 2 -> oneBody key
            | 4 ->
                let p, q, r, s = int x.[0], int x.[1], int x.[2], int x.[3]
                let v = tPhys p q r s
                if abs v > 1e-15 then Some (Complex(v, 0.0)) else None
            | _ -> None
        /// Old PRE-ADAPTED factory F("p,q,r,s") = ½·⟨pq|sr⟩ = ½·T[p,q,s,r].
        /// Under the new contract this double-counts (migration hazard).
        let preAdapted (key : string) =
            let x = key.Split(',')
            match x.Length with
            | 2 -> oneBody key
            | 4 ->
                let p, q, r, s = int x.[0], int x.[1], int x.[2], int x.[3]
                let v = 0.5 * tPhys p q s r
                if abs v > 1e-15 then Some (Complex(v, 0.0)) else None
            | _ -> None

    let private oneNorm (h : PauliRegisterSequence) =
        h.DistributeCoefficient.SummandTerms |> Array.sumBy (fun t -> Complex.Abs t.Coefficient)

    [<Fact>]
    let ``H2 chemist Hamiltonian matches canonical 1-norm 2.699278`` () =
        let factory, nso = h2Factory ()
        let ham = computeHamiltonianWith jordanWignerTerms factory (uint32 nso)
        Assert.Equal(2.699278, oneNorm ham, 5)

    [<Fact>]
    let ``raw physicist tensor produces the correct H2 directly (new contract)`` () =
        let chemF, nso = h2Factory ()
        let chem = computeHamiltonianWith jordanWignerTerms chemF (uint32 nso)
        let raw  = computeHamiltonianWith jordanWignerTerms Phys.raw (uint32 nso)
        // The raw physicist tensor is fed directly and reproduces the FCIDUMP
        // Hamiltonian term-for-term — the book's input now works without adaptation.
        Assert.Equal(chem.DistributeCoefficient.ToString(), raw.DistributeCoefficient.ToString())
        Assert.Equal(-0.8121706072, coeffOf raw "IIII", 8)
        Assert.Equal(-0.0453026155, coeffOf raw "XXYY", 8)
        Assert.Equal(2.699278, oneNorm raw, 5)

    [<Fact>]
    let ``pre-adapted half-folded factory now double-counts (migration hazard)`` () =
        // A factory written for the OLD contract (½·⟨pq|sr⟩) now double-applies the
        // ½ and re-swaps, so it no longer yields the correct H2. Custom pre-adapted
        // factories must drop their ½/swap (or switch to the Fcidump adapters).
        let preAdapted = computeHamiltonianWith jordanWignerTerms Phys.preAdapted 4u
        Assert.True(abs (coeffOf preAdapted "IIII" - (-0.8121706072)) > 0.01,
            sprintf "pre-adapted IIII=%f should differ materially from the correct -0.8121706072"
                (coeffOf preAdapted "IIII"))

    [<Fact>]
    let ``H2 Hamiltonian carries no numerical-zero terms`` () =
        // Fermionic cancellations previously left 8 float-noise zero terms
        // (XXXY, XXYX, …) inflating CostAnalysis to 23 terms / weight 64.
        let factory, nso = h2Factory ()
        let ham = computeHamiltonianWith jordanWignerTerms factory (uint32 nso)
        for t in ham.DistributeCoefficient.SummandTerms do
            Assert.True(t.Coefficient.Magnitude > 1e-12,
                sprintf "term %s has a numerical-zero coefficient %A" t.Signature t.Coefficient)

    [<Fact>]
    let ``H2 CostAnalysis reports 15 terms and weight 32 (no zero inflation)`` () =
        // Retained exact/near-zero terms previously inflated the analysis to 23
        // terms / total weight 64. After boundary pruning the H₂ Hamiltonian has
        // exactly 15 nonzero Pauli terms; CostAnalysis must reflect that.
        let factory, nso = h2Factory ()
        let ham = computeHamiltonianWith jordanWignerTerms factory (uint32 nso)
        let costs = CostAnalysis.hamiltonianCosts ham
        Assert.Equal(15, costs.TermCount)
        Assert.Equal(32, costs.TotalPauliWeight)
        Assert.Equal(4, costs.MaxPauliWeight)
        Assert.Equal(-0.8121706072, costs.IdentityCoeff, 8)

    [<Fact>]
    let ``H2 downstream Trotter metrics are consistent with 15 terms (36 CNOTs, not 84)`` () =
        // The retained zero terms previously inflated the first-order Trotter step to
        // 23 rotations / 84 CNOTs. With the numerical-zero prune the H₂ step has 15
        // rotations and 36 CNOTs (Σ 2(w−1): six weight-2 terms → 12, four weight-4 → 24).
        let factory, nso = h2Factory ()
        let ham = computeHamiltonianWith jordanWignerTerms factory (uint32 nso)
        let stats = trotterStepStats (firstOrderTrotter 0.1 ham)
        Assert.Equal(15, stats.RotationCount)
        Assert.Equal(36, stats.CnotCount)
        Assert.Equal(4, stats.MaxWeight)

    [<Fact>]
    let ``H2 JW Hamiltonian matches the complete exact 15-signature coefficient map`` () =
        // The full canonical H₂/STO-3G Jordan-Wigner coefficient map. Locks every
        // signature and coefficient (not just IIII/four-body), so a wrong index
        // mapping or a dropped/duplicated term is caught exactly.
        let expected =
            [ "IIII", -0.8121706072
              "IIIZ", -0.2234315369; "IIZI", -0.2234315369; "IIZZ",  0.1744128761
              "IZII",  0.1714128264; "IZIZ",  0.1206252348; "IZZI",  0.1659278503
              "XXYY", -0.0453026155; "XYYX",  0.0453026155; "YXXY",  0.0453026155; "YYXX", -0.0453026155
              "ZIII",  0.1714128264; "ZIIZ",  0.1659278503; "ZIZI",  0.1206252348; "ZZII",  0.1686889817 ]
            |> Map.ofList
        let factory, nso = h2Factory ()
        let terms = (computeHamiltonianWith jordanWignerTerms factory (uint32 nso)).DistributeCoefficient.SummandTerms
        Assert.Equal(expected.Count, terms.Length)
        for t in terms do
            match Map.tryFind t.Signature expected with
            | Some c ->
                Assert.Equal(c, t.Coefficient.Real, 8)
                Assert.True(abs t.Coefficient.Imaginary < 1e-9,
                    sprintf "%s should be real, got %A" t.Signature t.Coefficient)
            | None -> Assert.True(false, sprintf "unexpected signature %s (%A)" t.Signature t.Coefficient)
        for kv in expected do
            Assert.True(terms |> Array.exists (fun t -> t.Signature = kv.Key),
                sprintf "missing signature %s" kv.Key)

    [<Fact>]
    let ``H2 encoded dense matrix equals an independent raw-integral oracle entry by entry`` () =
        // Non-circular dense check. The library builds H₂ through the FCIDUMP-derived
        // factory (h2Factory) and encodes with Jordan-Wigner; the oracle is a direct
        // dense fermionic matrix built from the hard-coded spatial integrals (Phys.raw
        // via gChem/hSpatial), NOT the FCIDUMP parser. Comparing EVERY entry in the
        // shared occupation basis (mode j → bit 2ʲ) locks the FCIDUMP index mapping and
        // the encoding basis — a spectrum-only check cannot (bit reversal is isospectral).
        let factory, nso = h2Factory ()
        let lib = Enc.matrixOfCOcc (computeHamiltonianWith jordanWignerTerms factory (uint32 nso))
        let oracle = Fermion.matrixOf Phys.raw nso
        let mutable maxErr = 0.0
        for i in 0 .. Fermion.dim - 1 do
            for j in 0 .. Fermion.dim - 1 do
                Assert.True(abs lib.[i, j].Imaginary < 1e-9,
                    sprintf "entry (%d,%d) should be real, got %A" i j lib.[i, j])
                maxErr <- max maxErr (abs (lib.[i, j].Real - oracle.[i, j]))
        Assert.True(maxErr < 1e-8, sprintf "max dense entry error %g exceeds tolerance" maxErr)

    [<Fact>]
    let ``all five Hamiltonian builders agree on H2`` () =
        let factory, nso = h2Factory ()
        let n = uint32 nso
        let s (h : PauliRegisterSequence) = h.DistributeCoefficient.ToString()
        let seqH    = computeHamiltonianWith jordanWignerTerms factory n
        let parH    = computeHamiltonianWithParallel jordanWignerTerms factory n
        let cacheH  = computeHamiltonianCached jordanWignerTerms factory n
        let fullSk  = applyCoefficients (computeHamiltonianSkeleton jordanWignerTerms n) factory
        let sparseSk = applyCoefficients (computeHamiltonianSkeletonFor jordanWignerTerms factory n) factory
        Assert.Equal(s seqH, s parH)
        Assert.Equal(s seqH, s cacheH)
        Assert.Equal(s seqH, s fullSk)
        Assert.Equal(s seqH, s sparseSk)

    [<Fact>]
    let ``H2 direct-oracle anchors: interleaved spin expansion, trace, HF diagonal`` () =
        // Independent direct-oracle acceptance anchors (encodings-research):
        //  • interleaved spin expansion → 4 nonzero one-body + 32 nonzero two-body
        //    factory entries;
        //  • Tr(H)/16 = the identity coefficient IIII = −0.8121706072 (basis-invariant);
        //  • occupation-basis diagonal at the HF state (integer 3 = 0b0011, modes 0,1
        //    occupied) = the electronic HF energy −1.8318636465, with Vnn kept separate.
        let factory, nso = h2Factory ()
        // (1) Factory entry counts (interleaved 0α,0β,1α,1β spin-orbitals).
        let mutable one = 0
        let mutable two = 0
        for i in 0 .. nso - 1 do
            for j in 0 .. nso - 1 do
                if (factory (sprintf "%d,%d" i j)).IsSome then one <- one + 1
                for k in 0 .. nso - 1 do
                    for l in 0 .. nso - 1 do
                        if (factory (sprintf "%d,%d,%d,%d" i j k l)).IsSome then two <- two + 1
        Assert.Equal(4, one)
        Assert.Equal(32, two)
        // (2) Direct dense fermionic matrix (mode j → bit 2ʲ occupation basis).
        let fermionic = Fermion.matrixOf factory nso
        let trace = [ for i in 0 .. Fermion.dim - 1 -> fermionic.[i, i] ] |> List.sum
        Assert.Equal(-0.8121706072, trace / float Fermion.dim, 8)
        Assert.Equal(-1.8318636465, fermionic.[3, 3], 8)

    [<Fact>]
    let ``H2 JW-encoded HF diagonal equals the HF energy in the occupation basis`` () =
        // The encoded (qubit-0-leftmost) Hamiltonian, read in the occupation basis
        // (signature reversed so mode j → bit 2ʲ), must have its HF diagonal entry
        // (integer 3, modes 0,1 occupied) equal to the electronic HF energy. A bit
        // reversal would move this off diagonal index 3, so this is a state-resolved
        // check the spectrum alone cannot make.
        let factory, nso = h2Factory ()
        let ham = computeHamiltonianWith jordanWignerTerms factory (uint32 nso)
        let occ = Enc.matrixOfCOcc ham
        Assert.Equal(-1.8318636465, occ.[3, 3].Real, 8)
        // Trace is basis-invariant → identity coefficient.
        let trace = [ for i in 0 .. 15 -> occ.[i, i] ] |> List.sumBy (fun c -> c.Real)
        Assert.Equal(-0.8121706072, trace / 16.0, 8)

    [<Fact>]
    let ``H2 spectrum agrees across all six fermion-to-qubit encodings`` () =
        // Jordan-Wigner, Bravyi-Kitaev, Parity, and the three tree encodings
        // (balanced binary, ternary, Vlasov) must all reproduce the full
        // 16-eigenvalue H₂ spectrum (ground −1.852388 Ha). The tree encodings
        // emit Y–Y couplings, so their dense matrices are genuinely complex-
        // Hermitian; the spectrum must be taken with `hermEigenvalues`, not by
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
    let ``H2 assembly is wrong if the half is dropped or the r-s swap is omitted`` () =
        // Proves each defect changes the physics independently (compared via spectra,
        // since the encoded matrix uses qubit-0-leftmost and the fermionic oracle
        // uses mode-0-LSB — a bit reversal that preserves eigenvalues).
        let factory, nso = h2Factory ()
        let libSpec = Fermion.eigenvalues (Enc.matrixOf (computeHamiltonianWith jordanWignerTerms factory (uint32 nso)))
        let specOfOracle half swap = Fermion.eigenvalues (Fermion.matrixOfWith half swap factory nso)
        let correct = specOfOracle 0.5 true
        let noHalf  = specOfOracle 1.0 true    // missing ½
        let noSwap  = specOfOracle 0.5 false   // unswapped annihilators
        let eq a b = List.forall2 (fun (x : float) y -> abs (x - y) < 1e-8) a b
        Assert.True(eq libSpec correct, "library must match the ½+swap oracle spectrum")
        Assert.False(eq libSpec noHalf, "dropping the ½ must change the spectrum")
        Assert.False(eq libSpec noSwap, "omitting the r↔s swap must change the spectrum")

    [<Fact>]
    let ``legitimate small nonzero coefficients survive the zero filter`` () =
        // One-body h = 2e-6 → ½h·I and −½h·Z = ±1e-6, far above the 1e-12 threshold.
        let factory (key : string) = if key = "0,0" then Some (Complex(2e-6, 0.0)) else None
        let ham = computeHamiltonianWith jordanWignerTerms factory 2u
        Assert.Equal(1e-6, coeffOf ham "II", 12)
        Assert.Equal(-1e-6, coeffOf ham "ZI", 12)
        Assert.Equal(2, ham.DistributeCoefficient.SummandTerms.Length)
