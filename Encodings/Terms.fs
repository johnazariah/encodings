namespace Encodings
open System.Numerics
open System.Collections.Generic

[<AutoOpen>]
module Coeff =
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

    type S< ^term when ^term : equality and ^term : (static member Combine : C< ^term > * C< ^term > -> C< ^term >)> =
        | SumTerm of C<Map<string, C< ^term >>>
    with
        member inline this.Unapply = match this with SumTerm st -> st
        member inline this.Coeff = this.Unapply.Coeff
        member inline this.Terms = this.Unapply.Item.Values

        static member inline internal ApplyInternal (coeff : Complex) =
            let toTuple (t : C<_>) =
                let scaled = t.ScaleCoefficient coeff
                (scaled.ToString(), scaled)

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
            >> (curry C<_>.Apply) Complex.One
            >> SumTerm

        static member inline Unit =
            S<'term>.ApplyInternal (Complex.One) ([||])

        static member inline Zero =
            S<'term>.ApplyInternal (Complex.Zero) ([||])

        member inline this.Reduce =
            lazy
                if this.Coeff.IsZero then
                    [||]
                else
                    [|
                        for pt in this.Terms do
                            if (not pt.IsZero) then yield pt
                    |]
                |> S<_>.ApplyInternal Complex.One

        member inline this.Normalize =
            S<_>.ApplyInternal this.Coeff this.Terms

        static member inline (*) (l : S<_>, r : S<_>) =
            let combiner = (fun (a, b) -> (^term: (static member Combine : C< ^term > * C< ^term > -> C< ^term >)(a, b)))

            [|
                for lt in l.Normalize.Terms do
                    for gt in r.Normalize.Terms do
                        yield combiner (lt, gt)
            |]
            |> S<_>.ApplyInternal Complex.One

        static member inline (+) (l : S<_>, r : S<_>) =
            [|
                yield! l.Normalize.Terms
                yield! r.Normalize.Terms
            |]
            |> S<_>.ApplyInternal Complex.One

        member inline this.IsZero =
            let anyNonZeroTerms = this.Terms |> (Seq.exists (fun t -> not t.IsZero))
            let allZeroTerms = not anyNonZeroTerms
            this.Coeff.IsZero || allZeroTerms

        static member inline Apply =
            uncurry S<'term>.ApplyInternal >> (fun t -> t.Reduce.Value)


[<AutoOpen>]
module DenseRepresentation =
    type R< ^unit when ^unit : (static member Identity : ^unit) and ^unit : (static member Combine : ^unit * ^unit -> C< ^unit >) and ^unit : equality> =
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
            let uid = (^unit : (static member Identity : ^unit) ())
            let identity = C<_>.Apply (Complex.One, uid)

            let pairwiseCombine (unitCombine : ^unit * ^unit -> C< ^unit >) rgls rgrs =
                let c (ca, cb) =
                    let cc = unitCombine (ca.Item, cb.Item)
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
            let combiner = (fun (a : ^unit, b : ^unit) -> (^unit: (static member Combine : ^unit * ^unit -> C< ^unit >)(a, b)))
            let units' = pairwiseCombine combiner l.Units r.Units
            R< ^unit >.Apply (coeff', units')

[<AutoOpen>]
module SparseRepresentation =
    type I<'idx, 'op when 'idx : comparison and 'op : equality> =
        { Index : 'idx; Op : 'op }
    with
        static member Apply (index : 'idx, op : 'op) =
            { Index = index; Op = op }

    type CIxOp<'idx, 'op when 'idx : comparison and 'op : equality> =
        | Indexed of C<I<'idx, 'op>>
    with
        member this.Unapply = match this with Indexed c -> c
        member this.IsZero = this.Unapply.IsZero
        static member Unit =
            CIxOp<'idx, 'op>.Indexed <| C<_>.Unit
        static member Apply (coeff, unit) =
            CIxOp<'idx, 'op>.Indexed <| C<_>.Apply (coeff, unit)
        static member Apply (unit) =
            CIxOp<'idx, 'op>.Indexed <| C<_>.Apply (unit)

    type PIxOp<'idx, 'op when 'idx : comparison and 'op : equality> =
        | ProductTerm of C<CIxOp<'idx,'op>[]>
    with
        member this.Unapply = match this with ProductTerm pt -> pt
        member this.Coeff = this.Unapply.Coeff
        member this.Units = this.Unapply.Item

        static member Apply = C<CIxOp<'idx, 'op>>.Apply >> PIxOp<'idx, 'op>.ProductTerm

        static member inline Unit = PIxOp<_,_>.Apply (Complex.One,  [||])
        static member inline Zero = PIxOp<_,_>.Apply (Complex.Zero, [||])

        member this.Reduce =
            lazy PIxOp<_,_>.ProductTerm this.Unapply.Reduce.Value

        member this.IsZero = this.Unapply.IsZero

        static member (*) (l : PIxOp<_,_>, r : PIxOp<_,_>) =
            PIxOp<_,_>.Apply (l.Coeff * r.Coeff, Array.concat [| l.Units; r.Units |])

        member this.ScaleCoefficient scale =
            PIxOp<_,_>.ProductTerm <| this.Unapply.ScaleCoefficient scale

        member this.AddCoefficient   coeff =
            PIxOp<_,_>.ProductTerm <| this.Unapply.AddCoefficient   coeff

    type SIxOp<'idx, 'op when 'idx : comparison and 'op : equality> =
        | SumTerm of S<PIxOp<'idx, 'op>>
    with
        member this.Unapply = match this with SumTerm st -> st
        member this.Coeff = this.Unapply.Coeff
        member this.Terms = this.Unapply.Unapply.Item.Values

        static member internal ApplyInternal (coeff : Complex) =
            SI<_,_>.SumTerm << S<_>.ApplyInternal coeff

        static member Unit = SI<'idx, 'op>.SumTerm <| S<_>.Unit
        static member Zero = SI<'idx, 'op>.SumTerm <| S<_>.Zero

        member this.Reduce =
            lazy SI<'idx, 'op>.SumTerm this.Unapply.Reduce.Value

        member this.Normalize =
            SI<'idx, 'op>.SumTerm <| this.Unapply.Normalize

        static member (*) (l : SI<_,_>, r : SI<_,_>) =
            SI<'idx, 'op>.SumTerm <| l.Unapply * r.Unapply

        static member (+) (l : SI<_,_>, r : SI<_,_>) =
            SI<'idx, 'op>.SumTerm <| l.Unapply + r.Unapply

        member this.IsZero = this.Unapply.IsZero

        static member Apply (coeff : Complex, terms : P<CI<'idx, 'op>>[]) =
            terms
            |> S<_>.ApplyInternal coeff
            |> SI<_,_>.SumTerm

        static member Apply (coeff : Complex, terms : PI<'idx, 'op>[]) =
            terms
            |> Array.map (fun t -> t.Unapply)
            |> S<_>.ApplyInternal coeff
            |> SI<_,_>.SumTerm

[<AutoOpen>]
module PrettyPrint =
    open System.Numerics

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
            sprintf "{Coeff = %O;\n Item = %A;}" this.Coeff this.Item

    let prettyPrintP (this : P<_>) =
        this.Units
        |> Array.map prettyPrintC
        |> (fun rg -> System.String.Join (" | ", rg))
        |> sprintf "[%s]"

    let prettyPrintS (this : S<_>) =
        this.Terms
        |> Array.map prettyPrintP
        |> (fun rg -> System.String.Join ("; ", rg))
        |> sprintf "{%s}"

[<AutoOpen>]
module StringInterop =
    open System.Numerics

    let IndexedOpFromString
        (unitFactory : string -> 'op option)
        (s : System.String) =
        try
            s.Trim().TrimStart('(').TrimEnd(')').Split(',')
            |> Array.map (fun s -> s.Trim ())
            |> (fun rg ->
                unitFactory (rg.[0])
                |> Option.map (fun op ->
                    (System.UInt32.Parse rg.[1], op)
                    |> I.Apply
                    |> (curry CI<_,_>.Apply) Complex.One))
        with
        | _ -> None

    let ProductTermFromString
        (unitFactory : string -> 'unit option)
        (s : System.String) : PI<_,_> option =
        let toC = ((curry C<_>.Apply) Complex.One)
        try
            s.Trim().TrimStart('[').TrimEnd(']').Split('|')
            |> Array.choose (IndexedOpFromString unitFactory)
            |> Array.map toC
            |> (curry PI<_,_>.Apply) Complex.One
            |> Some
        with
        | _ -> None

    let SumTermFromString
        (unitFactory : string -> 'unit option)
        (s : System.String) : SI<_,_> option =
        try
            s.Trim().TrimStart('{').TrimEnd('}').Split(';')
            |> Array.choose (ProductTermFromString unitFactory)
            |> (fun ts -> SI<_,_>.Apply (Complex.One, ts))
            |> Some
        with
        | _ -> None