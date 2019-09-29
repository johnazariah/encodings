namespace Encodings
[<AutoOpen>]
module Demo =
    type Test< ^op > =
        { Op : ^op }
    with
        member inline __.Signature = ""

    type (*and*) Wrapper< ^test when ^test : equality and ^test : (member Signature : string) > =
        | W of ^test

    type (*and*) Wrapper'< ^test when ^test : equality and ^test : (member Signature : string) > =
        | W' of Wrapper<Test< ^test >>


[<AutoOpen>]
module TypeExtensions =
    open System
    open System.Numerics

    let uncurry (f : 'x -> 'y -> 'r) =
        (fun (x, y) -> f x y)

    let curry (f : ('x * 'y) -> 'r) =
        (fun x y -> f (x, y))

    let equalsOn f x (objY : obj) =
        match objY with
        | :? 'T as y -> (f x = f y)
        | _ -> false

    let hashOn f x =  hash (f x)

    let compareOn f x (objY: obj) =
        match objY with
        | :? 'T as y -> compare (f x) (f y)
        | _ -> invalidArg "yobj" "cannot compare values of different types"

    type Complex
    with
        static member SwapSignMultiple n (c : Complex) =
            [0..(n - 1)] |> Seq.fold (fun c' _ -> -c') c

        static member MinusOne = Complex.One |> Complex.Negate

        member this.IsNonZero =
            Complex.IsFinite this && (this <> Complex.Zero)

        member this.IsZero =
            not this.IsNonZero

        member this.Reduce =
            let round (d : Double) = Math.Round(d, 12, MidpointRounding.AwayFromZero)

            if Complex.IsFinite this then
                Complex(round this.Real, round this.Imaginary)
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
