﻿namespace Encodings

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

    and IxOp< ^idx, ^op when ^idx : comparison and ^op : equality> =
        { Index : ^idx; Op : ^op }
    with
        static member inline Apply (index : ^idx, op : ^op) = { Index = index; Op = op }
        static member inline (.>=.) (l : IxOp< ^idx, ^op >, r : IxOp< ^idx, ^op >) = l.Index >= r.Index
        static member inline (.<=.) (l : IxOp< ^idx, ^op >, r : IxOp< ^idx, ^op >) = l.Index <= r.Index
        static member inline IndicesInOrder (indexOrder : IndexOrder) (ops : IxOp< ^idx, ^op> seq) : bool =
            let comparer =
                match indexOrder with
                | Ascending  -> (.<=.)
                | Descending -> (.>=.)
            ops.IsOrdered comparer

        member inline this.Signature =
            sprintf "%O%O" this.Op this.Index

    type CIxOp< ^idx, ^op when ^idx : comparison and ^op : equality> =
        | Indexed of C<IxOp< ^idx, ^op>>
    with
        member inline this.Unapply = match this with Indexed c -> c
        member inline this.IsZero = this.Unapply.IsZero
        member inline this.Coeff  = this.Unapply.Coeff
        member inline this.IndexedOp = this.Unapply.Item
        member inline this.Normalize = this.Unapply.Normalize

        static member inline Unit =
            CIxOp< ^idx, ^op>.Indexed <| C<_>.Unit

        static member inline Apply (coeff, unit) =
            CIxOp< ^idx, ^op>.Indexed <| C<_>.Apply (coeff, unit)

        member inline this.Signature =
            this.IndexedOp.Signature

    type PIxOp< ^idx, ^op when ^idx : comparison and ^op : equality> =
        | ProductTerm of C<IxOp< ^idx, ^op>[]>
    with
        member inline this.Unapply = match this with ProductTerm pt -> pt
        member inline this.Coeff = this.Unapply.Coeff
        member inline this.IndexedOps = this.Unapply.Item

        static member inline internal ApplyInternal =
            C<_>.Apply >> PIxOp< ^idx, ^op >.ProductTerm

        static member inline Unit = PIxOp<_,_>.ApplyInternal (Complex.One,  [||])
        static member inline Zero = PIxOp<_,_>.ApplyInternal (Complex.Zero, [||])

        static member inline (*) (l : PIxOp<_,_>, r : PIxOp<_,_>) =
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
                |> IxOp<_,_>.IndicesInOrder indexOrder

        static member inline Apply (coeff : Complex, units : CIxOp< ^idx, ^op >[]) =
            let extractedCoeff =
                units
                |> Array.fold (fun coeff curr -> coeff * curr.Coeff) Complex.One

            let indexedOps =
                units
                |> Array.map (fun curr -> curr.IndexedOp)

            PIxOp<_,_>.ApplyInternal (coeff * extractedCoeff, indexedOps)

    type SIxOp< ^idx, ^op when ^idx : comparison and ^op : equality> =
        | SumTerm of SC<PIxOp< ^idx, ^op >>
    with
        member inline this.Unapply = match this with SumTerm st -> st
        member inline __.Coeff = Complex.One

        member inline this.Terms =
            this.Unapply.Terms

        static member inline Apply (coeff : Complex, terms : PIxOp< ^idx, ^op>[]) : SIxOp< ^idx, ^op> =
            terms
            |> Array.map (curry C<_>.Apply Complex.One)
            |> (curry SC<PIxOp< ^idx, ^op>>.Apply coeff)
            |> SIxOp< ^idx, ^op>.SumTerm

        static member inline (+) (l : SIxOp< ^idx, ^op>, r : SIxOp< ^idx, ^op>) =
            l.Unapply + r.Unapply
            |> SIxOp<_, _>.SumTerm

        member inline this.IsZero =
            this.Unapply.IsZero

        member inline this.AllTermsIndexOrdered indexOrder =
            let isIndexOrdered result (curr : PIxOp<_,_>) =
                let currIsIndexOrdered = curr.IsInIndexOrder indexOrder
                result && currIsIndexOrdered.Value
            lazy
                this.Terms
                |> Seq.map (fun t -> t.Item)
                |> Seq.fold isIndexOrdered true

    and PIxOp< ^idx, ^op when ^idx : comparison and ^op : equality> with
        static member inline (+) (l : PIxOp<_,_>, r : PIxOp<_,_>) : SIxOp< ^idx, ^op >=
            SIxOp< ^idx, ^op >.Apply(Complex.One, [| l; r |])
