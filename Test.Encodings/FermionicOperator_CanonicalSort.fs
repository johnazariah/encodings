namespace Tests

open FsCheck.Xunit
open System.Numerics
open Encodings
open Encodings.FermionicOperator_Order
open Xunit
open System.Collections.Generic

[<Properties (Arbitrary = [|typeof<ComplexGenerator>|], QuietOnSuccess = true) >]
module FermionicOperator_CanonicalSort =

    let ``FindNextIndex finds the next index`` apply compare indices =
        let ixops =
            indices
            |> Array.map apply

        let expected =
            indices
            |> Array.fold
                (fun (index, result) curr ->
                    let result' =
                        match result with
                        | None -> Some (curr, index)
                        | Some (r, _) -> if compare curr r then Some (curr, index) else result
                    (index + 1, result'))
                (0, None)
            |> snd

        let actual =
            ixops
            |> findNextIndex
            |> Option.map (fun (ixop, index) -> (ixop.Index, index))

        Assert.Equal (expected, actual)

    [<Property>]
    let ``FindNextIndex finds the smallest Cr as the next index`` (indices : uint32[]) =
        ``FindNextIndex finds the next index``
            (fun ix -> IxOp<_,_>.Apply(ix, FermionicOperator.Cr))
            (<=)
            indices

    [<Property>]
    let ``FindNextIndex finds the largest An as the next index`` (indices : uint32[]) =
        ``FindNextIndex finds the next index``
            (fun ix -> IxOp<_,_>.Apply(ix, FermionicOperator.An))
            (>=)
            indices

    [<Property>]
    let ``FindNextIndex finds Cr before An as the next index`` (indices : uint32[]) =
        let ixops =
            [|
                yield! indices |> Array.map (fun ix -> IxOp<_,_>.Apply(ix, FermionicOperator.An))
                yield IxOp<_,_>.Apply(27u, FermionicOperator.Cr)
            |]

        let expected = Some (27u, indices.Length)
        let actual =
            ixops
            |> findNextIndex
            |> Option.map (fun (ixop, index) -> (ixop.Index, index))

        Assert.Equal (expected, actual)

    [<Property>]
    let ``Behead extracts the desired item and its coefficient`` (ixops : IxOp<uint32, FermionicOperator>[]) =
        ixops
        |> findNextIndex
        |> Option.iter
            (fun (expectedItem, expectedIndex) ->
                let (head, _) = ixops |> behead expectedIndex
                let expectedCoeff = if expectedIndex % 2 = 0 then Complex.One else Complex.MinusOne
                Assert.Equal(expectedCoeff, head.Coeff)
                Assert.Equal(expectedItem, head.Thunk))

    [<Fact>]
    let ``Behead extracts the desired item and its coefficient (Regression 1)`` () =
        let ixops =
            [|
                IxOp<_,_>.Apply (0u, Cr)
            |]
        ``Behead extracts the desired item and its coefficient`` ixops

    [<Property>]
    let ``ChunkByIndex returns chunks with the same index`` (ixops : IxOp<uint32, FermionicOperator>[]) =
        let allElementsHaveSameIndex (curr : PIxOp<uint32, FermionicOperator>) =
            if curr.IndexedOps.Length = 0 then
                true
            else
                let target = curr.IndexedOps.[0].Index
                curr.IndexedOps
                |> Array.map (fun ixop -> ixop.Index)
                |> Array.fold (fun result curr -> result && (curr = target)) true
        let allChunksHaveSameIndex result curr =
            result && allElementsHaveSameIndex curr

        ixops
        |> chunkByIndex
        |> Array.fold allChunksHaveSameIndex true
        |> Assert.True

    [<Property>]
    let ``ChunkByIndex returns chunks in index order`` (ixops : IxOp<uint32, FermionicOperator>[]) =
        let chunksAreInIndexOrder result (curr : PIxOp<_,_>) =
            let (prevIndex, inOrder) = result
            match prevIndex with
            | None ->
                findNextIndex curr.IndexedOps
                |> Option.map (fun (firstOp, _) ->
                    (Some firstOp, true))
                |> Option.defaultValue (None, false)
            | Some firstOp ->
                findNextIndex curr.IndexedOps
                |> Option.map (fun (secondOp, _) ->
                    (Some secondOp, inOrder && FermionicOperator.InIndexOrder(firstOp, secondOp)))
                |> Option.defaultValue (None, false)

        ixops
        |> chunkByIndex
        |> Array.fold chunksAreInIndexOrder (None, true)
        |> snd
        |> Assert.True

    [<Fact>]
    let ``ChunkByIndex returns chunks in index order (Regression 1)`` () =
        [|
            IxOp<_,_>.Apply (0u, FermionicOperator.An)
            IxOp<_,_>.Apply (1u, FermionicOperator.An)
            IxOp<_,_>.Apply (0u, FermionicOperator.Cr)
        |]
        |> ``ChunkByIndex returns chunks in index order``

    [<Fact>]
    let ``ChunkByIndex returns chunks in index order (Regression 2)`` () =
        [|
            IxOp<_,_>.Apply (0u, FermionicOperator.An)
            IxOp<_,_>.Apply (1u, FermionicOperator.An)
            IxOp<_,_>.Apply (2u, FermionicOperator.An)
            IxOp<_,_>.Apply (3u, FermionicOperator.An)
            IxOp<_,_>.Apply (4u, FermionicOperator.An)
            IxOp<_,_>.Apply (5u, FermionicOperator.An)
            IxOp<_,_>.Apply (6u, FermionicOperator.An)
        |]
        |> ``ChunkByIndex returns chunks in index order``

    [<Fact>]
    let ``ChunkByIndex returns chunks in index order (Regression 3)`` () =
        [|
            IxOp<_,_>.Apply (0u, FermionicOperator.Cr)
            IxOp<_,_>.Apply (1u, FermionicOperator.Cr)
            IxOp<_,_>.Apply (2u, FermionicOperator.Cr)
            IxOp<_,_>.Apply (3u, FermionicOperator.Cr)
            IxOp<_,_>.Apply (4u, FermionicOperator.Cr)
            IxOp<_,_>.Apply (5u, FermionicOperator.Cr)
            IxOp<_,_>.Apply (6u, FermionicOperator.Cr)
        |]
        |> ``ChunkByIndex returns chunks in index order``

    [<Fact>]
    let ``ChunkByIndex returns chunks in index order (Regression 4)`` () =
        let ixops =
            [|
                IxOp<_,_>.Apply (0u, FermionicOperator.An)
                IxOp<_,_>.Apply (1u, FermionicOperator.An)
                IxOp<_,_>.Apply (2u, FermionicOperator.An)
                IxOp<_,_>.Apply (3u, FermionicOperator.An)
                IxOp<_,_>.Apply (4u, FermionicOperator.An)
                IxOp<_,_>.Apply (5u, FermionicOperator.An)
                IxOp<_,_>.Apply (6u, FermionicOperator.An)
            |]
        let chunked = chunkByIndex ixops
        Assert.Equal (ixops.Length, chunked.Length)
        Assert.Equal<IEnumerable<uint32>>
            (
                ixops   |> Seq.map (fun io -> io.Index) |> Seq.rev,
                chunked |> Seq.map (fun pi -> pi.IndexedOps.[0].Index)
            )

    [<Fact>]
    let ``ChunkByIndex returns chunks in index order (Regression 5)`` () =
        let ixops =
            [|
                IxOp<_,_>.Apply (0u, FermionicOperator.Cr)
                IxOp<_,_>.Apply (1u, FermionicOperator.Cr)
                IxOp<_,_>.Apply (2u, FermionicOperator.Cr)
                IxOp<_,_>.Apply (3u, FermionicOperator.Cr)
                IxOp<_,_>.Apply (4u, FermionicOperator.Cr)
                IxOp<_,_>.Apply (5u, FermionicOperator.Cr)
                IxOp<_,_>.Apply (6u, FermionicOperator.Cr)
            |]
        let chunked = chunkByIndex ixops
        Assert.Equal (ixops.Length, chunked.Length)
        Assert.Equal<IEnumerable<uint32>>
            (
                ixops   |> Seq.map (fun io -> io.Index),
                chunked |> Seq.map (fun pi -> pi.IndexedOps.[0].Index)
            )

    [<Theory>]
    [<InlineData("", "{}")>]
    [<InlineData("[]", "{}")>]
    [<InlineData("[(R,1)]", "{[(R,1)]}")>]
    [<InlineData("[(R,1)|(L,1)]", "{[(R,1)|(L,1)]}")>]
    [<InlineData("[(R,1)|(L,1)|(L,1)]", "{[(R,1)|(L,1)|(L,1)]}")>]
    [<InlineData("[(R,1)|(L,1)|(L,2)|(L,1)]", "{-[(R,1)|(L,1)|(L,1)];[(L,2)]}")>]
    [<InlineData("[(R,1)|(R,2)]", "{[(R,1)];[(R,2)]}")>]
    [<InlineData("[(R,1)|(R,2)|(L,1)]", "{-[(R,1)|(L,1)];[(R,2)]}")>]
    [<InlineData("[(R,2)|(R,1)|(L,1)]", "{[(R,1)|(L,1)];[(R,2)]}")>]
    [<InlineData("[(R,1)|(R,2)|(L,1)|(L,2)]", "{-[(R,1)|(L,1)];[(R,2)|(L,2)]}")>]
    [<InlineData("[(R,1)|(R,2)|(L,2)|(L,1)]", "{[(R,1)|(L,1)];[(R,2)|(L,2)]}")>]
    [<InlineData("[(R,2)|(L,2)|(R,1)|(L,1)]", "{[(R,1)|(L,1)];[(R,2)|(L,2)]}")>]
    let ``ChunkByIndex groups operators into indexed-based chunks`` (input, expected) =
        match PIxOpFromString FermionicOperator.FromString input with
        | Some pixop ->
            let actual =
                pixop.IndexedOps
                |> chunkByIndex
                |> (prettyPrintPIxOps >> shrinkString)
            Assert.Equal (expected, actual)
        | None -> Assert.True (false)

    [<Theory>]
    //[<InlineData("", "{}")>]
    //[<InlineData("[]", "{}")>]
    [<InlineData("[(R,1)|(I,1)]", "{[(R,1)]}")>]
    [<InlineData("[(I,1)|(L,1)]", "{[(L,1)]}")>]
    [<InlineData("[(R,1)|(R,1)]", "{}")>]
    [<InlineData("[(L,1)|(L,1)]", "{}")>]
    [<InlineData("[(R,1)|(L,1)]", "{[(R,1)|(L,1)]}")>]
    [<InlineData("[(L,1)|(R,1)]", "{[1];-[(R,1)|(L,1)]}")>]
    let ``SortChunk sorts single chunk product terms`` (input, expected) =
        match PIxOpFromString FermionicOperator.FromString input with
        | Some pixop ->
            let actual =
                pixop.IndexedOps
                |> chunkByIndex
                |> Array.map sortChunk
                |> (fun rg -> rg.[0])
                |> (prettyPrintSIxOp >> shrinkString)
            Assert.Equal (expected, actual)
        | None -> Assert.True (false)
