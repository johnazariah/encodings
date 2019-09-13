namespace Encodings

[<AutoOpen>]
module Terms =
    open System.Numerics
    open System.Collections.Generic

    type I<'idx, 'op when 'idx : comparison and 'op : equality> =
        { Index : 'idx; Op : 'op }
    with
        static member Apply (index : 'idx, op : 'op) =
            { Index = index; Op = op }

    type C<'unit when 'unit : equality> =
        { Coeff : Complex; Item : 'unit }
    with
        member this.IsZero = this.Coeff.IsZero

        static member Unit =
            { Coeff = Complex.One; Item = Unchecked.defaultof<'unit> }

        static member Apply (coeff : Complex, unit) =
            { Coeff = coeff.Reduce; Item = unit }

        static member Apply (unit) =
            C<'unit>.Apply(Complex.One, unit)

        member this.Reduce =
            lazy { this with Coeff = this.Coeff.Reduce }

        member this.Normalize =
            { this with Coeff = Complex.One }

        static member (~-) (v : C<'unit>) =
            ({ v with Coeff = - v.Coeff }).Reduce.Value

    and CI<'idx, 'op when 'idx : comparison and 'op : equality> =
        | Indexed of C<I<'idx, 'op>>
    with
        member this.Unapply = match this with Indexed c -> c
        member this.IsZero = this.Unapply.IsZero
        static member Unit =
            CI<'idx, 'op>.Indexed <| C<_>.Unit
        static member Apply (coeff, unit) =
            CI<'idx, 'op>.Indexed <| C<_>.Apply (coeff, unit)
        static member Apply (unit) =
            CI<'idx, 'op>.Indexed <| C<_>.Apply (unit)

        member this.Reduce =
            lazy CI<'idx, 'op>.Indexed this.Unapply.Reduce.Value

        member this.Normalize =
            CI<'idx, 'op>.Indexed <| this.Unapply.Normalize

        static member (~-) (v : CI<_,_>) =
            CI<'idx, 'op>.Indexed <| - (v.Unapply)

    and P<'unit when 'unit : equality> =
        | ProductTerm of C<C<'unit>[]>
    with
        member this.Unapply = match this with ProductTerm pt -> pt
        member this.Coeff = this.Unapply.Coeff
        member this.Units = this.Unapply.Item

        static member internal ApplyInternal (coeff : Complex) (units : C<_>[]) =
            C<_>.Apply(coeff.Reduce, units |> Array.map (fun u -> u.Reduce.Value))
            |> ProductTerm

        static member Unit =
            P<'unit>.ApplyInternal (Complex.One) ([||])

        static member Zero =
            P<'unit>.ApplyInternal (Complex.Zero) ([||])

        member this.Reduce =
            let normalize (coeff : Complex, units) (curr : C<_>) =
                if (coeff.IsZero || curr.IsZero) then
                    (Complex.Zero, [||])
                else
                    (coeff * curr.Coeff, [| yield! units; yield curr.Normalize |])

            let checkForZero (coeff : Complex, units) =
                if ((coeff.IsZero) || (units = [||])) then
                    (Complex.Zero, [||])
                else
                    (coeff, units)
            lazy
                this.Units
                |> Array.fold normalize (this.Coeff, [||])
                |> checkForZero
                |> uncurry P<'unit>.ApplyInternal

        static member Apply =
            uncurry P<_>.ApplyInternal >> (fun t -> t.Reduce.Value)

        member this.IsZero =
            this.Coeff.IsZero || (this.Units |> Seq.exists (fun item -> item.IsZero))

        static member (*) (l : P<_>, r : P<_>) =
            P<'unit>.Apply ((l.Coeff * r.Coeff), Array.concat [| l.Units; r.Units |])

        member this.ScaleCoefficient scale =
            P<'unit>.Apply (this.Coeff * scale, this.Units)
        member this.AddCoefficient coeff =
            P<'unit>.Apply (this.Coeff + coeff, this.Units)

    and  PI<'idx, 'op when 'idx : comparison and 'op : equality> =
        | ProductTerm of P<CI<'idx,'op>>
    with
        member this.Unapply = match this with ProductTerm pt -> pt
        member this.Coeff = this.Unapply.Coeff
        member this.Units = this.Unapply.Unapply.Item

        static member internal ApplyInternal (coeff : Complex) (units : C<_>[]) =
            PI<_,_>.ProductTerm <| P<_>.ApplyInternal coeff units

        static member Unit  = PI<_,_>.ProductTerm <| P<_>.Unit
        static member Zero  = PI<_,_>.ProductTerm <| P<_>.Zero

        member this.Reduce =
            lazy PI<_,_>.ProductTerm this.Unapply.Reduce.Value

        static member Apply =
            PI<_,_>.ProductTerm << P<CI<'idx, 'op>>.Apply

        member this.IsZero = this.Unapply.IsZero

        static member (*) (l : PI<_,_>, r : PI<_,_>) =
            PI<_,_>.ProductTerm <| l.Unapply * r.Unapply

        member this.ScaleCoefficient scale =
            PI<_,_>.ProductTerm <| this.Unapply.ScaleCoefficient scale
        member this.AddCoefficient   coeff =
            PI<_,_>.ProductTerm <| this.Unapply.AddCoefficient   coeff

    and S<'unit when 'unit : equality> =
        | SumTerm of C<Map<string, P<'unit>>>
    with
        member this.Unapply = match this with SumTerm st -> st
        member this.Coeff = this.Unapply.Coeff
        member this.Terms = this.Unapply.Item.Values

        static member internal ApplyInternal (coeff : Complex) =
            let toTuple (t : P<'unit>) =
                let scaled = t.ScaleCoefficient coeff
                (scaled.ToString(), scaled)

            let createMap =
                let addOrUpdate (m : Dictionary<'key, P<_>>) (key : 'key, value : P<_>) =
                    if m.ContainsKey key then
                        m.[key] <- m.[key].AddCoefficient value.Coeff
                    else
                        m.[key] <- value
                    m
                let dictToMap (d : Dictionary<'key, P<_>>) =
                    seq { for kvp in d do yield (kvp.Key, kvp.Value) }
                    |> Map.ofSeq

                Array.fold addOrUpdate (new Dictionary<string, P<_>>())
                >> dictToMap

            Array.map toTuple
            >> createMap
            >> (curry C<_>.Apply) Complex.One
            >> SumTerm

        static member Unit =
            S<'unit>.ApplyInternal (Complex.One) ([||])

        static member Zero =
            S<'unit>.ApplyInternal (Complex.Zero) ([||])

        member this.Reduce =
            lazy
                if this.Coeff.IsZero then
                    [||]
                else
                    [|
                        for pt in this.Terms do
                            if pt.Units <> [||] then
                                yield pt
                    |]
                |> S<_>.ApplyInternal Complex.One

        member this.Normalize =
            S<_>.ApplyInternal this.Coeff this.Terms

        static member (*) (l : S<_>, r : S<_>) =
            [|
                for lt in l.Normalize.Terms do
                    for gt in r.Normalize.Terms do
                        yield lt * gt
            |]
            |> S<_>.ApplyInternal Complex.One

        static member (+) (l : S<_>, r : S<_>) =
            [|
                yield! l.Normalize.Terms
                yield! r.Normalize.Terms
            |]
            |> S<_>.ApplyInternal Complex.One

        member this.IsZero =
            this.Coeff.IsZero || (this.Terms |> Seq.exists (fun term -> term.IsZero))

        static member Apply =
            uncurry S<'unit>.ApplyInternal >> (fun t -> t.Reduce.Value)

    and SI<'idx, 'op when 'idx : comparison and 'op : equality> =
        | SumTerm of S<CI<'idx, 'op>>
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