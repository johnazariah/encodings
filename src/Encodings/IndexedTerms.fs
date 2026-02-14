namespace Encodings

/// <summary>
/// Indexed operator types for tracking qubit/mode assignments.
/// </summary>
/// <remarks>
/// Each IxOp wraps an operator unit with an integer
/// index identifying which qubit (or fermionic mode) it acts on.
/// This is essential for multi-qubit Pauli strings where the
/// position of each Pauli matters: X₀ Z₁ I₂ Y₃ = XZIY.
/// </remarks>
[<AutoOpen>]
module IndexedTerms =
    /// <summary>
    /// Checks whether a sequence is ordered according to a given comparator.
    /// </summary>
    /// <param name="comparer">A function that returns true if the first argument should come before or equal to the second.</param>
    /// <param name="xs">The sequence to check for ordering.</param>
    /// <returns>True if all adjacent pairs satisfy the comparator; false otherwise.</returns>
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

    /// <summary>
    /// Specifies the ordering direction for indexed operators.
    /// </summary>
    /// <remarks>
    /// Used to validate or enforce a particular ordering of operator indices
    /// in product terms. Ascending order (0, 1, 2, ...) is common for
    /// Pauli strings, while descending may be used in other contexts.
    /// </remarks>
    type IndexOrder =
        /// <summary>Indices should be in ascending order (0, 1, 2, ...).</summary>
        | Ascending
        /// <summary>Indices should be in descending order (..., 2, 1, 0).</summary>
        | Descending

    /// <summary>
    /// An indexed operator that pairs a site/qubit index with an operator.
    /// </summary>
    /// <typeparam name="idx">The type of the index (e.g., int, uint32). Must support comparison.</typeparam>
    /// <typeparam name="op">The type of the operator (e.g., Pauli, LadderOp). Must support equality.</typeparam>
    /// <remarks>
    /// <para>
    /// IxOp is the fundamental building block for multi-site quantum operators.
    /// For example, the Pauli string X₀Z₂Y₃ is represented as a sequence of
    /// IxOp values: [(X, 0), (Z, 2), (Y, 3)].
    /// </para>
    /// <para>
    /// The index typically represents a qubit number (for Pauli operators)
    /// or a fermionic mode number (for ladder operators).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// let xOnQubit0 = IxOp.Apply(0, Pauli.X)
    /// let zOnQubit2 = IxOp.Apply(2, Pauli.Z)
    /// </code>
    /// </example>
    and IxOp<'idx, 'op when 'idx : comparison and 'op : equality> =
        { /// <summary>The site or qubit index where this operator acts.</summary>
          Index : 'idx
          /// <summary>The operator acting at this index.</summary>
          Op : 'op }
    with
        /// <summary>
        /// Creates an IxOp from an index and operator.
        /// </summary>
        /// <param name="index">The site/qubit index.</param>
        /// <param name="op">The operator to apply at that index.</param>
        /// <returns>A new IxOp pairing the index with the operator.</returns>
        static member Apply (index, (op : 'op)) = { Index = index; Op = op }

        /// <summary>Greater-than-or-equal comparison by index.</summary>
        static member (.>=.) (l : IxOp<'idx, 'op>, r : IxOp<'idx, 'op>) = l.Index >= r.Index

        /// <summary>Less-than-or-equal comparison by index.</summary>
        static member (.<=.) (l : IxOp<'idx, 'op>, r : IxOp<'idx, 'op>) = l.Index <= r.Index

        /// <summary>
        /// Checks whether a sequence of IxOp values has indices in the specified order.
        /// </summary>
        /// <param name="indexOrder">The expected ordering direction.</param>
        /// <param name="ops">The sequence of indexed operators to check.</param>
        /// <returns>True if all indices satisfy the ordering constraint.</returns>
        static member IndicesInOrder (indexOrder : IndexOrder) (ops : IxOp<'idx, 'op> seq) : bool =
            let comparer =
                match indexOrder with
                | Ascending  -> (.<=.)
                | Descending -> (.>=.)
            ops |> isOrdered comparer

        /// <summary>
        /// Attempts to parse an IxOp from a string representation.
        /// </summary>
        /// <param name="indexParser">A function that parses the index from a string.</param>
        /// <param name="unitFactory">A function that parses the operator from a string.</param>
        /// <param name="s">The string to parse, expected in format "(op, index)".</param>
        /// <returns>Some IxOp if parsing succeeds; None otherwise.</returns>
        /// <example>
        /// <code>
        /// // Parse "(X, 0)" with uint32 index and Pauli operator
        /// IxOp.TryCreateFromStringWith parseUint32 Pauli.TryParse "(X, 0)"
        /// </code>
        /// </example>
        static member TryCreateFromStringWith
            (indexParser : string -> 'idx option)
            (unitFactory : string -> 'op option)
            (s : System.String) =
            try
                s.Trim().TrimStart('(').TrimEnd(')').Split(',')
                |> Array.map (fun s -> s.Trim ())
                |> (fun rg ->
                    match unitFactory rg.[0], indexParser rg.[1] with
                    | Some op, Some idx -> Some (IxOp<_,_>.Apply(idx, op))
                    | _ -> None)
            with
            | _ -> None

        /// <summary>
        /// Returns a string representation in the format "(Op, Index)".
        /// </summary>
        override this.ToString() =
            sprintf "(%O, %O)" this.Op this.Index

    /// <summary>
    /// Parses an IxOp with a uint32 index from a string like "(op, 123)".
    /// </summary>
    /// <param name="unitFactory">A function that parses the operator from a string.</param>
    /// <returns>A parser function that returns Some IxOp on success, None on failure.</returns>
    /// <remarks>
    /// This is a convenience function for the common case where indices are
    /// unsigned 32-bit integers (qubit numbers).
    /// </remarks>
    let tryParseIxOpUint32 (unitFactory : string -> 'op option) : string -> IxOp<uint32, 'op> option =
        let parseUint32 (s : string) =
            match System.UInt32.TryParse s with
            | true, v -> Some v
            | false, _ -> None
        IxOp<_,_>.TryCreateFromStringWith parseUint32 unitFactory

    /// <summary>
    /// A product of indexed operators, representing a tensor product of operators at specific sites.
    /// </summary>
    /// <typeparam name="idx">The type of the index (e.g., int, uint32).</typeparam>
    /// <typeparam name="op">The type of the operator (e.g., Pauli, LadderOp).</typeparam>
    /// <remarks>
    /// <para>
    /// PIxOp wraps a product term (P) of indexed operators. For example,
    /// the Pauli string X₀Z₂Y₃ would be a PIxOp containing three IxOp values.
    /// </para>
    /// <para>
    /// The product includes a coefficient (from the underlying P type) and
    /// a sequence of indexed operator units.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // A Pauli product X₀ ⊗ Z₁ with coefficient 0.5
    /// let pauliString = PIxOp.TryCreateFromString Pauli.TryParse "0.5 (X, 0) (Z, 1)"
    /// </code>
    /// </example>
    type PIxOp<'idx, 'op when 'idx : comparison and 'op : equality> =
        | ProductTerm of P<IxOp<'idx, 'op>>
    with
        /// <summary>Extracts the underlying product term.</summary>
        member this.Unapply = match this with ProductTerm term -> term

        /// <summary>Lazily retrieves the sequence of operator units in this product.</summary>
        member this.Units = lazy this.Unapply.Units

        /// <summary>
        /// Attempts to parse a PIxOp from a string representation.
        /// </summary>
        /// <param name="unitFactory">A function that parses operators from strings.</param>
        /// <returns>A parser function for product terms.</returns>
        static member TryCreateFromString (unitFactory : string -> 'op option) =
            P<IxOp<uint32, 'op>>.TryCreateFromString (tryParseIxOpUint32 unitFactory)
            >> Option.map ProductTerm

        /// <summary>
        /// Checks whether the operator indices are in the specified order.
        /// </summary>
        /// <param name="indexOrder">The expected ordering direction.</param>
        /// <returns>True if indices satisfy the ordering constraint.</returns>
        member this.IsInIndexOrder indexOrder =
            this.Units.Value
            |> Seq.map (fun u -> u.Item)
            |> IxOp<_,_>.IndicesInOrder indexOrder

        /// <summary>Checks whether indices are in ascending order (0, 1, 2, ...).</summary>
        member this.IsInIndexOrderAscending  = this.IsInIndexOrder Ascending

        /// <summary>Checks whether indices are in descending order (..., 2, 1, 0).</summary>
        member this.IsInIndexOrderDescending = this.IsInIndexOrder Descending

        /// <summary>Returns a string representation of the product term.</summary>
        override this.ToString() = this.Unapply.ToString()

    /// <summary>
    /// A sum of products of indexed operators, representing a linear combination.
    /// </summary>
    /// <typeparam name="idx">The type of the index (e.g., int, uint32).</typeparam>
    /// <typeparam name="op">The type of the operator (e.g., Pauli, LadderOp).</typeparam>
    /// <remarks>
    /// <para>
    /// SIxOp wraps a sum expression (S) of indexed operator products. This is
    /// the most general form of a quantum operator in this library, capable of
    /// representing Hamiltonians as sums of Pauli strings or fermionic terms.
    /// </para>
    /// <para>
    /// Example: H = 0.5 X₀Z₁ + 0.3 Y₀Y₁ - 0.2 Z₀
    /// </para>
    /// </remarks>
    and SIxOp<'idx, 'op when 'idx : comparison and 'op : equality> =
        | SumTerm of S<IxOp<'idx, 'op>>
    with
        /// <summary>Extracts the underlying sum expression.</summary>
        member this.Unapply = match this with SumTerm term -> term

        /// <summary>Lazily retrieves the sequence of product terms in this sum.</summary>
        member this.ProductTerms = this.Unapply.ProductTerms

        /// <summary>
        /// Attempts to parse an SIxOp from a string representation.
        /// </summary>
        /// <param name="unitFactory">A function that parses operators from strings.</param>
        /// <returns>A parser function for sum expressions.</returns>
        static member TryCreateFromString (unitFactory : string -> 'op option) =
            S<IxOp<uint32,'op>>.TryCreateFromString (tryParseIxOpUint32 unitFactory)
            >> Option.map SumTerm

        /// <summary>
        /// Lazily checks whether all product terms have indices in the specified order.
        /// </summary>
        /// <param name="indexOrder">The expected ordering direction.</param>
        /// <returns>A lazy boolean that is true if all terms satisfy the ordering.</returns>
        member this.AllTermsIndexOrdered indexOrder  =
            lazy
                this.ProductTerms.Value
                |> Seq.map ProductTerm
                |> Seq.fold (fun result curr -> result && curr.IsInIndexOrder indexOrder) true

        /// <summary>Returns a string representation of the sum expression.</summary>
        override this.ToString() = this.Unapply.ToString()
