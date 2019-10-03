﻿namespace Tests

open System.Collections
open Encodings
open Xunit
open FsCheck.Xunit
open System.Numerics

[<Properties (Arbitrary = [|typeof<ComplexGenerator>|]) >]
module Terms_SIxOp =

    [<Theory>]
    [<InlineData("{}", "{}", "{}")>]
    [<InlineData("{[(R,1)|(R,2)];[(L,1)|(L,2)]}", "{}", "{[(L,1)|(L,2)];[(R,1)|(R,2)]}")>]
    [<InlineData("{[(R,1)|(R,2)]}", "{[(R,1)|(R,2)]}", "{[(R,1)|(R,2)]}")>]
    [<InlineData("{[(R,1)|(R,2)]}", "{[(R,2)|(R,1)]}", "{[(R,1)|(R,2)];[(R,2)|(R,1)]}")>]
    [<InlineData("{[(R,1)|(R,2)]}", "{[(L,1)|(L,2)]}", "{[(L,1)|(L,2)];[(R,1)|(R,2)]}")>]
    let ``S + S is computed correctly``(leftStr, rightStr, expected) =
        let left  = SIxOpFromString Wick.FromString leftStr
        let right = SIxOpFromString Wick.FromString rightStr
        match (left, right) with
        | Some l, Some r -> Assert.Equal(expected, prettyPrintSIxOp (l + r) |> shrinkString)
        | _, _ -> Assert.True (false)


    [<Theory>]
    [<InlineData("{[(R,1)|(R,2)]}", "{[(R,1)|(R,2)]}", "{[(R,1)|(R,2)|(R,1)|(R,2)]}")>]
    [<InlineData("{[(R,1)|(R,2)];[(L,1)|(L,2)]}", "{[(R,1)|(R,2)]}", "{[(L,1)|(L,2)|(R,1)|(R,2)];[(R,1)|(R,2)|(R,1)|(R,2)]}")>]
    [<InlineData("{[(R,1)|(R,2)];[(L,1)|(L,2)]}", "{}", "{}")>]
    [<InlineData("{}", "{}", "{}")>]
    let ``S * S is computed correctly``(leftStr, rightStr, expected) =
        let left  = SIxOpFromString Wick.FromString leftStr
        let right = SIxOpFromString Wick.FromString rightStr
        match (left, right) with
        | Some l, Some r -> Assert.Equal(expected, prettyPrintSIxOp (l * r) |> shrinkString)
        | _, _ -> Assert.True (false)