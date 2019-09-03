namespace Encodings

[<AutoOpen>]
module IndexedLadderOperator =
    open System.Numerics

    type LadderOperatorUnit =
        | Identity
        | Raise
        | Lower
    with
        static member Apply = function
            | "I" -> Some Identity
            | "u" -> Some Raise
            | "d" -> Some Lower
            | _ -> None

        member this.AsString =
            match this with
            | Identity -> "I"
            | Raise    -> "u"
            | Lower    -> "d"

        override this.ToString() = this.AsString

        static member TryCreateFromString =
            IndexedOperator<LadderOperatorUnit>.TryCreateFromString LadderOperatorUnit.Apply

        static member FromUnit : (bool * uint32 -> IndexedOperator<LadderOperatorUnit>) = function
            | (true,  index) -> IndexedOperator<LadderOperatorUnit>.Apply(index, Raise)
            | (false, index) -> IndexedOperator<LadderOperatorUnit>.Apply(index, Lower)

        static member FromTuple (ladderOperatorUnit, index) =
            IndexedOperator<LadderOperatorUnit>.Apply (index, ladderOperatorUnit)

    and RaiseOperatorIndexSort() =
        class
            inherit SwapTrackingSort<IndexedOperator<LadderOperatorUnit>, Complex>
                (curry IndexedOperator<_>.(.<=.), IndexedOperator<_>.WithMaxIndex, Complex.SwapSignMultiple)

            member this.SortRaiseOperators rg =
                let isRaise io = io.Op.Item = Raise
                rg |> Array.where isRaise |> this.Sort Complex.One
        end

    and LowerOperatorIndexSort() =
        class
            inherit SwapTrackingSort<IndexedOperator<LadderOperatorUnit>, Complex>
                (curry IndexedOperator<_>.(.>=.), IndexedOperator<_>.WithMinIndex, Complex.SwapSignMultiple)

            member this.SortLowerOperators rg =
                let isLower io = io.Op.Item = Lower
                rg |> Array.where isLower |> this.Sort Complex.One
        end

    and LadderOperatorProductTerm =
        | ProductTerm of ProductOfIndexedOperators<LadderOperatorUnit>
    with
        static member private RaiseOperatorIndexSort = new RaiseOperatorIndexSort()
        static member private LowerOperatorIndexSort = new LowerOperatorIndexSort()

        static member internal Apply =
            ProductOfIndexedOperators.ProductTerm
            >> LadderOperatorProductTerm.ProductTerm

        member this.Unapply = match this with ProductTerm term -> term.Unapply
        member this.Coeff  = this.Unapply.Coeff
        member this.Units  = this.Unapply.Units
        member this.Reduce = this.Unapply.Reduce

        static member TryCreateFromString s =
            ProductOfIndexedOperators<LadderOperatorUnit>.TryCreateFromString LadderOperatorUnit.Apply s
            |> Option.map ProductTerm

        static member FromUnits =
            Array.map LadderOperatorUnit.FromUnit
            >> P<IndexedOperator<LadderOperatorUnit>>.Apply
            >> LadderOperatorProductTerm.Apply

        static member FromTuples =
            Array.map LadderOperatorUnit.FromTuple
            >> P<IndexedOperator<LadderOperatorUnit>>.Apply
            >> LadderOperatorProductTerm.Apply

        static member (*) (ProductTerm l, ProductTerm r) =
            l.Unapply * r.Unapply |> LadderOperatorProductTerm.Apply

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
                |> LadderOperatorProductTerm.RaiseOperatorIndexSort.SortRaiseOperators

            let (sortedAnnihilationOps, annihilatedPhase) =
                this.Unapply.Units
                |> Array.map (fun u -> u.Item)
                |> LadderOperatorProductTerm.LowerOperatorIndexSort.SortLowerOperators

            let phase = this.Coeff * createdPhase * annihilatedPhase
            let ops   = Array.concat [| sortedCreationOps; sortedAnnihilationOps |]

            (phase, ops)
            |> (P<IndexedOperator<LadderOperatorUnit>>.Apply >> LadderOperatorProductTerm.Apply)

        override this.ToString() =
            this.Unapply.ToString()

    and LadderOperatorSumExpression =
        | SumTerm of SumOfProductsOfIndexedOperators<LadderOperatorUnit>
    with
        member this.Unapply = match this with SumTerm term -> term.Unapply

        static member internal Apply (expr : S<IndexedOperator<LadderOperatorUnit>>) =
            expr
            |> SumOfProductsOfIndexedOperators.SumTerm
            |> LadderOperatorSumExpression.SumTerm

        static member internal Apply (terms : P<IndexedOperator<LadderOperatorUnit>>[]) =
            terms
            |> S<IndexedOperator<LadderOperatorUnit>>.Apply
            |> LadderOperatorSumExpression.Apply

        static member TryCreateFromString =
            SumOfProductsOfIndexedOperators<LadderOperatorUnit>.TryCreateFromString LadderOperatorUnit.Apply
            >> Option.map SumTerm

        member this.Coeff = this.Unapply.Coeff

        member this.ProductTerms =
            this.Unapply.Terms.Values
            |> Seq.map LadderOperatorProductTerm.Apply

        member this.AllTermsNormalOrdered =
            this.ProductTerms
            |> Seq.fold (fun result curr -> result && curr.IsInNormalOrder) true

        member this.AllTermsIndexOrdered  =
            this.ProductTerms
            |> Seq.fold (fun result curr -> result && curr.IsInIndexOrder) true

        static member (*) (SumTerm l, SumTerm r) =
            l.Unapply * r.Unapply |> LadderOperatorSumExpression.Apply

        static member (+) (SumTerm l, SumTerm r) =
            l.Unapply + r.Unapply |> LadderOperatorSumExpression.Apply

        override this.ToString() =
            this.Unapply.ToString()
