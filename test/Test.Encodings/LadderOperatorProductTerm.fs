namespace Tests

module LadderOperatorProductTerm =
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
        match LadderOperatorProductTerm.TryCreateFromString input with
        | Some l -> Assert.Equal(expected, l.ToString())
        | None   -> Assert.Equal(expected, "")

    [<Property>]
    let ``Synthesized ladder operators have the right readable representation`` (units : (bool * uint32) []) =
        let actual = LadderOperatorProductTerm.FromUnits units

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
            |> LadderOperatorProductTerm.FromTuples
        Assert.Equal (isSortedAlready, raiseOperatorWithRandomIndices.IsInIndexOrder)

        let raiseOperatorWithSortedIndices =
            randomIndices
            |> Array.sort
            |> Array.map (fun index -> (Raise, index))
            |> LadderOperatorProductTerm.FromTuples
        Assert.Equal (true, raiseOperatorWithSortedIndices.IsInIndexOrder)

    [<Property>]
    let ``IsInIndexOrder is computed correctly for Lower operators`` (randomIndices : uint32[]) =
        let isSortedAlready =
            randomIndices = (randomIndices |> Array.sortDescending)

        let lowerOperatorWithRandomIndices =
            randomIndices
            |> Array.map (fun index -> (Lower, index))
            |> LadderOperatorProductTerm.FromTuples
        Assert.Equal (isSortedAlready, lowerOperatorWithRandomIndices.IsInIndexOrder)

        let lowerOperatorWithSortedIndices =
            randomIndices
            |> Array.sortDescending
            |> Array.map (fun index -> (Lower, index))
            |> LadderOperatorProductTerm.FromTuples
        Assert.Equal (true, lowerOperatorWithSortedIndices.IsInIndexOrder)

    [<Property>]
    let ``Multiplying two ladder operators results in a single ladder operator built by concatenation`` (l : (bool * uint32)[], r : (bool * uint32)[]) =
        let lo = LadderOperatorProductTerm.FromUnits l
        let ro = LadderOperatorProductTerm.FromUnits r

        let actual = lo * ro
        let expected = LadderOperatorProductTerm.FromUnits <| Array.concat [|l ; r|]
        Assert.Equal (expected.ToString(), actual.ToString())

    [<Fact>]
    let ``LadderOperatorProductTerm exposes units and coefficient`` () =
        let parsed = LadderOperatorProductTerm.TryCreateFromString "[(u, 1) | (d, 0)]"
        Assert.True(parsed.IsSome)

        let term = parsed.Value
        Assert.Equal(System.Numerics.Complex.One, term.Coeff)
        Assert.Equal(2, term.Units.Length)

    [<Fact>]
    let ``LadderOperatorProductTerm Reduce keeps canonical form`` () =
        let term = LadderOperatorProductTerm.FromTuples [| (Raise, 2u); (Lower, 1u) |]
        let reduced = term.Reduce.Value

        Assert.Equal(System.Numerics.Complex.One, reduced.Coeff)
        Assert.Equal(2, reduced.Units.Length)

    [<Fact>]
    let ``LadderOperatorProductTerm detects normal ordering`` () =
        let term = LadderOperatorProductTerm.FromTuples [| (Raise, 0u); (Raise, 2u); (Lower, 3u) |]
        Assert.True(term.IsInNormalOrder)

    [<Fact>]
    let ``LadderOperatorProductTerm detects non-normal ordering`` () =
        let term = LadderOperatorProductTerm.FromTuples [| (Lower, 1u); (Raise, 0u) |]
        Assert.False(term.IsInNormalOrder)
    //let ``FromString creates a round-trippable ladder operator``(input : string, expected : string) =
    //    match LadderOperatorProductTerm.TryCreateFromString input with
    //    | Some l -> Assert.Equal(expected, l.ToString())
    //    | None   -> Assert.Equal(expected, "")
