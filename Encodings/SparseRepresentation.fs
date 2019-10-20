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
            and ^op : (member IsIdentity  : bool)
            and ^op : (static member InIndexOrder    : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> bool)
            and ^op : (static member InOperatorOrder : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> bool)
            and ^op : (static member ToIndexOrder    : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> C<IxOp< ^idx, ^op >[]>[])
            and ^op : (static member ToOperatorOrder : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> C<IxOp< ^idx, ^op >[]>[])
            and ^op : (static member NextIndexLocation : ^op * IxOp< ^idx, ^op >[] -> ^idx option )
            and ^op : comparison> =
        | ProductTerm of C<IxOp< ^idx, ^op>[]>
    with
        static member inline Op_IsIdentityOperator op =
            (^op : (member IsIdentity              : bool)(op))
        static member inline Op_InIndexOrder    a b =
            (^op : (static member InIndexOrder    : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> bool)(a, b))
        static member inline Op_InOperatorOrder a b =
            (^op : (static member InOperatorOrder : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> bool)(a, b))
        static member inline Op_ToOperatorOrder a b =
            (^op : (static member ToOperatorOrder : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> C<IxOp< ^idx, ^op >[]>[])(a, b))
        static member inline Op_ToIndexOrder a b =
            (^op : (static member ToIndexOrder    : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> C<IxOp< ^idx, ^op >[]>[])(a, b))

        member inline this.Unapply    = match this with ProductTerm pt -> pt
        member inline this.Coeff      = this.Unapply.Coeff
        member inline this.IndexedOps = this.Unapply.Thunk

        static member inline internal ApplyInternal (coeff, ixops : IxOp<_,_>[]) =
            let isIdentityTerm (t : IxOp<_,_>) = PIxOp< ^idx, ^op>.Op_IsIdentityOperator t.Op
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

            (coeff, ops) |> (C<_>.Apply >> ProductTerm)

        static member inline Unit = PIxOp< ^idx, ^op >.ApplyInternal (Complex.One,  [| |])
        static member inline Zero = PIxOp< ^idx, ^op >.ApplyInternal (Complex.Zero, [| |])

        static member inline (<*>) (l : PIxOp<_,_>, r : PIxOp<_,_>) =
            let indexedOps = Array.concat [| l.IndexedOps; r.IndexedOps |]
            let coeff = l.Coeff * r.Coeff
            PIxOp< ^idx, ^op >.ApplyInternal (coeff, indexedOps)

        member inline this.ScaleCoefficient scale = scale |> (this.Unapply.ScaleCoefficient >> ProductTerm)
        member inline this.AddCoefficient   diff  = diff  |> (this.Unapply.AddCoefficient   >> ProductTerm)

        member inline this.IsZero = this.Coeff.IsZero || this.IndexedOps = [| |]

        member inline this.Reduce =
            if this.IsZero then
                PIxOp< ^idx, ^op >.Zero
            else
                this

        member inline this.Signature =
            this.Reduce.IndexedOps
            |> Array.fold
                (fun result curr -> sprintf "%s%s" result curr.Signature)
                ""

        member inline this.IsInOperatorOrder =
            lazy
                this.IndexedOps
                |> (fun ops -> ops.IsOrdered PIxOp< ^idx, ^op>.Op_InOperatorOrder)

        member inline this.IsInIndexOrder =
            lazy
                this.IsInOperatorOrder.Value &&
                this.IndexedOps
                |> (fun ops -> ops.IsOrdered PIxOp< ^idx, ^op>.Op_InIndexOrder)

        static member inline FindItemsWithMatchingIndex (target, (coeff, ixops)) =
            let findLocationOfMatched pred t =
                Array.fold
                    (fun (matched, index) curr ->
                        let matched' =
                            if (curr.Op = t) then
                                matched
                                |> Option.map (fun (item, location) -> if (pred curr.Index item.Index) then (curr, index) else (item, location))
                                |> Option.defaultValue (curr, index)
                                |> Some
                            else
                                matched
                        (matched', index + 1u))
                    (None, 0u)

            let rec findItemsWithMatchingIndex result target indexedOps =
                let rec findItemWithMatchingIndex index pre (ops : IxOp<_,_>[]) =
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
                            findItemWithMatchingIndex index ((Array.append pre ([| head |]))) rest

                let includeSingleOp target (curr : IxOp<_,_>)  (c, xs) =
                    let product = PIxOp<_,_>.Op_ToOperatorOrder curr target
                    System.Diagnostics.Debug.Assert(product.Length = 1, "algebra limitation: commuting terms with same index produced a sum term!")
                    (c * product.[0].Coeff, Array.append product.[0].Thunk.[1..] xs)

                let (pre, found, post) = findItemWithMatchingIndex target.Index [| |] indexedOps

                let ((resultCoeff, resultMatching), resultRest) = result

                match found with
                | Some head ->
                    let (coeff, swapped) =
                        Array.foldBack (includeSingleOp head) pre (Complex.One, [| |])
                    let result' = ((coeff * resultCoeff, Array.append resultMatching [| head |]), Array.append resultRest swapped)
                    findItemsWithMatchingIndex result' target post
                | None ->
                    let result' = ((resultCoeff, resultMatching), Array.append resultRest pre)
                    result'
            in
            findItemsWithMatchingIndex ((coeff, [| |]),[| |]) target ixops

        member inline this.IndexedOpsGroupedByIndex =
            let rec groupedByIndex (coeff, ixops) (result) =
                match ixops with
                | [| |] -> result

                | [| first |] ->
                    [|
                        yield! result
                        yield PIxOp<_,_>.ApplyInternal(coeff, [| first |])
                    |]

                | _ ->
                    let first = ixops.[0]
                    let ((coeff', matching), others) =  PIxOp< ^idx, ^op>.FindItemsWithMatchingIndex (first, (coeff, ixops.[1..]))
                    [|
                        yield! result
                        yield PIxOp<_,_>.ApplyInternal(coeff', [| first; yield! matching |])
                    |]
                    |> groupedByIndex (coeff, others)
            in
            lazy
                groupedByIndex (this.Coeff, this.IndexedOps) ([| |])

        member inline this.ToOperatorOrder =
            let joinTerm (sorted : PIxOp<_,_>) (candidate : PIxOp<_,_>) : PIxOp<_,_> =
                let rec includeSingleOp (op : IxOp<_,_>) (coeff, pre, post) : PIxOp<_,_> =
                    let candidate = PIxOp<_,_>.ApplyInternal (coeff, [| yield! pre; op; yield! post |])
                    if candidate.IsInOperatorOrder.Value then
                        candidate
                    else
                        let swapped = PIxOp<_,_>.Op_ToOperatorOrder (Array.last pre) op
                        System.Diagnostics.Debug.Assert (swapped.Length = 1, "algebra limitation - commuting terms with different index produced a sum term!")
                        let swapped' = swapped.[0]
                        includeSingleOp op (coeff * swapped'.Coeff, (Array.allButLast pre), ([| Array.last pre; yield! post |]))

                candidate.IndexedOps
                |> Array.fold (fun result curr -> includeSingleOp curr (result.Coeff, result.IndexedOps, [| |])) sorted
                |> (fun result -> result.ScaleCoefficient candidate.Coeff)

            let sortTerm (chunk : PIxOp<_,_>) : PIxOp<_,_>[] =
                match chunk.IndexedOps.Length with
                | 0 -> [| |]
                | 1 -> [| chunk |]
                | 2 ->
                    if chunk.IsInOperatorOrder.Value then
                        [| chunk |]
                    else
                        PIxOp<_,_>.Op_ToOperatorOrder chunk.IndexedOps.[0] chunk.IndexedOps.[1]
                        |> Array.map (fun c -> PIxOp<_,_>.ApplyInternal(c.Coeff * chunk.Coeff, c.Thunk))
                | _ -> [| |] // JOHNAZ: bug for bosons!

            let includeChunk (state : PIxOp<_, _>[][]) (chunk : PIxOp<_,_>) : PIxOp<_, _>[][] =
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

        member inline this.ToIndexOrder =
            if not this.IsInOperatorOrder.Value then
                failwith "P term must be in operator (normal) order before being put in index order"




        static member inline Apply (coeff : Complex, units : CIxOp< ^idx, ^op >[]) =
            let extractedCoeff = units |> Array.fold (fun coeff curr -> coeff * curr.Coeff) Complex.One
            let indexedOps     = units |> Array.map  (fun curr -> curr.IndexedOp)
            PIxOp<_,_>.ApplyInternal (coeff * extractedCoeff, indexedOps)

    type SIxOp< ^idx, ^op
            when ^idx : comparison
            and ^op : equality
            and ^op : (member IsIdentity  : bool)
            and ^op : (static member InIndexOrder    : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> bool)
            and ^op : (static member InOperatorOrder : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> bool)
            and ^op : (static member ToIndexOrder    : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> C<IxOp< ^idx, ^op >[]>[])
            and ^op : (static member ToOperatorOrder : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> C<IxOp< ^idx, ^op >[]>[])
            and ^op : (static member NextIndexLocation : ^op * IxOp< ^idx, ^op >[] -> ^idx option )
            and ^op : comparison> =
        | SumTerm of S<PIxOp< ^idx, ^op >>
    with
        member inline this.Unapply = match this with SumTerm st -> st
        member inline __.Coeff     = Complex.One
        member inline this.Terms   = this.Unapply.Terms
        static member inline Apply = S<PIxOp< ^idx, ^op >>.Apply >> SumTerm
        member inline this.IsZero  = this.Unapply.IsZero

        static member inline (<+>) (l : SIxOp< ^idx, ^op>, r : SIxOp< ^idx, ^op>) = l.Unapply <+> r.Unapply |> SumTerm
        static member inline (<*>) (l : SIxOp< ^idx, ^op>, r : SIxOp< ^idx, ^op>) = l.Unapply <*> r.Unapply |> SumTerm

        member inline this.AllTermsIndexOrdered =
            lazy
                this.Terms
                |> Seq.fold
                    (fun result curr -> result && curr.IsInIndexOrder.Value)
                    true

        member inline this.AllTermsOperatorOrdered =
            lazy
                this.Terms
                |> Seq.fold
                    (fun result curr -> result && curr.IsInOperatorOrder.Value)
                    true


        member inline this.ToOperatorOrder =
            if this.AllTermsOperatorOrdered.Value then
                lazy this
            else
                let sortSingleProductTerm (p : PIxOp< ^idx, ^op >) =
                    p.ToOperatorOrder.Value
                    |> Array.map (curry SIxOp< ^idx, ^op>.Apply Complex.One)
                    |> Array.reduce (<+>)

                lazy
                    this.Terms
                    |> Array.map sortSingleProductTerm
                    |> Array.reduce (<+>)

    type PIxOp< ^idx, ^op
            when ^idx : comparison
            and ^op : equality
            and ^op : (member IsIdentity  : bool)
            and ^op : (static member InIndexOrder    : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> bool)
            and ^op : (static member InOperatorOrder : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> bool)
            and ^op : (static member ToIndexOrder    : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> C<IxOp< ^idx, ^op >[]>[])
            and ^op : (static member ToOperatorOrder : IxOp< ^idx, ^op > -> IxOp< ^idx, ^op > -> C<IxOp< ^idx, ^op >[]>[])
            and ^op : (static member NextIndexLocation : ^op * IxOp< ^idx, ^op >[] -> ^idx option )
            and ^op : comparison>
    with
        static member inline (+) (l : PIxOp<_,_>, r : PIxOp<_,_>) : SIxOp< ^idx, ^op >=
            SIxOp< ^idx, ^op >.Apply(Complex.One, [| l; r |])

