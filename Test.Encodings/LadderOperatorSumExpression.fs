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