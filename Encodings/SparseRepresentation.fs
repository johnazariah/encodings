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

    and CIxOp< ^idx, ^op when ^idx : comparison and ^op : equality> =
        | Indexed of C<IxOp< ^idx, ^op>>
    with
        member inline this.Unapply = match this with Indexed c -> c
        member inline this.IsZero = this.Unapply.IsZero
        static member inline Unit =
            CIxOp< ^idx, ^op>.Indexed <| C<_>.Unit
        static member inline Apply (coeff, unit) =
            CIxOp< ^idx, ^op>.Indexed <| C<_>.Apply (coeff, unit)
        static member inline Apply (unit) =
            CIxOp< ^idx, ^op>.Indexed <| C<_>.Apply (unit)

    and PIxOp< ^idx, ^op when ^idx : comparison and ^op : equality> =
        | ProductTerm of C<CIxOp< ^idx, ^op>[]>
    with
        member inline this.Unapply = match this with ProductTerm pt -> pt
        member inline this.Coeff = this.Unapply.Coeff
        member inline this.Units = this.Unapply.Item

        static member inline Apply = C<CIxOp< ^idx, ^op>>.Apply >> PIxOp< ^idx, ^op>.ProductTerm

        static member inline Unit = PIxOp<_,_>.Apply (Complex.One,  [||])
        static member inline Zero = PIxOp<_,_>.Apply (Complex.Zero, [||])

        member inline this.Reduce =
            lazy PIxOp<_,_>.ProductTerm this.Unapply.Reduce.Value

        member inline this.IsZero = this.Unapply.IsZero

        static member inline (*) (l : PIxOp<_,_>, r : PIxOp<_,_>) =
            PIxOp<_,_>.Apply (l.Coeff * r.Coeff, Array.concat [| l.Units; r.Units |])

        member inline this.ScaleCoefficient scale =
            PIxOp<_,_>.ProductTerm <| this.Unapply.ScaleCoefficient scale

        member inline this.AddCoefficient   coeff =
            PIxOp<_,_>.ProductTerm <| this.Unapply.AddCoefficient coeff

        member inline this.IsInIndexOrder indexOrder =
            lazy
                this.Units
                |> Seq.map (fun u -> u.Unapply.Item)
                |> IxOp<_,_>.IndicesInOrder indexOrder

    and SIxOp< ^idx, ^op when ^idx : comparison and ^op : equality> =
        | SumTerm of SC<CIxOp< ^idx, ^op>[]>
    with
        member inline this.Unapply = match this with SumTerm st -> st
        member inline __.Coeff = Complex.One
        member inline this.Terms = this.Unapply.Terms |> Array.map PIxOp< ^idx, ^op>.ProductTerm

        static member inline Apply (coeff : Complex, terms : PIxOp< ^idx, ^op>[]) =
            terms
            |> Array.map (fun pi -> pi.Unapply)
            |> (curry SC<_>.Apply coeff)
            |> SIxOp< ^idx, ^op>.SumTerm

        static member inline (+) (l : SIxOp< ^idx, ^op>, r : SIxOp< ^idx, ^op>) =
            l.Unapply + r.Unapply
            |> SIxOp<_, _>.SumTerm

        member inline this.IsZero = this.Unapply.IsZero

        member inline this.AllTermsIndexOrdered indexOrder =
            let isIndexOrdered result (curr : PIxOp<_,_>) =
                let currIsIndexOrdered = curr.IsInIndexOrder indexOrder
                result && currIsIndexOrdered.Value
            lazy
                this.Terms
                |> Seq.fold isIndexOrdered true

