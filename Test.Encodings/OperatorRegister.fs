namespace Tests

module OperatorRegister =
    open Encodings
    open Xunit

    [<Fact>]
    let ``OperatorRegister : default register is all identities``() =
        let reg = OperatorRegister.LittleEndianRegister (4u)
        Assert.Equal("IIII", reg.ToString())
        let reg = OperatorRegister.BigEndianRegister (4u)
        Assert.Equal("IIII", reg.ToString())

    [<Fact>]
    let ``OperatorRegister : LittleEndian register can be accessed correctly``() =
        let reg = OperatorRegister.LittleEndianRegister (4u)
        do reg.[0] <- X
        Assert.Equal(Some X, reg.[0])
        Assert.Equal("IIIX", reg.ToString())

    [<Fact>]
    let ``OperatorRegister : BigEndian register can be accessed correctly``() =
        let reg = OperatorRegister.BigEndianRegister (4u)
        do reg.[0] <- X
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
    let ``OperatorRegister : FromString creates a round-trippable register``(s : string) =
        let reg = OperatorRegister.FromString s
        Assert.Equal(s, reg.ToString())

    [<Theory>]
    [<InlineData("IIIX", 3)>]
    [<InlineData("IIXI", 2)>]
    [<InlineData("IXII", 1)>]
    [<InlineData("XIII", 0)>]
    let ``OperatorRegister : FromString creates a BigEndian register``(s : string, index) =
        let reg = OperatorRegister.FromString s
        Assert.Equal(Some X, reg.[index])

    [<Theory>]
    [<InlineData("IIII",     "", "(1, 0)",  "IIII")>]
    [<InlineData("IIII", "IIII", "(1, 0)",  "IIII")>]
    [<InlineData("IIII", "IIIX", "(1, 0)",  "IIIX")>]
    [<InlineData("IIII", "XIII", "(1, 0)",  "XIII")>]
    [<InlineData("XIII", "YIII", "(0, 1)",  "ZIII")>]
    [<InlineData("XXII", "YYII", "(-1, 0)", "ZZII")>]
    [<InlineData("XXIZ", "YYII", "(-1, 0)", "ZZIZ")>]
    [<InlineData("XXYI", "YYZI", "(0, -1)", "ZZXI")>]
    [<InlineData("XXYZ", "YYZX", "(1, 0)",  "ZZXY")>]
    let ``OperatorRegister : Basic multiply two registers`` (l, r, expectedPhase, expectedRegister) =
        let l_reg = OperatorRegister.FromString l
        let r_reg = OperatorRegister.FromString r
        let result = l_reg * r_reg
        Assert.Equal(expectedPhase, result.GlobalPhase.ToString())
        Assert.Equal(expectedRegister, result.ToString())
