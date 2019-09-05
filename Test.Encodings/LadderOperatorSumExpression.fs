namespace Tests

module LadderOperatorSumExpression =
    open Encodings
    open Xunit
    open FsCheck.Xunit

    [<Theory>]
    [<InlineData("{[(u, 1)]}",     "{[(u, 1)]}")>]
    [<InlineData("{[(d, 0)]}",     "{[(d, 0)]}")>]
    [<InlineData("{[(u, 1.0)]}",   "{[]}")>]
    [<InlineData("{[(w, 0)}",      "{[]}")>]
    [<InlineData("{[(u, -1)}",     "{[]}")>]
    [<InlineData("{[(u, -1.)}",    "{[]}")>]
    [<InlineData("{}",             "{[]}")>]
    [<InlineData("",               "{[]}")>]
    [<InlineData("{[(u, 1) | (u, 2) | (d, 3)]; [(d, 2)]}", "{[(d, 2)]; [(u, 1) | (u, 2) | (d, 3)]}")>]
    let ``FromString creates a round-trippable sum expression``(input : string, expected : string) =
        match LadderOperatorSumExpression.TryCreateFromString input with
        | Some l -> Assert.Equal(expected, l.ToString())
        | None   -> Assert.Equal(expected, "")

    [<Property>]
    let ``Multiplying two ladder operators results in a single ladder operator built by concatenation``
        (l : (bool * uint32)[], r : (bool * uint32)[]) =
        let lo = LadderOperatorProductTerm.FromUnits l
        let ro = LadderOperatorProductTerm.FromUnits r

        let actual = lo * ro
        let expected = LadderOperatorProductTerm.FromUnits <| Array.concat [|l ; r|]
        Assert.Equal (expected.ToString(), actual.ToString())

    [<Theory>]
    [<InlineData("{[(u, 1)]}", "{[(u, 1)]}")>]
    [<InlineData("{[(d, 0)]}", "{[(d, 0)]}")>]
    [<InlineData("{[(u, 1) | (u, 0)]}", "{[(u, 1) | (u, 0)]}")>]
    [<InlineData("{[(u, 1) | (d, 0)]}", "{[(u, 1) | (d, 0)]}")>]
    [<InlineData("{[(d, 1) | (u, 0)]}", "{[(I, 0) | (u, 0) | (d, 1)]}")>]
    [<InlineData("{[(d, 1) | (u, 1)]}", "{[(I, 0) | (u, 1) | (d, 1)]; [(I, 0)]}")>]
    let ``Can successfully sort fermionic sum-expression to normal order``
        (input : string, expected : string) =
        match LadderOperatorSumExpression.TryCreateFromString input with
        | Some l ->
            NormalOrderedLadderOperatorSumExprNormalOrderedLadderOperatorSumExpr.Construct l
            |> Option.iter (fun nols -> Assert.Equal (expected, nols.ToString()))
            | None -> Assert.Equal(expected, "")