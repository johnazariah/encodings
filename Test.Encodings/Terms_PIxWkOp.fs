namespace Tests

open Encodings
open Xunit
open FsCheck.Xunit
open System.Numerics
open System

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
        match PIxOpFromString FermionicOperator.FromString input with
        | Some pixop -> Assert.Equal (expected, (PIxWkOp.ProductTerm pixop).IsInNormalOrder.Value)
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
        match PIxOpFromString FermionicOperator.FromString input with
        | Some pixop -> Assert.Equal (expected, (PIxWkOp.ProductTerm pixop).IsInIndexOrder.Value)
        | None -> Assert.True (false)

    [<Theory>]
    [<InlineData("", "{}")>]
    [<InlineData("[]", "{}")>]
    [<InlineData("[(R,1)]", "{[(R,1)]}")>]
    [<InlineData("[(R,1)|(L,1)]", "{[(R,1)|(L,1)]}")>]
    [<InlineData("[(R,1)|(L,1)|(L,1)]", "{[(R,1)|(L,1)|(L,1)]}")>]
    [<InlineData("[(R,1)|(L,1)|(L,2)|(L,1)]", "{-[(R,1)|(L,1)|(L,1)];[(L,2)]}")>]
    [<InlineData("[(R,1)|(R,2)]", "{[(R,1)];[(R,2)]}")>]
    [<InlineData("[(R,1)|(R,2)|(L,1)]", "{-[(R,1)|(L,1)];[(R,2)]}")>]
    [<InlineData("[(R,1)|(R,2)|(L,1)|(L,2)]", "{-[(R,1)|(L,1)];[(R,2)|(L,2)]}")>]
    [<InlineData("[(R,1)|(R,2)|(L,2)|(L,1)]", "{[(R,1)|(L,1)];[(R,2)|(L,2)]}")>]
    let ``P IndexedOpsGroupedByIndex is computed correctly``(input, expected) =
        match PIxWkOpFromString FermionicOperator.FromString input with
        | Some pixop ->
            let actual =
                pixop.IndexedOpsGroupedByIndex.Value
                |> prettyPrintPIxWkOps
                |> shrinkString

            Assert.Equal (expected, actual)
        | None -> Assert.True (false)

    [<Theory>]
    [<InlineData("[(R,1)|(R,2)]", "{[(R,1)|(R,2)]}")>]
    [<InlineData("[(R,2)|(R,1)]", "{[(R,2)|(R,1)]}")>]
    [<InlineData("[(L,1)|(L,2)]", "{[(L,1)|(L,2)]}")>]
    [<InlineData("[(L,2)|(L,1)]", "{[(L,2)|(L,1)]}")>]
    [<InlineData("[(R,1)|(L,2)]", "{[(R,1)|(L,2)]}")>]
    [<InlineData("[(L,1)|(R,2)]", "{-[(R,2)|(L,1)]}")>]
    [<InlineData("[(R,1)|(L,1)]", "{[(R,1)|(L,1)]}")>]
    [<InlineData("[(L,1)|(R,1)]", "{[1];-[(R,1)|(L,1)]}")>]
    [<InlineData("[(L,2)|(R,1)]", "{-[(R,1)|(L,2)]}")>]
    [<InlineData("[(L,1)|(R,1)|(R,2)]", "{[(R,2)];[(R,1)|(R,2)|(L,1)]}")>]
    [<InlineData("[(R,3)|(L,1)|(R,1)|(R,2)]", "{[(R,3)|(R,2)];[(R,3)|(R,1)|(R,2)|(L,1)]}")>]
    [<InlineData("[(L,1)|(R,1)|(R,2)|(R,3)|(R,4)]", "{[(R,2)|(R,3)|(R,4)];[(R,1)|(R,2)|(R,3)|(R,4)|(L,1)]}")>]
    [<InlineData("[(L,1)|(L,2)|(R,1)|(R,2)]", "{-[1];[(R,2)|(L,2)];[(R,1)|(L,1)];[(R,1)|(R,2)|(L,1)|(L,2)]}")>]
    [<InlineData("[(L,1)|(L,2)|(L,3)|(R,4)]", "{-[(R,4)|(L,1)|(L,2)|(L,3)]}")>]
    let ``P ToNormalOrder is computed correctly``(input, expected) =
        match PIxWkOpFromString FermionicOperator.FromString input with
        | Some pixop ->
            let actual =
                pixop.ToNormalOrder 
                |> (fun t -> t.Value)
                |> Array.map (prettyPrintPIxWkOps >> shrinkString)
                |> (fun rg -> String.Join(":", rg))
            Assert.Equal (expected, actual)
        | None -> Assert.True (false)
