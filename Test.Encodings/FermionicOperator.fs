namespace Tests

module FermionicOperatorProductTerm =
    open Encodings
    open Xunit
    open FsCheck.Xunit

    [<Theory>]
    [<InlineData("[(u, 1)]",     "[(u, 1)]")>]
    [<InlineData("[(d, 0)]",     "[(d, 0)]")>]
    [<InlineData("[(u, 1.0)]",   "[]")>]
    [<InlineData("[(w, 0)",      "[]")>]
    [<InlineData("[(u, -1)",     "[]")>]
    [<InlineData("[(u, -1.)",    "[]")>]
    [<InlineData("{}",           "[]")>]
    [<InlineData("",             "[]")>]
    [<InlineData("[(u, 1) | (u, 2) | (d, 3) | (d, 2)]", "[(u, 1) | (u, 2) | (d, 3) | (d, 2)]")>]
    let ``FromString creates a round-trippable ladder operator``(input : string, expected : string) =
        match FermionicOperatorProductTerm.TryCreateFromString input with
        | Some l -> Assert.Equal(expected, l.ToString())
        | None   -> Assert.Equal(expected, "")

    [<Property>]
    let ``Synthesized ladder operators have the right readable representation`` (units : (bool * uint32) []) =
        let actual = FermionicOperatorProductTerm.FromUnits units

        let expected =
            units
            |> Array.map (fun (isRaisingOperator, index) ->
                sprintf "(%s, %u)" (if isRaisingOperator then "u" else "d") index)
            |> (fun rg -> System.String.Join (" | ", rg))
            |> sprintf "[%s]"

        Assert.Equal (expected, actual.ToString())

    [<Property>]
    let ``IsInIndexOrder is computed correctly for Raise operators`` (randomIndices : uint32[]) =
        let isSortedAlready =
            randomIndices = (randomIndices |> Array.sort)

        let raiseOperatorWithRandomIndices =
            randomIndices
            |> Array.map (fun index -> (Raise, index))
            |> FermionicOperatorProductTerm.FromTuples
        Assert.Equal (isSortedAlready, raiseOperatorWithRandomIndices.IsInIndexOrder)

        let raiseOperatorWithSortedIndices =
            randomIndices
            |> Array.sort
            |> Array.map (fun index -> (Raise, index))
            |> FermionicOperatorProductTerm.FromTuples
        Assert.Equal (true, raiseOperatorWithSortedIndices.IsInIndexOrder)

    [<Property>]
    let ``IsInIndexOrder is computed correctly for Lower operators`` (randomIndices : uint32[]) =
        let isSortedAlready =
            randomIndices = (randomIndices |> Array.sortDescending)

        let lowerOperatorWithRandomIndices =
            randomIndices
            |> Array.map (fun index -> (Lower, index))
            |> FermionicOperatorProductTerm.FromTuples
        Assert.Equal (isSortedAlready, lowerOperatorWithRandomIndices.IsInIndexOrder)

        let lowerOperatorWithSortedIndices =
            randomIndices
            |> Array.sortDescending
            |> Array.map (fun index -> (Lower, index))
            |> FermionicOperatorProductTerm.FromTuples
        Assert.Equal (true, lowerOperatorWithSortedIndices.IsInIndexOrder)

    [<Property>]
    let ``Multiplying two ladder operators results in a single ladder operator built by concatenation`` (l : (bool * uint32)[], r : (bool * uint32)[]) =
        let lo = FermionicOperatorProductTerm.FromUnits l
        let ro = FermionicOperatorProductTerm.FromUnits r

        let actual = lo * ro
        let expected = FermionicOperatorProductTerm.FromUnits <| Array.concat [|l ; r|]
        Assert.Equal (expected.ToString(), actual.ToString())