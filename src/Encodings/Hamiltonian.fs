namespace Encodings

/// <summary>
/// Hamiltonian construction from one-body and two-body integrals.
/// </summary>
/// <remarks>
/// Assembles the second-quantized electronic Hamiltonian:
///
///   H = Σ_{pq} h_{pq} a†_p a_q  +  ½ Σ_{pqrs} ⟨pq|rs⟩ a†_p a†_q a_s a_r
///
/// and encodes it as a sum of Pauli strings using any provided encoding function.
/// The function handles one-body terms, two-body terms, coefficient combination,
/// and zero-term dropping.
/// </remarks>
[<AutoOpen>]
module Hamiltonian =
    open System.Numerics

    /// <summary>
    /// A function type that encodes a ladder operator into qubit Pauli strings.
    /// </summary>
    /// <remarks>
    /// Takes a ladder operator, the mode index, and the total number of qubits,
    /// returning the Pauli string representation. Different encodings (Jordan-Wigner,
    /// Bravyi-Kitaev, etc.) implement this signature with different Z-chain structures.
    /// </remarks>
    type EncoderFn = LadderOperatorUnit -> uint32 -> uint32 -> PauliRegisterSequence

    /// <summary>
    /// Discriminated union representing a term in the Hamiltonian.
    /// </summary>
    type HamiltonianTerm =
    /// <summary>A one-body (overlap) term h_{ij} a†_i a_j.</summary>
    | Overlap  of OverlapTerm
    /// <summary>A two-body (exchange) term ⟨ij|kl⟩ a†_i a†_j a_l a_k.</summary>
    | Exchange of ExchangeTerm

    /// <summary>
    /// A one-body overlap term with indices i and j, representing h_{ij} a†_i a_j.
    /// </summary>
    and OverlapTerm  = {i : uint32; j : uint32}
    with
        member private this.ToEncodedTerms (encode : EncoderFn) n coeff =
            let product = (encode Raise this.i n) * (encode Lower this.j n)
            product.DistributeCoefficient
            |> fun prs ->
                prs.SummandTerms
                |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))
                |> PauliRegisterSequence

        member private this.ToJordanWignerTerms n coeff =
            this.ToEncodedTerms jordanWignerTerms n coeff

        static member internal ComputeTermsWith (encode : EncoderFn) coefficientFactory n =
            [|
                for i in 0u .. n do
                    for j in 0u .. n do
                        let key = sprintf "%u%u" i j
                        match coefficientFactory key with
                        | Some hij ->
                            let term = {OverlapTerm.i = i; OverlapTerm.j = j}
                            yield term.ToEncodedTerms encode n hij
                        | _ -> ()
            |]
            |> PauliRegisterSequence

        static member internal ComputeTerms coefficientFactory n =
            OverlapTerm.ComputeTermsWith jordanWignerTerms coefficientFactory n

    /// <summary>
    /// A two-body exchange term with indices i, j, k, l, representing ⟨ij|kl⟩ a†_i a†_j a_l a_k.
    /// </summary>
    and ExchangeTerm = {i : uint32; j : uint32; k : uint32; l : uint32}
    with
        member private this.ToEncodedTerms (encode : EncoderFn) n coeff termCoefficient =
            let product =
                (encode Raise this.i n) * (encode Raise this.j n)
                * (encode Lower this.k n) * (encode Lower this.l n)
            product.DistributeCoefficient
            |> fun prs ->
                prs.SummandTerms
                |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))
                |> PauliRegisterSequence

        member private this.ToJordanWignerTerms n coeff termCoefficient =
            this.ToEncodedTerms jordanWignerTerms n coeff termCoefficient

        static member internal ComputeTermsWith (encode : EncoderFn) coefficientFactory n =
            let termCoefficient = Complex (0.5, 0.)
            [|
                for i in 0u .. n do
                    for j in 0u .. n do
                        for k in 0u .. n do
                            for l in 0u .. n do
                                let key = sprintf "%u%u%u%u" i j k l
                                match coefficientFactory key with
                                | Some hijkl ->
                                    let term = {
                                        ExchangeTerm.i = i
                                        ExchangeTerm.j = j
                                        ExchangeTerm.k = k
                                        ExchangeTerm.l = l
                                    }
                                    yield term.ToEncodedTerms encode n hijkl termCoefficient
                                | _ -> ()
            |]
            |> PauliRegisterSequence

        static member internal ComputeTerms coefficientFactory n =
            ExchangeTerm.ComputeTermsWith jordanWignerTerms coefficientFactory n


    /// <summary>
    /// Compute a qubit Hamiltonian from integral coefficients using Jordan-Wigner encoding.
    /// </summary>
    /// <param name="coefficientFactory">A function that returns Some(coefficient) for a given index key, or None if the term should be skipped.</param>
    /// <param name="n">The number of qubits/modes in the system.</param>
    /// <returns>A PauliRegisterSequence representing the encoded Hamiltonian.</returns>
    /// <remarks>
    /// Iterates over all one-body (i,j) and two-body (i,j,k,l) index combinations,
    /// retrieves coefficients from the factory function, and encodes non-zero terms
    /// using the Jordan-Wigner transformation.
    /// </remarks>
    let computeHamiltonian coefficientFactory n =
        [|
            yield OverlapTerm.ComputeTerms  coefficientFactory n
            yield ExchangeTerm.ComputeTerms coefficientFactory n
        |]
        |> PauliRegisterSequence

    /// <summary>
    /// Compute a qubit Hamiltonian from integral coefficients using any encoding.
    /// </summary>
    /// <param name="encode">The encoding function to transform ladder operators to Pauli strings.</param>
    /// <param name="coefficientFactory">A function that returns Some(coefficient) for a given index key, or None if the term should be skipped.</param>
    /// <param name="n">The number of qubits/modes in the system.</param>
    /// <returns>A PauliRegisterSequence representing the encoded Hamiltonian.</returns>
    /// <remarks>
    /// Generic version that accepts any fermion-to-qubit encoding function.
    /// Useful for comparing different encodings (Jordan-Wigner, Bravyi-Kitaev, etc.)
    /// on the same Hamiltonian.
    /// </remarks>
    let computeHamiltonianWith (encode : EncoderFn) coefficientFactory n =
        [|
            yield OverlapTerm.ComputeTermsWith  encode coefficientFactory n
            yield ExchangeTerm.ComputeTermsWith encode coefficientFactory n
        |]
        |> PauliRegisterSequence
