namespace Tests

/// <summary>
/// Independent, authoritative acceptance lock for the canonical H₂/STO-3G
/// Hamiltonian. The integrals are the <b>byte-for-byte vendored</b> audited research
/// artifact (see <c>SourceRepo</c>/<c>SourceCommit</c>/<c>SourcePath</c>/<c>SourceSha256</c>
/// below); the test proves exact identity by recomputing the file's SHA-256 and
/// asserting it equals the authoritative hash. The integral inputs are NOT
/// regenerated in-test — only the direct 16×16 second-quantized oracle and the
/// per-particle-number sector eigenvalues (which consume those frozen inputs) are
/// computed here, and are asserted against literal frozen expected arrays.
/// </summary>
module HamiltonianFixtureLock =
    open System
    open System.IO
    open System.Text.Json
    open System.Security.Cryptography
    open System.Numerics
    open Encodings
    open Encodings.Hamiltonian
    open Encodings.JordanWigner
    open Xunit

    // ── Authoritative source identity (audited research artifact) ───────
    // johnazariah/encodings-research @ 1e000bbc..., papers/results/h2_sto3g/
    let [<Literal>] SourceRepo   = "johnazariah/encodings-research"
    let [<Literal>] SourceCommit = "1e000bbc9664b8e5cfef48608d07364279c0a54f"
    let [<Literal>] SourcePath   = "papers/results/h2_sto3g/physicist_spin_integrals.json"
    let [<Literal>] SourceSha256 = "6539afb30a1c03ec89202a2960a06c6580a91afaebf13a6cadbcfd32c2d71812"
    let [<Literal>] FixtureName  = "physicist_spin_integrals.json"

    // ── Fixture loading ─────────────────────────────────────────────────

    let private locateFixture (name : string) =
        let rec up (dir : DirectoryInfo) =
            if isNull dir then None
            else
                let candidate = Path.Combine(dir.FullName, "test", "Test.Encodings", "fixtures", name)
                if File.Exists candidate then Some candidate else up dir.Parent
        up (DirectoryInfo(AppContext.BaseDirectory))

    let private fixturePath () =
        match locateFixture FixtureName with
        | None -> failwithf "fixtures/%s not found" FixtureName
        | Some path -> path

    let private fixtureBytes () = File.ReadAllBytes(fixturePath ())

    let private sha256Hex (bytes : byte[]) =
        use sha = SHA256.Create()
        sha.ComputeHash bytes |> Array.map (fun b -> b.ToString("x2")) |> String.concat ""

    // Native artifact schema: one_body_spin / two_body_spin_physicist arrays of
    // {p,q,r,s?,value} objects. Raw single-bar physicist tensor ⟨pq|rs⟩.
    let private loadArtifact () =
        let doc = JsonDocument.Parse(File.ReadAllText(fixturePath ()))
        let root = doc.RootElement
        let oneBody =
            [ for e in root.GetProperty("one_body_spin").EnumerateArray() ->
                (e.GetProperty("p").GetInt32(), e.GetProperty("q").GetInt32()),
                e.GetProperty("value").GetDouble() ]
            |> Map.ofList
        let twoBody =
            [ for e in root.GetProperty("two_body_spin_physicist").EnumerateArray() ->
                (e.GetProperty("p").GetInt32(), e.GetProperty("q").GetInt32(),
                 e.GetProperty("r").GetInt32(), e.GetProperty("s").GetInt32()),
                e.GetProperty("value").GetDouble() ]
            |> Map.ofList
        oneBody, twoBody

    /// Raw single-bar physicist factory backed purely by the vendored artifact maps.
    let private rawFactory () : string -> Complex option =
        let oneBody, twoBody = loadArtifact ()
        fun (key : string) ->
            let x = key.Split(',')
            match x.Length with
            | 2 -> Map.tryFind (int x.[0], int x.[1]) oneBody |> Option.map (fun v -> Complex(v, 0.0))
            | 4 -> Map.tryFind (int x.[0], int x.[1], int x.[2], int x.[3]) twoBody |> Option.map (fun v -> Complex(v, 0.0))
            | _ -> None

    let private coeffOf (h : PauliRegisterSequence) (sign : string) =
        match h.DistributeCoefficient.[sign] with
        | true, reg -> reg.Coefficient.Real
        | false, _ -> 0.0

    // ── (1) Byte-for-byte identity with the audited source artifact ─────

    [<Fact>]
    let ``vendored fixture is byte-for-byte identical to the audited research artifact`` () =
        // Proves the package fixture is the exact audited artifact, not a rounded
        // reconstruction: recomputed SHA-256 must equal the authoritative source hash.
        Assert.Equal(SourceSha256, sha256Hex (fixtureBytes ()))

    [<Fact>]
    let ``fixture declares 4 one-body and 32 raw two-body entries`` () =
        let oneBody, twoBody = loadArtifact ()
        Assert.Equal(4, oneBody.Count)
        Assert.Equal(32, twoBody.Count)

    [<Fact>]
    let ``fixture integral values exactly match the canonical artifact (bit-for-bit)`` () =
        // A handful of representative entries pinned to their exact IEEE-754 values
        // (last-bit precise, NOT tolerance) so any re-rounding of the vendored inputs
        // is caught independently of the hash.
        let oneBody, twoBody = loadArtifact ()
        Assert.Equal(-1.2533097866459773, oneBody.[(0, 0)])
        Assert.Equal(-0.4750688487721783, oneBody.[(2, 2)])
        Assert.Equal( 0.6747559268144484, twoBody.[(0, 0, 0, 0)])
        Assert.Equal( 0.18121046201519672, twoBody.[(0, 0, 2, 2)])
        Assert.Equal( 0.6637114013508132, twoBody.[(0, 2, 0, 2)])
        Assert.Equal( 0.6976515044904612, twoBody.[(3, 3, 3, 3)])

    // ── (2) Complete 15-entry coefficient map (exact) ───────────────────

    // Frozen expected coefficient map, computed from the canonical artifact via the
    // library (deterministic assembly order). Asserted to 12 dp — far tighter than
    // the four-body 0.0453 vs a dropped-½ 0.0906, so a convention regression fails.
    let private expectedMap =
        [ "IIII", -0.812170607249
          "IIIZ", -0.223431536908
          "IIZI", -0.223431536908
          "IIZZ",  0.174412876123
          "IZII",  0.171412826448
          "IZIZ",  0.120625234834
          "IZZI",  0.165927850338
          "XXYY", -0.045302615504
          "XYYX",  0.045302615504
          "YXXY",  0.045302615504
          "YYXX", -0.045302615504
          "ZIII",  0.171412826448
          "ZIIZ",  0.165927850338
          "ZIZI",  0.120625234834
          "ZZII",  0.168688981704 ]

    [<Fact>]
    let ``named raw adapter reproduces the complete 15-entry coefficient map`` () =
        let ham = computeHamiltonianFromPhysicist (rawFactory ()) 4u
        // Exactly the 15 expected terms — no extra, none missing.
        Assert.Equal(expectedMap.Length, ham.DistributeCoefficient.SummandTerms.Length)
        Assert.Equal(15, ham.DistributeCoefficient.SummandTerms.Length)
        for (sign, value) in expectedMap do
            Assert.Equal(value, coeffOf ham sign, 12)
        let expectedSigns = expectedMap |> List.map fst |> Set.ofList
        for t in ham.DistributeCoefficient.SummandTerms do
            Assert.True(expectedSigns.Contains t.Signature,
                sprintf "unexpected term %s not in the frozen coefficient map" t.Signature)

    // ── Direct 16×16 second-quantized oracle (consumes frozen inputs) ───
    module private Oracle =
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
        /// H = Σ h_pq a†_p a_q + ½ Σ ⟨pq|rs⟩ a†_p a†_q a_s a_r, from the raw factory.
        let matrixOf (rawFactory : string -> Complex option) n =
            let h = Array2D.zeroCreate dim dim
            for i in 0 .. n - 1 do
                for j in 0 .. n - 1 do
                    match rawFactory (sprintf "%d,%d" i j) with
                    | Some c -> addScaled h c.Real (matmul (create i) (annihilate j))
                    | None -> ()
            for i in 0 .. n - 1 do
                for j in 0 .. n - 1 do
                    for k in 0 .. n - 1 do
                        for l in 0 .. n - 1 do
                            match rawFactory (sprintf "%d,%d,%d,%d" i j k l) with
                            | Some c ->
                                addScaled h (0.5 * c.Real)
                                    (matmul (matmul (create i) (create j)) (matmul (annihilate l) (annihilate k)))
                            | None -> ()
            h
        /// Eigenvalues of an m×m real-symmetric submatrix via cyclic Jacobi.
        let eigenvaluesOf (a0 : float[,]) (idx : int[]) =
            let m = idx.Length
            let a = Array2D.init m m (fun i j -> a0.[idx.[i], idx.[j]])
            let mutable off = 1.0
            let mutable pass = 0
            while off > 1e-14 && pass < 200 do
                pass <- pass + 1
                for p in 0 .. m - 2 do
                    for q in p + 1 .. m - 1 do
                        if abs a.[p, q] > 1e-300 then
                            let phi = (a.[q, q] - a.[p, p]) / (2.0 * a.[p, q])
                            let t = if phi = 0.0 then 1.0 else float (sign phi) / (abs phi + sqrt (phi * phi + 1.0))
                            let c = 1.0 / sqrt (t * t + 1.0)
                            let s = t * c
                            for k in 0 .. m - 1 do
                                let akp, akq = a.[k, p], a.[k, q]
                                a.[k, p] <- c * akp - s * akq
                                a.[k, q] <- s * akp + c * akq
                            for k in 0 .. m - 1 do
                                let apk, aqk = a.[p, k], a.[q, k]
                                a.[p, k] <- c * apk - s * aqk
                                a.[q, k] <- s * apk + c * aqk
                off <- 0.0
                for p in 0 .. m - 2 do
                    for q in p + 1 .. m - 1 do off <- off + a.[p, q] * a.[p, q]
            [ for i in 0 .. m - 1 -> a.[i, i] ] |> List.sort
        let popcount (x : int) = System.Numerics.BitOperations.PopCount(uint32 x)
        let sectorIndices n = [| for i in 0 .. dim - 1 do if popcount i = n then yield i |]

    // Encoded (qubit-0-leftmost) → occupation-basis dense matrix.
    module private Enc =
        let private cI = [| [| Complex.One; Complex.Zero |]; [| Complex.Zero; Complex.One |] |]
        let private cX = [| [| Complex.Zero; Complex.One |]; [| Complex.One; Complex.Zero |] |]
        let private cY = [| [| Complex.Zero; Complex(0.0, -1.0) |]; [| Complex(0.0, 1.0); Complex.Zero |] |]
        let private cZ = [| [| Complex.One; Complex.Zero |]; [| Complex.Zero; Complex(-1.0, 0.0) |] |]
        let private pm c = match c with 'I' -> cI | 'X' -> cX | 'Y' -> cY | 'Z' -> cZ | _ -> failwith "?"
        let private kron (a: Complex[][]) (b: Complex[][]) =
            let rb, cb = b.Length, b.[0].Length
            Array.init (a.Length * rb) (fun i -> Array.init (a.[0].Length * cb) (fun j -> a.[i / rb].[j / cb] * b.[i % rb].[j % cb]))
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

    // ── (3) Entrywise oracle match + exact per-sector eigenvalue lock ───

    [<Fact>]
    let ``JW-encoded fixture Hamiltonian matches the direct oracle entrywise (16x16)`` () =
        let factory = rawFactory ()
        let lib = Enc.matrixOfCOcc (computeHamiltonianFromPhysicist factory 4u)
        let oracle = Oracle.matrixOf factory 4
        for i in 0 .. Oracle.dim - 1 do
            for j in 0 .. Oracle.dim - 1 do
                Assert.Equal(oracle.[i, j], lib.[i, j].Real, 9)
                Assert.Equal(0.0, lib.[i, j].Imaginary, 9)

    // Literal frozen per-particle-number-sector eigenvalues, computed independently
    // (NumPy dense diagonalisation of the direct oracle on the canonical artifact),
    // cross-checked to equal the F# oracle's Jacobi eigenvalues below. Multiplicities
    // and ascending order are asserted. Union over sectors = the full 16-eigenvalue
    // spectrum; dims are 1,4,6,4,1.
    let private expectedSectors =
        [ 0, [ 0.0 ]
          1, [ -1.2533097866459773; -1.2533097866459773; -0.4750688487721783; -0.4750688487721783 ]
          2, [ -1.8523881735695826; -1.2458776960825393; -1.2458776960825393; -1.2458776960825393
               -0.8834567720521458; -0.2319616659618189 ]
          3, [ -1.1607201545632546; -1.1607201545632546; -0.3595836390134429; -0.3595836390134429 ]
          4, [ 0.2080748418414580 ] ]

    [<Fact>]
    let ``each particle-number sector matches its literal frozen eigenvalue array`` () =
        let oracle = Oracle.matrixOf (rawFactory ()) 4
        let dims = [ 0, 1; 1, 4; 2, 6; 3, 4; 4, 1 ]
        for (n, expectedDim) in dims do
            let idx = Oracle.sectorIndices n
            Assert.Equal(expectedDim, idx.Length)
        for (n, expected) in expectedSectors do
            let idx = Oracle.sectorIndices n
            let computed = Oracle.eigenvaluesOf oracle idx    // ascending
            let expectedSorted = List.sort expected
            Assert.Equal(expected.Length, idx.Length)         // multiplicity/count
            Assert.Equal(expected.Length, computed.Length)
            // Order + multiplicity: ascending arrays compared element-by-element.
            List.iter2 (fun (e : float) c -> Assert.Equal(e, c, 8)) expectedSorted computed

    [<Fact>]
    let ``sector eigenvalues union to the full 16-eigenvalue spectrum with the canonical ground`` () =
        let oracle = Oracle.matrixOf (rawFactory ()) 4
        let full =
            [ for n in 0 .. 4 -> Oracle.eigenvaluesOf oracle (Oracle.sectorIndices n) ]
            |> List.concat |> List.sort
        let expectedFull = expectedSectors |> List.collect snd |> List.sort
        Assert.Equal(16, full.Length)
        List.iter2 (fun (e : float) c -> Assert.Equal(e, c, 8)) expectedFull full
        // The physical ground state lives in the N = 2 sector.
        Assert.Equal(-1.8523881735695826, List.head full, 8)
        // HF determinant (integer 3 = 0b0011, modes 0,1 occupied): electronic HF energy.
        Assert.Equal(-1.8318636464775060, oracle.[3, 3], 8)

    // ── (4) Metrics lock + legacy/raw equivalence on the frozen data ────

    [<Fact>]
    let ``fixture metrics lock: 15 terms, weight 32, 15 rotations, 36 CNOTs, 1-norm`` () =
        let ham = computeHamiltonianFromPhysicist (rawFactory ()) 4u
        let costs = CostAnalysis.hamiltonianCosts ham
        Assert.Equal(15, costs.TermCount)
        Assert.Equal(32, costs.TotalPauliWeight)
        let step = Trotterization.firstOrderTrotter 1.0 ham
        Assert.Equal(15, step.Rotations.Length)
        Assert.Equal(36, Trotterization.trotterCnotCount step)
        let oneNorm = ham.DistributeCoefficient.SummandTerms |> Array.sumBy (fun t -> Complex.Abs t.Coefficient)
        Assert.Equal(2.6992778241451574, oneNorm, 10)

    [<Fact>]
    let ``legacy weighted path on fixture-derived weighted data equals the raw-adapter path`` () =
        // Independently pre-adapt the frozen raw integrals to weighted form
        // (½·⟨pq|sr⟩ for weighted key p,q,r,s) and feed the legacy weighted API; it
        // must match the named raw adapter on the same vendored artifact.
        let oneBody, twoBody = loadArtifact ()
        let preAdapted (key : string) =
            let x = key.Split(',')
            match x.Length with
            | 2 -> Map.tryFind (int x.[0], int x.[1]) oneBody |> Option.map (fun v -> Complex(v, 0.0))
            | 4 ->
                // weighted key p,q,r,s ← raw key p,q,s,r, halved
                Map.tryFind (int x.[0], int x.[1], int x.[3], int x.[2]) twoBody
                |> Option.map (fun v -> Complex(0.5 * v, 0.0))
            | _ -> None
        let viaLegacy = computeHamiltonianWith jordanWignerTerms preAdapted 4u
        let viaRaw    = computeHamiltonianFromPhysicist (rawFactory ()) 4u
        Assert.Equal(viaRaw.DistributeCoefficient.ToString(), viaLegacy.DistributeCoefficient.ToString())
