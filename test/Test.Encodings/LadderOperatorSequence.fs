namespace Tests

module LadderOperatorSequence =
    open Encodings
    open Xunit
    open FsCheck.Xunit

    let private makeIx operator index =
        IxOp<uint32, LadderOperatorUnit>.Apply(index, operator)

    let private makeProduct (operators : (LadderOperatorUnit * uint32)[]) =
        operators
        |> Array.map (fun (operator, index) -> makeIx operator index)
        |> P<IxOp<uint32, LadderOperatorUnit>>.Apply

    let private parseSumExpression (input : string) =
        SIxOp<uint32, LadderOperatorUnit>.TryCreateFromString LadderOperatorUnit.Apply input
        |> Option.map (fun sumTerm -> sumTerm.Unapply)

    [<Property>]
    let ``toIndexOrder sorts raise and lower operator indices`` (raiseIndices : uint32[], lowerIndices : uint32[]) =
        let raiseOperators = raiseIndices |> Array.map (fun index -> (Raise, index))
        let lowerOperators = lowerIndices |> Array.map (fun index -> (Lower, index))
        let productTerm = makeProduct (Array.concat [| raiseOperators; lowerOperators |])

        let sorted = toIndexOrder productTerm

        Assert.True(isInIndexOrder sorted)

    [<Fact>]
    let ``toIndexOrder preserves operator multiset`` () =
        let productTerm = makeProduct [| (Raise, 2u); (Raise, 0u); (Lower, 1u); (Lower, 3u) |]
        let sorted = toIndexOrder productTerm

        let original = productTerm.Units |> Array.map (fun unit -> unit.Item.Op, unit.Item.Index) |> Array.sort
        let reordered = sorted.Units |> Array.map (fun unit -> unit.Item.Op, unit.Item.Index) |> Array.sort

        Assert.Equal<(LadderOperatorUnit * uint32)[]>(original, reordered)

    [<Fact>]
    let ``ConstructNormalOrdered normal-orders unsorted expressions`` () =
        let candidate = parseSumExpression "{[(d, 1) | (u, 1)]}"
        Assert.True(candidate.IsSome)

        let normalOrdered = LadderOperatorSumExpr<FermionicAlgebra>.ConstructNormalOrdered candidate.Value

        Assert.True(normalOrdered.IsSome)
        Assert.True(normalOrdered.Value.AllTermsNormalOrdered)

    [<Fact>]
    let ``ConstructNormalOrdered returns expression unchanged when already normal`` () =
        let candidate = parseSumExpression "{[(u, 1) | (d, 1)]}"
        Assert.True(candidate.IsSome)

        let normalOrdered = LadderOperatorSumExpr<FermionicAlgebra>.ConstructNormalOrdered candidate.Value

        Assert.True(normalOrdered.IsSome)
        Assert.Equal(candidate.Value.ToString(), normalOrdered.Value.ToString())

    [<Fact>]
    let ``ConstructIndexOrdered enforces canonical index order`` () =
        let candidate = parseSumExpression "{[(u, 2) | (u, 0) | (d, 1) | (d, 3)]}"
        Assert.True(candidate.IsSome)

        let ordered = LadderOperatorSumExpr<FermionicAlgebra>.ConstructIndexOrdered candidate.Value

        Assert.True(ordered.IsSome)
        Assert.True(ordered.Value.AllTermsIndexOrdered)
        Assert.True(ordered.Value.AllTermsNormalOrdered)

    [<Fact>]
    let ``ConstructIndexOrdered handles non-normal input`` () =
        let candidate = parseSumExpression "{[(d, 2) | (u, 1) | (u, 0)]}"
        Assert.True(candidate.IsSome)

        let ordered = LadderOperatorSumExpr<FermionicAlgebra>.ConstructIndexOrdered candidate.Value

        Assert.True(ordered.IsSome)
        Assert.True(ordered.Value.AllTermsNormalOrdered)
        Assert.True(ordered.Value.AllTermsIndexOrdered)
