namespace Encodings
[<AutoOpen>]
module SwapTrackingSort =
    type SwapTrackingSort<'a, 'coeff when 'a : equality>
        (
            compareFunction : 'a -> 'a -> bool,
            zero : 'a,
            trackingFunction : int -> 'coeff -> 'coeff
        ) =
        class
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



