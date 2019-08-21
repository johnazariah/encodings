namespace Tests

module Pauli =
    open Encodings
    open System
    open Xunit
    open FsCheck.Xunit

    [<Property>]
    let ``Pauli : Operator I leaves pauli unchanged`` (v : Pauli) =
        Assert.Equal (v, Pauli.Unity * v)
        Assert.Equal (v, v * Pauli.Unity)

    [<Property>]
    let ``Pauli : Operators are their own inverse`` (x : Pauli) =
        Assert.Equal (I, (x * x).Op)

    [<Property>]
    let ``Pauli : Operators commute with I and themselves, and anti-commute with others`` (l : Pauli, r : Pauli) =
        match (l.Op, r.Op) with
        | (I, _)
        | (_, I)
        | (X, X)
        | (Y, Y)
        | (Z, Z) ->
            Assert.Equal (l * r, r * l)
        | (_, _) ->
            let fwd = l * r
            let expected = { fwd with Ph = fwd.Ph * M1 }
            Assert.Equal (expected, r * l)

    [<Fact>]
    let ``Pauli : X * Y -> iZ`` () =
        let expected = { Pauli.Unity with Op = Z; Ph = Pi}
        let actual   = { Pauli.Unity with Op = X } * { Pauli.Unity with Op = Y }
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)

    [<Fact>]
    let ``Pauli : Y * Z -> iX`` () =
        let expected = { Pauli.Unity with Op = X; Ph = Pi}
        let actual   = { Pauli.Unity with Op = Y } * { Pauli.Unity with Op = Z }
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)

    [<Fact>]
    let ``Pauli : Z * X -> iY`` () =
        let expected = { Pauli.Unity with Op = Y; Ph = Pi}
        let actual   = { Pauli.Unity with Op = Z } * { Pauli.Unity with Op = X }
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)

    [<Property>]
    let ``Pauli : Normalized Paulis have P1 phase and fixed up coefficient`` (x : Pauli) =
        Assert.Equal (P1, x.Normalized.Ph)

        let f = x.Normalized.Cf
        let c = x.Cf
        match (x.Ph.IsPositive, x.Ph.IsComplex) with
        | true, false ->
            Assert.Equal(f, c)
        | false, false ->
            Assert.Equal(f, -c)
        | true, true ->
            Assert.Equal(f, c.TimesI)
        | false, true ->
            Assert.Equal(f, -c.TimesI)