namespace Encodings

[<AutoOpen>]
module IndexedOperatorOrderingExtensions =
    let isOrdered (comparer : 'a -> 'a -> bool) (xs : 'a seq) =
        let compareWithPrev (isOrdered, (prev : 'a option)) (curr : 'a) =
            let currAndPrevAreOrdered =
                prev
                |> Option.map (fun p -> comparer p curr)
                |> Option.defaultValue true
            ((isOrdered && currAndPrevAreOrdered), Some curr)
        xs
        |> Seq.fold compareWithPrev (true, None)
        |> fst

    let indicesInOrder ascending (ops : Ix<_> seq) =
        let comparer = if ascending then (.>=.) else (.<=.)
        ops |> isOrdered comparer



//[<AutoOpen>]
//module FermionicOperator =
    //type FermionicOperatorProductTerm =
    //    | FermionicOperatorProductTerm of P<I<LadderOperatorUnit>>
    //    with
    //    member this.IsInNormalOrder =
    //        let comparer p c =
    //            match (p, c) with
    //            | Lower, Raise -> false
    //            | _, _         -> true
    //        let (FermionicOperator term) = this
    //        term.Units
    //        |> Seq.map (fun ciu -> ciu.Item.Op)
    //        |> isOrdered comparer

    //    member this.IsInIndexOrder =
    //        let comparer p c =
    //            match (p, c) with
    //            | Lower, Raise -> false
    //            | _, _         -> true
    //        let (FermionicOperator term) = this
    //        term.Units
    //        |> Seq.map (fun ciu -> ciu.Item.Op)
    //        |> isOrdered comparer

    //type FermionicOperatorSumOfProductsTerm =
    //    | FermionicOperatorSequence of
    //    member __.AllTermsNormalOrdered =
    //        isIndexOrdered
    //        |> Option.defaultValue (
    //            isNormalOrdered
    //            |> Option.defaultValue false)

    //    member __.AllTermsIndexOrdered  =
    //        isIndexOrdered
    //        |> Option.defaultValue false

//    type FermionicCreationOperatorIndexSort() =
//        class
//            inherit SwapTrackingSort<LadderOperatorUnit, Complex>
//                ((.<=.), LadderOperatorUnit.WithMaxIndex, Complex.SwapSignMultiple)

//            member this.SortCreationOperators rg =
//                rg |> Array.where (function | Raise _ -> true | _ -> false) |> this.Sort Complex.One
//        end

//    type FermionicAnnihilationOperatorIndexSort() =
//        class
//            inherit SwapTrackingSort<LadderOperatorUnit, Complex>
//                ((.>=.), LadderOperatorUnit.WithMinIndex, Complex.SwapSignMultiple)

//            member this.SortAnnihilationOperators rg =
//                rg |> Array.where (function | Lower _ -> true | _ -> false) |> this.Sort Complex.One
//        end

//    type FermionicOperator internal (operatorUnits : LadderOperatorUnit [], coefficient : Complex) =
//        class
//            inherit IndexedOperator<LadderOperatorUnit> (operatorUnits, coefficient)

//            let isOrdered (comparer : 'a -> 'a -> bool) (xs : 'a seq) =
//                let compareWithPrev (isOrdered, (prev : 'a option)) (curr : 'a) =
//                    let currAndPrevAreOrdered = prev |> Option.map (fun p -> comparer p curr) |> Option.defaultValue true
//                    ((isOrdered && currAndPrevAreOrdered), Some curr)
//                xs |> Seq.fold compareWithPrev (true, None) |> fst

//            let indicesInOrder ascending ops =
//                let comparer (prev : IIndexedOperatorUnit) (curr : IIndexedOperatorUnit) =
//                    if ascending then
//                        curr.Index >= prev.Index
//                    else
//                        curr.Index <= prev.Index
//                ops |> isOrdered comparer

//            let isInNormalOrder =
//                lazy
//                    let comparer p c =
//                        match (p, c) with
//                        | Lower _, Raise _ -> false
//                        | _, _             -> true
//                    operatorUnits |> isOrdered comparer

//            let indicesAreAscending  = indicesInOrder true
//            let indicesAreDescending = indicesInOrder false

//            let raisingIndicesAreAscending =
//                Seq.filter (function | Raise _ -> true | _ -> false) >> indicesAreAscending

//            let loweringIndicesAreDescending =
//                Seq.filter (function | Lower _ -> true | _ -> false) >> indicesAreDescending

//            let isInIndexOrder =
//                lazy
//                    isInNormalOrder.Value &&
//                    raisingIndicesAreAscending   operatorUnits &&
//                    loweringIndicesAreDescending operatorUnits

//            new (operatorUnits : LadderOperatorUnit []) =
//                FermionicOperator (operatorUnits, Complex.One)

//            static member FromUnits =
//                Array.map (LadderOperatorUnit.FromTuple) >> FermionicOperator

//            static member FromString =
//                TryCreateIndexedOperator LadderOperatorUnit.FromStringAndIndex

//            member this.IsInNormalOrder = isInNormalOrder.Value
//            member this.IsInIndexOrder  = isInIndexOrder.Value
//        end

//    and FermionicOperatorSequence internal (operators : FermionicOperator[], coefficient, ?isNormalOrdered, ?isIndexOrdered) =
//        class
//            inherit IndexedOperatorSequence<LadderOperatorUnit>(operators |> Array.map (fun u -> upcast u), coefficient)

//            member __.AllTermsNormalOrdered =
//                isIndexOrdered
//                |> Option.defaultValue (
//                    isNormalOrdered
//                    |> Option.defaultValue false)

//            member __.AllTermsIndexOrdered  =
//                isIndexOrdered
//                |> Option.defaultValue false
//        end

//    and NormalOrderedFermionicOperator private (operator : FermionicOperator) =
//        class
//            inherit FermionicOperator (operator.OperatorUnits, operator.Coefficient)

//            static member Construct (candidate : FermionicOperator) : FermionicOperatorSequence =
//                let ensureNormalOrder (c : FermionicOperator) : (Complex * FermionicOperator[]) =
//                    if c.IsInNormalOrder then
//                        (Complex.One, [|candidate|])
//                    else
//                        failwith "Not Yet Implemented"

//                let (coeff, ops) = ensureNormalOrder candidate
//                FermionicOperatorSequence (ops, coeff, isNormalOrdered = true)
//        end

//    type IndexOrderedFermionicOperator private (operator : FermionicOperator) =
//        class
//            inherit FermionicOperator (operator.OperatorUnits, operator.Coefficient)

//            static member Construct (candidate : FermionicOperator) =
//                let ensureIndexOrder (c : FermionicOperator) : (Complex * FermionicOperator[]) =
//                    if c.IsInIndexOrder then
//                        (Complex.One, [| candidate |])
//                    else if c.IsInNormalOrder then
//                        let (sortedCreationOps, createdPhase) =
//                            candidate.OperatorUnits
//                            |> (new FermionicCreationOperatorIndexSort()).SortCreationOperators
//                        let (sortedAnnihilationOps, annihilatedPhase) =
//                            candidate.OperatorUnits
//                            |> (new FermionicAnnihilationOperatorIndexSort()).SortAnnihilationOperators
//                        let ops   = Array.concat [|sortedCreationOps; sortedAnnihilationOps|]
//                        let phase = createdPhase * annihilatedPhase
//                        let sortedFermionicOperator = FermionicOperator(ops, phase)
//                        (Complex.One, [| sortedFermionicOperator |])
//                    else
//                        failwith "Not Yet Implemented"

//                let (coeff, ops) = ensureIndexOrder candidate
//                FermionicOperatorSequence (ops, coeff, isIndexOrdered = true)
//        end
