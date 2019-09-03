namespace Encodings

[<AutoOpen>]
module Terms =
    open System.Numerics

    type C<'unit when 'unit : equality> =
        { Coeff : Complex; Item : 'unit }
    with
        static member Apply (unit : 'unit) =
            { Coeff = Complex.One;  Item = unit }
        static member Apply (coeff : Complex, unit : 'unit) =
            { Coeff = coeff.Reduce; Item = unit }

        static member (*) (l : C<'unit>, r : C<'unit>) =
            let coeff = (l.Coeff * r.Coeff).Reduce
            if (coeff.IsZero) then
                P<'unit>.Apply (Complex.Zero, [| l; r |])
            else
                P<'unit>.Apply (coeff, [| {l with Coeff = Complex.One}; {r with Coeff = Complex.One} |])

        static member (+) (l : C<'unit>, r : C<'unit>) =
            if (l.Item <> r.Item) then
                P<'unit>.Apply (Complex.One, [| l; r|])
            else
                P<'unit>.Apply ((l.Coeff + r.Coeff).Reduce, [| { l with C.Coeff = Complex.One } |])

        member this.IsZero = this.Coeff.IsZero

        member this.Reduce = { this with Coeff = this.Coeff.Reduce }

        override this.ToString() =
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

    and  P<'unit when 'unit : equality> =
        { Coeff : Complex; Units : C<'unit>[] }
    with
        static member private ApplyInternal (coeff : Complex) (units : C<'unit>[]) =
            {
                P.Coeff = coeff.Reduce
                P.Units = (units |> Array.map (fun u -> u.Reduce))
            }

        static member Apply (coeff : Complex, units : C<'unit>[]) : P<'unit> =
            P<'unit>.ApplyInternal coeff units

        static member Zero = { Coeff = Complex.Zero; Units = [||] }

        member this.Reduce =
            let normalize (resultCoeff : Complex, resultUnits) (currentUnit : C<'unit>) =
                if (resultCoeff.IsZero || currentUnit.IsZero) then
                    (Complex.Zero, [||])
                else
                    (resultCoeff * currentUnit.Coeff, Array.append resultUnits [| { currentUnit with Coeff = Complex.One }|])
            lazy
                this.Units
                |> Array.fold normalize (this.Coeff, [||])
                |> (fun (c, u) -> if ((c.IsZero) || (u = [||])) then (Complex.Zero, [||]) else (c, u))
                |> P<'unit>.Apply

        static member Apply (       item  : 'unit)      : P<'unit> = P<'unit>.Apply(Complex.One, [| item |> C<_>.Apply |])
        static member Apply (coeff, item  : 'unit)      : P<'unit> = P<'unit>.Apply(coeff,       [| item |> C<_>.Apply |])
        static member Apply (       items : 'unit[])    : P<'unit> = P<'unit>.Apply(Complex.One, (items |> Array.map C<_>.Apply))
        static member Apply (coeff, items : 'unit[])    : P<'unit> = P<'unit>.Apply(coeff,       (items |> Array.map C<_>.Apply))
        static member Apply (       unit  : C<'unit>)   : P<'unit> = P<'unit>.Apply(Complex.One, [| unit |])
        static member Apply (coeff, unit  : C<'unit>)   : P<'unit> = P<'unit>.Apply(coeff,       [| unit |])
        static member Apply (       units : C<'unit>[]) : P<'unit> = P<'unit>.Apply(Complex.One, units)

        static member (*) (l : P<'unit>, r : P<'unit>) =
            P<'unit>.Apply ((l.Coeff * r.Coeff).Reduce, Array.concat [| l.Units; r.Units |])

        member this.IsZero =
            (not this.Coeff.IsNonZero) || (this.Units |> Seq.exists (fun item -> item.IsZero))

        member this.ScaleCoefficient scale =
            { P.Coeff = this.Coeff * scale; Units = this.Units }

        member this.VerifyReduced =
            let coefficientIsZeroOnlyWhenNoProductTerms =
                if this.Units = [||] then
                    this.Coeff = Complex.Zero
                else
                    this.Coeff <> Complex.Zero

            let everyUnitInProductTermHasUnitCoefficient =
                this.Units
                |> Seq.exists (fun u -> u.Coeff <> Complex.One)
                |> not

            let result =
                coefficientIsZeroOnlyWhenNoProductTerms &&
                everyUnitInProductTermHasUnitCoefficient
#if DEBUG
            if not result then
                System.Diagnostics.Debugger.Break ()
#endif
            result

        static member TryCreateFromString
            (unitFactory : string -> 'unit option)
            (s : System.String) : P<'unit> option =
            try
                s.Trim().TrimStart('[').TrimEnd(']').Split('|')
                |> Array.choose (unitFactory)
                |> P<'unit>.Apply
                |> Some
            with
            | _ -> None

        override this.ToString() =
            this.Units
            |> Array.map (sprintf "%O")
            |> (fun rg -> System.String.Join (" | ", rg))
            |> sprintf "[%s]"

    and S<'unit when 'unit : equality> =
        { Coeff : Complex; Terms : Map<string, P<'unit>> }
    with
        static member private ApplyInternal coeff terms =
            terms
            |> Seq.map (fun (t : P<'unit>) ->
                let scaled = t.ScaleCoefficient coeff
                let reduced = scaled.Reduce.Value
                (reduced.ToString(), reduced))
            |> Map.ofSeq
            |> (fun terms' -> { S.Coeff = Complex.One; S.Terms = terms' })

        member this.NormalizeTermCoefficient =
            S<'unit>.ApplyInternal this.Coeff this.Terms.Values

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
                for lt in l.NormalizeTermCoefficient.Terms.Values do
                    for gt in r.NormalizeTermCoefficient.Terms.Values do
                        yield lt * gt
            |]
            |> S<'unit>.Apply

        static member (+) (l : 'unit S, r : 'unit S) =
            Array.concat
                [|
                    l.NormalizeTermCoefficient.Terms.Values
                    r.NormalizeTermCoefficient.Terms.Values
                |]
            |> S<'unit>.Apply

        member this.Reduce =
            lazy
                if this.Coeff.IsZero then
                    [||]
                else
                    [|
                        for pt in this.Terms.Values do
                            let pt' = pt.Reduce.Value
                            if pt'.Units <> [||] then
                                yield pt'
                    |]
                |> S<'unit>.Apply

        member this.AppendToTerms (u : 'unit) =
            let this' = this.Reduce.Value
            if this'.Coeff.IsZero then
                [||]
            else if this'.Terms.Count = 0 then
                [| P<_>.Apply u |]
            else
                [|
                    for pt in this'.Terms.Values do
                        yield { pt with Units = Array.concat [| pt.Units; [|C<_>.Apply u|]|]}
                |]
            |> S<_>.ApplyInternal this'.Coeff

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
            this.Terms.Values
            |> Array.map (sprintf "%O")
            |> (fun rg -> System.String.Join ("; ", rg))
            |> sprintf "{%s}"