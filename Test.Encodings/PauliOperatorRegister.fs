namespace Tests

module PauliOperatorRegister =
    open Encodings
    open Xunit
    open System.Numerics

    [<Fact>]
    let ``OperatorRegister : default register is all identities``() =
        let reg = PauliOperatorRegister.LittleEndianRegister (4u)
        Assert.Equal("1.0 IIII", reg.ToString())
        let reg = PauliOperatorRegister.BigEndianRegister (4u)
        Assert.Equal("1.0 IIII", reg.ToString())

    [<Fact>]
    let ``OperatorRegister : LittleEndian register can be accessed correctly``() =
        let reg = PauliOperatorRegister.LittleEndianRegister (4u)
        do reg.[0] <- X
        Assert.Equal(Some X, reg.[0])
        Assert.Equal("1.0 IIIX", reg.ToString())

    [<Fact>]
    let ``OperatorRegister : BigEndian register can be accessed correctly``() =
        let reg = PauliOperatorRegister.BigEndianRegister (4u)
        do reg.[0] <- X
        Assert.Equal(Some X, reg.[0])
        Assert.Equal("1.0 XIII", reg.ToString())

    [<Theory>]
    [<InlineData("IIIIII")>]
    [<InlineData("IIII")>]
    [<InlineData("III")>]
    [<InlineData("IIIIIIIIII")>]
    [<InlineData("IIIX")>]
    [<InlineData("IIXI")>]
    [<InlineData("IXII")>]
    [<InlineData("XIII")>]
    let ``OperatorRegister : FromString creates a round-trippable register``(s : string) =
        let reg = PauliOperatorRegister.FromString (s, Complex.One)
        let expectedPhase = Complex.One.PhasePrefix
        Assert.Equal(sprintf "%s%s" expectedPhase s, reg.ToString())

    [<Theory>]
    [<InlineData("IIIX", 3)>]
    [<InlineData("IIXI", 2)>]
    [<InlineData("IXII", 1)>]
    [<InlineData("XIII", 0)>]
    let ``OperatorRegister : FromString creates a BigEndian register``(s : string, index) =
        let reg = PauliOperatorRegister.FromString (s, Complex.One)
        Assert.Equal(Some X, reg.[index])

    [<Theory>]
    [<InlineData("IIII",     "", "1.0 ",      "IIII")>]
    [<InlineData("IIII", "IIII", "1.0 ",      "IIII")>]
    [<InlineData("IIII", "IIIX", "1.0 ",      "IIIX")>]
    [<InlineData("IIII", "XIII", "1.0 ",      "XIII")>]
    [<InlineData("XIII", "YIII", "(1.0 i) ",  "ZIII")>]
    [<InlineData("XXII", "YYII", "-1.0 ",     "ZZII")>]
    [<InlineData("XXIZ", "YYII", "-1.0 ",     "ZZIZ")>]
    [<InlineData("XXYI", "YYZI", "(-1.0 i) ", "ZZXI")>]
    [<InlineData("XXYZ", "YYZX", "1.0 ",      "ZZXY")>]
    let ``OperatorRegister : Basic multiply two registers`` (l, r, expectedPhase, expectedRegister) =
        let l_reg = PauliOperatorRegister.FromString (l, Complex.One)
        let r_reg = PauliOperatorRegister.FromString (r, Complex.One)
        let result = l_reg * r_reg
        Assert.Equal(expectedPhase, result.GlobalPhase.PhasePrefix)
        Assert.Equal(sprintf "%s%s" expectedPhase expectedRegister, result.ToString())
