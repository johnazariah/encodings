namespace Tests

open Encodings
open Xunit
open FsCheck.Xunit
open System.Numerics

[<Properties (Arbitrary = [|typeof<ComplexGenerator>|], QuietOnSuccess = true) >]
module Terms_CIxOp =

    [<Property>]
    let ``Unapply round-trips with Apply`` (coeff : Complex, indexedOp : IxOp<uint32, CChar>) =
        let expected = C<_>.Apply(coeff, indexedOp)
        let actual = (CIxOp<_,_>.Apply (coeff, indexedOp)).Unapply
        Assert.Equal(expected, actual)
