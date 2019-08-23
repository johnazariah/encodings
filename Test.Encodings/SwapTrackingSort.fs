namespace Tests

open System.Collections

module SwapTrackingSort =
    open Encodings
    open Xunit
    open FsCheck.Xunit
    open System.Numerics

    let (gte, min) = ((>=), System.Int32.MinValue)
    let (lte, max) = ((<=), System.Int32.MaxValue)

    let isOrdered cf (rg : 'a[]) =
        if rg.Length <= 1 then
            true
        else
            rg
            |> Array.fold
                (fun (result, prev) curr ->
                    let f = (cf prev curr)
                    (result && cf prev curr, curr))
                (true, rg.[0])
            |> fst

    let isInAscendingOrder  = isOrdered lte
    let isInDescendingOrder = isOrdered gte

    type IntegerAscendingSort () =
        class
            inherit SwapTrackingSort<int, int>(lte, max, (fun times result -> result + times))
        end
    let ascendingSort = new IntegerAscendingSort()

    type IntegerDescendingSort () =
        class
            inherit SwapTrackingSort<int, int>(gte, min, (fun times result -> result + times))
        end
    let descendingSort = new IntegerDescendingSort()

    [<Property>]
    let ``SwapTrackingSort sorts int array ascending`` (input : int[]) =
        let (result, count) = ascendingSort.Sort 0 input
        Assert.True (isInAscendingOrder result)
        Assert.True(ascendingSort.IsSorted result)

    [<Property>]
    let ``SwapTrackingSort sorts int array descending`` (input : int[]) =
        let (result, count) = descendingSort.Sort 0 input
        Assert.True (isInDescendingOrder result)
        Assert.True(descendingSort.IsSorted result)

    type SortInput() =
        inherit TestcaseDatabase<int[]>(
            seq {
                yield [| [||]; [||] |]
                yield [| [|1|]; [|1|] |]
                yield [| [|1;2|]; [|1;2|] |]
                yield [| [|2;1|]; [|1;2|] |]
            })

    [<Theory; ClassData(typeof<SortInput>)>]
    let ``Can sort int array ascending`` (input : int[], expected : int[]) =
        let (actual, _) = ascendingSort.Sort 0 input
        let isSorted = isInAscendingOrder actual
        Assert.Equal<IEnumerable>(expected, actual)
        Assert.True(ascendingSort.IsSorted actual)
        Assert.True(isSorted)


    type TrackingInput() =
        inherit TestcaseDatabase<int[]>(
            seq {
                yield [| [||];      [|0|] |]
                yield [| [|1|];     [|0|] |]
                yield [| [|1;2|];   [|0|] |]
                yield [| [|2;1|];   [|1|] |]
                yield [| [|3;2;1|]; [|3|] |]
            })

    [<Theory; ClassData(typeof<TrackingInput>)>]
    let ``Can track swap counts``(input : int[], expected : int[]) =
        let (sorted, actual) = ascendingSort.Sort 0 input
        let isSorted = isInAscendingOrder sorted
        Assert.True(isSorted)
        Assert.Equal(expected.[0], actual)