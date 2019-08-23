namespace Encodings
[<AutoOpen>]
module Complex =
    open System
    open System.Numerics

    type Complex
    with
        static member SwapSignMultiple n (c : Complex) =
            [0..(n - 1)] |> Seq.fold (fun c' _ -> -c') c

        member this.TimesI = new Complex (-this.Imaginary, this.Real)
