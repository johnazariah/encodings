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
            and ^op : comparison> =
        | ProductTerm of C<IxOp< ^idx, ^op>[]>
    with
        static member inline Op_IsIdentityOperator op =
            (^op : (member IsIdentity              : bool)(op))

        member inline this.Unapply    = match this with ProductTerm pt -> pt
        member inline this.Coeff      = this.Unapply.Coeff
        member inline this.IndexedOps = this.Unapply.Thunk

        static member inline internal ApplyInternal (coeff, ixops : IxOp< ^idx, ^op>[]) =
            let isIdentityTerm (t : IxOp< ^idx, ^op>) = PIxOp< ^idx, ^op>.Op_IsIdentityOperator t.Op
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

        static member inline Apply (coeff : Complex, units : CIxOp< ^idx, ^op >[]) =
            let extractedCoeff = units |> Array.fold (fun coeff curr -> coeff * curr.Coeff) Complex.One
            let indexedOps     = units |> Array.map  (fun curr -> curr.IndexedOp)
            PIxOp<_,_>.ApplyInternal (coeff * extractedCoeff, indexedOps)

    type SIxOp< ^idx, ^op
            when ^idx : comparison
            and ^op : equality
            and ^op : (member IsIdentity  : bool)
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

        static member inline Unit = SIxOp< ^idx, ^op >.Apply (Complex.One,  [| PIxOp< ^idx, ^op>.Unit |])
        static member inline Zero = SIxOp< ^idx, ^op >.Apply (Complex.Zero, [| |])

    type PIxOp< ^idx, ^op
            when ^idx : comparison
            and ^op : equality
            and ^op : (member IsIdentity  : bool)
            and ^op : comparison>
    with
        static member inline (+) (l : PIxOp<_,_>, r : PIxOp<_,_>) : SIxOp< ^idx, ^op >=
            SIxOp< ^idx, ^op >.Apply(Complex.One, [| l; r |])

