namespace Encodings
open System.Numerics
open System.Collections.Generic

[<AutoOpen>]
module Terms =
    [<CustomEquality; CustomComparison>]
    type C<'unit
            when 'unit : equality
            and 'unit : comparison> =
        { Coeff : Complex; Thunk : 'unit }
    with
        member inline this.IsZero = this.Coeff.IsZero

        static member inline Unit =
            { Coeff = Complex.One; Thunk = Unchecked.defaultof<'unit> }

        static member inline Apply (coeff : Complex, unit) =
            { Coeff = coeff; Thunk = unit }

        member inline this.Normalize =
            { this with Coeff = Complex.One }

        static member inline (~-) (v : C<'unit>) =
            ({ v with Coeff = - v.Coeff })

        member inline this.ScaleCoefficient (scale : Complex)  =
            { this with Coeff = this.Coeff * scale }

        member inline this.AddCoefficient (diff : Complex) =
            { this with Coeff = this.Coeff + diff }

        // custom equality prevents inlining
        override this.Equals otherObj =
            match otherObj with
            | :? C<'unit> as other ->
                (this.Thunk = other.Thunk)
                && Complex.ApproximatelyEqual(this.Coeff, other.Coeff)
            | _ -> false

        override this.GetHashCode() = hash this.Coeff.Reduce ^^^ hash this.Thunk

        interface System.IComparable with
            member this.CompareTo otherObj =
                match (otherObj) with
                | :? C<'unit> as other -> compare this.Thunk other.Thunk
                | _ -> 0

        static member inline P1 (x : 'unit) = C<'unit>.Apply(Complex.One         , x)
        static member inline Pi (x : 'unit) = C<'unit>.Apply(Complex.ImaginaryOne, x)
        static member inline M1 (x : 'unit) = C<'unit>.Apply(Complex.MinusOne    , x)
        static member inline Mi (x : 'unit) = C<'unit>.Apply(Complex.MinusI      , x)

    type S< ^term
        when ^term : equality
        and  ^term : (member Signature        : string)
        and  ^term : (member ScaleCoefficient : Complex -> ^term)
        and  ^term : (member AddCoefficient   : Complex -> ^term)
        and  ^term : (member Coeff            : Complex)
        and  ^term : (member IsZero           : bool)
        and  ^term : (static member (<*>)     : ^term -> ^term -> ^term)> =
        | SumTerm of Map<string, ^term>
    with
        static member inline private Term_ScaleCoefficient (value, scale) = (^term : (member ScaleCoefficient : Complex -> ^term)        (value, scale))
        static member inline private Term_AddCoefficient   (value, diff)  = (^term : (member AddCoefficient   : Complex -> ^term)        (value, diff))
        static member inline private Term_Coeff            (value)        = (^term : (member Coeff            : Complex)                 (value))
        static member inline private Term_Signature        (value)        = (^term : (member Signature        : string)                  (value))
        static member inline private Term_IsZero           (value)        = (^term : (member IsZero           : bool)                    (value))
        static member inline private Term_Combine          (l, r)         = (^term : (static member (<*>)     : ^term -> ^term -> ^term) (l, r))

        member inline this.Unapply = match this with SumTerm st -> st
        member inline __.Coeff = Complex.One

        member inline this.Terms : ^term[] =
            [|
                for term in this.Unapply.Values do
                    if S< ^term >.Term_Signature(term) <> "" then
                        yield term
            |]

        static member inline Apply (coeff : Complex, terms) : S< ^term > =
            let createMap =
                let addOrUpdate (m : Dictionary<'key, ^term>) (key : 'key, value : ^term) =
                    if m.ContainsKey key then
                        m.[key] <- S<_>.Term_AddCoefficient (m.[key], S<_>.Term_Coeff value)
                    else
                        m.[key] <- value
                    m
                let dictToMap (d : Dictionary<'key, ^term>) =
                    seq { for kvp in d do yield (kvp.Key, kvp.Value) }
                    |> Map.ofSeq

                Array.fold addOrUpdate (new Dictionary<string, ^term>())
                >> dictToMap

            terms
            |> Array.map (fun t -> (S<_>.Term_Signature t, S<_>.Term_ScaleCoefficient (t, coeff)))
            |> createMap
            |> SumTerm

        static member inline Unit : S< ^term> =
            S< ^term >.Apply (Complex.One, [| |])

        static member inline Zero : S< ^term> =
            S< ^term >.Apply (Complex.Zero, [| |])

        member inline this.Reduce =
            lazy
                if this.Coeff.IsZero then
                    S< ^term >.Apply (Complex.Zero, [| |])
                else
                    [|
                        for pt in this.Terms do
                            if (not (S<_>.Term_IsZero pt)) then yield pt
                    |]
                    |> curry S< ^term >.Apply Complex.One

        static member inline (*) (l : S< ^term >, r : S< ^term >) =
            [|
                for lt in l.Terms do
                    for rt in r.Terms do
                        yield S< ^term >.Term_Combine (lt, rt)
            |]
            |> curry S<_>.Apply Complex.One

        static member inline (+) (l : S< ^term >, r : S< ^term >) =
            [|
                yield! l.Terms
                yield! r.Terms
            |]
            |> curry S<_>.Apply Complex.One

        member inline this.IsZero : bool =
            let allZeroTerms = this.Terms |> (Seq.exists (fun t -> not (S<_>.Term_IsZero t))) |> not
            this.Coeff.IsZero || allZeroTerms

        member inline this.ScaleCoefficient (scale : Complex) : S< ^term > =
            S<_>.Apply (scale, this.Terms)

        member inline this.AddCoefficient (diff: Complex) : S< ^term >  =
            let terms = this.Terms |> Array.map (fun t -> S<_>.Term_AddCoefficient(t, diff))
            S<_>.Apply (Complex.One, terms)

