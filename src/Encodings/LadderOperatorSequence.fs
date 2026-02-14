namespace Encodings

/// <summary>
/// Normal ordering of fermionic operator products.
/// </summary>
/// <remarks>
/// A product of ladder operators a†₂ a₀ a†₁ a₃ is in normal order when all
/// creation operators (a†) are to the left of all annihilation operators (a).
///
/// Reordering requires swapping adjacent operators. Each swap of two fermionic
/// operators introduces a factor of −1 (from the CAR), and swapping a†ᵢ past aᵢ
/// (same index) generates a δᵢⱼ term.
///
/// The ConstructNormalOrdered function performs this reordering, tracking all signs
/// and generated terms, producing an S type (sum of products).
/// </remarks>
[<AutoOpen>]
module LadderOperatorSequence =
    open System.Numerics

    /// <summary>
    /// Sorts the raise and lower operators within a product term into index order.
    /// </summary>
    /// <param name="productTerm">The product term containing ladder operators to sort.</param>
    /// <returns>A new product term with raise operators sorted ascending and lower operators descending by index, with phase adjusted for swaps.</returns>
    /// <remarks>
    /// Uses a selection sort that tracks sign changes from swaps. Raise operators
    /// are sorted in ascending order by index, while lower operators are sorted
    /// in descending order by index.
    /// </remarks>
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


    /// <summary>
    /// A sum expression of ladder operators parameterized by a combining algebra
    /// (e.g., fermionic anti-commutation relations).
    /// </summary>
    /// <typeparam name="'algebra">The combining algebra type that defines commutation/anti-commutation relations.</typeparam>
    /// <remarks>
    /// This type represents a sum of product terms of indexed ladder operators.
    /// The algebra parameter determines how operators combine when reordering
    /// (e.g., fermionic operators anti-commute, generating sign changes).
    /// </remarks>
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

            /// <summary>
            /// Constructs a normal-ordered version of the given sum expression.
            /// </summary>
            /// <param name="candidate">The sum expression to normal-order.</param>
            /// <returns>The normal-ordered sum expression, or None if construction fails.</returns>
            /// <remarks>
            /// Uses the combining algebra to apply commutation/anti-commutation relations.
            /// For fermionic operators, each swap of adjacent operators introduces a factor
            /// of −1, and swapping a†ᵢ past aᵢ generates delta terms. The result has all
            /// creation operators (a†) to the left of all annihilation operators (a).
            /// </remarks>
            static member ConstructNormalOrdered (candidate : S<IxOp<uint32, LadderOperatorUnit>>) : LadderOperatorSumExpr<'algebra> option =
                let sumExpr = LadderOperatorSumExpr<'algebra>(candidate.Reduce.Value)
                if sumExpr.AllTermsNormalOrdered then
                    Some sumExpr
                else
                    sumExpr.ProductTerms.Value
                    |> Array.collect LadderOperatorSumExpr<'algebra>.SortSingleProductTerm
                    |> (S<IxOp<uint32, LadderOperatorUnit>>.Apply >> LadderOperatorSumExpr<'algebra> >> Some)

            /// <summary>
            /// Constructs an index-ordered version of the given sum expression.
            /// </summary>
            /// <param name="candidate">The sum expression to index-order.</param>
            /// <returns>The index-ordered sum expression, or None if construction fails.</returns>
            /// <remarks>
            /// First normal-orders the expression, then sorts raise operators in ascending
            /// order by index and lower operators in descending order by index. This
            /// canonical form is useful for comparing operator expressions.
            /// </remarks>
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
