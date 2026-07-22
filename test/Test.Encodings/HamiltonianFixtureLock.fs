namespace Tests

/// <summary>
/// Independent frozen-fixture acceptance lock for the canonical H₂/STO-3G
/// Hamiltonian. Unlike <see cref="T:Tests.HamiltonianCoefficients"/> — which
/// derives its raw tensor procedurally from spatial ERIs — this suite loads the
/// integrals and every expected value from a committed JSON fixture
/// (<c>fixtures/h2_sto3g_raw.json</c>) with provenance metadata and a tamper-evident
/// integral hash. The inputs are NOT regenerated in-test: only the direct 16×16
/// second-quantized oracle (which consumes those frozen inputs) is computed here.
/// </summary>
module HamiltonianFixtureLock =
    open System
    open System.IO
    open System.Text.Json
    open System.Globalization
    open System.Security.Cryptography
    open System.Numerics
    open Encodings
    open Encodings.Hamiltonian
    open Encodings.JordanWigner
    open Xunit

    // ── Fixture loading ─────────────────────────────────────────────────

    let private locateFixture (name : string) =
        let rec up (dir : DirectoryInfo) =
            if isNull dir then None
            else
                let candidate = Path.Combine(dir.FullName, "test", "Test.Encodings", "fixtures", name)
                if File.Exists candidate then Some candidate else up dir.Parent
        up (DirectoryInfo(AppContext.BaseDirectory))

    let private fixtureDoc () =
        match locateFixture "h2_sto3g_raw.json" with
        | None -> failwith "fixtures/h2_sto3g_raw.json not found"
        | Some path -> JsonDocument.Parse(File.ReadAllText path)

    let private readMap (el : JsonElement) =
        [ for p in el.EnumerateObject() -> p.Name, p.Value.GetDouble() ] |> Map.ofList

    // ── Provenance / tamper-evidence ────────────────────────────────────

    [<Fact>]
    let ``fixture integral hash matches the recorded provenance hash`` () =
        use doc = fixtureDoc ()
        let root = doc.RootElement
        let oneBody = readMap (root.GetProperty "one_body")
        let twoBody = readMap (root.GetProperty "two_body")
        // Recompute the hash exactly as documented in provenance.integral_hash_recipe.
        let fmt (kv : System.Collections.Generic.KeyValuePair<string, float>) =
            kv.Key + "=" + kv.Value.ToString("R", CultureInfo.InvariantCulture)
        let payload =
            List.append
                (oneBody |> Seq.map fmt |> Seq.sort |> List.ofSeq)
                (twoBody |> Seq.map fmt |> Seq.sort |> List.ofSeq)
            |> String.concat "\n"
        use sha = SHA256.Create()
        let hash =
            sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes payload)
            |> Array.map (fun b -> b.ToString("x2"))
            |> String.concat ""
        let recorded = (root.GetProperty "provenance").GetProperty("integral_hash").GetString()
        Assert.Equal(recorded, hash)

    [<Fact>]
    let ``fixture declares 4 one-body and 32 raw two-body integral entries`` () =
        use doc = fixtureDoc ()
        let root = doc.RootElement
        Assert.Equal(4, (readMap (root.GetProperty "one_body")).Count)
        Assert.Equal(32, (readMap (root.GetProperty "two_body")).Count)
        Assert.Equal(4, (root.GetProperty "num_spin_orbitals").GetInt32())

    // ── Factory built from the frozen fixture ───────────────────────────

    /// Raw single-bar physicist factory backed purely by the frozen JSON maps.
    let private rawFactoryFromFixture (root : JsonElement) : string -> Complex option =
        let oneBody = readMap (root.GetProperty "one_body")
        let twoBody = readMap (root.GetProperty "two_body")
        fun (key : string) ->
            match key.Split(',').Length with
            | 2 -> oneBody |> Map.tryFind key |> Option.map (fun v -> Complex(v, 0.0))
            | 4 -> twoBody |> Map.tryFind key |> Option.map (fun v -> Complex(v, 0.0))
            | _ -> None

    let private coeffOf (h : PauliRegisterSequence) (sign : string) =
        match h.DistributeCoefficient.[sign] with
        | true, reg -> reg.Coefficient.Real
        | false, _ -> 0.0

    [<Fact>]
    let ``named raw adapter on the fixture reproduces the complete 15-entry coefficient map`` () =
        use doc = fixtureDoc ()
        let root = doc.RootElement
        let factory = rawFactoryFromFixture root
        let ham = computeHamiltonianFromPhysicist factory 4u
        let expected = readMap ((root.GetProperty "expected").GetProperty "coefficient_map")
        // Every stored term equals its fixture value, and there are exactly 15.
        Assert.Equal(expected.Count, ham.DistributeCoefficient.SummandTerms.Length)
        Assert.Equal(15, ham.DistributeCoefficient.SummandTerms.Length)
        for kv in expected do
            Assert.Equal(kv.Value, coeffOf ham kv.Key, 9)
        // No stored term is absent from the fixture map (no extra terms).
        for t in ham.DistributeCoefficient.SummandTerms do
            Assert.True(expected.ContainsKey t.Signature,
                sprintf "unexpected term %s not present in the fixture coefficient map" t.Signature)

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
        /// H = Σ h_ij a†_i a_j + ½ Σ ⟨ij|kl⟩ a†_i a†_j a_l a_k, from the raw factory.
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

    // Encoded (qubit-0-leftmost) → occupation-basis dense matrix (signature reversed).
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

    [<Fact>]
    let ``JW-encoded fixture Hamiltonian matches the direct oracle entrywise (16x16)`` () =
        use doc = fixtureDoc ()
        let root = doc.RootElement
        let factory = rawFactoryFromFixture root
        let lib = Enc.matrixOfCOcc (computeHamiltonianFromPhysicist factory 4u)
        let oracle = Oracle.matrixOf factory 4
        for i in 0 .. Oracle.dim - 1 do
            for j in 0 .. Oracle.dim - 1 do
                Assert.Equal(oracle.[i, j], lib.[i, j].Real, 9)
                Assert.Equal(0.0, lib.[i, j].Imaginary, 9)

    [<Fact>]
    let ``oracle HF row 3, particle sectors, and ground match the fixture`` () =
        use doc = fixtureDoc ()
        let root = doc.RootElement
        let expected = root.GetProperty "expected"
        let factory = rawFactoryFromFixture root
        let oracle = Oracle.matrixOf factory 4
        // HF state = integer 3 (0b0011, modes 0 and 1 occupied): electronic HF energy.
        Assert.Equal((expected.GetProperty "hf_diagonal_index3").GetDouble(), oracle.[3, 3], 8)
        // Exact particle-number superselection: no coupling between different occupations.
        let popcount (x : int) = System.Numerics.BitOperations.PopCount(uint32 x)
        for i in 0 .. Oracle.dim - 1 do
            for j in 0 .. Oracle.dim - 1 do
                if popcount i <> popcount j then Assert.Equal(0.0, oracle.[i, j], 12)
        // Full 16-eigenvalue spectrum: electronic ground state.
        let spec = Oracle.eigenvalues oracle
        Assert.Equal(16, spec.Length)
        Assert.Equal((expected.GetProperty "electronic_ground_state").GetDouble(), List.head spec, 8)

    [<Fact>]
    let ``fixture metrics lock: 15 terms, weight 32, 15 rotations, 36 CNOTs, 1-norm`` () =
        use doc = fixtureDoc ()
        let root = doc.RootElement
        let expected = root.GetProperty "expected"
        let factory = rawFactoryFromFixture root
        let ham = computeHamiltonianFromPhysicist factory 4u
        let costs = CostAnalysis.hamiltonianCosts ham
        Assert.Equal((expected.GetProperty "num_terms").GetInt32(), costs.TermCount)
        Assert.Equal((expected.GetProperty "total_pauli_weight").GetInt32(), costs.TotalPauliWeight)
        let step = Trotterization.firstOrderTrotter 1.0 ham
        Assert.Equal((expected.GetProperty "first_order_rotations").GetInt32(), step.Rotations.Length)
        Assert.Equal((expected.GetProperty "cnots").GetInt32(), Trotterization.trotterCnotCount step)
        let oneNorm = ham.DistributeCoefficient.SummandTerms |> Array.sumBy (fun t -> Complex.Abs t.Coefficient)
        Assert.Equal((expected.GetProperty "one_norm").GetDouble(), oneNorm, 9)

    [<Fact>]
    let ``legacy weighted path on fixture-derived weighted data equals the raw-adapter path`` () =
        // Independently pre-adapt the frozen raw integrals to weighted form
        // (½·⟨pq|sr⟩ for weighted key p,q,r,s) and feed the legacy weighted API; it
        // must match the named raw adapter on the same fixture — locking both surfaces
        // against the one frozen dataset.
        use doc = fixtureDoc ()
        let root = doc.RootElement
        let twoBody = readMap (root.GetProperty "two_body")
        let oneBody = readMap (root.GetProperty "one_body")
        let preAdapted (key : string) =
            let x = key.Split(',')
            match x.Length with
            | 2 -> oneBody |> Map.tryFind key |> Option.map (fun v -> Complex(v, 0.0))
            | 4 ->
                // weighted key p,q,r,s ← raw key p,q,s,r, halved
                let swapped = sprintf "%s,%s,%s,%s" x.[0] x.[1] x.[3] x.[2]
                twoBody |> Map.tryFind swapped |> Option.map (fun v -> Complex(0.5 * v, 0.0))
            | _ -> None
        let raw = rawFactoryFromFixture root
        let viaLegacy = computeHamiltonianWith jordanWignerTerms preAdapted 4u
        let viaRaw    = computeHamiltonianFromPhysicist raw 4u
        Assert.Equal(viaRaw.DistributeCoefficient.ToString(), viaLegacy.DistributeCoefficient.ToString())
