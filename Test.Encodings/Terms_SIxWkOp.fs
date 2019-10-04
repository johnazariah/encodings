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
