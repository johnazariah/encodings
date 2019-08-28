namespace Encodings

[<AutoOpen>]
module Terms =
    open System.Numerics

    [<AutoOpen>]
    module TermTypes =
        type Ix<'op when 'op : equality> =
            { Index : uint32; Op : 'op }
        with
            static member Apply (index, value) = { Index = index; Op = value }
            static member (.>=.) (l : Ix<'op>, r : Ix<'op>) = l.Index >= r.Index
            static member (.<=.) (l : Ix<'op>, r : Ix<'op>) = l.Index <= r.Index
            static member WithMinIndex = { Ix.Index = (System.UInt32.MinValue); Op = Unchecked.defaultof<'op> }
            static member WithMaxIndex = { Ix.Index = (System.UInt32.MaxValue); Op = Unchecked.defaultof<'op> }

            static member TryCreateFromString
                (unitFactory : string -> 'op option) (s : System.String) =
                try
                    s.Trim().TrimStart('(').TrimEnd(')').Split(',')
                    |> Array.map (fun s -> s.Trim ())
                    |> (fun rg ->
                        unitFactory (rg.[0])
                        |> Option.map (fun op ->
                            let index = System.UInt32.Parse rg.[1]
                            { Ix.Index = index; Op = op }))
                with
                | _ -> None

        and Cf<'unit when 'unit : equality> =
            { Coeff : Complex; Item : 'unit }
        with
            static member Apply (unit : 'unit) = { Coeff = Complex.One; Item = unit }
            static member Apply (coeff, unit)  = { Coeff = coeff;       Item = unit }

            static member (*) (l : Cf<'unit>, r : Cf<'unit>) =
                {
                    Pr.Coeff = l.Coeff * r.Coeff
                    Pr.Units = [| l ; r |]
                }

            static member (+) (l : Cf<'unit>, r : Cf<'unit>) =
                if (l.Item = r.Item) then
                    let unit = { Cf.Coeff = Complex.One; Cf.Item = l.Item }
                    { Pr.Coeff = l.Coeff + r.Coeff; Pr.Units = [| unit |]}
                else
                    { Pr.Coeff = Complex.One; Pr.Units = [| l ; r|]}

            member this.IsZero = not this.Coeff.IsNonZero

        and  Pr<'unit when 'unit : equality> =
            { Coeff : Complex; Units : Cf<'unit>[] }
        with
            static member private ApplyInternal coeff (units : Cf<'unit>[]) =
                let (coeff', units') =
                    units
                    |> Array.fold
                        (fun (c, us) curr -> (c * curr.Coeff, Array.append us [| { curr with Cf.Coeff = Complex.One } |]))
                        (coeff, [||])

                let anyElementsAreZero = units' |> Seq.exists (fun u -> u.IsZero)
                if (units'.Length = 0 || coeff' = Complex.Zero || anyElementsAreZero) then
                    { Pr.Coeff = Complex.Zero; Pr.Units = [||] }
                else
                    { Pr.Coeff = coeff'; Pr.Units = units' }

            static member Zero = { Coeff = Complex.Zero; Units = [||] }

            member this.NormalizeUnitCoefficients =
                Pr<'unit>.ApplyInternal this.Coeff this.Units

            static member Apply (item  : 'unit)              = Pr<'unit>.ApplyInternal Complex.One [| item |> Cf<'unit>.Apply |]
            static member Apply (items : 'unit[])            = Pr<'unit>.ApplyInternal Complex.One (items |> Array.map Cf<'unit>.Apply)
            static member Apply (unit  : Cf<'unit>)          = Pr<'unit>.ApplyInternal Complex.One [| unit |]
            static member Apply (units : Cf<'unit>[])        = Pr<'unit>.ApplyInternal Complex.One units
            static member Apply (coeff, item : 'unit)        = Pr<'unit>.ApplyInternal coeff       [| item |> Cf<'unit>.Apply |]
            static member Apply (coeff, items : 'unit[])     = Pr<'unit>.ApplyInternal coeff       (items |> Array.map Cf<'unit>.Apply)
            static member Apply (coeff, unit : Cf<'unit>)    = Pr<'unit>.ApplyInternal coeff       [| unit |]
            static member Apply (coeff, units : Cf<'unit>[]) = Pr<'unit>.ApplyInternal coeff       units

            static member (*) (l : Pr<'unit>, r : Pr<'unit>) =
                match (l.IsZero, r.IsZero) with
                | (false, false) -> Pr<'unit>.Apply (l.Coeff * r.Coeff, Array.concat [| l.Units; r.Units |])
                | (_, _) -> Pr<'unit>.Zero

            member this.IsZero =
                (not this.Coeff.IsNonZero) || (this.Units |> Seq.exists (fun item -> item.IsZero))

            member this.ScaleCoefficient scale =
                    if this.Coeff = Complex.Zero then
                        None
                    else
                        Some <| { Pr.Coeff = this.Coeff * scale; Units = this.Units }

            member this.VerifyIsValid =
                let coefficientIsZeroOnlyWhenNoProductTerms =
                    if this.Units = [||] then
                        this.Coeff = Complex.Zero
                    else
                        this.Coeff <> Complex.Zero

                let everyUnitInProductTermHasUnitCoefficient =
                    this.Units
                    |> Seq.exists (fun u -> u.Coeff <> Complex.One)
                    |> not

                coefficientIsZeroOnlyWhenNoProductTerms &&
                everyUnitInProductTermHasUnitCoefficient

            override this.ToString() =
                this.Units
                |> Array.map (sprintf "%O")
                |> (fun rg -> System.String.Join (" | ", rg))
                |> sprintf "[%s]"

        and  Sum<'unit when 'unit : equality> =
            { Coeff : Complex; Terms : Map<string, Pr<'unit>> }
        with
            static member private ApplyInternal coeff terms =
                let terms' =
                    terms
                    |> Seq.choose (fun (t : Pr<'unit>) -> t.ScaleCoefficient coeff)
                    |> Seq.map (fun t -> (t.ToString(), t))
                    |> Map.ofSeq
                { Sum.Coeff = Complex.One; Sum.Terms = terms' }

            member this.WithNormalizedTermCoefficient =
                Sum<'unit>.ApplyInternal this.Coeff this.Terms.Values

            static member Apply (item         : 'unit)        = Sum<'unit>.ApplyInternal Complex.One  [| item  |> Pr<'unit>.Apply |]
            static member Apply (unit         : Cf<'unit>)    = Sum<'unit>.ApplyInternal Complex.One  [| unit  |> Pr<'unit>.Apply |]
            static member Apply (units        : Cf<'unit>[])  = Sum<'unit>.ApplyInternal Complex.One  [| units |> Pr<'unit>.Apply |]
            static member Apply (term         : Pr<'unit>)    = Sum<'unit>.ApplyInternal Complex.One  [| term                     |]
            static member Apply (terms        : Pr<'unit> []) = Sum<'unit>.ApplyInternal Complex.One  terms
            static member Apply (coeff, item  : 'unit)        = Sum<'unit>.ApplyInternal coeff        [| item  |> Pr<'unit>.Apply |]
            static member Apply (coeff, unit  : Cf<'unit>)    = Sum<'unit>.ApplyInternal coeff        [| unit  |> Pr<'unit>.Apply |]
            static member Apply (coeff, units : Cf<'unit>[])  = Sum<'unit>.ApplyInternal coeff        [| units |> Pr<'unit>.Apply |]
            static member Apply (coeff, term  : Pr<'unit>)    = Sum<'unit>.ApplyInternal coeff        [| term                     |]
            static member Apply (coeff, terms : Pr<'unit>[])  = Sum<'unit>.ApplyInternal coeff        terms

            static member (*) (l : Sum<'unit>, r : Sum<'unit>) =
                [|
                    for lt in l.WithNormalizedTermCoefficient.Terms.Values do
                        for gt in r.WithNormalizedTermCoefficient.Terms.Values do
                            yield lt * gt
                |]
                |> Sum<'unit>.Apply

            static member (+) (l : 'unit Sum, r : 'unit Sum) =
                Array.concat
                    [|
                        l.WithNormalizedTermCoefficient.Terms.Values
                        r.WithNormalizedTermCoefficient.Terms.Values
                    |]
                |> Sum<'unit>.Apply

            member this.IsZero =
                let nonZeroTermCount =
                    this.Terms.Values
                    |> Seq.filter (fun c -> not c.IsZero)
                    |> Seq.length
                (not this.Coeff.IsNonZero) || (nonZeroTermCount = 0)

            member this.VerifyIsValid =
                let termCoefficientIsUnity =
                    this.Coeff = Complex.One

                let termCoefficientIsZeroWhenThereAreNoProductTerms =
                    (this.Terms.Values = [||]) &&
                    (this.Coeff = Complex.Zero)

                termCoefficientIsZeroWhenThereAreNoProductTerms ||
                termCoefficientIsUnity

            override this.ToString() =
                (this.Coeff, this.Terms.Values)
                |> sprintf ("%O")

    [<AutoOpen>]
    module IndexedOperatorExtensions =
        let TryCreateIndexedOperatorUnit
            (unitFactory : string -> 'op option)
            (s : System.String) : Cf<Ix<'op>> option =
            try
                s.Trim().TrimStart('(').TrimEnd(')').Split(',')
                |> Array.map (fun s -> s.Trim ())
                |> (fun rg ->
                    let index = System.UInt32.Parse rg.[1]
                    unitFactory (rg.[0])
                        |> Option.map (fun op -> Cf<Ix<'op>>.Apply <| {Ix.Index = index; Op = op}))
            with
            | _ -> None

        let TryCreateIndexedOperatorProductTerm
            (unitFactory : string -> 'op option)
            (s : System.String) : Pr<Ix<'op>> option =
            let f = TryCreateIndexedOperatorUnit unitFactory
            try
                s.Trim().TrimStart('[').TrimEnd(']').Split('|')
                |> Array.choose (f)
                |> Pr<Ix<'op>>.Apply
                |> Some
            with
            | _ -> None

        let TryCreateIndexedOperatorSumTerm
            (unitFactory : string -> 'op option)
            (s : System.String) : Sum<Ix<'op>> option =
            let f = TryCreateIndexedOperatorProductTerm unitFactory
            try
                s.Trim().TrimStart('{').TrimEnd('}').Split(';')
                |> Array.choose (f)
                |> Sum<Ix<'op>>.Apply
                |> Some
            with
            | _ -> None