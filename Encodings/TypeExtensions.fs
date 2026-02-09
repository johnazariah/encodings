namespace Encodings

open System.Numerics

[<AutoOpen>]
module TypeExtensions =
    open System

    let uncurry (f : 'x -> 'y -> 'r) =
        (fun (x, y) -> f x y)

    let curry (f : ('x * 'y) -> 'r) =
        (fun x y -> f (x, y))

    type Complex
    with
        static member SwapSignMultiple n (c : Complex) =
            [0..(n - 1)] |> Seq.fold (fun c' _ -> -c') c

        static member MinusOne = Complex.One |> Complex.Negate

        member this.IsFinite =
            not (System.Double.IsNaN this.Real || System.Double.IsInfinity this.Real) &&
            not (System.Double.IsNaN this.Imaginary || System.Double.IsInfinity this.Imaginary)

        member this.IsNonZero =
            let isFinite =
                not (System.Double.IsNaN this.Real || System.Double.IsInfinity this.Real) &&
                not (System.Double.IsNaN this.Imaginary || System.Double.IsInfinity this.Imaginary)
            isFinite && (this <> Complex.Zero)

        member this.IsZero =
            let isFinite =
                not (System.Double.IsNaN this.Real || System.Double.IsInfinity this.Real) &&
                not (System.Double.IsNaN this.Imaginary || System.Double.IsInfinity this.Imaginary)
            let isNonZero = isFinite && (this <> Complex.Zero)
            not isNonZero

        member this.Reduce =
            let isFinite =
                not (System.Double.IsNaN this.Real || System.Double.IsInfinity this.Real) &&
                not (System.Double.IsNaN this.Imaginary || System.Double.IsInfinity this.Imaginary)
            if isFinite then
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
        member this.Keys =
            this |> Map.toArray |> Array.map fst

        member this.Values =
            this |> Map.toArray |> Array.map snd
