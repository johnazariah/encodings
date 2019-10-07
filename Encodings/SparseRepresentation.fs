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

        static member inline (<+>) (l : SIxOp< ^idx, ^op>, r : SIxOp< ^idx, ^op>) =
            l.Unapply <+> r.Unapply |> SumTerm

        static member inline (<*>) (l : SIxOp< ^idx, ^op>, r : SIxOp< ^idx, ^op>) =
            l.Unapply <*> r.Unapply |> SumTerm

        member inline this.AllTermsIndexOrdered indexOrder =
            let isIndexOrdered result (curr : PIxOp<_,_>) =
                let currIsIndexOrdered = curr.IsInIndexOrder indexOrder
                result && currIsIndexOrdered.Value

            lazy
                this.Terms |> Seq.fold isIndexOrdered true

    type PIxWkOp< ^idx, ^op
            when ^idx : comparison
            and ^op : comparison
            and ^op : (member IsIdentity  : bool)
            and ^op : (member IsRaising  : bool)
            and ^op : (member IsLowering : bool)
            and ^op : (static member InNormalOrder : ^op -> ^op -> bool)
            and ^op : (static member Swap : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> C<IxOp< ^idx, ^op >[]>[])
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
        
        static member inline Op_IsIdentityOperator op =
            (^op : (member IsIdentity : bool)(op))

        static member inline Op_IsRaisingOperator op =
            (^op : (member IsRaising : bool)(op))

        static member inline Op_IsLoweringOperator op =
            (^op : (member IsLowering : bool)(op))

        static member inline Op_InNormalOrder l r =
            (^op : (static member InNormalOrder : ^op -> ^op -> bool)(l, r))

        static member inline Op_Swap this other =
            (^op : (static member Swap : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> C<IxOp< ^idx, ^op >[]>[])(this, other))

        static member inline Apply (coeff, ixops : IxOp<_,_>[]) =
            let isIdentityTerm (t : IxOp<_,_>) = PIxWkOp< ^idx, ^op>.Op_IsIdentityOperator t.Op
            let identityOpExists = 
                ixops
                |> Array.exists isIdentityTerm

            let opsExceptIdentity =
                ixops
                |> Array.filter (isIdentityTerm >> not)

            let ops = 
                if opsExceptIdentity = [| |] && identityOpExists then
                    [| ixops.[0] |]
                else
                    opsExceptIdentity

            (coeff, ops)
            |> (C<_>.Apply >> PIxOp< ^idx, ^op>.ProductTerm >> PIxWkOp< ^idx, ^op>.ProductTerm)

        static member inline (<*>) (l : PIxWkOp< ^idx, ^op>, r : PIxWkOp< ^idx, ^op>) =
            PIxOp< ^idx, ^op>.(<*>)(l.Unapply, r.Unapply) |> ProductTerm

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
            let rec findItemsWithMatchingIndex result target indexedOps =
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

                let includeSingleOp target (curr : IxOp<_,_>)  (c, xs) =
                    let product = PIxWkOp<_,_>.Op_Swap curr target
                    System.Diagnostics.Debug.Assert(product.Length = 1, "algebra limitation: commuting terms with same index produced a sum term!")
                    (c * product.[0].Coeff, Array.append product.[0].Thunk.[1..] xs)

                let (pre, found, post) = split target.Index [| |] indexedOps

                match found with
                | Some head ->
                    let (coeff, swapped) =
                        Array.foldBack (includeSingleOp head) pre (Complex.One, [| |])
                    let ((resultCoeff, resultMatching), resultRest) = result
                    let result' = ((coeff * resultCoeff, Array.append resultMatching [| head |]), Array.append resultRest swapped)
                    findItemsWithMatchingIndex result' target post
                | None ->
                    let ((resultCoeff, resultMatching), resultRest) = result
                    let result' = ((resultCoeff, resultMatching), Array.append resultRest pre)
                    result'

            let rec toChunksWithCommonIndex (coeff, ixops) (result) =
                match ixops with
                | [| |] -> result
                | [| first |] ->
                    [|
                        yield! result
                        yield PIxWkOp<_,_>.Apply(coeff, [| first |])
                    |]

                | _ ->
                    let first = ixops.[0]
                    let ((coeff', matching), others) = findItemsWithMatchingIndex ((coeff, [| |]),[| |]) first ixops.[1..]                    
                    [|
                        yield! result
                        yield PIxWkOp<_,_>.Apply(coeff', [| first; yield! matching |])
                    |]
                    |> toChunksWithCommonIndex (coeff, others)
            in
            lazy
                toChunksWithCommonIndex (this.Coeff, this.IndexedOps) ([| |])

        member inline this.ToNormalOrder = 
            let joinTerm (sorted : PIxWkOp<_,_>) (candidate : PIxWkOp<_,_>) : PIxWkOp<_,_> =
                let rec includeSingleOp (op : IxOp<_,_>) (coeff, pre, post) : PIxWkOp<_,_> =
                    let candidate = PIxWkOp<_,_>.Apply (coeff, [| yield! pre; op; yield! post |]) 
                    if candidate.IsInNormalOrder.Value then
                        candidate
                    else
                        let swapped = PIxWkOp<_,_>.Op_Swap (Array.last pre) op
                        System.Diagnostics.Debug.Assert (swapped.Length = 1, "algebra limitation - commuting terms with different index produced a sum term!")
                        let swapped' = swapped.[0]
                        includeSingleOp op (coeff * swapped'.Coeff, (Array.allButLast pre), ([| Array.last pre; yield! post |]))

                candidate.IndexedOps
                |> Array.fold (fun result curr -> includeSingleOp curr (result.Coeff, result.IndexedOps, [| |])) sorted
                |> (fun result -> result.ScaleCoefficient candidate.Coeff)

            let sortTerm (chunk : PIxWkOp<_,_>) : PIxWkOp<_,_>[] =
                match chunk.IndexedOps.Length with
                | 1 -> [| chunk |]
                | 2 ->
                    if chunk.IsInNormalOrder.Value then
                        [| chunk |]
                    else
                        PIxWkOp<_,_>.Op_Swap chunk.IndexedOps.[0] chunk.IndexedOps.[1]
                        |> Array.map (fun c -> PIxWkOp<_,_>.Apply(c.Coeff * chunk.Coeff, c.Thunk))
                | _ -> [| |]

            let includeChunk (state : PIxWkOp<_, _>[][]) (chunk : PIxWkOp<_,_>) : PIxWkOp<_, _>[][] =
                if state = [| |] then
                    [| 
                        sortTerm chunk
                    |]
                else
                    [|
                        for stateChunk in state do
                            [|
                                for sorted in stateChunk do 
                                    for candidate in sortTerm chunk do
                                        yield joinTerm sorted candidate
                            |]
                    |]

            lazy
                let chunks = this.IndexedOpsGroupedByIndex.Value
                chunks |> Array.fold includeChunk [| |]

    type SIxWkOp< ^idx, ^op
                    when ^idx : comparison
                    and ^op : comparison
                    and ^op : (member IsIdentity  : bool)
                    and ^op : (member IsRaising  : bool)
                    and ^op : (member IsLowering : bool)
                    and ^op : (static member InNormalOrder : ^op -> ^op -> bool)
                    and ^op : (static member Swap : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> C<IxOp< ^idx, ^op >[]>[])
                    and ^op : equality> =
        | SumTerm of S<PIxWkOp< ^idx, ^op>>
    with
        member inline this.Unapply = match this with SumTerm st -> st
        member inline __.Coeff     = Complex.One
        member inline this.IsZero  = this.Unapply.IsZero
        member inline this.Terms   = this.Unapply.Terms
        static member inline Apply = S<PIxWkOp< ^idx, ^op>>.Apply >> SumTerm

        static member inline (<+>) (l : SIxWkOp< ^idx, ^op>, r : SIxWkOp< ^idx, ^op>) =
            l.Unapply <+> r.Unapply |> SumTerm

        static member inline (<*>) (l : SIxWkOp< ^idx, ^op>, r : SIxWkOp< ^idx, ^op>) =
            l.Unapply <*> r.Unapply |> SumTerm

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

        member inline this.ToNormalOrder =
            if this.AllTermsNormalOrdered.Value then
                lazy this
            else
                let sortSingleProductTerm (p : PIxWkOp< ^idx, ^op >) = 
                    p.ToNormalOrder.Value
                    |> Array.map (curry SIxWkOp< ^idx, ^op>.Apply Complex.One)
                    |> Array.reduce (<+>)

                lazy
                    this.Terms
                    |> Array.map sortSingleProductTerm
                    |> Array.reduce (<+>)

    type PIxOp< ^idx, ^op
                    when ^idx : comparison
                    and ^op : equality
                    and ^op : comparison>
    with
        static member inline (+) (l : PIxOp<_,_>, r : PIxOp<_,_>) : SIxOp< ^idx, ^op >=
            SIxOp< ^idx, ^op >.Apply(Complex.One, [| l; r |])

