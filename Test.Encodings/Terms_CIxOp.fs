namespace Tests

module Terms_CIxOp =
    open Encodings
    open Xunit
    open FsCheck.Xunit
    open System.Numerics

    [<Property (Arbitrary = [|typeof<ComplexGenerator>|]) >]
    let ``Unapply round-trips with Apply`` (coeff : Complex, indexedOp : IxOp<uint32, char>) =
        let expected = C<_>.Apply(coeff, indexedOp)
        let actual = (CIxOp<_,_>.Apply (coeff, indexedOp)).Unapply
        Assert.Equal(expected, actual)
