namespace Tests

module Bosonic =
    open Encodings
    open Xunit
    open System.Numerics

    let private makeUnit operator index =
        C<IxOp<uint32, LadderOperatorUnit>>.Apply(IxOp<uint32, LadderOperatorUnit>.Apply(index, operator))

    let private makeProduct (operators : (LadderOperatorUnit * uint32)[]) =
        operators
        |> Array.map (fun (operator, index) -> makeUnit operator index)
        |> P<IxOp<uint32, LadderOperatorUnit>>.Apply

    let private operatorSequence (productTerm : P<IxOp<uint32, LadderOperatorUnit>>) =
        productTerm.Units |> Array.map (fun unit -> unit.Item.Op, unit.Item.Index)

    [<Fact>]
    let ``Bosonic combine swaps Lower followed by Raise at different indices without sign flip`` () =
        let algebra = BosonicAlgebra() :> ICombiningAlgebra<LadderOperatorUnit>
        let productTerm = makeProduct [| (Raise, 0u); (Lower, 1u); (Lower, 2u) |]
        let nextUnit = makeUnit Raise 5u

        let combined = algebra.Combine productTerm nextUnit

        Assert.Equal(1, combined.Length)
        Assert.Equal(Complex.One, combined.[0].Coeff)
        Assert.Equal<(LadderOperatorUnit * uint32)[]>(
            [| (Raise, 0u); (Lower, 1u); (Raise, 5u); (Lower, 2u) |],
            operatorSequence combined.[0])

    [<Fact>]
    let ``Bosonic combine generates identity and reordered terms for same index`` () =
        let algebra = BosonicAlgebra() :> ICombiningAlgebra<LadderOperatorUnit>
        let productTerm = makeProduct [| (Raise, 0u); (Lower, 1u); (Lower, 2u) |]
        let nextUnit = makeUnit Raise 2u

        let combined = algebra.Combine productTerm nextUnit

        Assert.Equal(2, combined.Length)

        Assert.Equal(Complex.One, combined.[0].Coeff)
        Assert.Equal<(LadderOperatorUnit * uint32)[]>(
            [| (Raise, 0u); (Lower, 1u) |],
            operatorSequence combined.[0])

        Assert.Equal(Complex.One, combined.[1].Coeff)
        Assert.Equal<(LadderOperatorUnit * uint32)[]>(
            [| (Raise, 0u); (Lower, 1u); (Raise, 2u); (Lower, 2u) |],
            operatorSequence combined.[1])

    [<Theory>]
    [<InlineData("{[(d, 1) | (u, 0)]}", "{[(I, 0) | (u, 0) | (d, 1)]}")>]
    [<InlineData("{[(d, 1) | (u, 1)]}", "{[(I, 0) | (u, 1) | (d, 1)]; [(I, 0)]}")>]
    let ``Bosonic normal ordering uses CCR signs`` (input : string, expected : string) =
        match LadderOperatorSumExpression.TryCreateFromString input with
        | Some expression ->
            BosonicLadderOperatorSumExpression.ConstructNormalOrdered expression.Unapply
            |> Option.iter (fun ordered -> Assert.Equal(expected, ordered.ToString()))
        | None -> Assert.Equal(expected, "")
