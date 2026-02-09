namespace Encodings

[<AutoOpen>]
module CombiningAlgebra =
    open System.Numerics

    /// Interface for algebras that define how to combine (commute/anti-commute)
    /// operators when sorting into normal order.
    type ICombiningAlgebra<'op when 'op : equality> =
        interface
            abstract Combine : ((*productTerm : *)P<IxOp<uint32, 'op>>) -> ((*nextUnit : *)C<IxOp<uint32, 'op>>) -> P<IxOp<uint32, 'op>>[]
        end

    /// Fermionic anti-commutation algebra:
    ///   {a†_i, a_j} = δ_ij
    ///   {a_i, a_j}   = 0
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
                                |] |> P<IxOp<uint32, LadderOperatorUnit>>.Apply
                            [| term.Reduce.Value |]
                        else
                            let leadingTerm =
                                [|
                                    yield! prefix
                                |] |> P<IxOp<uint32, LadderOperatorUnit>>.Apply
                            let trailingTerm =
                                [|
                                    yield! prefix
                                    yield { nextUnit with Coeff = Complex.MinusOne }
                                    yield lastUnit
                                |] |> P<IxOp<uint32, LadderOperatorUnit>>.Apply
                            [| leadingTerm.Reduce.Value; trailingTerm.Reduce.Value |]
                    | _, _ ->
                        let term =
                            [|
                                yield! productTerm.Units
                                yield nextUnit
                            |] |> P<IxOp<uint32, LadderOperatorUnit>>.Apply
                        [| term.Reduce.Value |]
        end
