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
                        let key = sprintf "%u,%u" i j
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
                                let key = sprintf "%u,%u,%u,%u" i j k l
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
    /// <param name="coefficientFactory">A function that returns Some(coefficient) for a given comma-separated index key (e.g., "0,1" for one-body, "0,1,2,3" for two-body), or None if the term should be skipped.</param>
    /// <param name="n">The number of qubits/modes in the system.</param>
    /// <returns>A PauliRegisterSequence representing the encoded Hamiltonian.</returns>
    /// <remarks>
    /// Iterates over all one-body (i,j) and two-body (i,j,k,l) index combinations,
    /// retrieves coefficients from the factory function, and encodes non-zero terms
    /// using the Jordan-Wigner transformation. Keys are formatted as comma-separated
    /// indices: "i,j" for one-body and "i,j,k,l" for two-body terms.
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
    /// <param name="coefficientFactory">A function that returns Some(coefficient) for a given comma-separated index key (e.g., "0,1" for one-body, "0,1,2,3" for two-body), or None if the term should be skipped.</param>
    /// <param name="n">The number of qubits/modes in the system.</param>
    /// <returns>A PauliRegisterSequence representing the encoded Hamiltonian.</returns>
    /// <remarks>
    /// Generic version that accepts any fermion-to-qubit encoding function.
    /// Useful for comparing different encodings (Jordan-Wigner, Bravyi-Kitaev, etc.)
    /// on the same Hamiltonian. Keys are formatted as comma-separated indices:
    /// "i,j" for one-body and "i,j,k,l" for two-body terms.
    /// </remarks>
    let computeHamiltonianWith (encode : EncoderFn) coefficientFactory n =
        [|
            yield OverlapTerm.ComputeTermsWith  encode coefficientFactory n
            yield ExchangeTerm.ComputeTermsWith encode coefficientFactory n
        |]
        |> PauliRegisterSequence


    // ── Parallel Hamiltonian construction ─────────────────────────────

    /// <summary>
    /// Parallel version of <see cref="computeHamiltonianWith"/>.
    /// Distributes encoding work across available CPU cores using Array.Parallel.
    /// </summary>
    /// <param name="encode">The encoding function to transform ladder operators to Pauli strings.</param>
    /// <param name="coefficientFactory">A function that returns Some(coefficient) for a given comma-separated index key.</param>
    /// <param name="n">The number of qubits/modes in the system.</param>
    /// <returns>A PauliRegisterSequence representing the encoded Hamiltonian.</returns>
    /// <remarks>
    /// The n² one-body and n⁴ two-body index loops are parallelised.
    /// Coefficient lookups remain sequential (cheap), while the expensive
    /// encode-and-multiply steps run across all cores. Produces results
    /// identical to the sequential <see cref="computeHamiltonianWith"/>.
    /// </remarks>
    let computeHamiltonianWithParallel (encode : EncoderFn) coefficientFactory n =
        let encodeOneBody (i, j, coeff) =
            let product = (encode Raise i n) * (encode Lower j n)
            product.DistributeCoefficient.SummandTerms
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))
            |> PauliRegisterSequence

        let encodeTwoBody (i, j, k, l, coeff) =
            let product =
                (encode Raise i n) * (encode Raise j n)
                * (encode Lower k n) * (encode Lower l n)
            product.DistributeCoefficient.SummandTerms
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))
            |> PauliRegisterSequence

        let oneBodyTerms =
            [| for i in 0u .. n do
                   for j in 0u .. n do
                       let key = sprintf "%u,%u" i j
                       match coefficientFactory key with
                       | Some hij -> yield (i, j, hij)
                       | _ -> () |]
            |> Array.Parallel.map encodeOneBody

        let twoBodyTerms =
            [| for i in 0u .. n do
                   for j in 0u .. n do
                       for k in 0u .. n do
                           for l in 0u .. n do
                               let key = sprintf "%u,%u,%u,%u" i j k l
                               match coefficientFactory key with
                               | Some hijkl -> yield (i, j, k, l, hijkl)
                               | _ -> () |]
            |> Array.Parallel.map encodeTwoBody

        Array.append oneBodyTerms twoBodyTerms
        |> PauliRegisterSequence

    /// <summary>
    /// Parallel version of <see cref="computeHamiltonian"/> (Jordan-Wigner).
    /// </summary>
    let computeHamiltonianParallel coefficientFactory n =
        computeHamiltonianWithParallel jordanWignerTerms coefficientFactory n


    // ── Pauli Skeleton: separate structure from coefficients ─────────

    /// <summary>
    /// A pre-computed Pauli term with its signature cached.
    /// </summary>
    /// <remarks>
    /// Caching the signature string and operator array avoids recomputing
    /// them during <see cref="applyCoefficients"/>, which is called once
    /// per geometry in a PES scan.
    /// </remarks>
    type SkeletonTerm =
        { /// <summary>Pauli signature string, e.g. "XYZII".</summary>
          Signature       : string
          /// <summary>Pauli operator array (shared, not copied).</summary>
          Operators       : Pauli[]
          /// <summary>Structural phase from the encoding algebra (no integral coefficient).</summary>
          StructuralCoeff : Complex }

    /// <summary>
    /// A single entry in a precomputed Hamiltonian skeleton.
    /// </summary>
    /// <remarks>
    /// Contains the index key and the pre-computed Pauli terms that result
    /// from encoding the corresponding operator product.  All phases are
    /// <em>structural</em> — they come from the encoding algebra, not
    /// from integral coefficients.
    /// </remarks>
    type SkeletonEntry =
        { /// <summary>Index key: "i,j" for one-body or "i,j,k,l" for two-body.</summary>
          Key   : string
          /// <summary>Pre-computed Pauli terms with cached signatures.</summary>
          Terms : SkeletonTerm[] }

    /// <summary>
    /// A precomputed encoding skeleton separating Pauli structure from
    /// integral coefficients.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Computing a qubit Hamiltonian has two independent parts:
    /// </para>
    /// <list type="number">
    ///   <item><description>
    ///     <b>Structure</b> — which Pauli strings appear for each operator product
    ///     (depends only on the encoding and system size).
    ///   </description></item>
    ///   <item><description>
    ///     <b>Coefficients</b> — the integral values that scale each term
    ///     (depends on the molecular geometry / parameters).
    ///   </description></item>
    /// </list>
    /// <para>
    /// The skeleton captures part (1) once, then <see cref="applyCoefficients"/>
    /// evaluates part (2) cheaply for any coefficient set.  This is ideal for
    /// potential energy surface scans where the basis set (and encoding
    /// structure) is fixed but integral values vary with geometry.
    /// </para>
    /// </remarks>
    type HamiltonianSkeleton =
        { /// <summary>One-body entries: a†_i a_j encoded as Pauli strings.</summary>
          OneBody   : SkeletonEntry[]
          /// <summary>Two-body entries: a†_i a†_j a_l a_k encoded as Pauli strings.</summary>
          TwoBody   : SkeletonEntry[]
          /// <summary>Number of qubits in the system.</summary>
          NumQubits : uint32 }

    /// <summary>
    /// Precompute the Pauli skeleton for a given encoding and system size.
    /// </summary>
    /// <param name="encode">The encoding function.</param>
    /// <param name="n">The number of qubits/modes.</param>
    /// <returns>
    /// A <see cref="HamiltonianSkeleton"/> containing precomputed Pauli
    /// structures for all one-body and two-body operator products.
    /// </returns>
    /// <remarks>
    /// Uses <c>Array.Parallel.map</c> on the n⁴ two-body index space.
    /// Signatures and operator arrays are cached in <see cref="SkeletonTerm"/>
    /// records so that <see cref="applyCoefficients"/> can accumulate
    /// coefficients without any Pauli algebra or string computation.
    /// </remarks>
    let computeHamiltonianSkeleton (encode : EncoderFn) (n : uint32) : HamiltonianSkeleton =
        let toSkeletonTerms (regs : PauliRegister[]) =
            regs |> Array.map (fun r ->
                { Signature       = r.Signature
                  Operators       = r.Operators
                  StructuralCoeff = r.Coefficient })

        let oneBody =
            [| for i in 0u .. n - 1u do
                   for j in 0u .. n - 1u -> (i, j) |]
            |> Array.Parallel.map (fun (i, j) ->
                let key = sprintf "%u,%u" i j
                let product = (encode Raise i n) * (encode Lower j n)
                let terms = product.DistributeCoefficient.SummandTerms |> toSkeletonTerms
                { Key = key; Terms = terms })
            |> Array.filter (fun e -> e.Terms.Length > 0)

        let twoBody =
            [| for i in 0u .. n - 1u do
                   for j in 0u .. n - 1u do
                       for k in 0u .. n - 1u do
                           for l in 0u .. n - 1u -> (i, j, k, l) |]
            |> Array.Parallel.map (fun (i, j, k, l) ->
                let key = sprintf "%u,%u,%u,%u" i j k l
                let product =
                    (encode Raise i n) * (encode Raise j n)
                    * (encode Lower k n) * (encode Lower l n)
                let terms = product.DistributeCoefficient.SummandTerms |> toSkeletonTerms
                { Key = key; Terms = terms })
            |> Array.filter (fun e -> e.Terms.Length > 0)

        { OneBody = oneBody; TwoBody = twoBody; NumQubits = n }

    /// <summary>
    /// Precompute a sparse Pauli skeleton using a coefficient factory to discover active keys.
    /// </summary>
    /// <param name="encode">The encoding function.</param>
    /// <param name="coefficientFactory">A function that returns Some for keys to include. Only the presence/absence matters; coefficient values are ignored.</param>
    /// <param name="n">The number of qubits/modes.</param>
    /// <returns>
    /// A <see cref="HamiltonianSkeleton"/> containing precomputed Pauli
    /// structures only for index combinations where the factory returns Some.
    /// </returns>
    /// <remarks>
    /// <para>
    /// For molecules, typically only 5–10% of possible index combinations have
    /// non-zero integrals.  This variant precomputes only those entries,
    /// giving a proportional speedup over the full
    /// <see cref="computeHamiltonianSkeleton"/>.
    /// </para>
    /// <para>
    /// Use when all geometries in a scan share the same sparsity pattern
    /// (same basis set → same non-zero integral indices).
    /// </para>
    /// </remarks>
    let computeHamiltonianSkeletonFor (encode : EncoderFn) (coefficientFactory : string -> Complex option) (n : uint32) : HamiltonianSkeleton =
        let toSkeletonTerms (regs : PauliRegister[]) =
            regs |> Array.map (fun r ->
                { Signature       = r.Signature
                  Operators       = r.Operators
                  StructuralCoeff = r.Coefficient })

        let oneBodyKeys =
            [| for i in 0u .. n - 1u do
                   for j in 0u .. n - 1u do
                       let key = sprintf "%u,%u" i j
                       match coefficientFactory key with
                       | Some _ -> yield (i, j, key)
                       | None -> () |]

        let twoBodyKeys =
            [| for i in 0u .. n - 1u do
                   for j in 0u .. n - 1u do
                       for k in 0u .. n - 1u do
                           for l in 0u .. n - 1u do
                               let key = sprintf "%u,%u,%u,%u" i j k l
                               match coefficientFactory key with
                               | Some _ -> yield (i, j, k, l, key)
                               | None -> () |]

        let oneBody =
            oneBodyKeys
            |> Array.Parallel.map (fun (i, j, key) ->
                let product = (encode Raise i n) * (encode Lower j n)
                let terms = product.DistributeCoefficient.SummandTerms |> toSkeletonTerms
                { Key = key; Terms = terms })
            |> Array.filter (fun e -> e.Terms.Length > 0)

        let twoBody =
            twoBodyKeys
            |> Array.Parallel.map (fun (i, j, k, l, key) ->
                let product =
                    (encode Raise i n) * (encode Raise j n)
                    * (encode Lower k n) * (encode Lower l n)
                let terms = product.DistributeCoefficient.SummandTerms |> toSkeletonTerms
                { Key = key; Terms = terms })
            |> Array.filter (fun e -> e.Terms.Length > 0)

        { OneBody = oneBody; TwoBody = twoBody; NumQubits = n }

    /// <summary>
    /// Apply integral coefficients to a precomputed skeleton.
    /// </summary>
    /// <param name="skeleton">The precomputed skeleton from <see cref="computeHamiltonianSkeleton"/>.</param>
    /// <param name="coefficientFactory">A function returning Some(coefficient) for a given comma-separated index key, or None to skip.</param>
    /// <returns>A PauliRegisterSequence representing the encoded Hamiltonian.</returns>
    /// <remarks>
    /// <para>
    /// Accumulates coefficients directly into a dictionary keyed by
    /// pre-computed Pauli signatures.  No Pauli algebra, no intermediate
    /// <c>PauliRegister</c> construction, and no re-computation of
    /// signature strings.  Final <c>PauliRegister</c> objects are created
    /// only for the combined result (~tens of terms, not thousands).
    /// </para>
    /// <para>
    /// Typical runtime is under 10 ms for systems up to ~20 qubits,
    /// making PES scans over hundreds of geometries essentially free
    /// after the one-time skeleton build.
    /// </para>
    /// </remarks>
    let applyCoefficients (skeleton : HamiltonianSkeleton) (coefficientFactory : string -> Complex option) : PauliRegisterSequence =
        // Accumulate coefficients by Pauli signature — no PauliRegister construction
        let coeffDict = System.Collections.Generic.Dictionary<string, Complex>()
        let opsDict   = System.Collections.Generic.Dictionary<string, Pauli[]>()

        let processEntries (entries : SkeletonEntry[]) =
            for entry in entries do
                match coefficientFactory entry.Key with
                | Some coeff ->
                    for term in entry.Terms do
                        let scaledCoeff = term.StructuralCoeff * coeff
                        match coeffDict.TryGetValue term.Signature with
                        | true, existing ->
                            let newCoeff = existing + scaledCoeff
                            if newCoeff.IsZero then
                                ignore <| coeffDict.Remove term.Signature
                            else
                                coeffDict.[term.Signature] <- newCoeff
                        | false, _ ->
                            coeffDict.[term.Signature] <- scaledCoeff
                            opsDict.[term.Signature] <- term.Operators
                | None -> ()

        processEntries skeleton.OneBody
        processEntries skeleton.TwoBody

        // Construct PauliRegisters only for the final combined terms
        coeffDict
        |> Seq.map (fun kvp -> PauliRegister(opsDict.[kvp.Key], kvp.Value))
        |> Seq.toArray
        |> PauliRegisterSequence
