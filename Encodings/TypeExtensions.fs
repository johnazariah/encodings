namespace Encodings
[<AutoOpen>]
module TypeExtensions =
    open System
    open System.Numerics

    let uncurry (f : 'x -> 'y -> 'r) =
        (fun (x, y) -> f x y)

    let curry (f : ('x * 'y) -> 'r) =
        (fun x y -> f (x, y))

    type System.Double
    with
        member this.IsFinite =
            if Double.IsInfinity this then false
            else if Double.IsNaN this then false
            else true

    type Complex
    with
        static member SwapSignMultiple n (c : Complex) =
            [0..(n - 1)] |> Seq.fold (fun c' _ -> -c') c

        member this.IsFinite =
            this.Real.IsFinite && this.Imaginary.IsFinite

        member this.IsNonZero =
            this.IsFinite && (this <> Complex.Zero)

        member this.IsZero =
            not this.IsNonZero

        member this.Reduce =
            if this.IsFinite then
                this
            else
                Complex.Zero

        member this.TimesI = new Complex (-this.Imaginary, this.Real)

        member this.ToPhasePrefix =
            match (this.Real, this.Imaginary) with
            | (+1., 0.) -> ""
            | (-1., 0.) -> " -"
            | (0., +1.) -> "( i) "
            | (0., -1.) -> "(-i) "
            | (r, 0.)   -> sprintf "%A " r
            | (0., i)   -> sprintf "(%A i) " i
            | _ -> sprintf "%A" this

        member this.ToPhaseConjunction =
            match (this.Real, this.Imaginary) with
            | (+1., 0.) -> " + "
            | (-1., 0.) -> " - "
            | (0., +1.) -> " + i "
            | (0., -1.) -> " - i "
            | (r, 0.) when r >= 0. -> sprintf " + %A "     <| Math.Abs r
            | (r, 0.) when r <  0. -> sprintf " - %A "     <| Math.Abs r
            | (0., i) when i >= 0. -> sprintf " + (%A i) " <| Math.Abs i
            | (0., i) when i <  0. -> sprintf " - (%A i) " <| Math.Abs i
            | _ -> sprintf " + %A" this

    type Map<'Key, 'Value when 'Key : comparison>
    with
        member this.Key =
            this |> Map.fold (fun s k _ ->Array.concat [| s; [| k |] |]) [||]

        member this.Values =
            this |> Map.fold (fun s _ v ->Array.concat [| s; [| v |] |]) [||]
