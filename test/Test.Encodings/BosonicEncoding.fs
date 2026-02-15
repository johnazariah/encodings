namespace Tests

module BosonicEncoding =
    open Encodings
    open Xunit
    open System.Numerics

    // ──────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────

    let private assertTerms (expected : (string * Complex) list) (prs : PauliRegisterSequence) =
        let terms = prs.SummandTerms
        Assert.Equal(expected.Length, terms.Length)
        for (sig', coeff) in expected do
            match prs.[sig'] with
            | true, reg ->
                Assert.True(
                    (reg.Coefficient - coeff).Magnitude < 1e-10,
                    sprintf "Term %s: expected %A but got %A" sig' coeff reg.Coefficient)
            | false, _ ->
                Assert.Fail (sprintf "Missing expected term with signature %s" sig')

    let private assertTermCount (n : int) (prs : PauliRegisterSequence) =
        Assert.Equal(n, prs.SummandTerms.Length)

    let private pauliWeight (reg : PauliRegister) =
        reg.Signature |> Seq.sumBy (fun c -> if c = 'I' then 0 else 1)

    let private maxPauliWeight (prs : PauliRegisterSequence) =
        if prs.SummandTerms.Length = 0 then 0
        else prs.SummandTerms |> Array.map pauliWeight |> Array.max

    let private allRegisterWidths (prs : PauliRegisterSequence) =
        prs.SummandTerms |> Array.map (fun r -> r.Signature.Length) |> Array.distinct

    // ──────────────────────────────────────────────────
    //  Helper unit tests
    // ──────────────────────────────────────────────────

    [<Theory>]
    [<InlineData(1, 1)>]
    [<InlineData(2, 1)>]
    [<InlineData(3, 2)>]
    [<InlineData(4, 2)>]
    [<InlineData(5, 3)>]
    [<InlineData(8, 3)>]
    [<InlineData(9, 4)>]
    [<InlineData(16, 4)>]
    let ``ceilLog2 returns correct values`` (d : int, expected : int) =
        Assert.Equal(expected, ceilLog2 d)

    [<Theory>]
    [<InlineData(0, 0)>]
    [<InlineData(1, 1)>]
    [<InlineData(2, 3)>]
    [<InlineData(3, 2)>]
    [<InlineData(4, 6)>]
    [<InlineData(5, 7)>]
    [<InlineData(6, 5)>]
    [<InlineData(7, 4)>]
    let ``grayCodeBasisMap encodes correctly`` (n : int, expected : int) =
        Assert.Equal(expected, grayCodeBasisMap n)

    // ──────────────────────────────────────────────────
    //  Bosonic matrix construction
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``bosonicCreationMatrix d=2 is correct`` () =
        let m = bosonicCreationMatrix 2
        Assert.Equal(Complex.Zero, m.[0, 0])
        Assert.Equal(Complex.Zero, m.[0, 1])
        Assert.Equal(Complex.One,  m.[1, 0])
        Assert.Equal(Complex.Zero, m.[1, 1])

    [<Fact>]
    let ``bosonicCreationMatrix d=4 has sqrt entries`` () =
        let m = bosonicCreationMatrix 4
        Assert.Equal(Complex(1., 0.), m.[1, 0])
        Assert.True((m.[2, 1] - Complex(sqrt 2., 0.)).Magnitude < 1e-14)
        Assert.True((m.[3, 2] - Complex(sqrt 3., 0.)).Magnitude < 1e-14)
        // off-diagonal zeros
        Assert.Equal(Complex.Zero, m.[0, 0])
        Assert.Equal(Complex.Zero, m.[3, 3])
        Assert.Equal(Complex.Zero, m.[0, 2])

    [<Fact>]
    let ``bosonicAnnihilationMatrix is transpose of creation`` () =
        for d in [2; 3; 4; 8] do
            let cr = bosonicCreationMatrix d
            let an = bosonicAnnihilationMatrix d
            for i in 0 .. d - 1 do
                for j in 0 .. d - 1 do
                    Assert.True(
                        (cr.[i, j] - an.[j, i]).Magnitude < 1e-14,
                        sprintf "d=%d: cr[%d,%d] ≠ an[%d,%d]" d i j j i)

    [<Fact>]
    let ``bosonicNumberMatrix d=4 is diagonal 0,1,2,3`` () =
        let m = bosonicNumberMatrix 4
        for i in 0 .. 3 do
            Assert.Equal(Complex(float i, 0.), m.[i, i])
            for j in 0 .. 3 do
                if i <> j then
                    Assert.Equal(Complex.Zero, m.[i, j])

    // ──────────────────────────────────────────────────
    //  Pauli decomposition
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``allPauliStrings q=1 gives 4 strings`` () =
        let ps = allPauliStrings 1
        Assert.Equal(4, ps.Length)

    [<Fact>]
    let ``allPauliStrings q=2 gives 16 strings`` () =
        let ps = allPauliStrings 2
        Assert.Equal(16, ps.Length)

    [<Fact>]
    let ``decomposeIntoPaulis identity gives identity`` () =
        let identity = array2D [| [| Complex.One; Complex.Zero |]; [| Complex.Zero; Complex.One |] |]
        let terms = decomposeIntoPaulis identity 1
        Assert.Equal(1, terms.Length)
        let (coeff, paulis) = terms.[0]
        Assert.True((coeff - Complex.One).Magnitude < 1e-12)
        Assert.Equal<Pauli[]>([| Pauli.I |], paulis)

    [<Fact>]
    let ``decomposeIntoPaulis sigma_plus gives X and Y terms`` () =
        // σ⁺ = |1⟩⟨0| = ½(X - iY)
        let sigmaPlus = array2D [| [| Complex.Zero; Complex.Zero |]; [| Complex.One; Complex.Zero |] |]
        let terms = decomposeIntoPaulis sigmaPlus 1
        Assert.Equal(2, terms.Length)
        let termMap = terms |> Array.map (fun (c, ps) -> (ps.[0], c)) |> Map.ofArray
        Assert.True((termMap.[Pauli.X] - Complex(0.5, 0.)).Magnitude < 1e-12)
        Assert.True((termMap.[Pauli.Y] - Complex(0., -0.5)).Magnitude < 1e-12)

    // ──────────────────────────────────────────────────
    //  Edge cases — all encodings
    // ──────────────────────────────────────────────────

    [<Theory>]
    [<InlineData("unary")>]
    [<InlineData("binary")>]
    [<InlineData("gray")>]
    let ``Identity operator returns empty sequence`` (encoding : string) =
        let encode =
            match encoding with
            | "unary"  -> unaryBosonTerms
            | "binary" -> binaryBosonTerms
            | "gray"   -> grayCodeBosonTerms
            | _        -> failwith "unknown"
        let result = encode Identity 0u 1u 4u
        Assert.Equal(0, result.SummandTerms.Length)

    [<Theory>]
    [<InlineData("unary")>]
    [<InlineData("binary")>]
    [<InlineData("gray")>]
    let ``d=1 returns empty (trivial Fock space)`` (encoding : string) =
        let encode =
            match encoding with
            | "unary"  -> unaryBosonTerms
            | "binary" -> binaryBosonTerms
            | "gray"   -> grayCodeBosonTerms
            | _        -> failwith "unknown"
        let result = encode Raise 0u 1u 1u
        Assert.Equal(0, result.SummandTerms.Length)

    [<Theory>]
    [<InlineData("unary")>]
    [<InlineData("binary")>]
    [<InlineData("gray")>]
    let ``mode index out of range returns empty`` (encoding : string) =
        let encode =
            match encoding with
            | "unary"  -> unaryBosonTerms
            | "binary" -> binaryBosonTerms
            | "gray"   -> grayCodeBosonTerms
            | _        -> failwith "unknown"
        let result = encode Raise 2u 2u 4u   // j=2 but only 2 modes (0,1)
        Assert.Equal(0, result.SummandTerms.Length)

    // ──────────────────────────────────────────────────
    //  Unary encoding: d=2, 1 mode
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``Unary d=2 creation has 4 weight-2 terms`` () =
        let result = unaryBosonTerms Raise 0u 1u 2u
        result |> assertTermCount 4
        Assert.Equal(2, maxPauliWeight result)
        // √1 = 1, quarter = 0.25
        result |> assertTerms [
            "XX", Complex(0.25, 0.)
            "YY", Complex(0.25, 0.)
            "XY", Complex(0., -0.25)
            "YX", Complex(0., 0.25)
        ]

    [<Fact>]
    let ``Unary d=2 annihilation has conjugate signs`` () =
        let result = unaryBosonTerms Lower 0u 1u 2u
        result |> assertTermCount 4
        result |> assertTerms [
            "XX", Complex(0.25, 0.)
            "YY", Complex(0.25, 0.)
            "XY", Complex(0., 0.25)
            "YX", Complex(0., -0.25)
        ]

    [<Fact>]
    let ``Unary d=2 creation and annihilation XY signs are opposite`` () =
        let cr = unaryBosonTerms Raise 0u 1u 2u
        let an = unaryBosonTerms Lower 0u 1u 2u
        match cr.["XY"], an.["XY"] with
        | (true, crReg), (true, anReg) ->
            Assert.True(
                (crReg.Coefficient + anReg.Coefficient).Magnitude < 1e-12,
                "XY coefficients should be negatives")
        | _ -> Assert.Fail "Missing XY term"

    // ──────────────────────────────────────────────────
    //  Unary encoding: d=3, 1 mode
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``Unary d=3 creation has 8 terms (4 per transition)`` () =
        let result = unaryBosonTerms Raise 0u 1u 3u
        result |> assertTermCount 8
        Assert.Equal(2, maxPauliWeight result)
        // All registers have width 3
        Assert.Equal<int[]>([| 3 |], allRegisterWidths result)

    [<Fact>]
    let ``Unary d=3 second transition scaled by sqrt(2)`` () =
        let result = unaryBosonTerms Raise 0u 1u 3u
        // Transition 1→2 at qubits (1,2): XX has signature "IXX"
        // coefficient = √2 / 4
        match result.["IXX"] with
        | true, reg ->
            Assert.True(
                (reg.Coefficient - Complex(sqrt 2.0 / 4.0, 0.)).Magnitude < 1e-12,
                sprintf "Expected √2/4 but got %A" reg.Coefficient)
        | _ -> Assert.Fail "Missing IXX term"

    // ──────────────────────────────────────────────────
    //  Unary encoding: multi-mode
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``Unary d=2 two modes: mode 0 acts on qubits 0-1, mode 1 on qubits 2-3`` () =
        let cr0 = unaryBosonTerms Raise 0u 2u 2u
        let cr1 = unaryBosonTerms Raise 1u 2u 2u
        // Register width = 2 modes × 2 qubits/mode = 4
        Assert.Equal<int[]>([| 4 |], allRegisterWidths cr0)
        Assert.Equal<int[]>([| 4 |], allRegisterWidths cr1)
        // Mode 0: XX at positions 0,1 → "XXII"
        match cr0.["XXII"] with
        | true, _ -> ()
        | _ -> Assert.Fail "Mode 0 should have XXII"
        // Mode 1: XX at positions 2,3 → "IIXX"
        match cr1.["IIXX"] with
        | true, _ -> ()
        | _ -> Assert.Fail "Mode 1 should have IIXX"

    // ──────────────────────────────────────────────────
    //  Binary encoding: d=2, 1 mode (hard-core boson)
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``Binary d=2 creation equals sigma_plus (½X − ½iY)`` () =
        let result = binaryBosonTerms Raise 0u 1u 2u
        result |> assertTermCount 2
        result |> assertTerms [
            "X", Complex(0.5, 0.)
            "Y", Complex(0., -0.5)
        ]

    [<Fact>]
    let ``Binary d=2 annihilation equals sigma_minus (½X + ½iY)`` () =
        let result = binaryBosonTerms Lower 0u 1u 2u
        result |> assertTermCount 2
        result |> assertTerms [
            "X", Complex(0.5, 0.)
            "Y", Complex(0., 0.5)
        ]

    // ──────────────────────────────────────────────────
    //  Binary encoding: d=4, 1 mode
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``Binary d=4 uses 2 qubits per mode`` () =
        Assert.Equal(2, binaryQubitsPerMode 4)

    [<Fact>]
    let ``Binary d=4 creation has non-trivial terms`` () =
        let result = binaryBosonTerms Raise 0u 1u 4u
        // Register width = 2
        Assert.Equal<int[]>([| 2 |], allRegisterWidths result)
        // Should have multiple Pauli terms from decomposing the 4×4 matrix
        Assert.True(result.SummandTerms.Length >= 2, "Expected multiple Pauli terms")
        Assert.True(maxPauliWeight result <= 2, "Max weight should be ≤ ⌈log₂4⌉ = 2")

    // ──────────────────────────────────────────────────
    //  Gray code encoding: d=2
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``Gray d=2 matches binary (Gray code trivial at d=2)`` () =
        // Gray(0) = 0, Gray(1) = 1, so identical to binary for d=2
        let binaryResult = binaryBosonTerms Raise 0u 1u 2u
        let grayResult = grayCodeBosonTerms Raise 0u 1u 2u
        Assert.Equal(binaryResult.SummandTerms.Length, grayResult.SummandTerms.Length)
        for term in binaryResult.SummandTerms do
            match grayResult.[term.Signature] with
            | true, gTerm ->
                Assert.True(
                    (term.Coefficient - gTerm.Coefficient).Magnitude < 1e-12,
                    sprintf "Mismatch at %s" term.Signature)
            | _ ->
                Assert.Fail (sprintf "Gray encoding missing term %s" term.Signature)

    // ──────────────────────────────────────────────────
    //  Gray code encoding: d=4
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``Gray d=4 creation has same number of qubits as binary`` () =
        let binaryResult = binaryBosonTerms Raise 0u 1u 4u
        let grayResult = grayCodeBosonTerms Raise 0u 1u 4u
        Assert.Equal<int[]>(allRegisterWidths binaryResult, allRegisterWidths grayResult)

    [<Fact>]
    let ``Gray d=4 differs from binary d=4 (different basis mapping)`` () =
        let binaryResult = binaryBosonTerms Raise 0u 1u 4u
        let grayResult = grayCodeBosonTerms Raise 0u 1u 4u
        // The term sets should differ because Gray code remaps basis states
        let binarySigs = binaryResult.SummandTerms |> Array.map (fun t -> t.Signature) |> Set.ofArray
        let graySigs = grayResult.SummandTerms |> Array.map (fun t -> t.Signature) |> Set.ofArray
        // They might share some terms but not be identical overall
        let binaryCoeffs = binaryResult.SummandTerms |> Array.map (fun t -> (t.Signature, t.Coefficient)) |> Map.ofArray
        let grayCoeffs = grayResult.SummandTerms |> Array.map (fun t -> (t.Signature, t.Coefficient)) |> Map.ofArray
        let areSame = (binaryCoeffs = grayCoeffs)
        Assert.False(areSame, "Gray and binary encodings should differ for d=4")

    // ──────────────────────────────────────────────────
    //  Pauli weight bounds
    // ──────────────────────────────────────────────────

    [<Theory>]
    [<InlineData(2u)>]
    [<InlineData(3u)>]
    [<InlineData(4u)>]
    [<InlineData(5u)>]
    let ``Unary max Pauli weight is 2`` (d : uint32) =
        let cr = unaryBosonTerms Raise 0u 1u d
        let an = unaryBosonTerms Lower 0u 1u d
        if cr.SummandTerms.Length > 0 then
            Assert.True(maxPauliWeight cr <= 2, sprintf "Raise weight > 2 for d=%d" d)
        if an.SummandTerms.Length > 0 then
            Assert.True(maxPauliWeight an <= 2, sprintf "Lower weight > 2 for d=%d" d)

    [<Theory>]
    [<InlineData(2u, 1)>]
    [<InlineData(4u, 2)>]
    [<InlineData(8u, 3)>]
    let ``Binary max Pauli weight is at most ceil_log2 d`` (d : uint32, expectedMaxWeight : int) =
        let cr = binaryBosonTerms Raise 0u 1u d
        if cr.SummandTerms.Length > 0 then
            Assert.True(
                maxPauliWeight cr <= expectedMaxWeight,
                sprintf "Binary weight %d > %d for d=%d" (maxPauliWeight cr) expectedMaxWeight d)

    [<Theory>]
    [<InlineData(2u, 1)>]
    [<InlineData(4u, 2)>]
    [<InlineData(8u, 3)>]
    let ``Gray max Pauli weight is at most ceil_log2 d`` (d : uint32, expectedMaxWeight : int) =
        let cr = grayCodeBosonTerms Raise 0u 1u d
        if cr.SummandTerms.Length > 0 then
            Assert.True(
                maxPauliWeight cr <= expectedMaxWeight,
                sprintf "Gray weight %d > %d for d=%d" (maxPauliWeight cr) expectedMaxWeight d)

    // ──────────────────────────────────────────────────
    //  Term count
    // ──────────────────────────────────────────────────

    [<Theory>]
    [<InlineData(2u, 4)>]
    [<InlineData(3u, 8)>]
    [<InlineData(4u, 12)>]
    let ``Unary term count is 4(d-1)`` (d : uint32, expectedTerms : int) =
        let result = unaryBosonTerms Raise 0u 1u d
        result |> assertTermCount expectedTerms

    // ──────────────────────────────────────────────────
    //  Qubit count helpers
    // ──────────────────────────────────────────────────

    [<Theory>]
    [<InlineData(2, 2)>]
    [<InlineData(4, 4)>]
    [<InlineData(8, 8)>]
    let ``unaryQubitsPerMode equals d`` (d : int, expected : int) =
        Assert.Equal(expected, unaryQubitsPerMode d)

    [<Theory>]
    [<InlineData(2, 1)>]
    [<InlineData(3, 2)>]
    [<InlineData(4, 2)>]
    [<InlineData(8, 3)>]
    let ``binaryQubitsPerMode equals ceil_log2`` (d : int, expected : int) =
        Assert.Equal(expected, binaryQubitsPerMode d)

    // ──────────────────────────────────────────────────
    //  Cross-encoding consistency: binary b† * b in d=2
    //  should give number operator σ⁺σ⁻ = ½(I − Z)
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``Binary d=2 b†b gives number operator`` () =
        let cr = binaryBosonTerms Raise 0u 1u 2u
        let an = binaryBosonTerms Lower 0u 1u 2u
        let product = cr * an
        // b†b = σ⁺σ⁻ = ½(I − Z)
        // So we expect: 0.5 I + (-0.5) Z
        match product.["I"], product.["Z"] with
        | (true, iReg), (true, zReg) ->
            Assert.True((iReg.Coefficient - Complex(0.5, 0.)).Magnitude < 1e-10)
            Assert.True((zReg.Coefficient - Complex(-0.5, 0.)).Magnitude < 1e-10)
        | _ -> Assert.Fail "Expected I and Z terms"
        // Should have exactly 2 terms
        product |> assertTermCount 2

    // ──────────────────────────────────────────────────
    //  Embedding: matrix embed + decompose roundtrip
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``embedMatrix with identity mapping preserves matrix`` () =
        let m = bosonicCreationMatrix 2
        let embedded = embedMatrix m 1 binaryBasisMap
        for i in 0 .. 1 do
            for j in 0 .. 1 do
                Assert.True(
                    (m.[i, j] - embedded.[i, j]).Magnitude < 1e-14,
                    sprintf "Mismatch at [%d,%d]" i j)

    [<Fact>]
    let ``embedMatrix with Gray mapping for d=4 rearranges entries`` () =
        let m = bosonicCreationMatrix 4
        let embedded = embedMatrix m 2 grayCodeBasisMap
        // Gray mapping: 0→0, 1→1, 2→3, 3→2
        // So b†[1,0] = √1 should appear at embedded[Gray(1), Gray(0)] = embedded[1, 0]
        Assert.True((embedded.[1, 0] - Complex(1., 0.)).Magnitude < 1e-14)
        // b†[2,1] = √2 should appear at embedded[Gray(2), Gray(1)] = embedded[3, 1]
        Assert.True((embedded.[3, 1] - Complex(sqrt 2., 0.)).Magnitude < 1e-14)
        // b†[3,2] = √3 should appear at embedded[Gray(3), Gray(2)] = embedded[2, 3]
        Assert.True((embedded.[2, 3] - Complex(sqrt 3., 0.)).Magnitude < 1e-14)

    // ──────────────────────────────────────────────────
    //  Multi-mode binary: 2 modes, d=2
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``Binary d=2 two modes have disjoint qubit support`` () =
        let cr0 = binaryBosonTerms Raise 0u 2u 2u
        let cr1 = binaryBosonTerms Raise 1u 2u 2u
        // 2 modes × 1 qubit/mode = 2 total qubits
        Assert.Equal<int[]>([| 2 |], allRegisterWidths cr0)
        Assert.Equal<int[]>([| 2 |], allRegisterWidths cr1)
        // Mode 0 terms should have identity on qubit 1 (signatures like "XI", "YI")
        for t in cr0.SummandTerms do
            Assert.Equal('I', t.Signature.[1])
        // Mode 1 terms should have identity on qubit 0 (signatures like "IX", "IY")
        for t in cr1.SummandTerms do
            Assert.Equal('I', t.Signature.[0])
