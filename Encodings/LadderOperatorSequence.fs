namespace Encodings

[<AutoOpen>]
module Algebra =
    type IAlgebra =
        interface
        end

[<AutoOpen>]
module LadderOperatorSequence =
    type LadderOperatorSequence<'algebra when 'algebra :> IAlgebra> internal (sumTerm : LadderOperatorSumExpression) =
        class
            member internal __.Unapply = sumTerm

            static member TryCreateFromString = LadderOperatorSumExpression.TryCreateFromString >> Option.map LadderOperatorSequence
            member this.Coeff                 = this.Unapply.Coeff
            member this.ProductTerms          = this.Unapply.ProductTerms
            member this.AllTermsNormalOrdered = this.Unapply.AllTermsNormalOrdered
            member this.AllTermsIndexOrdered  = this.Unapply.AllTermsIndexOrdered
            override this.ToString()          = this.Unapply.ToString()
        end

    and NormalOrderedLadderOperatorSequence<'algebra when 'algebra :> IAlgebra> internal (sumTerm : LadderOperatorSumExpression) =
        class
            inherit LadderOperatorSequence<'algebra> (sumTerm)

            static member Construct (candidate : LadderOperatorSumExpression) : NormalOrderedLadderOperatorSequence<'algebra> option =
                if candidate.AllTermsNormalOrdered then
                    NormalOrderedLadderOperatorSequence candidate
                    |> Some
                else
                    failwith "Not Yet Implemented"
        end

    and IndexOrderedLadderOperatorSequence<'algebra when 'algebra :> IAlgebra>  private (sumTerm : LadderOperatorSumExpression) =
        class
            inherit NormalOrderedLadderOperatorSequence<'algebra> (sumTerm)

            static member Construct (candidate : LadderOperatorSumExpression) : IndexOrderedLadderOperatorSequence<'algebra> option =
                if candidate.AllTermsIndexOrdered then
                    candidate
                    |> (IndexOrderedLadderOperatorSequence >> Some)
                else if candidate.AllTermsNormalOrdered then
                    [|
                        for productTerm in candidate.ProductTerms do
                            yield productTerm.ToIndexOrder.Unapply
                    |]
                    |> (fun terms -> (candidate.Coeff, terms))
                    |> (S<IndexedOperator<LadderOperatorUnit>>.Apply >> LadderOperatorSumExpression.Apply)
                    |> (IndexOrderedLadderOperatorSequence >> Some)
                else
                    candidate
                    |> NormalOrderedLadderOperatorSequence<'algebra>.Construct
                    |> Option.bind (fun c ->
#if DEBUG
                        System.Diagnostics.Debug.Assert c.Unapply.AllTermsNormalOrdered
#endif
                        IndexOrderedLadderOperatorSequence.Construct c.Unapply)
    end