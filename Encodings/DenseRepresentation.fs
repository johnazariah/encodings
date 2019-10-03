namespace Encodings

[<AutoOpen>]
module DenseRepresentation =
    open System.Numerics

    type R< ^unit
                when ^unit : (static member Identity : ^unit)
                and  ^unit : (static member Multiply : ^unit * ^unit -> C< ^unit >)
                and  ^unit : equality> =
        | Register of C< ^unit[] >
    with
        member inline this.Unapply = match this with Register pt -> pt
        member inline this.Coeff = this.Unapply.C
        member inline this.Units = this.Unapply.U

        member inline this.bindAtIndex f = function
            | n when n < 0 -> None
            | n when n >= this.Units.Length -> None
            | n -> n |> f

        member inline this.mapAtIndex f = this.bindAtIndex (f >> Some)

        static member inline ApplyInternal =
            C< ^unit >.Apply >> R< ^unit >.Register

        static member inline Unit = R< ^unit >.ApplyInternal (Complex.One,  [||])
        static member inline Zero = R< ^unit >.ApplyInternal (Complex.Zero, [||])

        member inline this.Item
            with get i =
                this.mapAtIndex (fun idx -> this.Units.[idx]) i
            and set i v =
                this.mapAtIndex (fun idx -> do this.Units.[idx] <- v) i
                |> ignore

        static member inline IdentityUnit =
            (^unit : (static member Identity : ^unit) ())

        static member inline MultiplyUnits (a, b) =
            (^unit: (static member Multiply : ^unit * ^unit -> C< ^unit >) (a, b))

        member inline this.ScaleCoefficient (scale : Complex) =
            this.Unapply.ScaleCoefficient scale
            |> R< ^unit >.Register

        member inline this.AddCoefficient (diff : Complex) =
            this.Unapply.AddCoefficient diff
            |> R< ^unit >.Register

        member inline this.IsZero =
            this.Coeff.IsZero || (this.Units = [||])

        member inline this.Reduce =
            if this.IsZero then
                R< ^unit >.Zero
            else
                this

        member inline this.Signature =
            this.Reduce.Units
            |> Array.fold (sprintf "%s%O") ""

        static member inline Apply (coeff : Complex, units : C< ^unit>[]) : R< ^unit > =
            let extractedCoeff =
                units
                |> Array.fold (fun coeff curr -> coeff * curr.C) Complex.One

            let indexedOps =
                units
                |> Array.map (fun curr -> curr.U)

            R< ^unit >.ApplyInternal (coeff * extractedCoeff, indexedOps)

        static member inline (*) (l : R< ^unit >, r : R< ^unit >) =
            let identityUnit =
                (^unit : (static member Identity : ^unit) ())

            let multiplyUnits (a, b) =
                (^unit: (static member Multiply : ^unit * ^unit -> C< ^unit >) (a, b))

            let pairwiseCombine rgls rgrs =
                let rec pairwiseCombine' ls rs =
                    match (ls, rs) with
                    | []      , []       -> []
                    | l :: ls', []       -> multiplyUnits (l, identityUnit) :: (pairwiseCombine' ls' [])
                    | []      , r :: rs' -> multiplyUnits (identityUnit, r) :: (pairwiseCombine' [] rs')
                    | l :: ls', r :: rs' -> multiplyUnits (l, r)            :: (pairwiseCombine' ls' rs')
                in
                pairwiseCombine' (rgls |> List.ofArray) (rgrs |> List.ofArray)
                |> Array.ofList

            let coeff' = l.Coeff * r.Coeff
            let units' = pairwiseCombine l.Units r.Units
            R< ^unit >.Apply (coeff', units')

        static member inline (<.>) (l : C<R< ^unit >>, r : C<R< ^unit >>) : C<C<R< ^unit >>[]> =
            [|
                C<_>.Apply(Complex.One, l.U.ScaleCoefficient l.C * r.U.ScaleCoefficient r.C)
            |]
            |> curry C<_>.Apply Complex.One

        static member inline New (length) =
            [| 1u .. length |]
            |> Array.map (fun _ -> R< ^unit>.IdentityUnit)
            |> curry R< ^unit >.ApplyInternal Complex.One

    type SR< ^unit
                when ^unit : (static member Identity : ^unit)
                and  ^unit : (static member Multiply : ^unit * ^unit -> C< ^unit >)
                and  ^unit : equality> =
        | SumTerm of SC<R< ^unit >>
    with
        member inline this.Unapply = match this with SumTerm st -> st
        member inline __.Coeff = Complex.One

        member inline this.Terms =
            this.Unapply.Terms

        static member inline Apply (coeff : Complex, terms : R< ^unit>[]) : SR< ^unit > =
            terms
            |> Array.map (curry C<_>.Apply Complex.One)
            |> (curry SC<R< ^unit>>.Apply coeff)
            |> SR< ^unit >.SumTerm

        static member inline (+) (l : SR< ^unit >, r : SR< ^unit >) =
            l.Unapply + r.Unapply
            |> SR< ^unit >.SumTerm

        static member inline (*) (l : SR< ^unit >, r : SR< ^unit >) =
            l.Unapply * r.Unapply
            |> SR< ^unit >.SumTerm

        member inline this.IsZero =
            this.Unapply.IsZero
