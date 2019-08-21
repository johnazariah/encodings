namespace Tests

module Pauli =
    open Encodings
    open System
    open Xunit
    open FsCheck.Xunit

    [<Property>]
    let ``Pauli : Operator I leaves pauli unchanged`` (v : Operator) =
        Assert.Equal (v, Operator.Unity * v)
        Assert.Equal (v, v * Operator.Unity)

    [<Property>]
    let ``Pauli : Operators are their own inverse`` (x : Operator) =
        Assert.Equal (I, (x * x).Op)

    [<Property>]
    let ``Pauli : Operators commute with I and themselves, and anti-commute with others`` (l : Operator, r : Operator) =
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
        let expected = { Operator.Unity with Op = Z; Ph = Pi}
        let actual   = { Operator.Unity with Op = X } * { Operator.Unity with Op = Y }
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)

    [<Fact>]
    let ``Pauli : Y * Z -> iX`` () =
        let expected = { Operator.Unity with Op = X; Ph = Pi}
        let actual   = { Operator.Unity with Op = Y } * { Operator.Unity with Op = Z }
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)

    [<Fact>]
    let ``Pauli : Z * X -> iY`` () =
        let expected = { Operator.Unity with Op = Y; Ph = Pi}
        let actual   = { Operator.Unity with Op = Z } * { Operator.Unity with Op = X }
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)
