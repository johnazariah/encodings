namespace Encodings
[<AutoOpen>]
module Complex =
    open System
    open System.Numerics

    type Complex
    with
        member this.TimesI = new Complex (-this.Imaginary, this.Real)
        member this.ConjoiningSignAndPhase =
            match (this.Real, this.Imaginary) with
            | (r, 0.) when r >= 0. -> sprintf " + %A "   <| Math.Abs r
            | (r, 0.) when r <  0. -> sprintf " - %A "   <| Math.Abs r
            | (0., i) when i >= 0. -> sprintf " + (%A i) " <| Math.Abs i
            | (0., i) when i <  0. -> sprintf " - (%A i) " <| Math.Abs i
            | _ -> sprintf " + %A" this

        member this.PhasePrefix =
            match (this.Real, this.Imaginary) with
            | (r, 0.) -> sprintf "%A " r
            | (0., i) -> sprintf "(%A i) " i
            | _ -> sprintf "%A" this