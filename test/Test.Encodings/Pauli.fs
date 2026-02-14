namespace Tests

module Pauli =
    open Encodings
    open System
    open Xunit
    open FsCheck.Xunit

    [<Property>]
    let ``Operator I leaves pauli unchanged`` (v : Pauli) =
        Assert.Equal ((v, P1) , I * v)
        Assert.Equal ((v, P1) , v * I)

    [<Property>]
    let ``Operators are their own inverse`` (x : Pauli) =
        Assert.Equal (I, (x * x) |> fst)

    [<Property>]
    let ``Operators commute with I and themselves, and anti-commute with others`` (l : Pauli, r : Pauli) =
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
    let ``X * Y -> iZ`` () =
        let expected = (Z, Pi)
        let actual   = (X * Y)
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)

    [<Fact>]
    let ``Y * Z -> iX`` () =
        let expected = (X, Pi)
        let actual   = Y * Z
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)

    [<Fact>]
    let ``Z * X -> iY`` () =
        let expected = (Y, Pi)
        let actual   = Z * X
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)

    [<Theory>]
    [<InlineData("I", true)>]
    [<InlineData("X", true)>]
    [<InlineData("Y", true)>]
    [<InlineData("Z", true)>]
    [<InlineData("Q", false)>]
    let ``Apply parses valid Pauli labels`` (input : string, shouldSucceed : bool) =
        let parsed = Pauli.Apply input
        Assert.Equal(shouldSucceed, parsed.IsSome)

    [<Theory>]
    [<InlineData('I', true)>]
    [<InlineData('X', true)>]
    [<InlineData('Y', true)>]
    [<InlineData('Z', true)>]
    [<InlineData('q', false)>]
    let ``FromChar parses valid Pauli labels`` (input : char, shouldSucceed : bool) =
        let parsed = Pauli.FromChar input
        Assert.Equal(shouldSucceed, parsed.IsSome)

    [<Fact>]
    let ``Pauli multiplication table is closed and complete`` () =
        let all = [| I; X; Y; Z |]

        for left in all do
            for right in all do
                let (op, phase) = left * right
                Assert.Contains(op, all)
                Assert.Contains(phase, [| P1; M1; Pi; Mi |])
