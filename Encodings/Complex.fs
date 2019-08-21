namespace Encodings
[<AutoOpen>]
module Complex =
    open System.Numerics

    type Complex
    with
        member this.TimesI = new Complex (-this.Imaginary, this.Real)