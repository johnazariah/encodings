namespace Encodings
open System.Numerics
open System.Collections.Generic

[<AutoOpen>]
module Terms =
    type C<'unit when 'unit : equality> =
        { Coeff : Complex; Item : 'unit }
    with
        member this.IsZero = this.Coeff.IsZero

        static member Unit =
            { Coeff = Complex.One; Item = Unchecked.defaultof<'unit> }

        static member Apply (coeff : Complex, unit) =
            { Coeff = coeff.Reduce; Item = unit }

        member this.Reduce =
            lazy { this with Coeff = this.Coeff.Reduce }

        member this.Normalize =
            { this with Coeff = Complex.One }

        static member (~-) (v : C<'unit>) =
            ({ v with Coeff = - v.Coeff }).Reduce.Value

        member inline this.ScaleCoefficient scale =
            { this with Coeff = this.Coeff * scale }

        member inline this.AddCoefficient coeff =
            { this with Coeff = this.Coeff + coeff }

    type SC< ^term when ^term : equality> =
        | SumTerm of Map<string, C< ^term >>
    with
        member inline this.Unapply = match this with SumTerm st -> st
        member inline __.Coeff = Complex.One
        member inline this.Terms = this.Unapply.Values

        static member inline internal ApplyInternal (coeff : Complex) =
            let toTuple (t : C<_>) =
                let scaled = t.ScaleCoefficient coeff
                (t.Item.ToString(), scaled)

            let createMap =
                let addOrUpdate (m : Dictionary<'key, C<_>>) (key : 'key, value : C<_>) =
                    if m.ContainsKey key then
                        m.[key] <- m.[key].AddCoefficient value.Coeff
                    else
                        m.[key] <- value
                    m
                let dictToMap (d : Dictionary<'key, C<_>>) =
                    seq { for kvp in d do yield (kvp.Key, kvp.Value) }
                    |> Map.ofSeq

                Array.fold addOrUpdate (new Dictionary<string, C< ^term >>())
                >> dictToMap

            Array.map toTuple
            >> createMap
            >> SumTerm

        static member inline Unit =
            SC<_>.ApplyInternal (Complex.One) ([||])

        static member inline Zero =
            SC<_>.ApplyInternal (Complex.Zero) ([||])

        member inline this.Reduce =
            lazy
                if this.Coeff.IsZero then
                    [||]
                    |> SC<_>.ApplyInternal Complex.Zero
                else
                    [|
                        for pt in this.Terms do
                            if (not pt.IsZero) then yield pt
                    |]
                    |> SC<_>.ApplyInternal Complex.One

        static member inline
            Multiply
                (multiplier : C< ^term > * C< ^term > -> SC< ^term >)
                (l : SC< ^term >, r : SC< ^term >) =
            [|
                for lt in l.Terms do
                    for rt in r.Terms do
                        let ct = multiplier (lt, rt)
                        yield! ct.Terms
            |]
            |> SC<_>.ApplyInternal Complex.One

        static member inline (+) (l : SC<_>, r : SC<_>) =
            [|
                yield! l.Terms
                yield! r.Terms
            |]
            |> SC<_>.ApplyInternal Complex.One

        member inline this.IsZero : bool =
            let allZeroTerms = this.Terms |> (Seq.exists (fun t -> not t.IsZero)) |> not
            this.Coeff.IsZero || allZeroTerms

        static member inline Apply (coeff : Complex, terms : C< ^term > []) : SC< ^term > =
            SC<_>.ApplyInternal coeff terms
            |> (fun t -> t.Reduce.Value)

        member inline this.ScaleCoefficient scale =
            SC<_>.ApplyInternal scale this.Terms

        member inline this.AddCoefficient coeff =
            let terms = this.Terms |> Array.map (fun t -> t.AddCoefficient coeff)
            SC<_>.ApplyInternal Complex.One terms

[<AutoOpen>]
module DenseRepresentation =
    type R< ^unit when ^unit : (static member Identity : ^unit) and ^unit : (static member Combine : C< ^unit > * C< ^unit > -> C< ^unit >) and ^unit : equality> =
        | Register of C<C<'unit>[]>
    with
        member inline this.Unapply = match this with Register pt -> pt
        member inline this.Coeff = this.Unapply.Coeff
        member inline this.Units = this.Unapply.Item

        static member inline Apply (coeff : Complex, units : C< ^unit>[]) =
            C<_>.Apply(coeff.Reduce, units |> Array.map (fun u -> u.Reduce.Value))
            |> (fun t -> t.Reduce.Value)
            |> Register

        static member inline Unit =
            R< ^unit >.Apply (Complex.One, [||])

        static member inline Zero =
            R< ^unit >.Apply (Complex.Zero, [||])

        member inline this.Reduce =
            let normalize (coeff : Complex, units) (curr : C< ^unit >) =
                if (coeff.IsZero || curr.IsZero) then
                    (Complex.Zero, [||])
                else
                    ((coeff * curr.Coeff).Reduce, [| yield! units; yield curr.Normalize.Reduce.Value |])

            let checkForZero (coeff : Complex, units : C< ^unit >[]) =
                if ((coeff.IsZero) || (units = [||])) then
                    (Complex.Zero, [||])
                else
                    (coeff, units)
            lazy
                this.Units
                |> Array.fold normalize (this.Coeff, [||])
                |> checkForZero
                |> C<_>.Apply
                |> Register

        member inline this.IsZero =
            this.Coeff.IsZero || (this.Units |> Seq.exists (fun item -> item.IsZero))

        static member inline (*) (l : R< ^unit >, r : R< ^unit >) =
            let identity =
                (^unit : (static member Identity : ^unit) ())
                |> (curry C<_>.Apply Complex.One)

            let pairwiseCombine (combine : C< ^unit > * C< ^unit > -> C< ^unit >) rgls rgrs =
                let c (ca, cb) =
                    let cc = combine (ca, cb)
                    C< ^unit >.Apply (ca.Coeff * cb.Coeff * cc.Coeff, cc.Item)

                let rec pairwiseCombine' ls rs =
                    match (ls, rs) with
                    | []      , []       -> []
                    | l :: ls', []       -> c (l, identity) :: (pairwiseCombine' ls' [])
                    | []      , r :: rs' -> c (identity, r) :: (pairwiseCombine' [] rs')
                    | l :: ls', r :: rs' -> c (l, r)        :: (pairwiseCombine' ls' rs')
                in
                pairwiseCombine' (rgls |> List.ofArray) (rgrs |> List.ofArray)
                |> Array.ofList

            let coeff' = l.Coeff * r.Coeff
            let combiner = (fun (a, b) -> (^unit: (static member Combine : C< ^unit > * C< ^unit > -> C< ^unit >)(a, b)))
            let units' = pairwiseCombine combiner l.Units r.Units
            R< ^unit >.Apply (coeff', units')

[<AutoOpen>]
module SparseRepresentation =
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

[<AutoOpen>]
module PrettyPrint =
    let prettyPrintC (this : C<'unit>) =
        let itemString = sprintf "%O" this.Item
        if this.Coeff = Complex.Zero then
            ""
        else if this.Coeff = Complex.One then
            sprintf "%s" itemString
        else if this.Coeff = - Complex.One then
            sprintf "(- %s)" itemString
        else if this.Coeff = Complex.ImaginaryOne then
            sprintf "(i %s)" itemString
        else if this.Coeff = - Complex.ImaginaryOne then
            sprintf "(-i %s)" itemString
        else if this.Coeff.Imaginary = 0. then
            sprintf "(%O %s)" this.Coeff.Real itemString
        else if this.Coeff.Imaginary = 1. then
            sprintf "(%Oi %s)" this.Coeff.Real itemString
        else if this.Coeff.Imaginary = -1. then
            sprintf "(-%Oi %s)" this.Coeff.Real itemString
        else
            sprintf "%O" this

    let prettyPrintI (this : IxOp<_,_>) =
        sprintf "(%s, %i)" this.Op this.Index

    let prettyPrintCIxOp (this : CIxOp<_,_>) =
        prettyPrintI this.Unapply.Item

    let prettyPrintPIxOp (this : PIxOp<_,_>) =
        this.Units
        |> Array.map prettyPrintCIxOp
        |> (fun rg -> System.String.Join (" | ", rg))
        |> sprintf "[%s]"

    let prettyPrintSIxOp (this : SIxOp<_,_>) =
        this.Terms
        |> Array.map prettyPrintPIxOp
        |> (fun rg -> System.String.Join ("; ", rg))
        |> sprintf "{%s}"

[<AutoOpen>]
module StringInterop =
    let inline IndexedOpFromString< ^op when ^op : equality>
        (unitFactory : string ->  ^op  option)
        (s : System.String) =
        try
            s.Trim().TrimStart('(').TrimEnd(')').Split(',')
            |> Array.map (fun s -> s.Trim ())
            |> (fun rg ->
                unitFactory (rg.[0])
                |> Option.map (fun op ->
                    (System.UInt32.Parse rg.[1], op)
                    |> IxOp<uint32, ^op >.Apply
                    |> (curry CIxOp<_,_>.Apply) Complex.One))
        with
        | _ -> None

    let inline ProductTermFromString< ^op when ^op : equality>
        (unitFactory : string ->  ^op  option)
        (s : System.String) : PIxOp<uint32, ^op > option =
        try
            s.Trim().TrimStart('[').TrimEnd(']').Split('|')
            |> Array.choose (IndexedOpFromString unitFactory)
            |> (curry PIxOp<_,_>.Apply) Complex.One
            |> Some
        with
        | _ -> None

    let inline SumTermFromString< ^op when ^op : equality>
        (unitFactory : string ->  ^op  option)
        (s : System.String) : SIxOp<uint32, ^op > option =
        try
            s.Trim().TrimStart('{').TrimEnd('}').Split(';')
            |> Array.choose (ProductTermFromString unitFactory)
            |> (curry SIxOp<_,_>.Apply) Complex.One
            |> Some
        with
        | _ -> None