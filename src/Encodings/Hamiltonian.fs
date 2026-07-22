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
///
/// <para>
/// <b>Factory contract.</b> The coefficient factory returns the RAW physicist
/// two-electron integral ⟨pq|rs⟩ for key "p,q,r,s" (and h_pq for "p,q"); the
/// library applies the ½ and the a_s a_r annihilator order internally. Do NOT
/// pre-fold the ½ or pre-swap the last two indices. From chemist integrals,
/// ⟨pq|rs⟩ = (pr|qs).
/// </para>
/// <para>
/// <b>Migration example.</b> For a raw physicist integral ⟨pq|rs⟩:
/// new contract — key "p,q,r,s" → value ⟨pq|rs⟩ (correct as-is);
/// the previous contract instead required key "p,q,s,r" → value 0.5·⟨pq|rs⟩.
/// Feeding raw ⟨pq|rs⟩ at "p,q,r,s" to the OLD builder was the source of the
/// doubled/mis-ordered coefficients (IIII = −3.5608, four-body 2× too large).
/// </para>
/// </remarks>
module Hamiltonian =
    open System.Numerics
    open Encodings.JordanWigner

    /// <summary>Mode indices 0 .. n-1, or an empty sequence when n = 0.</summary>
    /// <remarks>Guards against uint32 underflow of <c>n - 1u</c> when n = 0.</remarks>
    let private modeRange (n : uint32) : seq<uint32> =
        if n = 0u then Seq.empty else seq { 0u .. n - 1u }

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
    /// Structural Pauli terms for the two-body operator ½·a†_p a†_q a_s a_r.
    /// </summary>
    /// <remarks>
    /// The library assembles the standard electronic Hamiltonian two-body term
    /// under the unrestricted physicist sum ½·Σ_pqrs ⟨pq|rs⟩ a†_p a†_q a_s a_r.
    /// The internal ½ and the <c>a_s a_r</c> annihilator order are applied here so
    /// that every builder (sequential, parallel, cached, and both skeletons) and
    /// <see cref="applyCoefficients"/> agree. The factory supplies the raw integral
    /// ⟨pq|rs⟩ for key "p,q,r,s"; the caller multiplies these structural terms by it.
    /// </remarks>
    let private twoBodyStructuralTerms (encode : EncoderFn) (p : uint32) (q : uint32) (r : uint32) (s : uint32) (n : uint32) : PauliRegister[] =
        let product =
            (encode Raise p n) * (encode Raise q n)
            * (encode Lower s n) * (encode Lower r n)
        product.DistributeCoefficient.SummandTerms
        |> Array.map (fun t -> t.ResetPhase (t.Coefficient * Complex(0.5, 0.0)))

    /// <summary>Drop assembled terms whose coefficient is numerically zero (|c| ≤ 1e-12).</summary>
    /// <remarks>
    /// Fermionic cancellations can leave float-noise residues (~1e-18) that would
    /// otherwise inflate term counts and Pauli weights (e.g. H₂ carrying 8 spurious
    /// zero terms). Applied at Hamiltonian-assembly boundaries only, not to the core
    /// Pauli algebra.
    /// </remarks>
    let private dropNumericalZeros (h : PauliRegisterSequence) : PauliRegisterSequence =
        h.DistributeCoefficient.SummandTerms
        |> Array.filter (fun t -> t.Coefficient.Magnitude > 1e-12)
        |> PauliRegisterSequence

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
                for i in modeRange n do
                    for j in modeRange n do
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
    /// A two-body exchange term with indices i, j, k, l representing the operator
    /// <c>a†_i a†_j a_l a_k</c> scaled by ½ — the standard two-body term for the
    /// raw physicist integral ⟨ij|kl⟩ under the unrestricted sum.
    /// </summary>
    /// <remarks>
    /// The factory value for key <c>"i,j,k,l"</c> is the RAW physicist integral
    /// ⟨ij|kl⟩ (unrestricted sum, no antisymmetrisation). The library applies the
    /// ½ prefactor and the <c>a_l a_k</c> annihilator order internally
    /// (see <see cref="twoBodyStructuralTerms"/>); the caller must NOT pre-fold the ½.
    /// </remarks>
    and ExchangeTerm = {i : uint32; j : uint32; k : uint32; l : uint32}
    with
        member private this.ToEncodedTerms (encode : EncoderFn) n coeff =
            twoBodyStructuralTerms encode this.i this.j this.k this.l n
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))
            |> PauliRegisterSequence

        member private this.ToJordanWignerTerms n coeff =
            this.ToEncodedTerms jordanWignerTerms n coeff

        static member internal ComputeTermsWith (encode : EncoderFn) coefficientFactory n =
            [|
                for i in modeRange n do
                    for j in modeRange n do
                        for k in modeRange n do
                            for l in modeRange n do
                                let key = sprintf "%u,%u,%u,%u" i j k l
                                match coefficientFactory key with
                                | Some hijkl ->
                                    let term = {
                                        ExchangeTerm.i = i
                                        ExchangeTerm.j = j
                                        ExchangeTerm.k = k
                                        ExchangeTerm.l = l
                                    }
                                    yield term.ToEncodedTerms encode n hijkl
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
    /// <para>
    /// <b>Coefficient contract.</b> The factory returns the raw physical integral;
    /// this function applies the standard prefactors and operator order:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>"i,j" → h_ij, the coefficient of <c>a†_i a_j</c>.</description></item>
    ///   <item><description>"i,j,k,l" → the raw physicist two-electron integral
    ///   ⟨ij|kl⟩ under the unrestricted sum. The library builds
    ///   <c>½·⟨ij|kl⟩·a†_i a†_j a_l a_k</c> — i.e. it applies the ½ of
    ///   ½·Σ ⟨pq|rs⟩ a†_p a†_q a_s a_r and the <c>a_l a_k</c> annihilator order.
    ///   Do NOT pre-fold the ½ or antisymmetrise (that is the ¼ convention).</description></item>
    /// </list>
    /// <para>
    /// The nuclear/constant term is not added (callers may add E_nuc·I separately).
    /// Use <see cref="T:Encodings.Fcidump"/> to build a conforming factory from an
    /// FCIDUMP: it returns the raw ⟨pq|rs⟩ = (pr|qs) with spin(p)=spin(r), spin(q)=spin(s).
    /// </para>
    /// </remarks>
    let computeHamiltonian coefficientFactory n =
        [|
            yield OverlapTerm.ComputeTerms  coefficientFactory n
            yield ExchangeTerm.ComputeTerms coefficientFactory n
        |]
        |> PauliRegisterSequence
        |> dropNumericalZeros

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
    /// <para>
    /// <b>Coefficient contract (same as <see cref="computeHamiltonian"/>).</b> The
    /// factory returns raw physical integrals: "i,j" → coefficient of <c>a†_i a_j</c>;
    /// "i,j,k,l" → the raw physicist ⟨ij|kl⟩ under the unrestricted sum. The library
    /// applies the ½ and builds <c>½·⟨ij|kl⟩·a†_i a†_j a_l a_k</c> — do NOT pre-fold
    /// the ½. Build a conforming factory with <see cref="T:Encodings.Fcidump"/>.
    /// </para>
    /// </remarks>
    let computeHamiltonianWith (encode : EncoderFn) coefficientFactory n =
        [|
            yield OverlapTerm.ComputeTermsWith  encode coefficientFactory n
            yield ExchangeTerm.ComputeTermsWith encode coefficientFactory n
        |]
        |> PauliRegisterSequence
        |> dropNumericalZeros


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
            twoBodyStructuralTerms encode i j k l n
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))
            |> PauliRegisterSequence

        let oneBodyTerms =
            [| for i in modeRange n do
                   for j in modeRange n do
                       let key = sprintf "%u,%u" i j
                       match coefficientFactory key with
                       | Some hij -> yield (i, j, hij)
                       | _ -> () |]
            |> Array.Parallel.map encodeOneBody

        let twoBodyTerms =
            [| for i in modeRange n do
                   for j in modeRange n do
                       for k in modeRange n do
                           for l in modeRange n do
                               let key = sprintf "%u,%u,%u,%u" i j k l
                               match coefficientFactory key with
                               | Some hijkl -> yield (i, j, k, l, hijkl)
                               | _ -> () |]
            |> Array.Parallel.map encodeTwoBody

        Array.append oneBodyTerms twoBodyTerms
        |> PauliRegisterSequence
        |> dropNumericalZeros

    /// <summary>
    /// Parallel version of <see cref="computeHamiltonian"/> (Jordan-Wigner).
    /// </summary>
    let computeHamiltonianParallel coefficientFactory n =
        computeHamiltonianWithParallel jordanWignerTerms coefficientFactory n


    // ── Cached Hamiltonian construction ──────────────────────────────

    /// <summary>
    /// Optimised Hamiltonian construction with operator caching.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pre-computes and caches all 2n encoded ladder operators (n raises + n lowers)
    /// before assembling the Hamiltonian. This avoids redundant encoding computations
    /// when the same operator appears in many terms — each a†_i or a_j is computed
    /// once and reused across all one-body and two-body terms that reference it.
    /// </para>
    /// <para>
    /// For a system with n spin-orbitals and N_nz non-zero two-body integrals,
    /// this reduces encoding calls from 4·N_nz to 2n, with the multiplication
    /// cost remaining O(N_nz). Typical speedup is 5–20× for molecular systems
    /// where N_nz ≪ n⁴.
    /// </para>
    /// </remarks>
    /// <param name="encode">The encoding function.</param>
    /// <param name="coefficientFactory">Coefficient lookup.</param>
    /// <param name="n">Number of spin-orbitals (qubits).</param>
    let computeHamiltonianCached (encode : EncoderFn) coefficientFactory (n : uint32) =
        // Pre-cache all encoded raise and lower operators
        let raiseOps = Array.init (int n) (fun i -> encode Raise (uint32 i) n)
        let lowerOps = Array.init (int n) (fun i -> encode Lower (uint32 i) n)

        let encodeOneBody (i : int, j : int, coeff) =
            let product = raiseOps.[i] * lowerOps.[j]
            product.DistributeCoefficient.SummandTerms
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))
            |> PauliRegisterSequence

        let encodeTwoBody (i : int, j : int, k : int, l : int, coeff) =
            // ½·a†_i a†_j a_l a_k (raw physicist ⟨ij|kl⟩; see twoBodyStructuralTerms).
            let product = raiseOps.[i] * raiseOps.[j] * lowerOps.[l] * lowerOps.[k]
            product.DistributeCoefficient.SummandTerms
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff * Complex(0.5, 0.0)))
            |> PauliRegisterSequence

        let ni = int n

        let oneBodyTerms =
            [| for i in 0 .. ni - 1 do
                   for j in 0 .. ni - 1 do
                       let key = sprintf "%d,%d" i j
                       match coefficientFactory key with
                       | Some hij -> yield (i, j, hij)
                       | _ -> () |]
            |> Array.Parallel.map encodeOneBody

        let twoBodyTerms =
            [| for i in 0 .. ni - 1 do
                   for j in 0 .. ni - 1 do
                       for k in 0 .. ni - 1 do
                           for l in 0 .. ni - 1 do
                               let key = sprintf "%d,%d,%d,%d" i j k l
                               match coefficientFactory key with
                               | Some hijkl -> yield (i, j, k, l, hijkl)
                               | _ -> () |]
            |> Array.Parallel.map encodeTwoBody

        Array.append oneBodyTerms twoBodyTerms
        |> PauliRegisterSequence
        |> dropNumericalZeros


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
            [| for i in modeRange n do
                   for j in modeRange n -> (i, j) |]
            |> Array.Parallel.map (fun (i, j) ->
                let key = sprintf "%u,%u" i j
                let product = (encode Raise i n) * (encode Lower j n)
                let terms = product.DistributeCoefficient.SummandTerms |> toSkeletonTerms
                { Key = key; Terms = terms })
            |> Array.filter (fun e -> e.Terms.Length > 0)

        let twoBody =
            [| for i in modeRange n do
                   for j in modeRange n do
                       for k in modeRange n do
                           for l in modeRange n -> (i, j, k, l) |]
            |> Array.Parallel.map (fun (i, j, k, l) ->
                let key = sprintf "%u,%u,%u,%u" i j k l
                let terms = twoBodyStructuralTerms encode i j k l n |> toSkeletonTerms
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
            [| for i in modeRange n do
                   for j in modeRange n do
                       let key = sprintf "%u,%u" i j
                       match coefficientFactory key with
                       | Some _ -> yield (i, j, key)
                       | None -> () |]

        let twoBodyKeys =
            [| for i in modeRange n do
                   for j in modeRange n do
                       for k in modeRange n do
                           for l in modeRange n do
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
                let terms = twoBodyStructuralTerms encode i j k l n |> toSkeletonTerms
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

        // Construct PauliRegisters only for the final combined terms (dropping
        // numerical-zero residues from fermionic cancellations).
        coeffDict
        |> Seq.filter (fun kvp -> kvp.Value.Magnitude > 1e-12)
        |> Seq.map (fun kvp -> PauliRegister(opsDict.[kvp.Key], kvp.Value))
        |> Seq.toArray
        |> PauliRegisterSequence
