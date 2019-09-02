namespace Encodings

[<AutoOpen>]
module FermionicOperator =
    open System.Numerics

    type FermionicRaiseOperatorIndexSort() =
        class
            inherit SwapTrackingSort<IndexedOperator<LadderOperatorUnit>, Complex>
                (curry IndexedOperator<_>.(.<=.), IndexedOperator<_>.WithMaxIndex, Complex.SwapSignMultiple)

            member this.SortRaiseOperators rg =
                let isRaise io = io.Op.Item = Raise
                rg |> Array.where isRaise |> this.Sort Complex.One
        end

    type FermionicLowerOperatorIndexSort() =
        class
            inherit SwapTrackingSort<IndexedOperator<LadderOperatorUnit>, Complex>
                (curry IndexedOperator<_>.(.>=.), IndexedOperator<_>.WithMinIndex, Complex.SwapSignMultiple)

            member this.SortLowerOperators rg =
                let isLower io = io.Op.Item = Lower
                rg |> Array.where isLower |> this.Sort Complex.One
        end

    type FermionicOperatorUnit =
    | Op of IndexedOperator<LadderOperatorUnit>
    with
        member this.Unapply =
            let (Op indexedOperator) = this
            indexedOperator
        static member TryCreateFromString =
            IndexedOperator<LadderOperatorUnit>.TryCreateFromString LadderOperatorUnit.Apply

    type FermionicOperatorProductTerm =
    | ProductTerm of ProductOfIndexedOperators<LadderOperatorUnit>
        member this.Unapply = match this with ProductTerm term -> term.Unapply
        static member private RaiseOperatorIndexSort = new FermionicRaiseOperatorIndexSort()
        static member private LowerOperatorIndexSort = new FermionicLowerOperatorIndexSort()

        member this.Coeff = this.Unapply.Coeff

        static member internal Apply =
            ProductOfIndexedOperators.ProductTerm
            >> FermionicOperatorProductTerm.ProductTerm

        static member TryCreateFromString s =
            ProductOfIndexedOperators<LadderOperatorUnit>.TryCreateFromString LadderOperatorUnit.Apply s
            |> Option.map ProductTerm

        static member FromUnits =
            Array.map LadderOperatorUnit.FromUnit
            >> P<IndexedOperator<LadderOperatorUnit>>.Apply
            >> FermionicOperatorProductTerm.Apply

        static member FromTuples =
            Array.map LadderOperatorUnit.FromTuple
            >> P<IndexedOperator<LadderOperatorUnit>>.Apply
            >> FermionicOperatorProductTerm.Apply

        static member (*) (ProductTerm l, ProductTerm r) =
            l.Unapply * r.Unapply

        member this.IsInNormalOrder =
            let comparer p c =
                match (p, c) with
                | Lower, Raise -> false
                | _, _         -> true

            this.Unapply.Units
            |> Seq.map (fun ciu -> ciu.Item.Op.Item)
            |> isOrdered comparer

        member this.IsInIndexOrder =
            let operators =
                this.Unapply.Units
                |> Seq.map (fun ciu -> ciu.Item)

            let raisingOperatorsAreAscending =
                operators
                |> Seq.where (fun ico -> ico.Op.Item = Raise)
                |> IndexedOperator<_>.IndicesInOrder Ascending

            let loweringOperatorsAreDescending =
                operators
                |> Seq.where (fun ico -> ico.Op.Item = Lower)
                |> IndexedOperator<_>.IndicesInOrder Descending

            raisingOperatorsAreAscending && loweringOperatorsAreDescending

        member this.ToIndexOrder =
            let (sortedCreationOps, createdPhase) =
                this.Unapply.Units
                |> Array.map (fun u -> u.Item)
                |> FermionicOperatorProductTerm.RaiseOperatorIndexSort.SortRaiseOperators

            let (sortedAnnihilationOps, annihilatedPhase) =
                this.Unapply.Units
                |> Array.map (fun u -> u.Item)
                |> FermionicOperatorProductTerm.LowerOperatorIndexSort.SortLowerOperators

            let phase = this.Coeff * createdPhase * annihilatedPhase
            let ops   = Array.concat [| sortedCreationOps; sortedAnnihilationOps |]

            (phase, ops)
            |> (P<IndexedOperator<LadderOperatorUnit>>.Apply >> FermionicOperatorProductTerm.Apply)

        override this.ToString() =
            this.Unapply.ToString()

    and FermionicOperatorSumExpression =
    | SumTerm of SumOfProductsOfIndexedOperators<LadderOperatorUnit>
        member this.Unapply = match this with SumTerm term -> term.Unapply

        static member internal Apply =
            SumOfProductsOfIndexedOperators.SumTerm
            >> FermionicOperatorSumExpression.SumTerm

        static member TryCreateFromString =
            SumOfProductsOfIndexedOperators<LadderOperatorUnit>.TryCreateFromString LadderOperatorUnit.Apply

        member this.Coeff = this.Unapply.Coeff

        member this.ProductTerms =
            this.Unapply.Terms.Values
            |> Seq.map FermionicOperatorProductTerm.Apply

        member this.AllTermsNormalOrdered =
            this.ProductTerms
            |> Seq.fold (fun result curr -> result && curr.IsInNormalOrder) true

        member this.AllTermsIndexOrdered  =
            this.ProductTerms
            |> Seq.fold (fun result curr -> result && curr.IsInIndexOrder) true

        override this.ToString() =
            this.Unapply.ToString()

    and NormalOrderedFermionicOperatorSequence private (sumTerm : FermionicOperatorSumExpression) =
        class
            member __.Unapply = sumTerm

            static member Construct (candidate : FermionicOperatorSumExpression) : NormalOrderedFermionicOperatorSequence option =
                if candidate.AllTermsNormalOrdered then
                    NormalOrderedFermionicOperatorSequence candidate
                    |> Some
                else
                    failwith "Not Yet Implemented"
        end

    and IndexOrderedFermionicOperatorSequence private (sumTerm : FermionicOperatorSumExpression) =
        class
            member __.Unapply = sumTerm

            static member Construct (candidate : FermionicOperatorSumExpression) : IndexOrderedFermionicOperatorSequence option =
                if candidate.AllTermsIndexOrdered then
                    candidate
                    |> (IndexOrderedFermionicOperatorSequence >> Some)
                else if candidate.AllTermsNormalOrdered then
                    [|
                        for productTerm in candidate.ProductTerms do
                            yield productTerm.ToIndexOrder.Unapply
                    |]
                    |> (fun terms -> (candidate.Coeff, terms))
                    |> (S<IndexedOperator<LadderOperatorUnit>>.Apply >> FermionicOperatorSumExpression.Apply)
                    |> (IndexOrderedFermionicOperatorSequence >> Some)
                else
                    candidate
                    |> NormalOrderedFermionicOperatorSequence.Construct
                    |> Option.bind (fun c ->
#if DEBUG
                        System.Diagnostics.Debug.Assert c.Unapply.AllTermsNormalOrdered
#endif
                        IndexOrderedFermionicOperatorSequence.Construct c.Unapply)
    end