namespace Tests

module Terms_IxOp =
    open Encodings
    open Xunit
    open FsCheck.Xunit

    [<Property>]
    let ``I (.<=.) I`` (i1 : uint32, i2 : uint32, randomString : CChar) =
        let min = IxOp<uint32,CChar>.Apply (System.Math.Min (i1, i2), randomString)
        let max = IxOp<uint32,CChar>.Apply (System.Math.Max (i1, i2), randomString)
        Assert.True (min .<=. max)

    [<Property>]
    let ``I (.>=.) I`` (i1 : uint32, i2 : uint32, randomString : CChar) =
        let min = IxOp<uint32,CChar>.Apply (System.Math.Min (i1, i2), randomString)
        let max = IxOp<uint32,CChar>.Apply (System.Math.Max (i1, i2), randomString)
        Assert.True (max .>=. min)

    [<Property>]
    let ``IndicesInOrder Ascending works`` (indices : uint32[]) =
        let items = indices |> Array.map (fun index -> IxOp<uint32,CChar>.Apply(index, CC 'X'))
        if (IxOp<_,_>.IndicesInOrder IndexOrder.Ascending items) then
            Assert.True(true)
        else
            let sorted = items |> Seq.sortBy (fun item -> item.Index)
            Assert.True (IxOp<_,_>.IndicesInOrder IndexOrder.Ascending sorted)

    [<Property>]
    let ``IndicesInOrder Descending works`` (indices : uint32[]) =
        let items = indices |> Array.map (fun index -> IxOp<uint32,CChar>.Apply(index, CC 'X'))
        if (IxOp<_,_>.IndicesInOrder IndexOrder.Descending items) then
            Assert.True(true)
        else
            let sorted = items |> Seq.sortByDescending (fun item -> item.Index)
            Assert.True (IxOp<_,_>.IndicesInOrder IndexOrder.Descending sorted)

