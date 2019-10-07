namespace Tests

open Encodings
open Xunit
open FsCheck.Xunit

[<Properties (Arbitrary = [|typeof<ComplexGenerator>|]) >]
module Terms_SIxWkOp =
    [<Theory>]
    [<InlineData("{[(R,1)|(R,2)]}", true)>]
    [<InlineData("{[(L,1)|(L,2)]}", true)>]
    [<InlineData("{[(R,1)|(L,2)]}", true)>]
    [<InlineData("{[(L,1)|(R,2)]}", false)>]
    [<InlineData("{[(R,1)|(L,1)|(R,2)]}", false)>]
    [<InlineData("{[(R,1)|(R,1)|(L,1)|(L,1)]}", true)>]
    [<InlineData("{[(R,1)|(R,2)]       ; [(R,1)|(R,1)|(L,1)|(L,1)]}", true)>]
    [<InlineData("{[(R,1)|(R,2)]       ; [(R,1)|(L,1)|(R,2)]}", false)>]
    [<InlineData("{[(R,1)|(L,1)|(R,2)] ; [(R,1)|(L,2)]}", false)>]
    [<InlineData("{[(R,1)|(L,1)|(R,2)] ; [(L,1)|(R,2)]}", false)>]
    let ``S InNormalOrder is computed correctly``(input, expected) =
        match SIxWkOpFromString FermionicOperator.FromString input with
        | Some sixop -> Assert.Equal(expected, sixop.AllTermsNormalOrdered.Value)
        | None -> Assert.True (false)

    [<Theory>]
    [<InlineData("{[(R,1)|(R,2)]}", true)>]
    [<InlineData("{[(R,2)|(R,1)]}", false)>]
    [<InlineData("{[(L,1)|(L,2)]}", false)>]
    [<InlineData("{[(L,2)|(L,1)]}", true)>]
    [<InlineData("{[(R,1)|(L,2)]}", true)>]
    [<InlineData("{[(L,1)|(R,2)]}", false)>]
    [<InlineData("{[(R,1)|(L,1)|(R,2)]}", false)>]
    [<InlineData("{[(R,1)|(R,1)|(L,1)|(L,1)]}", true)>]
    [<InlineData("{[(R,1)|(R,2)|(L,2)|(L,1)]}", true)>]
    [<InlineData("{[(R,2)|(R,1)|(L,2)|(L,1)]}", false)>]
    [<InlineData("{[(R,1)|(R,2)|(L,1)|(L,2)]}", false)>]
    [<InlineData("{[(R,1)|(R,1)|(L,1)|(L,1)] ; [(R,1)|(R,2)]}", true)>]
    [<InlineData("{[(R,1)|(R,1)|(L,1)|(L,1)] ; [(L,1)|(L,2)]}", false)>]
    [<InlineData("{[(R,2)|(R,1)|(L,2)|(L,1)] ; [(R,1)|(R,2)]}", false)>]
    [<InlineData("{[(R,2)|(R,1)|(L,2)|(L,1)] ; [(L,1)|(R,2)]}", false)>]
    let ``S InIndexOrder is computed correctly``(input, expected) =
        match SIxWkOpFromString FermionicOperator.FromString input with
        | Some sixop -> Assert.Equal(expected, sixop.AllTermsIndexOrdered.Value)
        | None -> Assert.True (false)


    [<Theory>]
    [<InlineData("{[(R,1)|(R,2)]}", "{[(R,1)|(R,2)]}")>]
    [<InlineData("{[(R,2)|(R,1)]}", "{[(R,2)|(R,1)]}")>]
    [<InlineData("{[(L,1)|(L,2)]}", "{[(L,1)|(L,2)]}")>]
    [<InlineData("{[(L,2)|(L,1)]}", "{[(L,2)|(L,1)]}")>]
    [<InlineData("{[(R,1)|(L,2)]}", "{[(R,1)|(L,2)]}")>]
    [<InlineData("{[(L,1)|(R,2)]}", "{-[(R,2)|(L,1)]}")>]
    [<InlineData("{[(L,1)|(R,1)]}", "{[1];-[(R,1)|(L,1)]}")>]
    [<InlineData("{[(L,1)|(R,1)|(R,2)]}", "{[(R,1)|(R,2)|(L,1)];[(R,2)]}")>]
    [<InlineData("{[(R,3)|(L,1)|(R,1)|(R,2)]}", "{[(R,3)|(R,1)|(R,2)|(L,1)];[(R,3)|(R,2)]}")>]
    [<InlineData("{[(L,1)|(R,1)|(R,2)|(R,3)|(R,4)]}", "{[(R,1)|(R,2)|(R,3)|(R,4)|(L,1)];[(R,2)|(R,3)|(R,4)]}")>]
    [<InlineData("{[(L,1)|(L,2)|(R,1)|(R,2)]}", "{-[1];[(R,1)|(L,1)];[(R,1)|(R,2)|(L,1)|(L,2)];[(R,2)|(L,2)]}")>]
    [<InlineData("{[(L,1)|(L,2)|(R,1)|(R,2)];[(R,3)|(L,1)|(R,1)|(R,2)]}", "{-[1];[(R,1)|(L,1)];[(R,1)|(R,2)|(L,1)|(L,2)];[(R,2)|(L,2)];[(R,3)|(R,1)|(R,2)|(L,1)];[(R,3)|(R,2)]}")>]
    [<InlineData("{[(L,1)|(R,1)];[(L,2)|(R,2)]}", "{(2)[1];-[(R,1)|(L,1)];-[(R,2)|(L,2)]}")>]
    [<InlineData("{[(L,1)|(R,1)];[(L,1)|(R,1)]}", "{(2)[1];(-2)[(R,1)|(L,1)]}")>]
    let ``S ToNormalOrder is computed correctly`` (input, expected) =
        match SIxWkOpFromString FermionicOperator.FromString input with
        | Some sixop -> Assert.Equal(expected, prettyPrintSIxWkOp (sixop.ToNormalOrder.Value) |> shrinkString)
        | None -> Assert.True (false)