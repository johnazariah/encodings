namespace Tests

module IndexedTerms_Extra =
    open Encodings
    open Xunit
    open FsCheck.Xunit

    [<Property>]
    let ``isOrdered detects ascending sequences`` (numbers : int[]) =
        let sortedNumbers = numbers |> Array.sort
        Assert.True(isOrdered (<=) sortedNumbers)

    [<Property>]
    let ``isOrdered detects descending sequences`` (numbers : int[]) =
        let sortedNumbers = numbers |> Array.sortDescending
        Assert.True(isOrdered (>=) sortedNumbers)

    [<Fact>]
    let ``isOrdered returns false for out-of-order pair`` () =
        let values = [ 0; 2; 1 ]
        Assert.False(isOrdered (<=) values)

    [<Fact>]
    let ``IndicesInOrder validates ascending and descending`` () =
        let ascending =
            [|
                IxOp<uint32, string>.Apply(0u, "a")
                IxOp<uint32, string>.Apply(2u, "b")
                IxOp<uint32, string>.Apply(5u, "c")
            |]

        let descending = ascending |> Array.rev

        Assert.True(IxOp<uint32, string>.IndicesInOrder Ascending ascending)
        Assert.False(IxOp<uint32, string>.IndicesInOrder Ascending descending)
        Assert.True(IxOp<uint32, string>.IndicesInOrder Descending descending)
        Assert.False(IxOp<uint32, string>.IndicesInOrder Descending ascending)

    [<Fact>]
    let ``TryCreateFromStringWith parses valid IxOp string`` () =
        let indexParser (value : string) =
            match System.UInt32.TryParse value with
            | true, parsed -> Some parsed
            | false, _ -> None

        let operatorFactory = function
            | "X" -> Some "X"
            | _ -> None

        let parsed = IxOp<uint32, string>.TryCreateFromStringWith indexParser operatorFactory "(X, 3)"
        Assert.True(parsed.IsSome)
        Assert.Equal(Some(IxOp<uint32, string>.Apply(3u, "X")), parsed)

    [<Theory>]
    [<InlineData("(X, nope)")>]
    [<InlineData("(Q, 2)")>]
    [<InlineData("garbage")>]
    let ``TryCreateFromStringWith rejects invalid input`` (input : string) =
        let indexParser (value : string) =
            match System.UInt32.TryParse value with
            | true, parsed -> Some parsed
            | false, _ -> None

        let operatorFactory = function
            | "X" -> Some "X"
            | _ -> None

        let parsed = IxOp<uint32, string>.TryCreateFromStringWith indexParser operatorFactory input
        Assert.True(parsed.IsNone)

    [<Fact>]
    let ``tryParseIxOpUint32 parses Pauli operator and index`` () =
        let parser = tryParseIxOpUint32 Pauli.Apply
        let parsed = parser "(X, 4)"
        Assert.Equal(Some(IxOp<uint32, Pauli>.Apply(4u, X)), parsed)

    [<Fact>]
    let ``PIxOp TryCreateFromString parses product terms`` () =
        let parsed = PIxOp<uint32, Pauli>.TryCreateFromString Pauli.Apply "[(X, 0) | (Z, 1)]"
        Assert.True(parsed.IsSome)
        Assert.Equal("[(X, 0) | (Z, 1)]", parsed.Value.ToString())

    [<Fact>]
    let ``SIxOp TryCreateFromString parses sum terms`` () =
        let parsed = SIxOp<uint32, Pauli>.TryCreateFromString Pauli.Apply "{[(X, 0)]; [(Z, 1)]}"
        Assert.True(parsed.IsSome)
        Assert.Contains("(X, 0)", parsed.Value.ToString())
        Assert.Contains("(Z, 1)", parsed.Value.ToString())
