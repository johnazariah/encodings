namespace Tests

module PauliRegister =
    open Encodings
    open Xunit
    open System.Numerics

    [<Fact>]
    let ``Default register is all identities``() =
        let reg = PauliRegister(4u)
        Assert.Equal("IIII", reg.ToString())

    [<Fact>]
    let ``Register is Big Endian``() =
        let reg = PauliRegister(4u).WithOperatorAt 0 X
        Assert.Equal(Some X, reg.[0])
        Assert.Equal("XIII", reg.ToString())

    [<Theory>]
    [<InlineData("IIIIII")>]
    [<InlineData("IIII")>]
    [<InlineData("III")>]
    [<InlineData("IIIIIIIIII")>]
    [<InlineData("IIIX")>]
    [<InlineData("IIXI")>]
    [<InlineData("IXII")>]
    [<InlineData("XIII")>]
    let ``FromString creates a round-trippable register``(s : string) =
        let reg = PauliRegister (s, Complex.One)
        Assert.Equal(s, reg.ToString())

    [<Theory>]
    [<InlineData("IIIX", 3)>]
    [<InlineData("IIXI", 2)>]
    [<InlineData("IXII", 1)>]
    [<InlineData("XIII", 0)>]
    let ``FromString creates a BigEndian register``(s : string, index) =
        let reg = PauliRegister (s, Complex.One)
        Assert.Equal(Some X, reg.[index])

    [<Theory>]
    [<InlineData("IIII",     "", "",      "IIII")>]
    [<InlineData("IIII", "IIII", "",      "IIII")>]
    [<InlineData("IIII", "IIIX", "",      "IIIX")>]
    [<InlineData("IIII", "XIII", "",      "XIII")>]
    [<InlineData("XIII", "YIII", "( i) ", "ZIII")>]
    [<InlineData("XXII", "YYII", " -",    "ZZII")>]
    [<InlineData("XXIZ", "YYII", " -",    "ZZIZ")>]
    [<InlineData("XXYI", "YYZI", "(-i) ", "ZZXI")>]
    [<InlineData("XXYZ", "YYZX", "",      "ZZXY")>]
    let ``Two registers can be multiplied`` (l : string, r : string, expectedPhase, expectedRegister) =
        let l_reg = PauliRegister (l, Complex.One)
        let r_reg = PauliRegister (r, Complex.One)
        let result = l_reg * r_reg
        Assert.Equal(expectedPhase, result.PhasePrefix)
        Assert.Equal(sprintf "%s%s" expectedPhase expectedRegister, result.ToString())

    [<Fact>]
    let ``Item returns None for invalid indices`` () =
        let reg = PauliRegister(3u)
        Assert.Equal(None, reg.[-1])
        Assert.Equal(None, reg.[3])

    [<Fact>]
    let ``WithOperatorAt ignores out-of-range indices`` () =
        let reg = PauliRegister(3u)
        let unchangedLow = reg.WithOperatorAt -1 X
        let unchangedHigh = reg.WithOperatorAt 3 Z

        Assert.Equal("III", unchangedLow.Signature)
        Assert.Equal("III", unchangedHigh.Signature)

    [<Fact>]
    let ``Constructor with coefficient sets phase prefix`` () =
        let reg = PauliRegister(2u, Complex.MinusOne)
        Assert.Equal(" -", reg.PhasePrefix)
        Assert.Equal(" -II", reg.ToString())

    [<Fact>]
    let ``Sequence from array combines like terms`` () =
        let x1 = PauliRegister("XI", Complex(2.0, 0.0))
        let x2 = PauliRegister("XI", Complex(3.0, 0.0))
        let seq = PauliRegisterSequence [| x1; x2 |]

        Assert.Equal(1, seq.SummandTerms.Length)
        let found, term = seq.["XI"]
        Assert.True(found)
        Assert.Equal(Complex(5.0, 0.0), term.Coefficient)

    [<Fact>]
    let ``Sequence removes terms that cancel to zero`` () =
        let x1 = PauliRegister("XI", Complex(2.0, 0.0))
        let x2 = PauliRegister("XI", Complex(-2.0, 0.0))
        let seq = PauliRegisterSequence [| x1; x2 |]

        Assert.Empty(seq.SummandTerms)

    [<Fact>]
    let ``Sequence constructor from sequences distributes coefficients`` () =
        let baseTerm = PauliRegister("ZI", Complex.One)
        let seqA = PauliRegisterSequence [| baseTerm.ResetPhase(Complex(2.0, 0.0)) |]
        let seqB = PauliRegisterSequence [| baseTerm.ResetPhase(Complex(3.0, 0.0)) |]
        let combined = PauliRegisterSequence [| seqA; seqB |]

        let found, term = combined.["ZI"]
        Assert.True(found)
        Assert.Equal(Complex(5.0, 0.0), term.Coefficient)

    [<Fact>]
    let ``DistributeCoefficient multiplies all summand coefficients`` () =
        let baseSeq =
            PauliRegisterSequence [|
                PauliRegister("XI", Complex(2.0, 0.0))
                PauliRegister("IZ", Complex(3.0, 0.0))
            |]

        let wrapped = PauliRegisterSequence [| baseSeq; baseSeq |]
        let distributed = wrapped.DistributeCoefficient

        let foundXI, xi = distributed.["XI"]
        let foundIZ, iz = distributed.["IZ"]
        Assert.True(foundXI)
        Assert.True(foundIZ)
        Assert.True(xi.Coefficient.IsNonZero)
        Assert.True(iz.Coefficient.IsNonZero)

    [<Fact>]
    let ``Sequence lookup returns false for missing key`` () =
        let seq = PauliRegisterSequence [| PauliRegister("XI", Complex.One) |]
        let found, _ = seq.["ZZ"]
        Assert.False(found)

    [<Fact>]
    let ``Empty sequence constructors remain empty after distribution and multiplication`` () =
        let empty = PauliRegisterSequence()
        let emptyDistributed = empty.DistributeCoefficient
        let product = empty * empty

        Assert.Empty(empty.SummandTerms)
        Assert.Empty(emptyDistributed.SummandTerms)
        Assert.Empty(product.SummandTerms)

    [<Fact>]
    let ``Register multiplication handles different sizes both directions`` () =
        let leftLonger = PauliRegister("XYZ", Complex.One)
        let rightShorter = PauliRegister("X", Complex.One)
        let product1 = leftLonger * rightShorter
        let product2 = rightShorter * leftLonger

        Assert.Equal(3, product1.Signature.Length)
        Assert.Equal(3, product2.Signature.Length)

    [<Fact>]
    let ``String constructor ignores invalid Pauli characters`` () =
        let reg = PauliRegister("XQZI", Complex.One)
        Assert.Equal("XZI", reg.Signature)
