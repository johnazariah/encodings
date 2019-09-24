namespace Encodings

[<AutoOpen>]
module DenseRepresentation =
    open System.Numerics

    type R< ^unit when ^unit : (static member Identity : ^unit) and ^unit : (static member Combine : C< ^unit > * C< ^unit > -> C< ^unit >) and ^unit : equality> =
        | Register of C<C<'unit>[]>
    with
        member inline this.Unapply = match this with Register pt -> pt
        member inline this.Coeff = this.Unapply.Coeff
        member inline this.Units = this.Unapply.Item

        static member inline Apply (coeff : Complex, units : C< ^unit>[]) =
            C<_>.Apply(coeff.Reduce, units |> Array.map (fun u -> u.Reduce))
            |> (fun t -> t.Reduce)
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
                    ((coeff * curr.Coeff).Reduce, [| yield! units; yield curr.Normalize.Reduce |])

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

