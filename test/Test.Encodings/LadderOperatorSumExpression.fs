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
            LadderOperatorSumExpr<FermionicAlgebra>.ConstructNormalOrdered l.Unapply
            |> Option.iter (fun nols -> Assert.Equal (expected, nols.ToString()))
        | None -> Assert.Equal(expected, "")

    [<Fact>]
    let ``LadderOperatorSumExpression exposes coefficient and product terms`` () =
        let parsed = LadderOperatorSumExpression.TryCreateFromString "{[(u, 0)]; [(d, 1)]}"
        Assert.True(parsed.IsSome)

        let expression = parsed.Value
        Assert.Equal(System.Numerics.Complex.One, expression.Coeff)
        Assert.Equal(2, expression.ProductTerms.Length)

    [<Fact>]
    let ``LadderOperatorSumExpression Reduce preserves valid expression`` () =
        let parsed = LadderOperatorSumExpression.TryCreateFromString "{[(u, 0) | (d, 0)]}"
        Assert.True(parsed.IsSome)

        let reduced = parsed.Value.Reduce.Value
        Assert.True(reduced.AllTermsNormalOrdered)
        Assert.Equal(parsed.Value.ToString(), reduced.ToString())

    [<Fact>]
    let ``LadderOperatorSumExpression supports addition and multiplication`` () =
        let left = LadderOperatorSumExpression.TryCreateFromString "{[(u, 0)]}" |> Option.get
        let right = LadderOperatorSumExpression.TryCreateFromString "{[(d, 1)]}" |> Option.get

        let sum = left + right
        let product = left * right

        Assert.Contains("(u, 0)", sum.ToString())
        Assert.Contains("(d, 1)", sum.ToString())
        Assert.Contains("(u, 0)", product.ToString())
        Assert.Contains("(d, 1)", product.ToString())

    [<Fact>]
    let ``LadderOperatorSumExpression can be non-normal but index-ordered`` () =
        let expr = LadderOperatorSumExpression.TryCreateFromString "{[(d, 1) | (u, 0)]}" |> Option.get

        Assert.False(expr.AllTermsNormalOrdered)
        Assert.True(expr.AllTermsIndexOrdered)

    [<Fact>]
    let ``LadderOperatorSumExpression detects out-of-index-order raises`` () =
        let expr = LadderOperatorSumExpression.TryCreateFromString "{[(u, 2) | (u, 0)]}" |> Option.get

        Assert.True(expr.AllTermsNormalOrdered)
        Assert.False(expr.AllTermsIndexOrdered)

    [<Fact>]
    let ``LadderOperatorSumExpression detects normal and index-ordered terms`` () =
        let expr = LadderOperatorSumExpression.TryCreateFromString "{[(u, 0) | (u, 2) | (d, 3) | (d, 1)]}" |> Option.get

        Assert.True(expr.AllTermsNormalOrdered)
        Assert.True(expr.AllTermsIndexOrdered)

    [<Fact>]
    let ``ApplyFromProductTerms builds sum expression from wrapped terms`` () =
        let left =
            LadderOperatorProductTerm.TryCreateFromString "[(u, 0)]"
            |> Option.get
        let right =
            LadderOperatorProductTerm.TryCreateFromString "[(d, 1)]"
            |> Option.get

        let sum = LadderOperatorSumExpression.ApplyFromProductTerms [| left; right |]

        Assert.Equal(2, sum.ProductTerms.Length)
        Assert.Contains("(u, 0)", sum.ToString())
        Assert.Contains("(d, 1)", sum.ToString())

    [<Fact>]
    let ``ApplyFromPTerms builds sum expression from raw product terms`` () =
        let left =
            LadderOperatorProductTerm.TryCreateFromString "[(u, 2)]"
            |> Option.get
            |> fun pt -> pt.Unapply
        let right =
            LadderOperatorProductTerm.TryCreateFromString "[(d, 0)]"
            |> Option.get
            |> fun pt -> pt.Unapply

        let sum = LadderOperatorSumExpression.ApplyFromPTerms [| left; right |]

        Assert.Equal(2, sum.ProductTerms.Length)
        Assert.Contains("(u, 2)", sum.ToString())
        Assert.Contains("(d, 0)", sum.ToString())
