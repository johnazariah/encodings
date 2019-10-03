namespace Tests

open FsCheck.Xunit


[<Properties (Arbitrary = [|typeof<ComplexGenerator>|], QuietOnSuccess = true) >]
module PauliRegister =
    open Encodings
    open Xunit

    [<Property>]
    let ``Signature is computed correctly``(candidate : R<Pauli>) =
        Assert.Equal(prettyPrintRegister candidate, candidate.Signature)

    [<Fact>]
    let ``Default register is all identities``() =
        let reg = R<Pauli>.New 4u
        Assert.Equal("IIII", prettyPrintRegister reg)

    [<Fact>]
    let ``Register is Big Endian``() =
        let reg = RegisterFromString Pauli.Apply ("XIII")
        Assert.True(reg.IsSome)
        Assert.Equal(Some X, reg.Value.[0])
        Assert.Equal("XIII", prettyPrintRegister reg.Value)

    [<Theory>]
    [<InlineData("IIIIII")>]
    [<InlineData("IIII")>]
    [<InlineData("III")>]
    [<InlineData("IIIIIIIIII")>]
    [<InlineData("IIIX")>]
    [<InlineData("IIXI")>]
    [<InlineData("IXII")>]
    [<InlineData("XIII")>]
    let ``RegisterFromString creates a round-trippable register``(s : string) =
        let reg = RegisterFromString Pauli.Apply s
        Assert.True(reg.IsSome)
        Assert.Equal(s, prettyPrintRegister reg.Value)

    [<Theory>]
    [<InlineData("IIIX", 3)>]
    [<InlineData("IIXI", 2)>]
    [<InlineData("IXII", 1)>]
    [<InlineData("XIII", 0)>]
    let ``RegisterFromString creates a BigEndian register``(s : string, index) =
        let reg = RegisterFromString Pauli.Apply s
        Assert.True(reg.IsSome)
        Assert.Equal(Some X, reg.Value.[index])

    [<Theory>]
    [<InlineData("IIII",     "", "",     "IIII")>]
    [<InlineData("IIII", "IIII", "",     "IIII")>]
    [<InlineData("IIII", "IIIX", "",     "IIIX")>]
    [<InlineData("IIII", "XIII", "",     "XIII")>]
    [<InlineData("XIII", "YIII", "( i)", "ZIII")>]
    [<InlineData("XXII", "YYII", " -",   "ZZII")>]
    [<InlineData("XXIZ", "YYII", " -",   "ZZIZ")>]
    [<InlineData("XXYI", "YYZI", "(-i)", "ZZXI")>]
    [<InlineData("XXYZ", "YYZX", "",     "ZZXY")>]
    let ``PauliRegister * PauliRegister -> PauliRegister : phases and values computed correctly`` (l : string, r : string, expectedPhase, expectedRegister) =
        let l_reg = RegisterFromString Pauli.Apply l
        let r_reg = RegisterFromString Pauli.Apply r
        Assert.True(l_reg.IsSome)
        Assert.True(r_reg.IsSome)
        let result = l_reg.Value <*> r_reg.Value
        Assert.Equal(expectedPhase, prettyPrintPhase result.Unapply.Coeff)
        Assert.Equal(expectedRegister, prettyPrintRegister result)