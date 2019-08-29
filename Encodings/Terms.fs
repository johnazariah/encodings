namespace Encodings

[<AutoOpen>]
module Terms =
    open System.Numerics

    type C<'unit when 'unit : equality> =
        { Coeff : Complex; Item : 'unit }
    with
        static member Apply (unit : 'unit) = { Coeff = Complex.One; Item = unit }
        static member Apply (coeff, unit)  = { Coeff = coeff;       Item = unit }

        static member (*) (l : C<'unit>, r : C<'unit>) =
            {
                P.Coeff = l.Coeff * r.Coeff
                P.Units = [| l ; r |]
            }

        static member (+) (l : C<'unit>, r : C<'unit>) =
            if (l.Item = r.Item) then
                let unit = { C.Coeff = Complex.One; C.Item = l.Item }
                { P.Coeff = l.Coeff + r.Coeff; P.Units = [| unit |]}
            else
                { P.Coeff = Complex.One; P.Units = [| l ; r|]}

        member this.IsZero = not this.Coeff.IsNonZero

    and  P<'unit when 'unit : equality> =
        { Coeff : Complex; Units : C<'unit>[] }
    with
        static member private ApplyInternal coeff (units : C<'unit>[]) =
            let (coeff', units') =
                units
                |> Array.fold
                    (fun (c, us) curr -> (c * curr.Coeff, Array.append us [| { curr with C.Coeff = Complex.One } |]))
                    (coeff, [||])

            let anyElementsAreZero = units' |> Seq.exists (fun u -> u.IsZero)
            if (units'.Length = 0 || coeff' = Complex.Zero || anyElementsAreZero) then
                { P.Coeff = Complex.Zero; P.Units = [||] }
            else
                { P.Coeff = coeff'; P.Units = units' }

        static member Zero = { Coeff = Complex.Zero; Units = [||] }

        member this.NormalizeUnitCoefficients =
            P<_>.ApplyInternal this.Coeff this.Units

        static member Apply (item  : 'unit)             = P<_>.ApplyInternal Complex.One [| item |> C<_>.Apply |]
        static member Apply (items : 'unit[])           = P<_>.ApplyInternal Complex.One (items |> Array.map C<_>.Apply)
        static member Apply (unit  : C<'unit>)          = P<_>.ApplyInternal Complex.One [| unit |]
        static member Apply (units : C<'unit>[])        = P<_>.ApplyInternal Complex.One units
        static member Apply (coeff, item : 'unit)       = P<_>.ApplyInternal coeff       [| item |> C<_>.Apply |]
        static member Apply (coeff, items : 'unit[])    = P<_>.ApplyInternal coeff       (items |> Array.map C<_>.Apply)
        static member Apply (coeff, unit : C<'unit>)    = P<_>.ApplyInternal coeff       [| unit |]
        static member Apply (coeff, units : C<'unit>[]) = P<_>.ApplyInternal coeff       units

        static member (*) (l : P<'unit>, r : P<'unit>) =
            match (l.IsZero, r.IsZero) with
            | (false, false) -> P<'unit>.Apply (l.Coeff * r.Coeff, Array.concat [| l.Units; r.Units |])
            | (_, _) -> P<'unit>.Zero

        member this.IsZero =
            (not this.Coeff.IsNonZero) || (this.Units |> Seq.exists (fun item -> item.IsZero))

        member this.ScaleCoefficient scale =
                if this.Coeff = Complex.Zero then
                    None
                else
                    Some <| { P.Coeff = this.Coeff * scale; Units = this.Units }

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

    and S<'unit when 'unit : equality> =
        { Coeff : Complex; Terms : Map<string, P<'unit>> }
    with
        static member private ApplyInternal coeff terms =
            let terms' =
                terms
                |> Seq.choose (fun (t : P<'unit>) -> t.ScaleCoefficient coeff)
                |> Seq.map (fun t -> (t.ToString(), t))
                |> Map.ofSeq
            { S.Coeff = Complex.One; S.Terms = terms' }

        member this.WithNormalizedTermCoefficient =
            S<_>.ApplyInternal this.Coeff this.Terms.Values

        static member Apply (item         : 'unit)       = S<'unit>.ApplyInternal Complex.One  [| item  |> P<'unit>.Apply |]
        static member Apply (unit         : C<'unit>)    = S<'unit>.ApplyInternal Complex.One  [| unit  |> P<'unit>.Apply |]
        static member Apply (units        : C<'unit>[])  = S<'unit>.ApplyInternal Complex.One  [| units |> P<'unit>.Apply |]
        static member Apply (term         : P<'unit>)    = S<'unit>.ApplyInternal Complex.One  [| term                    |]
        static member Apply (terms        : P<'unit> []) = S<'unit>.ApplyInternal Complex.One  terms
        static member Apply (coeff, item  : 'unit)       = S<'unit>.ApplyInternal coeff        [| item  |> P<'unit>.Apply |]
        static member Apply (coeff, unit  : C<'unit>)    = S<'unit>.ApplyInternal coeff        [| unit  |> P<'unit>.Apply |]
        static member Apply (coeff, units : C<'unit>[])  = S<'unit>.ApplyInternal coeff        [| units |> P<'unit>.Apply |]
        static member Apply (coeff, term  : P<'unit>)    = S<'unit>.ApplyInternal coeff        [| term                    |]
        static member Apply (coeff, terms : P<'unit>[])  = S<'unit>.ApplyInternal coeff        terms

        static member (*) (l : S<'unit>, r : S<'unit>) =
            [|
                for lt in l.WithNormalizedTermCoefficient.Terms.Values do
                    for gt in r.WithNormalizedTermCoefficient.Terms.Values do
                        yield lt * gt
            |]
            |> S<'unit>.Apply

        static member (+) (l : 'unit S, r : 'unit S) =
            Array.concat
                [|
                    l.WithNormalizedTermCoefficient.Terms.Values
                    r.WithNormalizedTermCoefficient.Terms.Values
                |]
            |> S<'unit>.Apply

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
