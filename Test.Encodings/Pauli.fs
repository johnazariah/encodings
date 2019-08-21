namespace Tests

module Pauli =
    open Encodings
    open System
    open Xunit
    open FsCheck.Xunit

    [<Property>]
    let ``Pauli : Operator I leaves pauli unchanged`` (v : PauliOperator) =
        Assert.Equal (v, PauliOperator.Unity * v)
        Assert.Equal (v, v * PauliOperator.Unity)

    [<Property>]
    let ``Pauli : Operators are their own inverse`` (x : PauliOperator) =
        Assert.Equal (I, (x * x).Op)

    [<Property>]
    let ``Pauli : Operators commute with I and themselves, and anti-commute with others`` (l : PauliOperator, r : PauliOperator) =
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
        let expected = { PauliOperator.Unity with Op = Z; Ph = Pi}
        let actual   = { PauliOperator.Unity with Op = X } * { PauliOperator.Unity with Op = Y }
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)

    [<Fact>]
    let ``Pauli : Y * Z -> iX`` () =
        let expected = { PauliOperator.Unity with Op = X; Ph = Pi}
        let actual   = { PauliOperator.Unity with Op = Y } * { PauliOperator.Unity with Op = Z }
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)

    [<Fact>]
    let ``Pauli : Z * X -> iY`` () =
        let expected = { PauliOperator.Unity with Op = Y; Ph = Pi}
        let actual   = { PauliOperator.Unity with Op = Z } * { PauliOperator.Unity with Op = X }
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)
