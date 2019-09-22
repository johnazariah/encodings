namespace Encodings

[<AutoOpen>]
module LadderOperatorSequence =
    let isInNormalOrder productTerm =
        let comparer p c =
            match (p, c) with
            | Lower, Raise -> false
            | _, _         -> true
        productTerm.Units
        |> Seq.map (fun ciu -> ciu.Item.Op)
        |> isOrdered comparer


    let isInIndexOrder productTerm =
        let operators =
            productTerm.Units
            |> Seq.map (fun ciu -> ciu.Item)
        let raisingOperatorsAreAscending =
            operators
            |> Seq.where (fun ico -> ico.Op = Raise)
            |> IxOp<_>.IndicesInOrder Ascending
        let loweringOperatorsAreDescending =
            operators
            |> Seq.where (fun ico -> ico.Op = Lower)
            |> IxOp<_>.IndicesInOrder Descending
        raisingOperatorsAreAscending && loweringOperatorsAreDescending

    let toIndexOrder<'algebra when 'algebra :> ICombiningAlgebra<LadderOperatorUnit>> (productTerm) : P<IxOp<LadderOperatorUnit>> =
        let (sortedCreationOps, createdPhase) =
            productTerm.Units
            |> Array.map (fun u -> u.Item)
            |> LadderOperatorProductTerm.RaiseOperatorIndexSort.SortRaiseOperators

        let (sortedAnnihilationOps, annihilatedPhase) =
            productTerm.Units
            |> Array.map (fun u -> u.Item)
            |> LadderOperatorProductTerm.LowerOperatorIndexSort.SortLowerOperators

        let phase = productTerm.Coeff * createdPhase * annihilatedPhase
        let ops   = Array.concat [| sortedCreationOps; sortedAnnihilationOps |]

        (phase, ops)
        |> P<IxOp<LadderOperatorUnit>>.Apply


    type LadderOperatorSumExpr<'algebra when 'algebra :> ICombiningAlgebra<LadderOperatorUnit> and 'algebra : (new : unit -> 'algebra)>
        private (sumTerm : SC<IxOp<uint32, LadderOperatorUnit>>) =
        class
            private new (productTerms : P<IxOp<LadderOperatorUnit>>[]) =
                LadderOperatorSumExpr (SC<IxOp<LadderOperatorUnit>>.Apply productTerms)

            static member TryCreateFromString =
                (IxOp<_>.TryCreateFromString LadderOperatorUnit.Apply) |> SC<IxOp<LadderOperatorUnit>>.TryCreateFromString
                >> Option.map (LadderOperatorSumExpr<'algebra>)

            member internal __.Unapply = sumTerm
            member this.Coeff                 = this.Unapply.Coeff
            member this.ProductTerms          = this.Unapply.ProductTerms

            member this.AllTermsNormalOrdered =
                this.ProductTerms.Value
                |> Seq.fold (fun result curr -> result && isInNormalOrder curr) true

            member this.AllTermsIndexOrdered  =
                this.ProductTerms.Value
                |> Seq.fold (fun result curr -> result && isInIndexOrder curr) true

            static member (*) (l : LadderOperatorSumExpr<'algebra>, r : LadderOperatorSumExpr<'algebra>) =
                l.Unapply * r.Unapply |> LadderOperatorSumExpression.Apply

            static member (+) (l : LadderOperatorSumExpr<'algebra>, r : LadderOperatorSumExpr<'algebra>) =
                l.Unapply + r.Unapply |> LadderOperatorSumExpression.Apply

            override this.ToString() =
                this.Unapply.ToString()

            override this.ToString()  = this.Unapply.ToString()

            static member private CombiningAlgebra = new 'algebra ()

            static member private SortSingleProductTerm (productTerm : P<IxOp<LadderOperatorUnit>>) : P<IxOp<LadderOperatorUnit>>[] =
                let rec sortInternal (result : P<IxOp<LadderOperatorUnit>>[]) (remainingUnits : C<IxOp<LadderOperatorUnit>>[]) : P<IxOp<LadderOperatorUnit>>[] =
                    if remainingUnits.Length = 0 then
                        result
                    else
                        let nextUnit = remainingUnits.[0]
                        let remainingUnits' = remainingUnits.[1..]
                        let result' =
                            if result.Length = 0 then
                                [| P<IxOp<LadderOperatorUnit>>.Apply nextUnit |]
                            else
                                let appendUnitToProductTerm nu pt =
                                    LadderOperatorSumExpr<_>.CombiningAlgebra.Combine pt nu
                                    |> SC<IxOp<LadderOperatorUnit>>.Apply
                                    |> LadderOperatorSumExpr<'algebra>.ConstructNormalOrdered
                                    |> Option.map (fun nols -> nols.ProductTerms.Value) |> Option.defaultValue [||]
                                result |> Array.collect (appendUnitToProductTerm nextUnit)
                        sortInternal result' remainingUnits'
                sortInternal [||] productTerm.Reduce.Value.Units

            static member ConstructNormalOrdered (candidate : SC<IxOp<LadderOperatorUnit>>) : LadderOperatorSumExpr<'algebra> option =
                let sumExpr = LadderOperatorSumExpr<'algebra>(candidate.Reduce.Value)
                if sumExpr.AllTermsNormalOrdered then
                    Some sumExpr
                else
                    sumExpr.ProductTerms.Value
                    |> Array.collect LadderOperatorSumExpr<'algebra>.SortSingleProductTerm
                    |> (SC<IxOp<LadderOperatorUnit>>.Apply >> LadderOperatorSumExpr<'algebra> >> Some)

            static member ConstructIndexOrdered (candidate : SC<IxOp<LadderOperatorUnit>>) : LadderOperatorSumExpr<'algebra> option =
                let sumExpr = LadderOperatorSumExpr<'algebra>(candidate.Reduce.Value)
                if sumExpr.AllTermsIndexOrdered then
                    Some sumExpr
                else if sumExpr.AllTermsNormalOrdered then
                    sumExpr.ProductTerms.Value
                    |> Array.map (toIndexOrder<'algebra>)
                    |> (fun terms -> (sumExpr.Coeff, terms))
                    |> (SC<IxOp<LadderOperatorUnit>>.Apply >> LadderOperatorSumExpr<'algebra> >> Some)
                else
                    sumExpr.Unapply
                    |> LadderOperatorSumExpr<'algebra>.ConstructNormalOrdered
                    |> Option.bind ((fun se -> se.Unapply) >> LadderOperatorSumExpr<'algebra>.ConstructIndexOrdered)
        end
