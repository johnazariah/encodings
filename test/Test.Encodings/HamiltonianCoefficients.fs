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
        // 15 physical terms; the raw sum retains 8 zero-coefficient entries (23 total).
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
    let ``two-body coefficient is applied verbatim with no additional one-half`` () =
        // a†_0 a†_1 a_1 a_0 with factory("0,1,1,0")=V. Doubling V must double every
        // coefficient AND the absolute values are fixed by V alone (a spurious ½
        // would halve them). Compare V=1 against the known exact JW decomposition.
        let mk v = computeHamiltonianWith jordanWignerTerms
                    (fun key -> if key = "0,1,1,0" then Some (Complex(v, 0.0)) else None) 2u
        let h1 = mk 1.0
        let h2 = mk 2.0
        // linearity: h2 == 2 * h1 term-by-term
        for t in nonZero h2 do
            Assert.Equal(2.0 * coeffOf h1 t.Signature, t.Coefficient.Real, 10)
        // a†_0 a†_1 a_1 a_0 = n_0 n_1 = (I-Z_0)(I-Z_1)/4 = ¼(II - ZI - IZ + ZZ).
        // With V=1 the II coefficient is exactly ¼ (not ½ and not ⅛).
        Assert.Equal(0.25, coeffOf h1 "II", 10)
        Assert.Equal(0.25, coeffOf h1 "ZZ", 10)
        Assert.Equal(-0.25, coeffOf h1 "ZI", 10)
        Assert.Equal(-0.25, coeffOf h1 "IZ", 10)

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

        /// Direct dense H = Σ h_ij a†_i a_j + Σ V_ijkl a†_i a†_j a_k a_l.
        let matrixOf (factory : string -> Complex option) n =
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
                                addScaled h c.Real
                                    (matmul (matmul (create i) (create j)) (matmul (annihilate k) (annihilate l)))
                            | None -> ()
            h

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
    //  Chemist ↔ physicist conversion (explicit permutation test)
    //  ──────────────────────────────────────────────────────────────────
    //  FockMap's factory expects, for key "p,q,r,s" (operator a†_p a†_q a_r a_s),
    //  the coefficient ½·(ps|qr) chemist = ½·⟨pq|sr⟩ physicist. Given a physicist
    //  tensor T[a,b,c,d] = ⟨ab|cd⟩, the correct adaptation is
    //      F("p,q,r,s") = ½ · T[p, q, s, r]      (fold in ½ AND swap r↔s).
    //  Feeding the RAW physicist tensor (no ½, no swap) reproduces the book's
    //  reported error exactly (IIII=-3.5608, four-body 0.0906). Canonical source:
    //  encodings-research, R=1.3983973 bohr, interleaved 0α,0β,1α,1β, chemist ERIs.
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
        /// CORRECT adaptation: F("p,q,r,s") = ½·⟨pq|sr⟩ = ½·T[p,q,s,r].
        let correct (key : string) =
            let x = key.Split(',')
            match x.Length with
            | 2 -> oneBody key
            | 4 ->
                let p, q, r, s = int x.[0], int x.[1], int x.[2], int x.[3]
                let v = 0.5 * tPhys p q s r
                if abs v > 1e-15 then Some (Complex(v, 0.0)) else None
            | _ -> None
        /// NAIVE (book bug): raw physicist tensor, no ½, no r↔s swap.
        let naive (key : string) =
            let x = key.Split(',')
            match x.Length with
            | 2 -> oneBody key
            | 4 ->
                let p, q, r, s = int x.[0], int x.[1], int x.[2], int x.[3]
                let v = tPhys p q r s
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
    let ``physicist tensor needs the half and r-s swap to match chemist coefficients`` () =
        let chemF, nso = h2Factory ()
        let chem = computeHamiltonianWith jordanWignerTerms chemF (uint32 nso)
        let good = computeHamiltonianWith jordanWignerTerms Phys.correct (uint32 nso)
        // Correct adaptation reproduces the chemist Hamiltonian term-for-term.
        Assert.Equal(chem.DistributeCoefficient.ToString(), good.DistributeCoefficient.ToString())
        Assert.Equal(-0.8121706072, coeffOf good "IIII", 8)
        Assert.Equal(-0.0453026155, coeffOf good "XXYY", 8)
        Assert.Equal(2.699278, oneNorm good, 5)

    [<Fact>]
    let ``raw physicist adaptation reproduces the book's wrong coefficients`` () =
        // Negative control: dropping the ½ and the r↔s swap gives exactly the
        // book's reported IIII=-3.5608 and four-body 0.0906 (signatures still match).
        let bad = computeHamiltonianWith jordanWignerTerms Phys.naive 4u
        Assert.Equal(-3.5607946917, coeffOf bad "IIII", 6)
        Assert.Equal( 0.0906052310, coeffOf bad "XXYY" |> abs, 6)
        // The signatures are identical to the correct Hamiltonian — only coefficients differ.
        let good = computeHamiltonianWith jordanWignerTerms Phys.correct 4u
        let sigs (h : PauliRegisterSequence) =
            h.DistributeCoefficient.SummandTerms
            |> Array.filter (fun t -> Complex.Abs t.Coefficient > 1e-10)
            |> Array.map (fun t -> t.Signature) |> Array.sort
        Assert.Equal<string[]>(sigs good, sigs bad)
