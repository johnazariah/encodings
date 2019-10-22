namespace Tests

open Encodings
open Xunit
open FsCheck.Xunit

[<Properties (Arbitrary = [|typeof<ComplexGenerator>|], QuietOnSuccess = true) >]
module Terms_SIxOp =

    [<Theory>]
    [<InlineData("{}", "{}", "{}")>]
    [<InlineData("{[(R,1)|(R,2)];[(L,1)|(L,2)]}", "{}", "{[(L,1)|(L,2)];[(R,1)|(R,2)]}")>]
    [<InlineData("{[(R,1)|(R,2)]}", "{[(R,1)|(R,2)]}", "{(2)[(R,1)|(R,2)]}")>]
    [<InlineData("{[(R,1)|(R,2)]}", "{[(R,2)|(R,1)]}", "{[(R,1)|(R,2)];[(R,2)|(R,1)]}")>]
    [<InlineData("{[(R,1)|(R,2)]}", "{[(L,1)|(L,2)]}", "{[(L,1)|(L,2)];[(R,1)|(R,2)]}")>]
    let ``S + S is computed correctly``(leftStr, rightStr, expected) =
        let left  = SIxOpFromString FermionicOperator.FromString leftStr
        let right = SIxOpFromString FermionicOperator.FromString rightStr
        match (left, right) with
        | Some l, Some r -> Assert.Equal(expected, prettyPrintSIxOp (l <+> r) |> shrinkString)
        | _, _ -> Assert.True (false)


    [<Theory>]
    [<InlineData("{[(R,1)|(R,2)]}", "{[(R,1)|(R,2)]}", "{[(R,1)|(R,2)|(R,1)|(R,2)]}")>]
    [<InlineData("{[(R,1)|(R,2)];[(L,1)|(L,2)]}", "{[(R,1)|(R,2)]}", "{[(L,1)|(L,2)|(R,1)|(R,2)];[(R,1)|(R,2)|(R,1)|(R,2)]}")>]
    [<InlineData("{[(R,1)|(R,2)];[(L,1)|(L,2)]}", "{}", "{[(L,1)|(L,2)];[(R,1)|(R,2)]}")>]
    [<InlineData("{}", "{}", "{}")>]
    let ``S * S is computed correctly``(leftStr, rightStr, expected) =
        let left  = SIxOpFromString FermionicOperator.FromString leftStr
        let right = SIxOpFromString FermionicOperator.FromString rightStr
        match (left, right) with
        | Some l, Some r -> Assert.Equal(expected, prettyPrintSIxOp (l <*> r) |> shrinkString)
        | _, _ -> Assert.True (false)

    [<Theory>]
    [<InlineData("{[(R,1)|(R,2)]}", "{[(R,1)|(R,2)]}")>]
    [<InlineData("{[(R,2)|(R,1)]}", "{-[(R,1)|(R,2)]}")>]
    [<InlineData("{[(L,1)|(L,2)]}", "{-[(L,2)|(L,1)]}")>]
    [<InlineData("{[(L,2)|(L,1)]}", "{[(L,2)|(L,1)]}")>]
    [<InlineData("{[(R,1)|(L,2)]}", "{[(R,1)|(L,2)]}")>]
    [<InlineData("{[(L,1)|(R,2)]}", "{-[(R,2)|(L,1)]}")>]
    [<InlineData("{[(L,1)|(R,1)]}", "{[1];-[(R,1)|(L,1)]}")>]
    [<InlineData("{[(L,1)|(R,1)|(R,2)]}", "{[(R,1)|(R,2)|(L,1)];[(R,2)]}")>]
    [<InlineData("{[(R,3)|(L,1)|(R,1)|(R,2)]}", "{[(R,1)|(R,2)|(R,3)|(L,1)];-[(R,2)|(R,3)]}")>]
    [<InlineData("{[(L,1)|(R,1)|(R,2)|(R,3)|(R,4)]}", "{[(R,1)|(R,2)|(R,3)|(R,4)|(L,1)];[(R,2)|(R,3)|(R,4)]}")>]
    [<InlineData("{[(L,1)|(L,2)|(R,1)|(R,2)]}", "{-[1];[(R,1)|(L,1)];-[(R,1)|(R,2)|(L,2)|(L,1)];[(R,2)|(L,2)]}")>]
    [<InlineData("{[(L,1)|(L,2)|(R,1)|(R,2)];[(R,3)|(L,1)|(R,1)|(R,2)]}", "{-[1];[(R,1)|(L,1)];-[(R,1)|(R,2)|(L,2)|(L,1)];[(R,1)|(R,2)|(R,3)|(L,1)];[(R,2)|(L,2)];-[(R,2)|(R,3)]}")>]
    [<InlineData("{[(L,1)|(R,1)];[(L,2)|(R,2)]}", "{(2)[1];-[(R,1)|(L,1)];-[(R,2)|(L,2)]}")>]
    [<InlineData("{[(L,1)|(R,1)];[(L,1)|(R,1)]}", "{(2)[1];(-2)[(R,1)|(L,1)]}")>]
    let ``S ToCanonicalOrder is computed correctly`` (input, expected) =
        match SIxOpFromString FermionicOperator.FromString input with
        | Some sixop ->
            Assert.Equal(expected, prettyPrintSIxOp (sixop |> FermionicOperator.ToCanonicalOrder) |> shrinkString)
        | None -> Assert.True (false)