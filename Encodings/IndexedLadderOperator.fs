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
            lazy
                match this with
                | Identity -> "I"
                | Raise    -> "u"
                | Lower    -> "d"

        override this.ToString() = this.AsString.Value

        static member TryCreateFromString =
            IxOp<_>.TryCreateFromString LadderOperatorUnit.Apply

        static member FromUnit : (bool * uint32 -> IxOp<LadderOperatorUnit>) = function
            | (true,  index) -> IxOp<_>.Apply(index, Raise)
            | (false, index) -> IxOp<_>.Apply(index, Lower)

        static member FromTuple (ladderOperatorUnit, index) =
            IxOp<_>.Apply (index, ladderOperatorUnit)

    //and RaiseOperatorIndexSort() =
    //    class
    //        inherit SwapTrackingSort<IxOp<LadderOperatorUnit>, Complex>
    //            (curry IxOp<_>.(.<=.), IxOp<_>.WithMaxIndex, Complex.SwapSignMultiple)

    //        member this.SortRaiseOperators rg =
    //            let isRaise io = io.Op = Raise
    //            rg |> Array.where isRaise |> this.Sort Complex.One
    //    end

    //and LowerOperatorIndexSort() =
    //    class
    //        inherit SwapTrackingSort<IxOp<LadderOperatorUnit>, Complex>
    //            (curry IxOp<_>.(.>=.), IxOp<_>.WithMinIndex, Complex.SwapSignMultiple)

    //        member this.SortLowerOperators rg =
    //            let isLower io = io.Op = Lower
    //            rg |> Array.where isLower |> this.Sort Complex.One
    //    end

    and LadderOperatorProductTerm =
        | ProductTerm of PIxOp<LadderOperatorUnit>
    with
        //static member private RaiseOperatorIndexSort = new RaiseOperatorIndexSort()
        //static member private LowerOperatorIndexSort = new LowerOperatorIndexSort()

        static member internal Apply (units : C<IxOp<LadderOperatorUnit>>[]) =
            units
            |> P<IxOp<LadderOperatorUnit>>.Apply
            |> (PIxOp.ProductTerm >> LadderOperatorProductTerm.ProductTerm)

        member this.Unapply = match this with ProductTerm term -> term.Unapply
        member this.Coeff  = this.Unapply.Coeff
        member this.Units  = this.Unapply.Units
        member this.Reduce = this.Unapply.Reduce

        static member TryCreateFromString s =
            PIxOp<_>.TryCreateFromString LadderOperatorUnit.Apply s
            |> Option.map ProductTerm

        static member FromUnits =
            Array.map LadderOperatorUnit.FromUnit
            >> P<IxOp<LadderOperatorUnit>>.Apply
            >> PIxOp.ProductTerm
            >> LadderOperatorProductTerm.ProductTerm

        static member FromTuples =
            Array.map LadderOperatorUnit.FromTuple
            >> P<IxOp<LadderOperatorUnit>>.Apply
            >> PIxOp.ProductTerm
            >> LadderOperatorProductTerm.ProductTerm

        static member (*) (ProductTerm l, ProductTerm r) =
            l.Unapply * r.Unapply
            |> (PIxOp.ProductTerm >> LadderOperatorProductTerm.ProductTerm)

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
            |> P<IxOp<LadderOperatorUnit>>.Apply
            |> (PIxOp.ProductTerm >> LadderOperatorProductTerm.ProductTerm)

        override this.ToString() =
            this.Unapply.ToString()

    and LadderOperatorSumExpression =
        | SumTerm of SIxOp<LadderOperatorUnit>
    with
        member this.Unapply = match this with SumTerm term -> term.Unapply

        static member internal Apply (expr : S<IxOp<LadderOperatorUnit>>) =
            expr
            |> SIxOp.SumTerm
            |> LadderOperatorSumExpression.SumTerm

        static member internal Apply (terms : LadderOperatorProductTerm[]) =
            terms
            |> Array.map (fun pt -> pt.Unapply)
            |> LadderOperatorSumExpression.Apply

        static member internal Apply (terms : P<IxOp<LadderOperatorUnit>>[]) =
            terms
            |> S<IxOp<LadderOperatorUnit>>.Apply
            |> LadderOperatorSumExpression.Apply

        static member TryCreateFromString =
            SIxOp<_>.TryCreateFromString LadderOperatorUnit.Apply
            >> Option.map SumTerm

        member this.Coeff = this.Unapply.Coeff
        member this.Reduce =
            lazy
                this.Unapply.Reduce.Value |> LadderOperatorSumExpression.Apply

        member this.ProductTerms =
            this.Unapply.ProductTerms.Value
            |> Array.map (PIxOp.ProductTerm >> LadderOperatorProductTerm.ProductTerm)

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
