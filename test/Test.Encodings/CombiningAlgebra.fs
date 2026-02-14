namespace Tests

module CombiningAlgebra =
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
    let ``Combine swaps Lower followed by Raise at different indices`` () =
        let algebra = FermionicAlgebra() :> ICombiningAlgebra<LadderOperatorUnit>
        let productTerm = makeProduct [| (Raise, 0u); (Lower, 1u); (Lower, 2u) |]
        let nextUnit = makeUnit Raise 5u

        let combined = algebra.Combine productTerm nextUnit

        Assert.Equal(1, combined.Length)
        Assert.Equal(Complex.MinusOne, combined.[0].Coeff)
        Assert.Equal<(LadderOperatorUnit * uint32)[]>(
            [| (Raise, 0u); (Lower, 1u); (Raise, 5u); (Lower, 2u) |],
            operatorSequence combined.[0])

    [<Fact>]
    let ``Combine generates identity and swapped terms for same index`` () =
        let algebra = FermionicAlgebra() :> ICombiningAlgebra<LadderOperatorUnit>
        let productTerm = makeProduct [| (Raise, 0u); (Lower, 1u); (Lower, 2u) |]
        let nextUnit = makeUnit Raise 2u

        let combined = algebra.Combine productTerm nextUnit

        Assert.Equal(2, combined.Length)

        Assert.Equal(Complex.One, combined.[0].Coeff)
        Assert.Equal<(LadderOperatorUnit * uint32)[]>(
            [| (Raise, 0u); (Lower, 1u) |],
            operatorSequence combined.[0])

        Assert.Equal(Complex.MinusOne, combined.[1].Coeff)
        Assert.Equal<(LadderOperatorUnit * uint32)[]>(
            [| (Raise, 0u); (Lower, 1u); (Raise, 2u); (Lower, 2u) |],
            operatorSequence combined.[1])

    [<Fact>]
    let ``Combine appends operator for non Lower Raise pair`` () =
        let algebra = FermionicAlgebra() :> ICombiningAlgebra<LadderOperatorUnit>
        let productTerm = makeProduct [| (Raise, 0u); (Raise, 1u) |]
        let nextUnit = makeUnit Lower 3u

        let combined = algebra.Combine productTerm nextUnit

        Assert.Equal(1, combined.Length)
        Assert.Equal(Complex.One, combined.[0].Coeff)
        Assert.Equal<(LadderOperatorUnit * uint32)[]>(
            [| (Raise, 0u); (Raise, 1u); (Lower, 3u) |],
            operatorSequence combined.[0])

    [<Fact>]
    let ``Combine with short prefix inserts identity sentinel`` () =
        let algebra = FermionicAlgebra() :> ICombiningAlgebra<LadderOperatorUnit>
        let productTerm = makeProduct [| (Lower, 2u) |]
        let nextUnit = makeUnit Raise 2u

        let combined = algebra.Combine productTerm nextUnit

        Assert.Equal(2, combined.Length)
        Assert.Equal<(LadderOperatorUnit * uint32)[]>(
            [| (Identity, 0u) |],
            operatorSequence combined.[0])
