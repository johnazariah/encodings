namespace Tests

open Encodings
open Xunit
open FsCheck.Xunit

[<Properties (Arbitrary = [|typeof<ComplexGenerator>|]) >]
module Terms_PIxWkOp =
    [<Theory>]
    [<InlineData("[(R,1)|(R,2)]", true)>]
    [<InlineData("[(L,1)|(L,2)]", true)>]
    [<InlineData("[(R,1)|(L,2)]", true)>]
    [<InlineData("[(L,1)|(R,2)]", false)>]
    [<InlineData("[(R,1)|(L,1)|(R,2)]", false)>]
    [<InlineData("[(R,1)|(R,1)|(L,1)|(L,1)]", true)>]
    let ``P InNormalOrder is computed correctly``(input, expected) =
        match PIxOpFromString Wick.FromString input with
        | Some pixop ->
            pixop
            |> PIxWkOp.ProductTerm
            |> PIxWkOp<uint32, Wick>.IsInNormalOrder
            |> curry Assert.Equal expected
        | None -> Assert.True (false)

    [<Theory>]
    [<InlineData("[(R,1)|(R,2)]", true)>]
    [<InlineData("[(R,2)|(R,1)]", false)>]
    [<InlineData("[(L,1)|(L,2)]", false)>]
    [<InlineData("[(L,2)|(L,1)]", true)>]
    [<InlineData("[(R,1)|(L,2)]", true)>]
    [<InlineData("[(L,1)|(R,2)]", false)>]
    [<InlineData("[(R,1)|(L,1)|(R,2)]", false)>]
    [<InlineData("[(R,1)|(R,1)|(L,1)|(L,1)]", true)>]
    [<InlineData("[(R,1)|(R,2)|(L,2)|(L,1)]", true)>]
    [<InlineData("[(R,2)|(R,1)|(L,2)|(L,1)]", false)>]
    [<InlineData("[(R,1)|(R,2)|(L,1)|(L,2)]", false)>]
    let ``P InIndexOrder is computed correctly``(input, expected) =
        match PIxOpFromString Wick.FromString input with
        | Some pixop ->
            pixop
            |> PIxWkOp.ProductTerm
            |> PIxWkOp<uint32, Wick>.IsInIndexOrder
            |> curry Assert.Equal expected
        | None -> Assert.True (false)
