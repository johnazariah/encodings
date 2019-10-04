namespace Encodings

[<AutoOpen>]
module SparseRepresentation =
    open System.Numerics
    open System.Collections.Generic

    type IEnumerable<'a>
    with
        member xs.IsOrdered (comparer : 'a -> 'a -> bool) =
            let compareWithPrev (isOrdered, (prev : 'a option)) (curr : 'a) =
                let currAndPrevAreOrdered =
                    prev
                    |> Option.map (fun p -> comparer p curr)
                    |> Option.defaultValue true
                ((isOrdered && currAndPrevAreOrdered), Some curr)
            xs
            |> Seq.fold compareWithPrev (true, None)
            |> fst

    type IndexOrder =
        | Ascending
        | Descending

    type IxOp< ^idx, ^op
            when ^idx : comparison
            and ^op : equality
            and ^op : comparison> =
        { Index : ^idx; Op : ^op }
    with
        static member inline Apply (index : ^idx, op : ^op) = { Index = index; Op = op }
        static member inline (.>=.) (l : IxOp< ^idx, ^op >, r : IxOp< ^idx, ^op >) = l.Index >= r.Index
        static member inline (.<=.) (l : IxOp< ^idx, ^op >, r : IxOp< ^idx, ^op >) = l.Index <= r.Index

        static member inline InIndexOrder (indexOrder : IndexOrder) (ops : IxOp< ^idx, ^op> seq) : bool =
            let comparer =
                match indexOrder with
                | Ascending  -> (.<=.)
                | Descending -> (.>=.)
            ops.IsOrdered comparer

        member inline this.Signature =
            sprintf "%O%O" this.Op this.Index

    type CIxOp< ^idx, ^op
            when ^idx : comparison
            and ^op : equality
            and ^op : comparison> =
        | Indexed of C<IxOp< ^idx, ^op>>
    with
        member inline this.Unapply = match this with Indexed c -> c
        member inline this.IsZero = this.Unapply.IsZero
        member inline this.Coeff  = this.Unapply.Coeff
        member inline this.IndexedOp = this.Unapply.Thunk
        member inline this.Normalize = this.Unapply.Normalize

        static member inline Unit =
            CIxOp< ^idx, ^op>.Indexed <| C<_>.Unit

        static member inline Apply (coeff, unit) =
            CIxOp< ^idx, ^op>.Indexed <| C<_>.Apply (coeff, unit)

        member inline this.Signature =
            this.IndexedOp.Signature

    type PIxOp< ^idx, ^op
            when ^idx : comparison
            and ^op : equality
            and ^op : comparison> =
        | ProductTerm of C<IxOp< ^idx, ^op>[]>
    with
        member inline this.Unapply = match this with ProductTerm pt -> pt
        member inline this.Coeff = this.Unapply.Coeff
        member inline this.IndexedOps = this.Unapply.Thunk

        static member inline internal ApplyInternal =
            C<_>.Apply >> PIxOp< ^idx, ^op >.ProductTerm

        static member inline Unit = PIxOp< ^idx, ^op >.ApplyInternal (Complex.One,  [||])
        static member inline Zero = PIxOp< ^idx, ^op >.ApplyInternal (Complex.Zero, [||])

        static member inline (<*>) (l : PIxOp<_,_>, r : PIxOp<_,_>) =
            let indexedOps = Array.concat [| l.IndexedOps; r.IndexedOps |]
            let coeff = l.Coeff * r.Coeff
            PIxOp<_,_>.ApplyInternal (coeff, indexedOps)

        member inline this.ScaleCoefficient scale =
            this.Unapply.ScaleCoefficient scale
            |> PIxOp<_,_>.ProductTerm

        member inline this.AddCoefficient coeff =
            this.Unapply.AddCoefficient coeff
            |> PIxOp<_,_>.ProductTerm

        member inline this.IsZero =
            this.Coeff.IsZero || this.IndexedOps = [||]

        member inline this.Reduce =
            if this.IsZero then
                PIxOp< ^idx, ^op >.Zero
            else
                this

        member inline this.Signature =
            this.Reduce.IndexedOps
            |> Array.fold (fun result curr -> sprintf "%s%s" result curr.Signature) ""

        member inline this.IsInIndexOrder indexOrder =
            lazy
                this.IndexedOps
                |> IxOp<_,_>.InIndexOrder indexOrder

        static member inline Apply (coeff : Complex, units : CIxOp< ^idx, ^op >[]) =
            let extractedCoeff =
                units
                |> Array.fold (fun coeff curr -> coeff * curr.Coeff) Complex.One

            let indexedOps =
                units
                |> Array.map (fun curr -> curr.IndexedOp)

            PIxOp<_,_>.ApplyInternal (coeff * extractedCoeff, indexedOps)

    type SIxOp< ^idx, ^op
            when ^idx : comparison
            and ^op : equality
            and ^op : comparison> =
        | SumTerm of S<PIxOp< ^idx, ^op >>
    with
        member inline this.Unapply = match this with SumTerm st -> st
        member inline __.Coeff     = Complex.One
        member inline this.Terms   = this.Unapply.Terms
        static member inline Apply = S<PIxOp< ^idx, ^op >>.Apply >> SumTerm
        member inline this.IsZero  = this.Unapply.IsZero

        static member inline (+) (l : SIxOp< ^idx, ^op>, r : SIxOp< ^idx, ^op>) =
            l.Unapply + r.Unapply |> SumTerm

        static member inline (*) (l : SIxOp< ^idx, ^op>, r : SIxOp< ^idx, ^op>) =
            l.Unapply * r.Unapply |> SumTerm

        member inline this.AllTermsIndexOrdered indexOrder =
            let isIndexOrdered result (curr : PIxOp<_,_>) =
                let currIsIndexOrdered = curr.IsInIndexOrder indexOrder
                result && currIsIndexOrdered.Value

            lazy
                this.Terms |> Seq.fold isIndexOrdered true

    type PIxWkOp< ^idx, ^op
            when ^idx : comparison
            and ^op : comparison
            and ^op : (member IsRaising  : bool)
            and ^op : (member IsLowering : bool)
            and ^op : (static member InNormalOrder : ^op -> ^op -> bool)
            and ^op : equality> =
        | ProductTerm of PIxOp< ^idx, ^op>
    with
        member inline this.Unapply            = match this with ProductTerm pt -> pt
        member inline this.Coeff              = this.Unapply.Coeff
        member inline this.IndexedOps         = this.Unapply.IndexedOps
        member inline this.Signature          = this.Unapply.Signature
        member inline this.ScaleCoefficient c = this.Unapply.ScaleCoefficient c |> ProductTerm
        member inline this.AddCoefficient   c = this.Unapply.AddCoefficient   c |> ProductTerm
        member inline this.IsZero             = this.Unapply.IsZero
        static member inline (<*>) (l : PIxWkOp< ^idx, ^op>, r : PIxWkOp< ^idx, ^op>) =
            PIxOp< ^idx, ^op>.(<*>)(l.Unapply, r.Unapply)
            |> ProductTerm

        static member inline Op_IsRaisingOperator (op : ^op) =
            (^op : (member IsRaising : bool)(op))

        static member inline Op_IsLoweringOperator (op : ^op) =
            (^op : (member IsLowering : bool)(op))

        static member inline Op_InNormalOrder (l : ^op) (r : ^op) =
            (^op : (static member InNormalOrder : ^op -> ^op -> bool)(l, r))

        static member inline IsInNormalOrder (this : PIxWkOp< ^idx, ^op>) =
            this.IndexedOps
            |> Seq.map (fun ixop -> ixop.Op)
            |> (fun ops -> ops.IsOrdered PIxWkOp< ^idx, ^op>.Op_InNormalOrder)

        static member inline IsInIndexOrder (this : PIxWkOp< ^idx, ^op>) =
            let raisingOperatorsInOrder =
                this.IndexedOps
                |> Seq.filter (fun ixop -> PIxWkOp<_,_>.Op_IsRaisingOperator  ixop.Op)
                |> IxOp<_,_>.InIndexOrder Ascending

            let loweringOperatorsInOrder =
                this.IndexedOps
                |> Seq.filter (fun ixop -> PIxWkOp<_,_>.Op_IsLoweringOperator ixop.Op)
                |> IxOp<_,_>.InIndexOrder Descending

            PIxWkOp< ^idx, ^op>.IsInNormalOrder this && raisingOperatorsInOrder && loweringOperatorsInOrder


    type SIxWkOp< ^idx, ^op
            when ^idx : comparison
            and ^op : comparison
            and ^op : (member IsRaising  : bool)
            and ^op : (member IsLowering : bool)
            and ^op : (static member InNormalOrder : ^op -> ^op -> bool)
            and ^op : equality> =
        | SumTerm of S<PIxWkOp< ^idx, ^op>>
    with
        member inline this.Unapply = match this with SumTerm st -> st
        member inline __.Coeff     = Complex.One
        member inline this.IsZero  = this.Unapply.IsZero
        member inline this.Terms   = this.Unapply.Terms

        static member inline Apply = S<PIxWkOp< ^idx, ^op>>.Apply >> SumTerm

        static member inline (+) (l : SIxWkOp< ^idx, ^op>, r : SIxWkOp< ^idx, ^op>) =
            l.Unapply + r.Unapply
            |> SIxWkOp< ^idx, ^op>.SumTerm

        member inline this.AllTermsNormalOrdered () =
            this.Terms
            |> Seq.fold
                (fun result curr -> result && PIxWkOp<_,_>.IsInNormalOrder curr)
                true

        member inline this.AllTermsIndexOrdered  () =
            this.Terms
            |> Seq.fold
                (fun result curr -> result &&  PIxWkOp<_,_>.IsInIndexOrder curr)
                (this.AllTermsNormalOrdered ())


    type PIxOp< ^idx, ^op
                    when ^idx : comparison
                    and ^op : equality
                    and ^op : comparison>
    with
        static member inline (+) (l : PIxOp<_,_>, r : PIxOp<_,_>) : SIxOp< ^idx, ^op >=
            SIxOp< ^idx, ^op >.Apply(Complex.One, [| l; r |])
