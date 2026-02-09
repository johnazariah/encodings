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

    /// An indexed operator: pairs an index (e.g., qubit number) with an operator.
    and IxOp<'idx, 'op when 'idx : comparison and 'op : equality> =
        { Index : 'idx; Op : 'op }
    with
        static member Apply (index, (op : 'op)) = { Index = index; Op = op }
        static member (.>=.) (l : IxOp<'idx, 'op>, r : IxOp<'idx, 'op>) = l.Index >= r.Index
        static member (.<=.) (l : IxOp<'idx, 'op>, r : IxOp<'idx, 'op>) = l.Index <= r.Index
        static member IndicesInOrder (indexOrder : IndexOrder) (ops : IxOp<'idx, 'op> seq) : bool =
            let comparer =
                match indexOrder with
                | Ascending  -> (.<=.)
                | Descending -> (.>=.)
            ops |> isOrdered comparer

        /// Parse an IxOp from a string like "(op, index)", with a custom index parser.
        static member TryCreateFromStringWith
            (indexParser : string -> 'idx option)
            (unitFactory : string -> 'op option)
            (s : System.String) =
            try
                s.Trim().TrimStart('(').TrimEnd(')').Split(',')
                |> Array.map (fun s -> s.Trim ())
                |> (fun rg ->
                    match unitFactory rg.[0], indexParser rg.[1] with
                    | Some op, Some idx -> Some (IxOp<_,_>.Apply(idx, op))
                    | _ -> None)
            with
            | _ -> None

        override this.ToString() =
            sprintf "(%O, %O)" this.Op this.Index

    /// Parse an IxOp<uint32, 'op> from a string like "(op, 123)".
    let tryParseIxOpUint32 (unitFactory : string -> 'op option) : string -> IxOp<uint32, 'op> option =
        let parseUint32 (s : string) =
            match System.UInt32.TryParse s with
            | true, v -> Some v
            | false, _ -> None
        IxOp<_,_>.TryCreateFromStringWith parseUint32 unitFactory

    /// A product of indexed operators.
    type PIxOp<'idx, 'op when 'idx : comparison and 'op : equality> =
        | ProductTerm of P<IxOp<'idx, 'op>>
    with
        member this.Unapply = match this with ProductTerm term -> term
        member this.Units = lazy this.Unapply.Units

        static member TryCreateFromString (unitFactory : string -> 'op option) =
            P<IxOp<uint32, 'op>>.TryCreateFromString (tryParseIxOpUint32 unitFactory)
            >> Option.map ProductTerm

        member this.IsInIndexOrder indexOrder =
            this.Units.Value
            |> Seq.map (fun u -> u.Item)
            |> IxOp<_,_>.IndicesInOrder indexOrder

        member this.IsInIndexOrderAscending  = this.IsInIndexOrder Ascending
        member this.IsInIndexOrderDescending = this.IsInIndexOrder Descending

        override this.ToString() = this.Unapply.ToString()

    /// A sum of products of indexed operators.
    and SIxOp<'idx, 'op when 'idx : comparison and 'op : equality> =
        | SumTerm of S<IxOp<'idx, 'op>>
    with
        member this.Unapply = match this with SumTerm term -> term
        member this.ProductTerms = this.Unapply.ProductTerms

        static member TryCreateFromString (unitFactory : string -> 'op option) =
            S<IxOp<uint32,'op>>.TryCreateFromString (tryParseIxOpUint32 unitFactory)
            >> Option.map SumTerm

        member this.AllTermsIndexOrdered indexOrder  =
            lazy
                this.ProductTerms.Value
                |> Seq.map ProductTerm
                |> Seq.fold (fun result curr -> result && curr.IsInIndexOrder indexOrder) true
        override this.ToString() = this.Unapply.ToString()