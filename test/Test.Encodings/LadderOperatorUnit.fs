namespace Tests

module LadderOperatorUnit =
    open Encodings
    open Xunit
    open FsCheck.Xunit

    [<Theory>]
    [<InlineData("(u, 1)",     true)>]
    [<InlineData("(d, 0)",     true)>]
    [<InlineData("(u, 1.0)",   false)>]
    [<InlineData("(w, 0)",     false)>]
    [<InlineData("(u, -1)",    false)>]
    [<InlineData("(u, -1.)",   false)>]
    let ``FromString creates a round-trippable ladder operator``(expected : string, shouldSucceed) =
        match (shouldSucceed, LadderOperatorUnit.TryCreateFromString expected) with
        | true,  Some l -> Assert.Equal(expected, l.ToString())
        | false, None   -> Assert.True (true)
        | _             -> Assert.True (false)

    [<Property>]
    let ``Synthesized ladder operator units have the right readable representation`` (isRaisingOperator : bool, index : uint32) =
        let actual = LadderOperatorUnit.FromUnit (isRaisingOperator, index)

        let expected = sprintf "(%s, %u)" (if isRaisingOperator then "u" else "d") index
        Assert.Equal (expected, actual.ToString())

    [<Theory>]
    [<InlineData("I", true)>]
    [<InlineData("u", true)>]
    [<InlineData("d", true)>]
    [<InlineData("x", false)>]
    let ``Apply parses known ladder operator symbols`` (input : string, shouldSucceed : bool) =
        let parsed = LadderOperatorUnit.Apply input
        Assert.Equal(shouldSucceed, parsed.IsSome)

    [<Fact>]
    let ``FromTuple keeps operator and index`` () =
        let actual = LadderOperatorUnit.FromTuple (Lower, 9u)
        Assert.Equal("(d, 9)", actual.ToString())