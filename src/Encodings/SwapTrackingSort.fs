namespace Encodings

/// <summary>
/// Swap-tracking selection sort for computing fermionic phase factors.
/// </summary>
/// <remarks>
/// <para>
/// When reordering fermionic operators to normal order (all creation operators
/// left, annihilation right), each swap of adjacent operators introduces a
/// factor of −1. This module implements selection sort that counts swaps,
/// so the total fermionic sign (-1)^(number of swaps) can be computed.
/// </para>
/// <para>
/// The sort is O(n²) but n is always small (number of operators in a single
/// product term, typically 2-4).
/// </para>
/// </remarks>
[<AutoOpen>]
module SwapTrackingSort =
    /// <summary>
    /// A selection sort implementation that tracks position changes for computing phase factors.
    /// </summary>
    /// <typeparam name="a">The type of elements to sort. Must support equality.</typeparam>
    /// <typeparam name="coeff">The type of the phase/coefficient being tracked (e.g., int for swap count, or a phase type).</typeparam>
    /// <remarks>
    /// <para>
    /// This class implements selection sort with a twist: after each element is moved
    /// to its sorted position, a tracking function is called with the number of positions
    /// the element moved. This is crucial for fermionic operators where each adjacent
    /// swap introduces a factor of -1.
    /// </para>
    /// <para>
    /// For fermionic normal ordering, if an element at position i is moved to position 0,
    /// it effectively swapped past i elements, contributing a phase of (-1)^i.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a sort that counts total swaps
    /// let swapCounter = SwapTrackingSort((&lt;=), 0, fun pos count -&gt; count + pos)
    /// let (sorted, totalSwaps) = swapCounter.Sort 0 [| 3; 1; 2 |]
    /// // sorted = [| 1; 2; 3 |], totalSwaps = 2
    /// </code>
    /// </example>
    type SwapTrackingSort<'a, 'coeff when 'a : equality>
        (
            /// <summary>Comparison function: returns true if first arg should come before second.</summary>
            compareFunction : 'a -> 'a -> bool,
            /// <summary>The identity/zero element used as initial minimum in searches.</summary>
            zero : 'a,
            /// <summary>
            /// Function called after each element placement. Takes the position from which
            /// the element was moved and the current coefficient, returns the updated coefficient.
            /// </summary>
            trackingFunction : int -> 'coeff -> 'coeff
        ) =
        class
            /// <summary>
            /// Sorts the input array while tracking position changes via the tracking function.
            /// </summary>
            /// <param name="initialPhase">The initial coefficient/phase value before sorting.</param>
            /// <param name="inputArray">The array to sort (not modified; a new sorted array is returned).</param>
            /// <returns>
            /// A tuple of (sorted array, final coefficient) where the final coefficient
            /// reflects all position tracking accumulated during the sort.
            /// </returns>
            /// <remarks>
            /// The algorithm repeatedly finds the minimum element in the remaining unsorted
            /// portion and moves it to the sorted portion. Each time an element at position i
            /// is selected, the tracking function is called with i and the current phase,
            /// simulating i adjacent swaps.
            /// </remarks>
            member __.Sort initialPhase inputArray =
                let findMin rg =
                    rg
                    |> Array.fold (fun ((min, i_min), index) curr ->
                        if (compareFunction min curr) then
                            ((min, i_min), index + 1)
                        else
                            ((curr, index), index + 1))
                        ((zero, 0), 0)
                    |> fst

                let rec sort' (sorted, remaining, phase) =
                    if remaining = [||] then
                        (sorted, phase)
                    else
                        let (min, i_min) = findMin remaining
                        let phase' = trackingFunction i_min phase
                        let remaining' =
                            let pre  =
                                if i_min = 0 then
                                    [||]
                                else
                                    remaining.[.. (i_min - 1)]
                            let post =
                                if i_min = remaining.Length - 1 then
                                    [||]
                                else
                                    remaining.[(i_min + 1) ..]
                            Array.concat [| pre; post|]
                        let sorted' = Array.concat [| sorted; [|min|] |]
                        sort' (sorted', remaining', phase')

                sort' ([||], inputArray, initialPhase)

            /// <summary>
            /// Checks whether an array is already sorted according to the comparison function.
            /// </summary>
            /// <param name="candidate">The array to check.</param>
            /// <returns>True if the array is sorted (each element compares favorably with its successor); false otherwise.</returns>
            /// <remarks>
            /// An empty array or single-element array is considered sorted.
            /// This can be used to skip sorting when the input is already in order.
            /// </remarks>
            member __.IsSorted (candidate : 'a[]) =
                if candidate.Length <= 1 then
                    true
                else
                    candidate
                    |> Array.fold
                        (fun (result, prev) curr -> (result && compareFunction prev curr, curr))
                        (true, candidate.[0])
                    |> fst
        end



