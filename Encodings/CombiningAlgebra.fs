namespace Encodings

[<AutoOpen>]
module CombiningAlgebra =
    open System.Numerics

    type ICombiningAlgebra<'op when 'op : equality> =
        interface
            abstract Combine : ((*productTerm : *)P<IxOp<'op>>) -> ((*nextUnit : *)C<IxOp<'op>>) -> P<IxOp<'op>>[]
        end

    type FermionicAlgebra () =
        class
            interface ICombiningAlgebra<LadderOperatorUnit> with
                member __.Combine productTerm nextUnit =
                    let nUnits = productTerm.Units.Length
                    let prefix =
                        if nUnits > 2 then
                            productTerm.Units.[0..(nUnits - 2)]
                        else
                            [| C<_>.Apply { IxOp.Op = Identity;  IxOp.Index = 0u } |]
                    let lastUnit = productTerm.Units.[nUnits - 1]
                    match (lastUnit.Item.Op, nextUnit.Item.Op) with
                    | Lower, Raise ->
                        if lastUnit.Item.Index <> nextUnit.Item.Index then
                            let term =
                                [|
                                    yield! prefix
                                    yield { nextUnit with Coeff = Complex.MinusOne }
                                    yield lastUnit
                                |] |> P<IxOp<LadderOperatorUnit>>.Apply
                            [| term.Reduce.Value |]
                        else
                            let leadingTerm =
                                [|
                                    yield! prefix
                                |] |> P<IxOp<LadderOperatorUnit>>.Apply
                            let trailingTerm =
                                [|
                                    yield! prefix
                                    yield { nextUnit with Coeff = Complex.MinusOne }
                                    yield lastUnit
                                |] |> P<IxOp<LadderOperatorUnit>>.Apply
                            [| leadingTerm.Reduce.Value; trailingTerm.Reduce.Value |]
                    | _, _ ->
                        let term =
                            [|
                                yield! productTerm.Units
                                yield nextUnit
                            |] |> P<IxOp<LadderOperatorUnit>>.Apply
                        [| term.Reduce.Value |]
        end
