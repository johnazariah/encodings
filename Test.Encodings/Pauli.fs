namespace Tests

module Pauli =
    open Encodings
    open System
    open Xunit
    open FsCheck.Xunit

    [<Property>]
    let ``Pauli : Operator I leaves pauli unchanged`` (v : Pauli) =
        Assert.Equal ((v, P1) , I * v)
        Assert.Equal ((v, P1) , v * I)

    [<Property>]
    let ``Pauli : Operators are their own inverse`` (x : Pauli) =
        Assert.Equal (I, (x * x) |> fst)

    [<Property>]
    let ``Pauli : Operators commute with I and themselves, and anti-commute with others`` (l : Pauli, r : Pauli) =
        match (l, r) with
        | (I, _)
        | (_, I)
        | (X, X)
        | (Y, Y)
        | (Z, Z) ->
            Assert.Equal (l * r, r * l)
        | (_, _) ->
            let (op, phase) = l * r
            let expected = (op, phase * M1)
            Assert.Equal (expected, r * l)

    [<Fact>]
    let ``Pauli : X * Y -> iZ`` () =
        let expected = (Z, Pi)
        let actual   = (X * Y)
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)

    [<Fact>]
    let ``Pauli : Y * Z -> iX`` () =
        let expected = (X, Pi)
        let actual   = Y * Z
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)

    [<Fact>]
    let ``Pauli : Z * X -> iY`` () =
        let expected = (Y, Pi)
        let actual   = Z * X
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)
