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
        member inline this.Signature = sprintf "%O%O" this.Op this.Index

        static member inline InIndexOrder (indexOrder : IndexOrder) (ops : IxOp< ^idx, ^op> seq) : bool =
            let comparer =
                match indexOrder with
                | Ascending  -> (.<=.)
                | Descending -> (.>=.)
            ops.IsOrdered comparer

    type CIxOp< ^idx, ^op
            when ^idx : comparison
            and ^op : equality
            and ^op : comparison> =
        | Indexed of C<IxOp< ^idx, ^op>>
    with
        member inline this.Unapply   = match this with Indexed c -> c
        member inline this.IsZero    = this.Unapply.IsZero
        member inline this.Coeff     = this.Unapply.Coeff
        member inline this.IndexedOp = this.Unapply.Thunk
        member inline this.Normalize = this.Unapply.Normalize
        member inline this.Signature = this.IndexedOp.Signature
        static member inline Unit    = CIxOp< ^idx, ^op>.Indexed C<IxOp< ^idx, ^op>>.Unit
        static member inline Apply   = C<IxOp< ^idx, ^op>>.Apply >> CIxOp< ^idx, ^op>.Indexed
        static member inline New(coeff, index, op) = IxOp<_,_>.Apply(index, op) |> curry C<_>.Apply coeff |> Indexed

    type PIxOp< ^idx, ^op
            when ^idx : comparison
            and ^op : equality
            and ^op : comparison> =
        | ProductTerm of C<IxOp< ^idx, ^op>[]>
    with
        member inline this.Unapply    = match this with ProductTerm pt -> pt
        member inline this.Coeff      = this.Unapply.Coeff
        member inline this.IndexedOps = this.Unapply.Thunk

        static member inline internal ApplyInternal = C<_>.Apply >> ProductTerm

        static member inline Unit = PIxOp< ^idx, ^op >.ApplyInternal (Complex.One,  [| |])
        static member inline Zero = PIxOp< ^idx, ^op >.ApplyInternal (Complex.Zero, [| |])

        static member inline (<*>) (l : PIxOp<_,_>, r : PIxOp<_,_>) =
            let indexedOps = Array.concat [| l.IndexedOps; r.IndexedOps |]
            let coeff = l.Coeff * r.Coeff
            PIxOp< ^idx, ^op >.ApplyInternal (coeff, indexedOps)

        member inline this.ScaleCoefficient scale = this.Unapply.ScaleCoefficient scale |> PIxOp<_,_>.ProductTerm
        member inline this.AddCoefficient   diff  = this.Unapply.AddCoefficient   diff  |> PIxOp<_,_>.ProductTerm

        member inline this.IsZero = this.Coeff.IsZero || this.IndexedOps = [| |]

        member inline this.Reduce =
            if this.IsZero then
                PIxOp< ^idx, ^op >.Zero
            else
                this

        member inline this.Signature =
            this.Reduce.IndexedOps |> Array.fold (fun result curr -> sprintf "%s%s" result curr.Signature) ""

        member inline this.IsInIndexOrder indexOrder =
            lazy
                this.IndexedOps |> IxOp<_,_>.InIndexOrder indexOrder

        static member inline Apply (coeff : Complex, units : CIxOp< ^idx, ^op >[]) =
            let extractedCoeff = units |> Array.fold (fun coeff curr -> coeff * curr.Coeff) Complex.One
            let indexedOps     = units |> Array.map  (fun curr -> curr.IndexedOp)
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
            and ^op : (static member Commute : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> C<IxOp< ^idx, ^op >[]>[])
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

        static member inline Apply =
            C<_>.Apply
            >> PIxOp< ^idx, ^op>.ProductTerm
            >> PIxWkOp< ^idx, ^op>.ProductTerm

        static member inline (<*>) (l : PIxWkOp< ^idx, ^op>, r : PIxWkOp< ^idx, ^op>) =
            PIxOp< ^idx, ^op>.(<*>)(l.Unapply, r.Unapply) |> ProductTerm

        static member inline Op_IsRaisingOperator op =
            (^op : (member IsRaising : bool)(op))

        static member inline Op_IsLoweringOperator op =
            (^op : (member IsLowering : bool)(op))

        static member inline Op_InNormalOrder l r =
            (^op : (static member InNormalOrder : ^op -> ^op -> bool)(l, r))

        static member inline Op_Swap this other =
            (^op : (static member Commute : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> C<IxOp< ^idx, ^op >[]>[])(this, other))

        member inline this.IsInNormalOrder =
            lazy
                this.IndexedOps
                |> Seq.map (fun ixop -> ixop.Op)
                |> (fun ops -> ops.IsOrdered PIxWkOp< ^idx, ^op>.Op_InNormalOrder)

        member inline this.IsInIndexOrder =
            let raisingOperatorsInOrder =
                this.IndexedOps
                |> Seq.filter (fun ixop -> PIxWkOp<_,_>.Op_IsRaisingOperator  ixop.Op)
                |> IxOp<_,_>.InIndexOrder Ascending

            let loweringOperatorsInOrder =
                this.IndexedOps
                |> Seq.filter (fun ixop -> PIxWkOp<_,_>.Op_IsLoweringOperator ixop.Op)
                |> IxOp<_,_>.InIndexOrder Descending

            lazy
                this.IsInNormalOrder.Value &&
                raisingOperatorsInOrder &&
                loweringOperatorsInOrder

        member inline this.IndexedOpsGroupedByIndex =
            let extractItemsWithIndex target indexedOps =
                let rec split index pre (ops : IxOp<_,_>[]) =
                    match ops with
                    | [| |] -> (pre, None, [| |])
                    | [| first |] ->
                        if first.Index = index then
                            (pre, Some first, [| |])
                        else
                            (Array.append pre ([| first |]), None, [| |])
                    | _ ->
                        let head = ops.[0]
                        let rest = ops.[1..]
                        if (head.Index = index) then
                            (pre, Some head, rest)
                        else
                            split index ((Array.append pre ([| head |]))) rest

                let swapSingle target (curr : IxOp<_,_>)  (c, xs) =
                    let product = PIxWkOp<_,_>.Op_Swap curr target
                    System.Diagnostics.Debug.Assert(product.Length = 1, "algebra limitation: commuting terms with same index produced a sum term!")
                    (c * product.[0].Coeff, Array.append product.[0].Thunk.[1..] xs)

                let (pre, found, post) = split target.Index [| |] indexedOps

                match found with
                | Some head ->
                    let (coeff, swapped) =
                        Array.foldBack (swapSingle head) pre (Complex.One, [| |]) 
                    ((coeff, [| head |]), Array.append swapped post)
                | None ->
                    ((Complex.One, [| |]), Array.append pre post)

            let rec behead (ixops) (result) =
                match ixops with
                | [| |] -> result
                | [| first |] ->
                    [|
                        yield! result
                        yield PIxWkOp<_,_>.Apply(Complex.One, [| first |])
                    |]

                | _ ->
                    let first = ixops.[0]
                    let ((coeff, matching), others) =
                        ixops.[1..]
                        |> extractItemsWithIndex first
                    [|
                        yield! result
                        yield PIxWkOp<_,_>.Apply(coeff, [| first; yield! matching |])
                    |]
                    |> behead others
            in
            lazy
                behead (this.IndexedOps) ([| |])

        member inline this.ToNormalOrder =        
            let swapOps (term : PIxWkOp<_,_>) =
                PIxWkOp<_,_>.Op_Swap term.IndexedOps.[0] term.IndexedOps.[1]
                |> Array.map (fun t -> PIxWkOp<_,_>.Apply(t.Coeff, t.Thunk))
            lazy
                this.IndexedOpsGroupedByIndex.Value
                |> Array.map (fun productTerm ->
                    match productTerm.IndexedOps.Length with
                    | 1 -> 
                        [| productTerm |]
                    | 2 -> 
                        if productTerm.IsInNormalOrder.Value then
                            [| productTerm |]
                        else
                            swapOps productTerm
                    | _ -> [| |])

    type SIxWkOp< ^idx, ^op
                    when ^idx : comparison
                    and ^op : comparison
                    and ^op : (member IsRaising  : bool)
                    and ^op : (member IsLowering : bool)
                    and ^op : (static member InNormalOrder : ^op -> ^op -> bool)
                    and ^op : (static member Commute : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> C<IxOp< ^idx, ^op >[]>[])
                    and ^op : (static member Combine : PIxWkOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> PIxWkOp< ^idx, ^op >[])
                    and ^op : equality> =
        | SumTerm of S<PIxWkOp< ^idx, ^op>>
    with
        member inline this.Unapply = match this with SumTerm st -> st
        member inline __.Coeff     = Complex.One
        member inline this.IsZero  = this.Unapply.IsZero
        member inline this.Terms   = this.Unapply.Terms
        static member inline Apply = S<PIxWkOp< ^idx, ^op>>.Apply >> SumTerm

        static member inline (+) (l : SIxWkOp< ^idx, ^op>, r : SIxWkOp< ^idx, ^op>) =
            l.Unapply + r.Unapply |> SumTerm

        static member inline (*) (l : SIxWkOp< ^idx, ^op>, r : SIxWkOp< ^idx, ^op>) =
            l.Unapply * r.Unapply |> SumTerm

        member inline this.AllTermsNormalOrdered =
            lazy
                this.Terms
                |> Seq.fold
                    (fun result curr -> result && curr.IsInNormalOrder.Value)
                    true

        member inline this.AllTermsIndexOrdered =
            lazy
                this.Terms
                |> Seq.fold
                    (fun result curr -> result &&  curr.IsInIndexOrder.Value)
                    (this.AllTermsNormalOrdered.Value)

        member inline this.SortNormalOrder =
            if this.AllTermsNormalOrdered.Value then
                lazy this
            else
                let rec sortSingleProductTerm (p : PIxWkOp< ^idx, ^op >) =
                    let combine nextUnit productTerm =
                        (^op : (static member Combine : PIxWkOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> PIxWkOp< ^idx, ^op >[])(productTerm, nextUnit))

                    let rec sortInternal (result : PIxWkOp< ^idx, ^op >[]) (remainingUnits : IxOp< ^idx, ^op >[]) : PIxWkOp< ^idx, ^op >[]=
                        if remainingUnits.Length = 0 then
                            result
                        else
                            let nextUnit        = remainingUnits.[0]
                            let remainingUnits' = remainingUnits.[1..]
                            let result' =
                                if result.Length = 0 then
                                    [| PIxWkOp< ^idx, ^op >.Apply(Complex.One, [| nextUnit |]) |]
                                else
                                    result |> Array.collect (combine nextUnit)
                            sortInternal result' remainingUnits'

                    if p.IsInNormalOrder.Value then
                        [| p |]
                    else
                        sortInternal ([| |]) p.IndexedOps
                        |> Array.collect (sortSingleProductTerm)
                lazy
                    this.Terms
                    |> Array.collect sortSingleProductTerm
                    |> curry SIxWkOp< ^idx, ^op>.Apply Complex.One

    type PIxOp< ^idx, ^op
                    when ^idx : comparison
                    and ^op : equality
                    and ^op : comparison>
    with
        static member inline (+) (l : PIxOp<_,_>, r : PIxOp<_,_>) : SIxOp< ^idx, ^op >=
            SIxOp< ^idx, ^op >.Apply(Complex.One, [| l; r |])

