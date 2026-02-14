namespace Encodings

/// <summary>
/// Fermionic ladder operators with index tracking and ordering predicates.
/// </summary>
/// <remarks>
/// A LadderOperatorUnit is either Raise j (= a†ⱼ, creation) or Lower j (= aⱼ, annihilation),
/// where j is the spin-orbital index.
///
/// Products of ladder operators must respect normal ordering (all creation operators
/// to the left of all annihilation operators) and index ordering (within each group,
/// indices are sorted). The functions isInNormalOrder and isInIndexOrder test these conditions.
///
/// Normal ordering is required before encoding: the Jordan-Wigner and other transforms
/// assume the operator product is in canonical form.
/// </remarks>
[<AutoOpen>]
module IndexedLadderOperator =
    open System.Numerics

    /// <summary>
    /// Represents the type of a fermionic ladder operator.
    /// </summary>
    /// <remarks>
    /// <para>Ladder operators are fundamental building blocks in second quantization:</para>
    /// <list type="bullet">
    /// <item><description><c>Identity</c> - The identity operator (no operation)</description></item>
    /// <item><description><c>Raise</c> - Creation operator a†, creates a fermion in an orbital</description></item>
    /// <item><description><c>Lower</c> - Annihilation operator a, removes a fermion from an orbital</description></item>
    /// </list>
    /// <para>String representation: "I" for Identity, "u" for Raise (up), "d" for Lower (down).</para>
    /// </remarks>
    type LadderOperatorUnit =
        | Identity
        | Raise
        | Lower
    with
        static member Apply = function
            | "I" -> Some Identity
            | "u" -> Some Raise
            | "d" -> Some Lower
            | _ -> None

        member this.AsString =
            lazy
                match this with
                | Identity -> "I"
                | Raise    -> "u"
                | Lower    -> "d"

        override this.ToString() = this.AsString.Value

        static member TryCreateFromString =
            tryParseIxOpUint32 LadderOperatorUnit.Apply

        static member FromUnit : (bool * uint32 -> IxOp<uint32, LadderOperatorUnit>) = function
            | (true,  index) -> IxOp<_,_>.Apply(index, Raise)
            | (false, index) -> IxOp<_,_>.Apply(index, Lower)

        /// <summary>
        /// Creates an indexed ladder operator from a tuple of (operator, index).
        /// </summary>
        /// <param name="ladderOperatorUnit">The type of ladder operator (Raise or Lower).</param>
        /// <param name="index">The spin-orbital index for the operator.</param>
        /// <returns>An indexed operator combining the operator type and orbital index.</returns>
        static member FromTuple (ladderOperatorUnit, index) =
            IxOp<_,_>.Apply (index, ladderOperatorUnit)

    /// <summary>
    /// Checks whether a product term's operators are in normal order.
    /// </summary>
    /// <remarks>
    /// Normal order requires all creation (Raise) operators to appear before
    /// all annihilation (Lower) operators: a†ᵢ a†ⱼ ... aₖ aₗ
    /// This is the canonical form required by Jordan-Wigner and other encodings.
    /// </remarks>
    /// <param name="productTerm">The product term to check.</param>
    /// <returns>True if the term is in normal order; false otherwise.</returns>
    let isInNormalOrder (productTerm : P<IxOp<uint32, LadderOperatorUnit>>) =
        let comparer p c =
            match (p, c) with
            | Lower, Raise -> false
            | _, _         -> true
        productTerm.Units
        |> Seq.map (fun ciu -> ciu.Item.Op)
        |> isOrdered comparer

    /// <summary>
    /// Checks whether a product term's operators are in index order.
    /// </summary>
    /// <remarks>
    /// Index ordering requires:
    /// <list type="bullet">
    /// <item><description>Raise (creation) operators have ascending indices: a†ᵢ a†ⱼ where i &lt; j</description></item>
    /// <item><description>Lower (annihilation) operators have descending indices: aₖ aₗ where k &gt; l</description></item>
    /// </list>
    /// Combined with normal ordering, this gives the full canonical form.
    /// </remarks>
    /// <param name="productTerm">The product term to check.</param>
    /// <returns>True if indices are properly ordered; false otherwise.</returns>
    let isInIndexOrder (productTerm : P<IxOp<uint32, LadderOperatorUnit>>) =
        let operators =
            productTerm.Units
            |> Seq.map (fun ciu -> ciu.Item)
        let raisingOperatorsAreAscending =
            operators
            |> Seq.where (fun ico -> ico.Op = Raise)
            |> IxOp<_,_>.IndicesInOrder Ascending
        let loweringOperatorsAreDescending =
            operators
            |> Seq.where (fun ico -> ico.Op = Lower)
            |> IxOp<_,_>.IndicesInOrder Descending
        raisingOperatorsAreAscending && loweringOperatorsAreDescending

    /// <summary>
    /// Represents a product of indexed fermionic ladder operators with a coefficient.
    /// </summary>
    /// <remarks>
    /// <para>A product term represents an expression like c · a†ᵢ a†ⱼ aₖ aₗ where c is a complex coefficient.</para>
    /// <para>Product terms can be multiplied together and checked for normal/index ordering.</para>
    /// <para>Example string format: "(1+0i)[u0 u1 d3 d2]" for a†₀ a†₁ a₃ a₂</para>
    /// </remarks>
    type LadderOperatorProductTerm =
        | LadderProduct of PIxOp<uint32, LadderOperatorUnit>
    with
        static member internal Apply (units : C<IxOp<uint32, LadderOperatorUnit>>[]) =
            units
            |> P<IxOp<uint32, LadderOperatorUnit>>.Apply
            |> (PIxOp<uint32, LadderOperatorUnit>.ProductTerm >> LadderOperatorProductTerm.LadderProduct)

        member this.Unapply = match this with LadderProduct term -> term.Unapply
        member this.Coeff  = this.Unapply.Coeff
        member this.Units  = this.Unapply.Units
        member this.Reduce = this.Unapply.Reduce

        static member TryCreateFromString s =
            PIxOp<uint32, LadderOperatorUnit>.TryCreateFromString LadderOperatorUnit.Apply s
            |> Option.map LadderProduct

        static member FromUnits =
            Array.map LadderOperatorUnit.FromUnit
            >> P<IxOp<uint32, LadderOperatorUnit>>.Apply
            >> PIxOp<uint32, LadderOperatorUnit>.ProductTerm
            >> LadderOperatorProductTerm.LadderProduct

        static member FromTuples =
            Array.map LadderOperatorUnit.FromTuple
            >> P<IxOp<uint32, LadderOperatorUnit>>.Apply
            >> PIxOp<uint32, LadderOperatorUnit>.ProductTerm
            >> LadderOperatorProductTerm.LadderProduct

        static member (*) (LadderProduct l, LadderProduct r) =
            l.Unapply * r.Unapply
            |> (PIxOp<uint32, LadderOperatorUnit>.ProductTerm >> LadderOperatorProductTerm.LadderProduct)

        member this.IsInNormalOrder = isInNormalOrder this.Unapply

        member this.IsInIndexOrder = isInIndexOrder this.Unapply

        override this.ToString() =
            this.Unapply.ToString()

    /// <summary>
    /// Represents a sum of ladder operator product terms.
    /// </summary>
    /// <remarks>
    /// <para>A sum expression represents the sum of multiple product terms:</para>
    /// <para>c₁ · (product₁) + c₂ · (product₂) + ...</para>
    /// <para>Sum expressions support addition and multiplication, and can be checked
    /// to verify all constituent terms are in normal/index order.</para>
    /// <para>This is the primary type for representing fermionic Hamiltonians
    /// before encoding to qubit operators.</para>
    /// </remarks>
    type LadderOperatorSumExpression =
        | LadderSum of SIxOp<uint32, LadderOperatorUnit>
    with
        member this.Unapply = match this with LadderSum term -> term.Unapply

        static member internal Apply (expr : S<IxOp<uint32, LadderOperatorUnit>>) =
            expr
            |> SIxOp<uint32, LadderOperatorUnit>.SumTerm
            |> LadderOperatorSumExpression.LadderSum

        static member internal ApplyFromProductTerms (terms : LadderOperatorProductTerm[]) =
            terms
            |> Array.map (fun (pt : LadderOperatorProductTerm) -> pt.Unapply)
            |> S<IxOp<uint32, LadderOperatorUnit>>.Apply
            |> LadderOperatorSumExpression.Apply

        static member internal ApplyFromPTerms (terms : P<IxOp<uint32, LadderOperatorUnit>>[]) =
            terms
            |> S<IxOp<uint32, LadderOperatorUnit>>.Apply
            |> LadderOperatorSumExpression.Apply

        static member TryCreateFromString =
            SIxOp<uint32, LadderOperatorUnit>.TryCreateFromString LadderOperatorUnit.Apply
            >> Option.map LadderSum

        member this.Coeff = this.Unapply.Coeff
        member this.Reduce =
            lazy
                this.Unapply.Reduce.Value |> LadderOperatorSumExpression.Apply

        member this.ProductTerms =
            this.Unapply.ProductTerms.Value
            |> Array.map (PIxOp<uint32, LadderOperatorUnit>.ProductTerm >> LadderOperatorProductTerm.LadderProduct)

        member this.AllTermsNormalOrdered =
            this.ProductTerms
            |> Seq.fold (fun result curr -> result && curr.IsInNormalOrder) true

        member this.AllTermsIndexOrdered  =
            this.ProductTerms
            |> Seq.fold (fun result curr -> result && curr.IsInIndexOrder) true

        static member (*) (LadderSum l, LadderSum r) =
            l.Unapply * r.Unapply |> LadderOperatorSumExpression.Apply

        static member (+) (LadderSum l, LadderSum r) =
            l.Unapply + r.Unapply |> LadderOperatorSumExpression.Apply

        override this.ToString() =
            this.Unapply.ToString()
