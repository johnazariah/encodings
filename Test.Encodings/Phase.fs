namespace Tests

module Phase =
    open Encodings
    open System
    open Xunit
    open FsCheck.Xunit

    [<Property>]
    let ``Phase : P1 leaves phase unchanged`` (v : Phase) =
        Assert.Equal (v, P1 * v)
        Assert.Equal (v, v * P1)

    [<Property>]
    let ``Phase : M1 flips sign`` (v : Phase) =
        let op = M1
        Assert.Equal (not v.IsPositive, (op * v).IsPositive)
        Assert.Equal (not v.IsPositive, (v * op).IsPositive)

    [<Property>]
    let ``Phase : M1 leaves complex unchanged`` (v : Phase) =
        let op = M1
        Assert.Equal (v.IsComplex, (op * v).IsComplex)
        Assert.Equal (v.IsComplex, (v * op).IsComplex)

    [<Property>]
    let ``Phase : Pi flips complex and maybe sign`` (v : Phase) =
        let op = Pi
        Assert.Equal (not v.IsComplex, (op * v).IsComplex)
        Assert.Equal (not v.IsComplex, (v * op).IsComplex)
        match v.IsComplex with
        | false ->
            Assert.Equal (v.IsPositive, (op * v).IsPositive)
            Assert.Equal (v.IsPositive, (v * op).IsPositive)
        | true ->
            Assert.Equal (not v.IsPositive, (op * v).IsPositive)
            Assert.Equal (not v.IsPositive, (v * op).IsPositive)

    [<Property>]
    let ``Phase : Mi flips complex and maybe sign`` (v : Phase) =
        let op = Mi
        Assert.Equal (not v.IsComplex, (op * v).IsComplex)
        Assert.Equal (not v.IsComplex, (v * op).IsComplex)
        match v.IsComplex with
        | true ->
            Assert.Equal (v.IsPositive, (op * v).IsPositive)
            Assert.Equal (v.IsPositive, (v * op).IsPositive)
        | false ->
            Assert.Equal (not v.IsPositive, (op * v).IsPositive)
            Assert.Equal (not v.IsPositive, (v * op).IsPositive)

    [<Property>]
    let ``Phase : can fold into coefficient`` (p : Phase, c : Complex) =
        let f = c * p
        match (p.IsPositive, p.IsComplex) with
        | true, false ->
            Assert.Equal(f, c)
        | false, false ->
            Assert.Equal(f, -c)
        | true, true ->
            Assert.Equal(f, c.TimesI)
        | false, true ->
            Assert.Equal(f, -c.TimesI)

