namespace Encodings

[<AutoOpen>]
module FermionicOperator =
    open System.Numerics

    type FermionicRaiseOperatorIndexSort() =
        class
            inherit SwapTrackingSort<Ix<LadderOperatorUnit>, Complex>
                (uncurry Ix<_>.(.<=.), Ix<_>.WithMaxIndex, Complex.SwapSignMultiple)

            member this.SortRaiseOperators rg =
                let isRaise (io : Ix<LadderOperatorUnit>) = io.Op = Raise
                rg |> Array.where isRaise |> this.Sort Complex.One
        end

    type FermionicLowerOperatorIndexSort() =
        class
            inherit SwapTrackingSort<Ix<LadderOperatorUnit>, Complex>
                (uncurry Ix<_>.(.>=.), Ix<_>.WithMinIndex, Complex.SwapSignMultiple)

            member this.SortLowerOperators rg =
                let isLower (io : Ix<LadderOperatorUnit>) = io.Op = Lower
                rg |> Array.where isLower |> this.Sort Complex.One
        end

    type FermionicOperatorUnit =
    | Op of IndexedOperator<LadderOperatorUnit>
    with
        member this.Unapply =
            let (Op indexedOperator) = this
            indexedOperator
        static member TryCreateFromString =
            IndexedOperator<LadderOperatorUnit>.TryCreateFromString LadderOperatorUnit.Apply

    type FermionicOperatorProductTerm =
    | ProductTerm of ProductOfIndexedOperators<LadderOperatorUnit>
        member internal this.Unapply =
            let (ProductTerm productTerm) = this
            productTerm.Unapply

        static member internal Apply =
            ProductOfIndexedOperators.ProductTerm
            >> FermionicOperatorProductTerm.ProductTerm

        static member TryCreateFromString =
            ProductOfIndexedOperators<LadderOperatorUnit>.TryCreateFromString LadderOperatorUnit.Apply

        member this.IsInNormalOrder =
            let comparer p c =
                match (p, c) with
                | Lower, Raise -> false
                | _, _         -> true

            this.Unapply.Units
            |> Seq.map (fun ciu -> ciu.Item.Unapply.Op.Item)
            |> isOrdered comparer

        member this.IsInIndexOrder =
            let operators =
                this.Unapply.Units
                |> Seq.map (fun ciu -> ciu.Item.Unapply)

            let raisingOperatorsAreAscending =
                operators
                |> Seq.where (fun ico -> ico.Op.Item = Raise)
                |> Ix<_>.IndicesInOrder true

            let loweringOperatorsAreDescending =
                operators
                |> Seq.where (fun ico -> ico.Op.Item = Raise)
                |> Ix<_>.IndicesInOrder false

            raisingOperatorsAreAscending && loweringOperatorsAreDescending

    type FermionicOperatorSumExpression =
    | SumTerm of SumOfProductsOfIndexedOperators<LadderOperatorUnit>
        member this.Unapply =
            let (SumTerm sumTerm) = this
            sumTerm.Unapply

        static member TryCreateFromString =
            SumOfProductsOfIndexedOperators<LadderOperatorUnit>.TryCreateFromString LadderOperatorUnit.Apply

        member this.ProductTerms =
            this.Unapply.Terms.Values
            |> Seq.map FermionicOperatorProductTerm.Apply

        member this.AllTermsNormalOrdered =
            this.ProductTerms
            |> Seq.fold (fun result curr -> result && curr.IsInNormalOrder) true

        member this.AllTermsIndexOrdered  =
            this.ProductTerms
            |> Seq.fold (fun result curr -> result && curr.IsInIndexOrder) true

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
