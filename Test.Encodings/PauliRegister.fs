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
