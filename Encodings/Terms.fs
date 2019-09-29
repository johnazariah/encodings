namespace Encodings
open System.Numerics
open System.Collections.Generic

[<AutoOpen>]
module Terms =
    [<CustomEquality; NoComparison>]
    type C<'unit when 'unit : equality> =
        { Coeff : Complex; Item : 'unit }
    with
        member this.IsZero = this.Coeff.IsZero

        static member Unit =
            { Coeff = Complex.One; Item = Unchecked.defaultof<'unit> }

        static member Apply (coeff : Complex, unit) =
            { Coeff = coeff; Item = unit }

        member this.Reduce =
            { this with Coeff = this.Coeff.Reduce }

        member this.Normalize =
            { this with Coeff = Complex.One }

        static member (~-) (v : C<'unit>) =
            ({ v with Coeff = - v.Coeff })

        member inline this.ScaleCoefficient scale =
            { this with Coeff = this.Coeff * scale }

        member inline this.AddCoefficient coeff =
            { this with Coeff = this.Coeff + coeff }

        override x.Equals objY =
            match objY with
            | :? C<'unit> as y -> (x.Reduce.Coeff = y.Reduce.Coeff) && (x.Item = y.Item)
            | _ -> false

        override x.GetHashCode() = hash x.Reduce.Coeff ^^^ hash x.Item

    type SC< ^term when ^term : equality and ^term : (member Signature : string) > =
        | SumTerm of Map<string, C< ^term >>
    with
        member inline this.Unapply = match this with SumTerm st -> st
        member inline __.Coeff = Complex.One
        member inline this.Terms = this.Unapply.Values

        static member inline internal ApplyInternal (coeff : Complex) =
            let toTuple (t : C< ^term >) =
                let scaled = t.ScaleCoefficient coeff
                let key = (^term : (member Signature : string)(t.Item))
                (key, scaled)

            let createMap =
                let addOrUpdate (m : Dictionary<'key, C< ^term >>) (key : 'key, value : C< ^term >) =
                    if m.ContainsKey key then
                        m.[key] <- m.[key].AddCoefficient value.Coeff
                    else
                        m.[key] <- value
                    m
                let dictToMap (d : Dictionary<'key, C< ^term >>) =
                    seq { for kvp in d do yield (kvp.Key, kvp.Value) }
                    |> Map.ofSeq

                Array.fold addOrUpdate (new Dictionary<string, C< ^term >>())
                >> dictToMap

            Array.map toTuple
            >> createMap
            >> SumTerm

        static member inline Unit =
            SC< ^term >.ApplyInternal (Complex.One) ([||])

        static member inline Zero =
            SC< ^term >.ApplyInternal (Complex.Zero) ([||])

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

        member inline this.ScaleCoefficient scale =
            SC<_>.ApplyInternal scale this.Terms

        member inline this.AddCoefficient coeff =
            let terms = this.Terms |> Array.map (fun t -> t.AddCoefficient coeff)
            SC<_>.ApplyInternal Complex.One terms
