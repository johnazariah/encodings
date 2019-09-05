namespace Encodings

[<AutoOpen>]
module IndexedTerms =
    let isOrdered (comparer : 'a -> 'a -> bool) (xs : 'a seq) =
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

    and IxOp<'idx, 'op when 'idx : comparison and 'op : equality> =
        { Index : 'idx; Op : 'op }
    with
        static member Apply (index, (op : 'op)) = { Index = index; Op = op }
        static member (.>=.) (l : IxOp<'idx, 'op>, r : IxOp<'idx, 'op>) = l.Index >= r.Index
        static member (.<=.) (l : IxOp<'idx, 'op>, r : IxOp<'idx, 'op>) = l.Index <= r.Index
        static member WithMinIndex = { IxOp.Index = (System.UInt32.MinValue); Op = Unchecked.defaultof<'op> }
        static member WithMaxIndex = { IxOp.Index = (System.UInt32.MaxValue); Op = Unchecked.defaultof<'op> }
        static member IndicesInOrder (indexOrder : IndexOrder) (ops : IxOp<'idx, 'op> seq) : bool =
            let comparer =
                match indexOrder with
                | Ascending  -> (.<=.)
                | Descending -> (.>=.)
            ops |> isOrdered comparer

        static member TryCreateFromString
            (unitFactory : string -> 'op option)
            (s : System.String) =
            try
                s.Trim().TrimStart('(').TrimEnd(')').Split(',')
                |> Array.map (fun s -> s.Trim ())
                |> (fun rg ->
                    unitFactory (rg.[0])
                    |> Option.map (fun op ->
                        IxOp<_,_>.Apply(System.UInt32.Parse rg.[1], op)))
            with
            | _ -> None

        override this.ToString() =
            sprintf "(%O, %O)" this.Op this.Index

    and PIxOp<'idx, 'op when 'idx : comparison and 'op : equality> =
        | ProductTerm of P<IxOp<'idx, 'op>>
    with
        member this.Unapply = match this with ProductTerm term -> term
        member this.Units = lazy this.Unapply.Units

        static member TryCreateFromString (unitFactory : string -> 'op option) =
            P<IxOp<'idx, 'op>>.TryCreateFromString (IxOp<_,_>.TryCreateFromString unitFactory)
            >> Option.map ProductTerm

        member this.IsInIndexOrder indexOrder =
            this.Units.Value
            |> Seq.map (fun u -> u.Item)
            |> IxOp<_,_>.IndicesInOrder indexOrder

        member this.IsInIndexOrderAscending  = this.IsInIndexOrder Ascending
        member this.IsInIndexOrderDescending = this.IsInIndexOrder Descending

        override this.ToString() = this.Unapply.ToString()

    and SIxOp<'idx, 'op when 'idx : comparison and 'op : equality> =
        | SumTerm of S<IxOp<'idx, 'op>>
    with
        member this.Unapply = match this with SumTerm term -> term
        member this.ProductTerms = this.Unapply.ProductTerms

        static member TryCreateFromString (unitFactory : string -> 'op option) =
            S<IxOp<'idx,'op>>.TryCreateFromString (IxOp<_, _>.TryCreateFromString unitFactory)
            >> Option.map SumTerm

        member this.AllTermsIndexOrdered indexOrder  =
            lazy
                this.ProductTerms.Value
                |> Seq.map ProductTerm
                |> Seq.fold (fun result curr -> result && curr.IsInIndexOrder indexOrder) true
        override this.ToString() = this.Unapply.ToString()