namespace Encodings

/// <summary>
/// Multi-qubit Pauli strings with phase tracking and symbolic multiplication.
/// </summary>
/// <remarks>
/// <para>
/// This module provides two core types for representing quantum operators in the Pauli basis:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="PauliRegister"/>: A tensor product of single-qubit Pauli operators
///       with a global phase coefficient: c · P₀ ⊗ P₁ ⊗ ... ⊗ Pₙ₋₁
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="PauliRegisterSequence"/>: A sum of Pauli registers with complex
///       coefficients, representing a general qubit Hamiltonian: H = Σₐ cₐ σₐ
///     </description>
///   </item>
/// </list>
/// <para>
/// Multiplication of two Pauli registers is exact and O(n): each qubit position
/// is multiplied independently using the Pauli multiplication table, with phases
/// accumulated into the global coefficient.
/// </para>
/// </remarks>
[<AutoOpen>]
module PauliRegister =
    open System
    open System.Numerics
    open System.Collections.Generic

    /// <summary>
    /// Represents a tensor product of single-qubit Pauli operators with a complex coefficient.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A PauliRegister of size n represents an operator on n qubits:
    /// </para>
    /// <code>
    ///   coefficient · (P₀ ⊗ P₁ ⊗ ... ⊗ Pₙ₋₁)
    /// </code>
    /// <para>
    /// where each Pᵢ ∈ {I, X, Y, Z} is a single-qubit Pauli operator.
    /// </para>
    /// <para>
    /// Examples:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>"XII" with coefficient 1.0 = X₀ ⊗ I₁ ⊗ I₂</description></item>
    ///   <item><description>"ZZ" with coefficient -0.5 = -0.5 · (Z₀ ⊗ Z₁)</description></item>
    /// </list>
    /// </remarks>
    type PauliRegister internal (operators : Pauli[], phase) =
        class
            let bindAtIndex f = function
            | n when n < 0 -> None
            | n when n >= operators.Length -> None
            | n -> n |> f

            let mapAtIndex f = bindAtIndex (f >> Some)

            /// <summary>
            /// Creates a new PauliRegister with all identity operators.
            /// </summary>
            /// <param name="n">The number of qubits (register size).</param>
            /// <param name="coefficient">Optional complex coefficient (default: 1.0).</param>
            new (n : uint32, ?coefficient) =
                let operators = Array.create<Pauli> (int n) Pauli.Identity
                new PauliRegister (operators, coefficient |> Option.defaultValue Complex.One)

            /// <summary>
            /// Creates a PauliRegister from a string of Pauli characters.
            /// </summary>
            /// <param name="ops">A string like "XYZII" where each character is I, X, Y, or Z.</param>
            /// <param name="coefficient">The complex coefficient for this term.</param>
            /// <example>
            /// <code>
            /// let reg = PauliRegister("XZI", Complex.One)  // X₀ ⊗ Z₁ ⊗ I₂
            /// </code>
            /// </example>
            new (ops : string, coefficient) =
                let rg = ops |> Seq.choose (Pauli.FromChar) |> Seq.toArray
                new PauliRegister (rg, coefficient)

            /// <summary>
            /// Creates a PauliRegister from a list of Pauli operators.
            /// </summary>
            /// <param name="ops">List of Pauli operators [P₀; P₁; ...; Pₙ₋₁].</param>
            /// <param name="coefficient">The complex coefficient for this term.</param>
            new (ops : Pauli list, coefficient) =
                new PauliRegister (ops |> List.toArray, coefficient)

            /// <summary>
            /// Creates a PauliRegister from a sequence of Pauli operators.
            /// </summary>
            /// <param name="ops">Sequence of Pauli operators.</param>
            /// <param name="coefficient">The complex coefficient for this term.</param>
            new (ops : Pauli seq, coefficient) =
                new PauliRegister (ops |> Seq.toArray, coefficient)

            /// <summary>Gets the internal array of Pauli operators.</summary>
            member internal __.Operators = operators

            /// <summary>Gets the number of qubits this register acts on.</summary>
            member internal __.Size = operators.Length

            /// <summary>
            /// Creates a copy of this register with a new coefficient.
            /// </summary>
            /// <param name="p">The new complex coefficient.</param>
            /// <returns>A new PauliRegister with the same operators but different coefficient.</returns>
            member __.ResetPhase (p : Complex) = PauliRegister(operators |> Array.copy, p)

            /// <summary>Gets the complex coefficient of this Pauli term.</summary>
            member __.Coefficient      = phase

            /// <summary>
            /// Gets a formatted string prefix representing the coefficient phase.
            /// </summary>
            /// <remarks>Used for display purposes, e.g., "+", "-", "+i", "-i".</remarks>
            member __.PhasePrefix      = phase.ToPhasePrefix

            /// <summary>
            /// Gets a formatted conjunction string for combining terms in a sum.
            /// </summary>
            /// <remarks>Used when building sum expressions like "X + iY - Z".</remarks>
            member __.PhaseConjunction = phase.ToPhaseConjunction

            /// <summary>
            /// Gets the Pauli operator at the given qubit index.
            /// </summary>
            /// <param name="i">The zero-based qubit index.</param>
            /// <returns>Some Pauli if index is valid; None otherwise.</returns>
            member __.Item
                with get i =
                    mapAtIndex (fun idx -> operators.[idx]) i

            /// <summary>
            /// Returns a new PauliRegister with the operator at index i replaced.
            /// </summary>
            /// <param name="i">The zero-based qubit index to modify.</param>
            /// <param name="op">The new Pauli operator to place at index i.</param>
            /// <returns>A new PauliRegister with the updated operator.</returns>
            /// <remarks>
            /// This operation is immutable - it returns a new register without
            /// modifying the original.
            /// </remarks>
            member __.WithOperatorAt (i : int) (op : Pauli) =
                let ops' = operators |> Array.copy
                if i >= 0 && i < ops'.Length then
                    ops'.[i] <- op
                PauliRegister(ops', phase)

            /// <summary>
            /// Gets the signature string of this register (operators without coefficient).
            /// </summary>
            /// <returns>A string like "XYZII" representing the tensor product structure.</returns>
            /// <remarks>
            /// The signature uniquely identifies the Pauli structure and is used as a
            /// dictionary key when combining like terms in a PauliRegisterSequence.
            /// </remarks>
            member __.Signature =
                operators
                |> Array.map (sprintf "%A")
                |> (fun rgstr -> System.String.Join("", rgstr))

            member private this.AsString =
                let phasePrefix = this.Coefficient.ToPhasePrefix
                sprintf "%s%s" (phasePrefix) (this.Signature)

            /// <summary>
            /// Returns the full string representation including coefficient and operators.
            /// </summary>
            /// <returns>A string like "+iXYZ" or "-0.5ZZI".</returns>
            override this.ToString() =
                this.AsString

            /// <summary>
            /// Multiplies two PauliRegisters component-wise.
            /// </summary>
            /// <param name="l">The left PauliRegister.</param>
            /// <param name="r">The right PauliRegister.</param>
            /// <returns>
            /// A new PauliRegister representing the product, with coefficients and
            /// phases properly combined.
            /// </returns>
            /// <remarks>
            /// <para>
            /// Multiplication is performed qubit-by-qubit in O(n) time:
            /// </para>
            /// <code>
            ///   (c₁ · P₀⊗P₁⊗...⊗Pₙ) * (c₂ · Q₀⊗Q₁⊗...⊗Qₙ)
            ///   = (c₁·c₂·φ) · (P₀Q₀)⊗(P₁Q₁)⊗...⊗(PₙQₙ)
            /// </code>
            /// <para>
            /// where φ is the accumulated phase from all the individual Pauli multiplications.
            /// If registers have different sizes, missing qubits are treated as identity.
            /// </para>
            /// </remarks>
            static member (*) (l : PauliRegister, r : PauliRegister) =
                let buildOperatorListAndPhase (ops, globalPhase) (op, phase : Phase) =
                    (ops @ [op], phase.FoldIntoGlobalPhase globalPhase)

                let seed = ([], l.Coefficient * r.Coefficient)

                let combinePauli i =
                    match (l.[i], r.[i]) with
                    | None, None -> (I, P1)
                    | Some x, None
                    | None, Some x -> (x, P1)
                    | Some x, Some y -> x * y

                let n = Math.Max (l.Size, r.Size)

                [|
                    for i in 0..(n-1) do yield (combinePauli i)
                |]
                |> Array.fold buildOperatorListAndPhase seed
                |> PauliRegister
        end

    /// <summary>
    /// Represents a sum of PauliRegisters with complex coefficients.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A PauliRegisterSequence represents a linear combination of Pauli strings,
    /// which is the standard form for qubit Hamiltonians:
    /// </para>
    /// <code>
    ///   H = Σₐ cₐ σₐ = c₁·P₁ + c₂·P₂ + ... + cₘ·Pₘ
    /// </code>
    /// <para>
    /// Terms with identical Pauli signatures are automatically combined by summing
    /// their coefficients. Terms that cancel (coefficient becomes zero) are removed.
    /// </para>
    /// <para>
    /// Example: A 2-qubit Heisenberg coupling might be represented as:
    /// </para>
    /// <code>
    ///   H = J·(XX + YY + ZZ)
    /// </code>
    /// </remarks>
    and PauliRegisterSequence private (bag : Dictionary<string, PauliRegister>, coefficient : Complex) =
        class
            /// <summary>
            /// Adds a PauliRegister to a dictionary, combining with existing terms if present.
            /// </summary>
            /// <param name="d">The dictionary to modify.</param>
            /// <param name="r">The register to add.</param>
            /// <returns>The modified dictionary.</returns>
            /// <remarks>
            /// If a term with the same signature exists, coefficients are summed.
            /// If the resulting coefficient is zero, the term is removed entirely.
            /// </remarks>
            static member private AddToDictionary (d : Dictionary<string, PauliRegister>) (r : PauliRegister) =
                let key = r.Signature
                match (d.TryGetValue key) with
                | (true, existingValue) ->
                    let newPhase = existingValue.Coefficient + r.Coefficient
                    if (newPhase = Complex.Zero) then
                        ignore <| d.Remove key
                    else
                        d.[key] <- existingValue.ResetPhase newPhase
                | (false, _) ->
                    d.[key] <- r
                d

            /// <summary>
            /// Creates an empty PauliRegisterSequence with unit coefficient.
            /// </summary>
            new () = PauliRegisterSequence (new Dictionary<string, PauliRegister>(), Complex.One)

            /// <summary>
            /// Creates a PauliRegisterSequence from an array of PauliRegisters.
            /// </summary>
            /// <param name="registers">Array of Pauli terms to sum.</param>
            /// <remarks>
            /// Terms with identical signatures are automatically combined.
            /// </remarks>
            new (registers : PauliRegister[]) =
                let d =
                    registers
                    |> Array.fold PauliRegisterSequence.AddToDictionary (new Dictionary<string, PauliRegister>())
                new PauliRegisterSequence(d, Complex.One)

            /// <summary>
            /// Creates a PauliRegisterSequence by combining multiple sequences.
            /// </summary>
            /// <param name="registerSets">Array of PauliRegisterSequences to merge.</param>
            /// <remarks>
            /// All terms from all input sequences are combined, with like terms summed.
            /// Each input sequence's coefficient is distributed to its terms before merging.
            /// </remarks>
            new (registerSets : PauliRegisterSequence[]) =
                let addRegisterSetToDictionary result_d (rs : PauliRegisterSequence) =
                    rs.SummandTerms |> Array.fold PauliRegisterSequence.AddToDictionary result_d

                let d =
                    registerSets
                    |> Array.map (fun rs -> rs.DistributeCoefficient)
                    |> Array.fold addRegisterSetToDictionary (new Dictionary<string, PauliRegister>())
                PauliRegisterSequence (d, Complex.One)

            /// <summary>
            /// Returns a new sequence with the global coefficient distributed to all terms.
            /// </summary>
            /// <returns>
            /// A PauliRegisterSequence where each term's coefficient is multiplied by
            /// the sequence's global coefficient, and the global coefficient is reset to 1.
            /// </returns>
            /// <remarks>
            /// Transforms: c · (c₁σ₁ + c₂σ₂) → (c·c₁)σ₁ + (c·c₂)σ₂
            /// </remarks>
            member this.DistributeCoefficient =
                this.SummandTerms
                |> Array.map (fun r -> r.ResetPhase (r.Coefficient * this.Coefficient))
                |> PauliRegisterSequence

            /// <summary>
            /// Lazily computed string representation of the sum.
            /// </summary>
            member val AsString =
                let buildString result (term : PauliRegister) =
                    let termStr = term.Signature

                    if String.IsNullOrWhiteSpace result then
                        let phasePrefix = term.PhasePrefix
                        sprintf "%s%s" phasePrefix termStr
                    else
                        let conjoiningPhase = term.PhaseConjunction
                        sprintf "%s%s%s" result conjoiningPhase termStr
                lazy
                    bag
                    |> Seq.sortBy (fun kvp -> kvp.Key)
                    |> Seq.map (fun kvp -> kvp.Value)
                    |> Seq.fold buildString ""

            /// <summary>
            /// Returns the string representation of this Pauli sum.
            /// </summary>
            /// <returns>A formatted string like "XX + iYY - ZZ".</returns>
            override this.ToString() = this.AsString.Value

            /// <summary>
            /// Gets all the individual Pauli terms in this sequence.
            /// </summary>
            /// <returns>An array of PauliRegisters representing each term in the sum.</returns>
            member __.SummandTerms = bag.Values |> Seq.toArray

            /// <summary>Gets the global coefficient of this sequence.</summary>
            member __.Coefficient = coefficient

            /// <summary>
            /// Looks up a term by its Pauli signature.
            /// </summary>
            /// <param name="key">The signature string (e.g., "XYZ").</param>
            /// <returns>A tuple (found, register) where found indicates if the key exists.</returns>
            member __.Item
                with get key =
                    bag.TryGetValue key

            /// <summary>
            /// Multiplies two PauliRegisterSequences, distributing over all terms.
            /// </summary>
            /// <param name="l">The left sequence.</param>
            /// <param name="r">The right sequence.</param>
            /// <returns>
            /// A new PauliRegisterSequence containing all pairwise products of terms.
            /// </returns>
            /// <remarks>
            /// <para>
            /// Computes the full product by distributing multiplication:
            /// </para>
            /// <code>
            ///   (Σᵢ cᵢσᵢ) * (Σⱼ dⱼτⱼ) = Σᵢⱼ (cᵢdⱼ)(σᵢτⱼ)
            /// </code>
            /// <para>
            /// If both sequences have m and n terms respectively, the result may have
            /// up to m×n terms (fewer if some products combine or cancel).
            /// </para>
            /// </remarks>
            static member (*) (l : PauliRegisterSequence, r : PauliRegisterSequence) =
                let (l_normal, r_normal) = (l.DistributeCoefficient, r.DistributeCoefficient)
                [|
                    for lt in l_normal.SummandTerms do
                        for rt in r_normal.SummandTerms do
                            yield (lt * rt)
                |]
                |> PauliRegisterSequence
        end
