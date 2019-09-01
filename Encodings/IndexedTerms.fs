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


    type IndexOrder = Ascending | Descending
    and Ix<'op when 'op : equality> =
        { Index : uint32; Op : 'op }
    with
        static member Apply (index, value) = { Index = index; Op = value }
        static member (.>=.) (l : Ix<'op>, r : Ix<'op>) = l.Index >= r.Index
        static member (.<=.) (l : Ix<'op>, r : Ix<'op>) = l.Index <= r.Index
        static member WithMinIndex = { Ix.Index = (System.UInt32.MinValue); Op = Unchecked.defaultof<'op> }
        static member WithMaxIndex = { Ix.Index = (System.UInt32.MaxValue); Op = Unchecked.defaultof<'op> }
        static member IndicesInOrder indexOrder (ops : Ix<'op> seq) =
            let comparer =
                match indexOrder with
                | Ascending  -> (.<=.)
                | Descending -> (.>=.)
            ops |> isOrdered comparer

    and IndexedOperator<'op when 'op : equality> =
        | Op of Ix<C<'op>>
    with
        static member Apply op index = Op <| Ix<_>.Apply (index, C<_>.Apply op)
        member this.Unapply =
            let (Op op) = this
            op
        static member TryCreateFromString
            (unitFactory : string -> 'op option)
            (s : System.String) =
            try
                s.Trim().TrimStart('(').TrimEnd(')').Split(',')
                |> Array.map (fun s -> s.Trim ())
                |> (fun rg ->
                    unitFactory (rg.[0])
                    |> Option.map (fun op ->
                        let index = System.UInt32.Parse rg.[1]
                        (index, C<'op>.Apply op)
                        |> Ix<'op>.Apply
                        |> Op))
            with
            | _ -> None

        override this.ToString() =
            sprintf "(%O, %O)" this.Unapply.Op this.Unapply.Index

    and ProductOfIndexedOperators<'op when 'op : equality> =
        | ProductTerm of P<IndexedOperator<'op>>
    with
        member this.Unapply = match this with ProductTerm term -> term

        static member TryCreateFromString (unitFactory : string -> 'op option) =
            P<IndexedOperator<'op>>.TryCreateFromString (IndexedOperator<'op>.TryCreateFromString unitFactory)
            >> Option.map ProductTerm

        member this.IsInIndexOrder ascending =
            this.Unapply.Units
            |> Seq.map (fun u -> u.Item.Unapply)
            |> Ix<_>.IndicesInOrder ascending

        member this.IsInIndexOrderAscending  = this.IsInIndexOrder Ascending
        member this.IsInIndexOrderDescending = this.IsInIndexOrder Descending

        override this.ToString() = this.Unapply.ToString()

    and SumOfProductsOfIndexedOperators<'op when 'op : equality> =
        | SumTerm of S<IndexedOperator<'op>>
    with
        member this.Unapply =
            let (SumTerm term) = this;
            term
        static member TryCreateFromString
            (unitFactory : string -> 'op option)
            (s : System.String) : SumOfProductsOfIndexedOperators<'op> option =
            let f = ProductOfIndexedOperators<'op>.TryCreateFromString unitFactory
            try
                s.Trim().TrimStart('{').TrimEnd('}').Split(';')
                |> Array.choose (f >> Option.map (fun o -> o.Unapply))
                |> S<IndexedOperator<'op>>.Apply
                |> SumTerm
                |> Some
            with
            | _ -> None
