namespace Tests

module Terms_IxOp =
    open Encodings
    open Xunit
    open FsCheck.Xunit

    [<Property>]
    let ``I (.<=.) I`` (i1 : uint32, i2 : uint32, randomString : string) =
        let min = IxOp<_,_>.Apply (System.Math.Min (i1, i2), randomString)
        let max = IxOp<_,_>.Apply (System.Math.Max (i1, i2), randomString)
        Assert.True (min .<=. max)

    [<Property>]
    let ``I (.>=.) I`` (i1 : uint32, i2 : uint32, randomString : string) =
        let min = IxOp<_,_>.Apply (System.Math.Min (i1, i2), randomString)
        let max = IxOp<_,_>.Apply (System.Math.Max (i1, i2), randomString)
        Assert.True (max .>=. min)
