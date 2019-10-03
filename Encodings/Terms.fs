namespace Encodings
open System.Numerics
open System.Collections.Generic

[<AutoOpen>]
module Terms =
    [<CustomEquality; NoComparison>]
    type C<'unit when 'unit : equality> =
        { C : Complex; U : 'unit }
    with
        member inline this.IsZero = this.C.IsZero

        static member inline Unit =
            { C = Complex.One; U = Unchecked.defaultof<'unit> }

        static member inline Apply (coeff : Complex, unit) =
            { C = coeff; U = unit }

        member inline this.Normalize =
            { this with C = Complex.One }

        static member inline (~-) (v : C<'unit>) =
            ({ v with C = - v.C })

        member inline this.ScaleCoefficient (scale : Complex)  =
            { this with C = this.C * scale }

        member inline this.AddCoefficient (diff : Complex) =
            { this with C = this.C + diff }

        // custom equality prevents inlining
        override this.Equals otherObj =
            match otherObj with
            | :? C<'unit> as other -> Complex.ApproximatelyEqual(this.C, other.C) && (this.U = other.U)
            | _ -> false

        override this.GetHashCode() = hash this.C.Reduce ^^^ hash this.U

        static member inline P1 x = C<'unit>.Apply (Complex.One          , x)
        static member inline Pi x = C<'unit>.Apply (Complex.ImaginaryOne , x)
        static member inline M1 x = C<'unit>.Apply (Complex.MinusOne     , x)
        static member inline Mi x = C<'unit>.Apply (Complex.MinusI       , x)


    type SC< ^term
            when ^term : equality
            and ^term : (member Signature : string)
            and ^term : (static member (<.>) : C< ^term > -> C< ^term > -> C<C< ^term >[]>)> =
        | SumTerm of Map<string, C< ^term >>
    with
        static member inline private KeySignature(t : ^term) =
            (^term : (member Signature : string)(t))
        member inline this.Unapply = match this with SumTerm st -> st
        member inline __.Coeff = Complex.One
        member inline this.Terms = [| for term in this.Unapply.Values do if SC< ^term >.KeySignature(term.U) <> "" then yield term |]

        static member inline internal ApplyInternal (coeff : Complex) =
            let toTuple (t : C< ^term >) =
                let scaled = t.ScaleCoefficient coeff
                let key = (^term : (member Signature : string)(t.U))
                (key, scaled)

            let createMap =
                let addOrUpdate (m : Dictionary<'key, C< ^term >>) (key : 'key, value : C< ^term >) =
                    if m.ContainsKey key then
                        m.[key] <- m.[key].AddCoefficient value.C
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

        static member inline (*) (l : SC< ^term >, r : SC< ^term >) =
            let multiplier (lt, rt) = (^term : (static member (<.>) : C< ^term > -> C< ^term > -> C<C< ^term >[]>)(lt, rt))
            [|
                for lt in l.Terms do
                    for rt in r.Terms do
                        yield multiplier (lt, rt)
            |]
            |> Array.fold
                (fun (resultCoeff, resultTerms) curr ->
                    (resultCoeff * curr.C, [| yield! resultTerms; yield! curr.U |]))
                (Complex.One, [||])
            |> uncurry SC<_>.ApplyInternal

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

