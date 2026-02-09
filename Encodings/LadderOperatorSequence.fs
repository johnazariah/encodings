namespace Encodings

[<AutoOpen>]
module LadderOperatorSequence =
    open System.Numerics

    /// Sorts the raise and lower operators within a product term into index order
    /// using a selection sort that tracks sign changes from swaps.
    let toIndexOrder (productTerm : P<IxOp<uint32, LadderOperatorUnit>>) : P<IxOp<uint32, LadderOperatorUnit>> =
        let raiseSort =
            SwapTrackingSort<IxOp<uint32, LadderOperatorUnit>, Complex>(
                (fun a b -> a.Index <= b.Index),
                { IxOp.Index = System.UInt32.MaxValue; Op = Raise },
                Complex.SwapSignMultiple)

        let lowerSort =
            SwapTrackingSort<IxOp<uint32, LadderOperatorUnit>, Complex>(
                (fun a b -> a.Index >= b.Index),
                { IxOp.Index = System.UInt32.MinValue; Op = Lower },
                Complex.SwapSignMultiple)

        let raiseOps =
            productTerm.Units
            |> Array.map (fun u -> u.Item)
            |> Array.where (fun io -> io.Op = Raise)

        let lowerOps =
            productTerm.Units
            |> Array.map (fun u -> u.Item)
            |> Array.where (fun io -> io.Op = Lower)

        let (sortedRaise, raisePhase) = raiseSort.Sort Complex.One raiseOps
        let (sortedLower, lowerPhase) = lowerSort.Sort Complex.One lowerOps

        let phase = productTerm.Coeff * raisePhase * lowerPhase
        let ops   = Array.concat [| sortedRaise; sortedLower |]

        (phase, ops)
        |> P<IxOp<uint32, LadderOperatorUnit>>.Apply


    /// A sum expression of ladder operators parameterized by a combining algebra
    /// (e.g., fermionic anti-commutation relations).
    type LadderOperatorSumExpr<'algebra when 'algebra :> ICombiningAlgebra<LadderOperatorUnit> and 'algebra : (new : unit -> 'algebra)>
        private (sumTerm : S<IxOp<uint32, LadderOperatorUnit>>) =
        class
            private new (productTerms : P<IxOp<uint32, LadderOperatorUnit>>[]) =
                LadderOperatorSumExpr (S<IxOp<uint32, LadderOperatorUnit>>.Apply productTerms)

            static member TryCreateFromString =
                (tryParseIxOpUint32 LadderOperatorUnit.Apply) |> S<IxOp<uint32, LadderOperatorUnit>>.TryCreateFromString
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

            static member private CombiningAlgebra = new 'algebra ()

            static member private SortSingleProductTerm (productTerm : P<IxOp<uint32, LadderOperatorUnit>>) : P<IxOp<uint32, LadderOperatorUnit>>[] =
                let rec sortInternal (result : P<IxOp<uint32, LadderOperatorUnit>>[]) (remainingUnits : C<IxOp<uint32, LadderOperatorUnit>>[]) : P<IxOp<uint32, LadderOperatorUnit>>[] =
                    if remainingUnits.Length = 0 then
                        result
                    else
                        let nextUnit = remainingUnits.[0]
                        let remainingUnits' = remainingUnits.[1..]
                        let result' =
                            if result.Length = 0 then
                                [| P<IxOp<uint32, LadderOperatorUnit>>.Apply nextUnit |]
                            else
                                let appendUnitToProductTerm nu pt =
                                    LadderOperatorSumExpr<'algebra>.CombiningAlgebra.Combine pt nu
                                    |> S<IxOp<uint32, LadderOperatorUnit>>.Apply
                                    |> LadderOperatorSumExpr<'algebra>.ConstructNormalOrdered
                                    |> Option.map (fun nols -> nols.ProductTerms.Value) |> Option.defaultValue [||]
                                result |> Array.collect (appendUnitToProductTerm nextUnit)
                        sortInternal result' remainingUnits'
                sortInternal [||] productTerm.Reduce.Value.Units

            /// Constructs a normal-ordered version of the given sum expression
            /// using the combining algebra to apply commutation/anti-commutation relations.
            static member ConstructNormalOrdered (candidate : S<IxOp<uint32, LadderOperatorUnit>>) : LadderOperatorSumExpr<'algebra> option =
                let sumExpr = LadderOperatorSumExpr<'algebra>(candidate.Reduce.Value)
                if sumExpr.AllTermsNormalOrdered then
                    Some sumExpr
                else
                    sumExpr.ProductTerms.Value
                    |> Array.collect LadderOperatorSumExpr<'algebra>.SortSingleProductTerm
                    |> (S<IxOp<uint32, LadderOperatorUnit>>.Apply >> LadderOperatorSumExpr<'algebra> >> Some)

            /// Constructs an index-ordered version: first normal-orders, then sorts
            /// raise operators ascending and lower operators descending by index.
            static member ConstructIndexOrdered (candidate : S<IxOp<uint32, LadderOperatorUnit>>) : LadderOperatorSumExpr<'algebra> option =
                let sumExpr = LadderOperatorSumExpr<'algebra>(candidate.Reduce.Value)
                if sumExpr.AllTermsIndexOrdered then
                    Some sumExpr
                else if sumExpr.AllTermsNormalOrdered then
                    sumExpr.ProductTerms.Value
                    |> Array.map toIndexOrder
                    |> (fun terms -> (sumExpr.Coeff, terms))
                    |> (S<IxOp<uint32, LadderOperatorUnit>>.Apply >> LadderOperatorSumExpr<'algebra> >> Some)
                else
                    sumExpr.Unapply
                    |> LadderOperatorSumExpr<'algebra>.ConstructNormalOrdered
                    |> Option.bind ((fun se -> se.Unapply) >> LadderOperatorSumExpr<'algebra>.ConstructIndexOrdered)
        end
