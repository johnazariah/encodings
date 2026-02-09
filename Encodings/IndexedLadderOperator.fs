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
            tryParseIxOpUint32 LadderOperatorUnit.Apply

        static member FromUnit : (bool * uint32 -> IxOp<uint32, LadderOperatorUnit>) = function
            | (true,  index) -> IxOp<_,_>.Apply(index, Raise)
            | (false, index) -> IxOp<_,_>.Apply(index, Lower)

        static member FromTuple (ladderOperatorUnit, index) =
            IxOp<_,_>.Apply (index, ladderOperatorUnit)

    /// Checks whether a product term's operators are in normal order
    /// (all Raise operators before all Lower operators).
    let isInNormalOrder (productTerm : P<IxOp<uint32, LadderOperatorUnit>>) =
        let comparer p c =
            match (p, c) with
            | Lower, Raise -> false
            | _, _         -> true
        productTerm.Units
        |> Seq.map (fun ciu -> ciu.Item.Op)
        |> isOrdered comparer

    /// Checks whether a product term's operators are in index order:
    /// Raise operators in ascending index, Lower operators in descending index.
    let isInIndexOrder (productTerm : P<IxOp<uint32, LadderOperatorUnit>>) =
        let operators =
            productTerm.Units
            |> Seq.map (fun ciu -> ciu.Item)
        let raisingOperatorsAreAscending =
            operators
            |> Seq.where (fun ico -> ico.Op = Raise)
            |> IxOp<_,_>.IndicesInOrder Ascending
        let loweringOperatorsAreDescending =
            operators
            |> Seq.where (fun ico -> ico.Op = Lower)
            |> IxOp<_,_>.IndicesInOrder Descending
        raisingOperatorsAreAscending && loweringOperatorsAreDescending

    type LadderOperatorProductTerm =
        | LadderProduct of PIxOp<uint32, LadderOperatorUnit>
    with
        static member internal Apply (units : C<IxOp<uint32, LadderOperatorUnit>>[]) =
            units
            |> P<IxOp<uint32, LadderOperatorUnit>>.Apply
            |> (PIxOp<uint32, LadderOperatorUnit>.ProductTerm >> LadderOperatorProductTerm.LadderProduct)

        member this.Unapply = match this with LadderProduct term -> term.Unapply
        member this.Coeff  = this.Unapply.Coeff
        member this.Units  = this.Unapply.Units
        member this.Reduce = this.Unapply.Reduce

        static member TryCreateFromString s =
            PIxOp<uint32, LadderOperatorUnit>.TryCreateFromString LadderOperatorUnit.Apply s
            |> Option.map LadderProduct

        static member FromUnits =
            Array.map LadderOperatorUnit.FromUnit
            >> P<IxOp<uint32, LadderOperatorUnit>>.Apply
            >> PIxOp<uint32, LadderOperatorUnit>.ProductTerm
            >> LadderOperatorProductTerm.LadderProduct

        static member FromTuples =
            Array.map LadderOperatorUnit.FromTuple
            >> P<IxOp<uint32, LadderOperatorUnit>>.Apply
            >> PIxOp<uint32, LadderOperatorUnit>.ProductTerm
            >> LadderOperatorProductTerm.LadderProduct

        static member (*) (LadderProduct l, LadderProduct r) =
            l.Unapply * r.Unapply
            |> (PIxOp<uint32, LadderOperatorUnit>.ProductTerm >> LadderOperatorProductTerm.LadderProduct)

        member this.IsInNormalOrder = isInNormalOrder this.Unapply

        member this.IsInIndexOrder = isInIndexOrder this.Unapply

        override this.ToString() =
            this.Unapply.ToString()

    type LadderOperatorSumExpression =
        | LadderSum of SIxOp<uint32, LadderOperatorUnit>
    with
        member this.Unapply = match this with LadderSum term -> term.Unapply

        static member internal Apply (expr : S<IxOp<uint32, LadderOperatorUnit>>) =
            expr
            |> SIxOp<uint32, LadderOperatorUnit>.SumTerm
            |> LadderOperatorSumExpression.LadderSum

        static member internal ApplyFromProductTerms (terms : LadderOperatorProductTerm[]) =
            terms
            |> Array.map (fun (pt : LadderOperatorProductTerm) -> pt.Unapply)
            |> S<IxOp<uint32, LadderOperatorUnit>>.Apply
            |> LadderOperatorSumExpression.Apply

        static member internal ApplyFromPTerms (terms : P<IxOp<uint32, LadderOperatorUnit>>[]) =
            terms
            |> S<IxOp<uint32, LadderOperatorUnit>>.Apply
            |> LadderOperatorSumExpression.Apply

        static member TryCreateFromString =
            SIxOp<uint32, LadderOperatorUnit>.TryCreateFromString LadderOperatorUnit.Apply
            >> Option.map LadderSum

        member this.Coeff = this.Unapply.Coeff
        member this.Reduce =
            lazy
                this.Unapply.Reduce.Value |> LadderOperatorSumExpression.Apply

        member this.ProductTerms =
            this.Unapply.ProductTerms.Value
            |> Array.map (PIxOp<uint32, LadderOperatorUnit>.ProductTerm >> LadderOperatorProductTerm.LadderProduct)

        member this.AllTermsNormalOrdered =
            this.ProductTerms
            |> Seq.fold (fun result curr -> result && curr.IsInNormalOrder) true

        member this.AllTermsIndexOrdered  =
            this.ProductTerms
            |> Seq.fold (fun result curr -> result && curr.IsInIndexOrder) true

        static member (*) (LadderSum l, LadderSum r) =
            l.Unapply * r.Unapply |> LadderOperatorSumExpression.Apply

        static member (+) (LadderSum l, LadderSum r) =
            l.Unapply + r.Unapply |> LadderOperatorSumExpression.Apply

        override this.ToString() =
            this.Unapply.ToString()
