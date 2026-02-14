namespace Tests

module FenwickTree =
    open Encodings
    open Xunit

    // ──────────────────────────────────────────────────
    //  Integer-sum Fenwick tree
    // ──────────────────────────────────────────────────

    let private sumTree values =
        ofArray (+) 0 values

    [<Fact>]
    let ``prefixQuery on sum tree returns prefix sums`` () =
        //                 index: 0  1  2  3  4  5  6  7
        let tree = sumTree [| 1; 3; 5; 7; 9; 11; 13; 15 |]
        Assert.Equal(1,  prefixQuery tree 0)
        Assert.Equal(4,  prefixQuery tree 1)
        Assert.Equal(9,  prefixQuery tree 2)
        Assert.Equal(16, prefixQuery tree 3)
        Assert.Equal(25, prefixQuery tree 4)
        Assert.Equal(36, prefixQuery tree 5)
        Assert.Equal(49, prefixQuery tree 6)
        Assert.Equal(64, prefixQuery tree 7)

    [<Fact>]
    let ``update on sum tree adjusts prefix sums`` () =
        let tree  = sumTree [| 1; 2; 3; 4 |]
        let tree' = update tree 1 10        // index 1:  2 → 12
        Assert.Equal(1,  prefixQuery tree' 0)
        Assert.Equal(13, prefixQuery tree' 1)    // 1 + 12
        Assert.Equal(16, prefixQuery tree' 2)    // 1 + 12 + 3
        Assert.Equal(20, prefixQuery tree' 3)    // 1 + 12 + 3 + 4

    [<Fact>]
    let ``empty tree has identity prefix`` () =
        let tree = empty (+) 0 4
        for i in 0 .. 3 do
            Assert.Equal(0, prefixQuery tree i)

    [<Fact>]
    let ``size returns element count`` () =
        let tree = sumTree [| 10; 20; 30 |]
        Assert.Equal(3, size tree)

    // ──────────────────────────────────────────────────
    //  XOR Fenwick tree
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``XOR tree computes prefix XOR`` () =
        let tree = ofArray (^^^) 0 [| 1; 2; 3; 4 |]
        Assert.Equal(1,         prefixQuery tree 0)   // 1
        Assert.Equal(1 ^^^ 2,   prefixQuery tree 1)   // 3
        Assert.Equal(1 ^^^ 2 ^^^ 3, prefixQuery tree 2) // 0
        Assert.Equal(1 ^^^ 2 ^^^ 3 ^^^ 4, prefixQuery tree 3) // 4

    // ──────────────────────────────────────────────────
    //  Set-union Fenwick tree (the BK use case)
    // ──────────────────────────────────────────────────

    let private setUnionTree values =
        ofArray Set.union Set.empty values

    [<Fact>]
    let ``set-union tree computes prefix unions`` () =
        let values = [| set [0]; set [1]; set [2]; set [3] |]
        let tree = setUnionTree values
        Assert.Equal<Set<int>>(set [0],       prefixQuery tree 0)
        Assert.Equal<Set<int>>(set [0;1],     prefixQuery tree 1)
        Assert.Equal<Set<int>>(set [0;1;2],   prefixQuery tree 2)
        Assert.Equal<Set<int>>(set [0;1;2;3], prefixQuery tree 3)

    // ──────────────────────────────────────────────────
    //  Bit-twiddling helpers
    // ──────────────────────────────────────────────────

    [<Theory>]
    [<InlineData(1, 1)>]
    [<InlineData(2, 2)>]
    [<InlineData(3, 1)>]
    [<InlineData(4, 4)>]
    [<InlineData(6, 2)>]
    [<InlineData(12, 4)>]
    let ``lsb computes lowest set bit`` (k : int, expected : int) =
        Assert.Equal<int>(expected, lsb k)

    [<Fact>]
    let ``ancestors of 1 in n=8 are {2,4,8}`` () =
        let result = ancestors 8 1 |> Seq.toList
        Assert.Equal<int list>([2; 4; 8], result)

    [<Fact>]
    let ``ancestors of 3 in n=8 are {4,8}`` () =
        let result = ancestors 8 3 |> Seq.toList
        Assert.Equal<int list>([4; 8], result)

    [<Fact>]
    let ``ancestors of 8 in n=8 is empty`` () =
        let result = ancestors 8 8 |> Seq.toList
        Assert.Equal<int list>([], result)

    [<Fact>]
    let ``descendants of 4 are {3,2}`` () =
        let result = descendants 4 |> Seq.toList
        Assert.Equal<int list>([3; 2], result)

    [<Fact>]
    let ``descendants of 8 are {7,6,4}`` () =
        let result = descendants 8 |> Seq.toList
        Assert.Equal<int list>([7; 6; 4], result)

    [<Fact>]
    let ``descendants of 1 is empty`` () =
        let result = descendants 1 |> Seq.toList
        Assert.Equal<int list>([], result)

    [<Fact>]
    let ``prefixIndices of 7 are {7,6,4}`` () =
        let result = prefixIndices 7 |> Seq.toList
        Assert.Equal<int list>([7; 6; 4], result)

    [<Fact>]
    let ``prefixIndices of 5 are {5,4}`` () =
        let result = prefixIndices 5 |> Seq.toList
        Assert.Equal<int list>([5; 4], result)

    // ──────────────────────────────────────────────────
    //  BK index-set extraction via Fenwick structure
    // ──────────────────────────────────────────────────

    [<Theory>]
    [<InlineData(0, 8)>]
    [<InlineData(1, 8)>]
    [<InlineData(3, 8)>]
    [<InlineData(4, 8)>]
    [<InlineData(7, 8)>]
    let ``updateSet matches expected`` (j : int, n : int) =
        let expected =
            match j with
            | 0 -> set [1; 3; 7]
            | 1 -> set [3; 7]
            | 3 -> set [7]
            | 4 -> set [5; 7]
            | 7 -> Set.empty
            | _ -> failwith "unexpected"
        Assert.Equal<Set<int>>(expected, updateSet j n)

    [<Theory>]
    [<InlineData(0)>]
    [<InlineData(1)>]
    [<InlineData(3)>]
    [<InlineData(7)>]
    let ``paritySet matches expected`` (j : int) =
        let expected =
            match j with
            | 0 -> Set.empty
            | 1 -> set [0]
            | 3 -> set [1; 2]
            | 7 -> set [3; 5; 6]
            | _ -> failwith "unexpected"
        Assert.Equal<Set<int>>(expected, paritySet j)

    [<Theory>]
    [<InlineData(0)>]
    [<InlineData(1)>]
    [<InlineData(3)>]
    [<InlineData(7)>]
    let ``occupationSet matches expected`` (j : int) =
        let expected =
            match j with
            | 0 -> set [0]
            | 1 -> set [0; 1]
            | 3 -> set [1; 2; 3]
            | 7 -> set [3; 5; 6; 7]
            | _ -> failwith "unexpected"
        Assert.Equal<Set<int>>(expected, occupationSet j)

    [<Fact>]
    let ``remainderSet j=3 is empty`` () =
        Assert.Equal<Set<int>>(Set.empty, remainderSet 3)

    [<Fact>]
    let ``remainderSet j=5 is {3}`` () =
        Assert.Equal<Set<int>>(set [3], remainderSet 5)

    [<Fact>]
    let ``symmetricDifference works correctly`` () =
        Assert.Equal<Set<int>>(set [1; 4], symmetricDifference (set [1; 2; 3]) (set [2; 3; 4]))
        Assert.Equal<Set<int>>(Set.empty,  symmetricDifference (set [1; 2])    (set [1; 2]))
        Assert.Equal<Set<int>>(set [1; 2], symmetricDifference Set.empty       (set [1; 2]))

    // ──────────────────────────────────────────────────
    //  pointQuery for XOR tree recovers original values
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``pointQuery on XOR tree recovers elements`` () =
        let values = [| 5; 3; 7; 1 |]
        let tree = ofArray (^^^) 0 values
        for i in 0 .. 3 do
            Assert.Equal(values.[i], pointQuery tree i)

    // ──────────────────────────────────────────────────
    //  build from function
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``build with function produces same result as ofArray`` () =
        let values = [| 2; 4; 6; 8; 10 |]
        let t1 = ofArray (+) 0 values
        let t2 = build (+) 0 5 (fun i -> values.[i])
        for i in 0 .. 4 do
            Assert.Equal(prefixQuery t1 i, prefixQuery t2 i)
