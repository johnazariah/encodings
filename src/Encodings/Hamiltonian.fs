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
    /// Structural Pauli terms for the two-body operator <c>a†_i a†_j a_k a_l</c>.
    /// </summary>
    /// <remarks>
    /// The factory coefficient for key "i,j,k,l" is the FULL WEIGHTED prefactor of
    /// this operator string and is applied verbatim by the caller — this helper
    /// contributes only the structural Pauli phases from the encoding algebra, with
    /// the annihilators in the order <c>a_k</c> then <c>a_l</c>. Shared by every
    /// builder (sequential, parallel, cached, and both skeletons) and, via the
    /// skeleton, by <c>applyCoefficients</c>, so all surfaces agree.
    /// </remarks>
    let private twoBodyStructuralTerms (encode : EncoderFn) (i : uint32) (j : uint32) (k : uint32) (l : uint32) (n : uint32) : PauliRegister[] =
        let product =
            (encode Raise i n) * (encode Raise j n)
            * (encode Lower k n) * (encode Lower l n)
        product.DistributeCoefficient.SummandTerms

    /// <summary>Machine epsilon for IEEE-754 double precision (2^-52).</summary>
    let private machineEpsilon = 2.220446049250313e-16

    /// <summary>
    /// Cancellation-aware aggregation of assembled Pauli contributions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Groups contributions by Pauli signature and sums their coefficients, while
    /// tracking, per signature, the number of contributions and a magnitude
    /// <em>scale</em> (the sum of contribution magnitudes). A summed term is dropped
    /// only when it is an exact zero, or when it is a residual demonstrably produced
    /// by the cancellation of more than one contribution — i.e. the count is greater
    /// than one and the residual magnitude is at most a small multiple of machine
    /// epsilon times the scale.
    /// </para>
    /// <para>
    /// This is deliberately <b>not</b> an absolute threshold: a standalone term
    /// (count = 1) is never magnitude-pruned, so legitimate tiny coefficients
    /// (e.g. 1e-12, 1e-13, 1e-15) always survive. The core Pauli algebra remains
    /// exact; this reduction is applied only at Hamiltonian-assembly boundaries.
    /// </para>
    /// </remarks>
    let private reduceWithCancellation (contributions : PauliRegister[]) : PauliRegisterSequence =
        // K·eps·scale tolerance; K allows for accumulation over many contributions.
        let cancellationFactor = 32.0
        let reps   = System.Collections.Generic.Dictionary<string, PauliRegister>()
        let sums   = System.Collections.Generic.Dictionary<string, Complex>()
        let counts = System.Collections.Generic.Dictionary<string, int>()
        let scales = System.Collections.Generic.Dictionary<string, float>()
        for r in contributions do
            let key = r.Signature
            match sums.TryGetValue key with
            | true, existing ->
                sums.[key]   <- existing + r.Coefficient
                counts.[key] <- counts.[key] + 1
                scales.[key] <- scales.[key] + r.Coefficient.Magnitude
            | false, _ ->
                reps.[key]   <- r
                sums.[key]   <- r.Coefficient
                counts.[key] <- 1
                scales.[key] <- r.Coefficient.Magnitude
        [| for kvp in sums do
             let key   = kvp.Key
             let sum   = kvp.Value
             let count = counts.[key]
             let scale = scales.[key]
             let isCancellationResidue =
                 count > 1 && sum.Magnitude <= cancellationFactor * machineEpsilon * scale
             if (not sum.IsZero) && (not isCancellationResidue) then
                 yield reps.[key].ResetPhase sum |]
        |> PauliRegisterSequence

    /// <summary>
    /// Discriminated union representing a term in the Hamiltonian.
    /// </summary>
    type HamiltonianTerm =
    /// <summary>A one-body (overlap) term h_{ij} a†_i a_j.</summary>
    | Overlap  of OverlapTerm
    /// <summary>A two-body (exchange) term h_{ijkl} a†_i a†_j a_k a_l.</summary>
    | Exchange of ExchangeTerm

    /// <summary>
    /// A one-body overlap term with indices i and j, representing h_{ij} a†_i a_j.
    /// </summary>
    and OverlapTerm  = {i : uint32; j : uint32}
    with
        member private this.EncodedContributions (encode : EncoderFn) n coeff : PauliRegister[] =
            let product = (encode Raise this.i n) * (encode Lower this.j n)
            product.DistributeCoefficient.SummandTerms
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))

        /// Flat Pauli contributions for every one-body key the factory populates.
        static member internal ContributionsWith (encode : EncoderFn) coefficientFactory n : PauliRegister[] =
            [|
                for i in modeRange n do
                    for j in modeRange n do
                        let key = sprintf "%u,%u" i j
                        match coefficientFactory key with
                        | Some hij ->
                            let term = {OverlapTerm.i = i; OverlapTerm.j = j}
                            yield! term.EncodedContributions encode n hij
                        | _ -> ()
            |]

        static member internal Contributions coefficientFactory n =
            OverlapTerm.ContributionsWith jordanWignerTerms coefficientFactory n

    /// <summary>
    /// A two-body exchange term with indices i, j, k, l representing the operator
    /// <c>a†_i a†_j a_k a_l</c> (annihilators in the order k then l).
    /// </summary>
    /// <remarks>
    /// The coefficient supplied by the factory for key <c>"i,j,k,l"</c> is the FULL
    /// WEIGHTED prefactor of this operator string and is applied verbatim — the
    /// caller folds in the ½ of the two-body Hamiltonian term
    /// ½·Σ g_pqrs a†_p a†_q a_r a_s; no additional ½ is applied here. The
    /// <see cref="T:Encodings.Fcidump"/> adapters do this, mapping chemist-notation
    /// integrals to <c>½·(ps|qr)</c> for key "p,q,r,s". For raw physicist
    /// ⟨pq|rs⟩ tensors use <c>rawPhysicistToWeightedFactory</c> instead.
    /// </remarks>
    and ExchangeTerm = {i : uint32; j : uint32; k : uint32; l : uint32}
    with
        member private this.EncodedContributions (encode : EncoderFn) n coeff : PauliRegister[] =
            twoBodyStructuralTerms encode this.i this.j this.k this.l n
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))

        /// Flat Pauli contributions for every two-body key the factory populates.
        static member internal ContributionsWith (encode : EncoderFn) coefficientFactory n : PauliRegister[] =
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
                                    yield! term.EncodedContributions encode n hijkl
                                | _ -> ()
            |]

        static member internal Contributions coefficientFactory n =
            ExchangeTerm.ContributionsWith jordanWignerTerms coefficientFactory n


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
    /// <b>Coefficient contract (same as <c>computeHamiltonian</c>).</b> The
    /// factory returns the FULL WEIGHTED prefactor of the operator string, applied
    /// verbatim: "i,j" → coefficient of <c>a†_i a_j</c>; "i,j,k,l" → coefficient of
    /// <c>a†_i a†_j a_k a_l</c> <b>with the two-body ½ already folded in</b> (raw
    /// integrals yield a result twice too large). Build a conforming factory with
    /// <see cref="T:Encodings.Fcidump"/>, or wrap a raw physicist tensor with
    /// <c>rawPhysicistToWeightedFactory</c>.
    /// </para>
    /// </remarks>
    let computeHamiltonianWith (encode : EncoderFn) coefficientFactory n =
        Array.append
            (OverlapTerm.ContributionsWith  encode coefficientFactory n)
            (ExchangeTerm.ContributionsWith encode coefficientFactory n)
        |> reduceWithCancellation

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
    /// <b>Coefficient contract.</b> The factory returns the FULL WEIGHTED prefactor
    /// of the corresponding operator string, which this function applies verbatim:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>"i,j" → h_ij, the coefficient of <c>a†_i a_j</c>.</description></item>
    ///   <item><description>"i,j,k,l" → the coefficient of <c>a†_i a†_j a_k a_l</c>,
    ///   <b>with the ½ of the two-body term ½·Σ g_pqrs a†_p a†_q a_r a_s already folded in</b>.
    ///   No additional ½ is applied here — supplying a raw integral yields a result
    ///   twice too large. For raw physicist ⟨pq|rs⟩ tensors, wrap the factory with
    ///   <c>rawPhysicistToWeightedFactory</c> (or call
    ///   <c>computeHamiltonianFromPhysicist</c>).</description></item>
    /// </list>
    /// <para>
    /// The nuclear/constant term is not added (callers may add E_nuc·I separately).
    /// Use <see cref="T:Encodings.Fcidump"/> to build a conforming factory from an
    /// FCIDUMP: for key "p,q,r,s" it supplies <c>½·(ps|qr)</c> (chemist notation) =
    /// <c>½·⟨pq|sr⟩</c> (physicist notation). Numerically-zero residues from
    /// fermionic cancellation are removed (cancellation-aware), while standalone
    /// tiny coefficients are preserved.
    /// </para>
    /// </remarks>
    let computeHamiltonian coefficientFactory n =
        computeHamiltonianWith jordanWignerTerms coefficientFactory n


    // ── Parallel Hamiltonian construction ─────────────────────────────

    /// <summary>
    /// Parallel version of <c>computeHamiltonianWith</c>.
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
    /// identical to the sequential <c>computeHamiltonianWith</c>.
    /// </remarks>
    let computeHamiltonianWithParallel (encode : EncoderFn) coefficientFactory n =
        let encodeOneBody (i, j, coeff) : PauliRegister[] =
            let product = (encode Raise i n) * (encode Lower j n)
            product.DistributeCoefficient.SummandTerms
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))

        let encodeTwoBody (i, j, k, l, coeff) : PauliRegister[] =
            twoBodyStructuralTerms encode i j k l n
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))

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

        Array.append (Array.concat oneBodyTerms) (Array.concat twoBodyTerms)
        |> reduceWithCancellation

    /// <summary>
    /// Parallel version of <c>computeHamiltonian</c> (Jordan-Wigner).
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
    /// <para>
    /// <b>Coefficient contract (same as <c>computeHamiltonianWith</c>).</b>
    /// The factory returns the FULL WEIGHTED prefactor for each key, applied verbatim
    /// (two-body ½ pre-folded). Results are identical to the sequential builder,
    /// including the cancellation-aware removal of zero residues.
    /// </para>
    /// </remarks>
    /// <param name="encode">The encoding function.</param>
    /// <param name="coefficientFactory">Weighted coefficient lookup (see <c>computeHamiltonianWith</c>; wrap raw physicist tensors with <c>rawPhysicistToWeightedFactory</c>).</param>
    /// <param name="n">Number of spin-orbitals (qubits).</param>
    let computeHamiltonianCached (encode : EncoderFn) coefficientFactory (n : uint32) =
        // Pre-cache all encoded raise and lower operators
        let raiseOps = Array.init (int n) (fun i -> encode Raise (uint32 i) n)
        let lowerOps = Array.init (int n) (fun i -> encode Lower (uint32 i) n)

        let encodeOneBody (i : int, j : int, coeff) : PauliRegister[] =
            let product = raiseOps.[i] * lowerOps.[j]
            product.DistributeCoefficient.SummandTerms
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))

        let encodeTwoBody (i : int, j : int, k : int, l : int, coeff) : PauliRegister[] =
            // Verbatim a†_i a†_j a_k a_l (order k then l); factory coeff is the full
            // weighted prefactor (see twoBodyStructuralTerms).
            let product = raiseOps.[i] * raiseOps.[j] * lowerOps.[k] * lowerOps.[l]
            product.DistributeCoefficient.SummandTerms
            |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))

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

        Array.append (Array.concat oneBodyTerms) (Array.concat twoBodyTerms)
        |> reduceWithCancellation


    // ── Pauli Skeleton: separate structure from coefficients ─────────

    /// <summary>
    /// A pre-computed Pauli term with its signature cached.
    /// </summary>
    /// <remarks>
    /// Caching the signature string and operator array avoids recomputing
    /// them during <c>applyCoefficients</c>, which is called once
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
    /// The skeleton captures part (1) once, then <c>applyCoefficients</c>
    /// evaluates part (2) cheaply for any coefficient set.  This is ideal for
    /// potential energy surface scans where the basis set (and encoding
    /// structure) is fixed but integral values vary with geometry.
    /// </para>
    /// </remarks>
    type HamiltonianSkeleton =
        { /// <summary>One-body entries: a†_i a_j encoded as Pauli strings.</summary>
          OneBody   : SkeletonEntry[]
          /// <summary>Two-body entries: a†_i a†_j a_k a_l encoded as Pauli strings.</summary>
          TwoBody   : SkeletonEntry[]
          /// <summary>Number of qubits in the system.</summary>
          NumQubits : uint32 }

    /// <summary>
    /// Precompute the Pauli skeleton for a given encoding and system size.
    /// </summary>
    /// <param name="encode">The encoding function.</param>
    /// <param name="n">The number of qubits/modes.</param>
    /// <returns>
    /// A <see cref="T:Encodings.Hamiltonian.HamiltonianSkeleton"/> containing precomputed Pauli
    /// structures for all one-body and two-body operator products.
    /// </returns>
    /// <remarks>
    /// Uses <c>Array.Parallel.map</c> on the n⁴ two-body index space.
    /// Signatures and operator arrays are cached in <see cref="T:Encodings.Hamiltonian.SkeletonTerm"/>
    /// records so that <c>applyCoefficients</c> can accumulate
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
    /// A <see cref="T:Encodings.Hamiltonian.HamiltonianSkeleton"/> containing precomputed Pauli
    /// structures only for index combinations where the factory returns Some.
    /// </returns>
    /// <remarks>
    /// <para>
    /// For molecules, typically only 5–10% of possible index combinations have
    /// non-zero integrals.  This variant precomputes only those entries,
    /// giving a proportional speedup over the full
    /// <c>computeHamiltonianSkeleton</c>.
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
    /// <param name="skeleton">The precomputed skeleton from <c>computeHamiltonianSkeleton</c>.</param>
    /// <param name="coefficientFactory">A function returning Some(coefficient) for a given comma-separated index key, or None to skip. Same WEIGHTED contract as <c>computeHamiltonianWith</c> — the value is the full weighted prefactor (two-body ½ pre-folded), applied verbatim; wrap raw physicist tensors with <c>rawPhysicistToWeightedFactory</c>.</param>
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
    /// Zero residues are dropped cancellation-aware (exact zeros, or residues from
    /// more than one contribution below a small multiple of machine epsilon times the
    /// contribution scale), matching <c>computeHamiltonianWith</c>; standalone
    /// tiny coefficients are preserved.
    /// </para>
    /// <para>
    /// Typical runtime is under 10 ms for systems up to ~20 qubits,
    /// making PES scans over hundreds of geometries essentially free
    /// after the one-time skeleton build.
    /// </para>
    /// </remarks>
    let applyCoefficients (skeleton : HamiltonianSkeleton) (coefficientFactory : string -> Complex option) : PauliRegisterSequence =
        // Accumulate coefficients by Pauli signature — no PauliRegister construction.
        // Track per-signature contribution count and magnitude scale so the final
        // reduction is cancellation-aware (see reduceWithCancellation).
        let cancellationFactor = 32.0
        let sums   = System.Collections.Generic.Dictionary<string, Complex>()
        let counts = System.Collections.Generic.Dictionary<string, int>()
        let scales = System.Collections.Generic.Dictionary<string, float>()
        let opsDict = System.Collections.Generic.Dictionary<string, Pauli[]>()

        let processEntries (entries : SkeletonEntry[]) =
            for entry in entries do
                match coefficientFactory entry.Key with
                | Some coeff ->
                    for term in entry.Terms do
                        let scaledCoeff = term.StructuralCoeff * coeff
                        match sums.TryGetValue term.Signature with
                        | true, existing ->
                            sums.[term.Signature]   <- existing + scaledCoeff
                            counts.[term.Signature] <- counts.[term.Signature] + 1
                            scales.[term.Signature] <- scales.[term.Signature] + scaledCoeff.Magnitude
                        | false, _ ->
                            sums.[term.Signature]    <- scaledCoeff
                            counts.[term.Signature]  <- 1
                            scales.[term.Signature]  <- scaledCoeff.Magnitude
                            opsDict.[term.Signature] <- term.Operators
                | None -> ()

        processEntries skeleton.OneBody
        processEntries skeleton.TwoBody

        // Construct PauliRegisters only for the final combined terms, dropping exact
        // zeros and cancellation residues (|sum| ≤ K·eps·scale when count > 1) while
        // preserving standalone tiny coefficients.
        [| for kvp in sums do
             let sign  = kvp.Key
             let sum   = kvp.Value
             let count = counts.[sign]
             let scale = scales.[sign]
             let isCancellationResidue =
                 count > 1 && sum.Magnitude <= cancellationFactor * machineEpsilon * scale
             if (not sum.IsZero) && (not isCancellationResidue) then
                 yield PauliRegister(opsDict.[sign], sum) |]
        |> PauliRegisterSequence


    // ── Raw physicist integral adapter ───────────────────────────────

    /// <summary>
    /// Adapt a raw physicist two-electron integral factory to the weighted
    /// coefficient contract consumed by <c>computeHamiltonianWith</c> and
    /// every other builder.
    /// </summary>
    /// <param name="rawPhysicistFactory">
    /// A factory returning, for key <c>"p,q,r,s"</c>, the RAW single-bar physicist
    /// integral <c>⟨pq|rs⟩</c> under the unrestricted sum (no ½, no antisymmetrisation),
    /// and, for key <c>"p,q"</c>, the one-body coefficient <c>h_pq</c>.
    /// </param>
    /// <returns>
    /// A weighted factory. For a two-body query <c>"i,j,k,l"</c> it returns
    /// <c>½·⟨ij|lk⟩</c> — i.e. it maps a raw entry <c>(p,q,r,s) ↦ ⟨pq|rs⟩</c> to the
    /// weighted key <c>(p,q,s,r)</c> with value <c>½·⟨pq|rs⟩</c>, reproducing the
    /// physics <c>½·Σ ⟨pq|rs⟩ a†_p a†_q a_s a_r</c>. One-body keys pass through
    /// unchanged. The nuclear/core energy is not involved.
    /// </returns>
    /// <remarks>
    /// This is the single-bar (½) convention. An antisymmetrised double-bar tensor
    /// <c>⟨pq||rs⟩</c> would instead use a ¼ prefactor under its own explicitly named
    /// adapter (not provided here). Passing an already-weighted / pre-adapted factory
    /// through this adapter double-adapts it (a migration hazard); conversely, passing
    /// a raw physicist factory straight to <c>computeHamiltonianWith</c> is a
    /// caller error (the ½ and index swap would be missing).
    /// </remarks>
    let rawPhysicistToWeightedFactory (rawPhysicistFactory : string -> Complex option) : (string -> Complex option) =
        fun (key : string) ->
            let parts = key.Split(',')
            match parts.Length with
            | 4 ->
                // Weighted key (i,j,k,l) ← raw key (i,j,l,k); value = ½·⟨ij|lk⟩.
                let rawKey = sprintf "%s,%s,%s,%s" parts.[0] parts.[1] parts.[3] parts.[2]
                rawPhysicistFactory rawKey
                |> Option.map (fun g -> g * Complex(0.5, 0.0))
            | _ -> rawPhysicistFactory key

    /// <summary>
    /// Compute a qubit Hamiltonian directly from a raw physicist integral factory
    /// using any encoding.
    /// </summary>
    /// <param name="encode">The encoding function.</param>
    /// <param name="rawPhysicistFactory">A factory of raw physicist integrals ⟨pq|rs⟩ (see <c>rawPhysicistToWeightedFactory</c>).</param>
    /// <param name="n">The number of qubits/modes in the system.</param>
    /// <remarks>
    /// Convenience wrapper equivalent to
    /// <c>computeHamiltonianWith encode (rawPhysicistToWeightedFactory rawPhysicistFactory) n</c>.
    /// The nuclear/core energy is added separately by the caller.
    /// </remarks>
    let computeHamiltonianFromPhysicistWith (encode : EncoderFn) (rawPhysicistFactory : string -> Complex option) n =
        computeHamiltonianWith encode (rawPhysicistToWeightedFactory rawPhysicistFactory) n

    /// <summary>
    /// Compute a qubit Hamiltonian directly from a raw physicist integral factory
    /// using Jordan-Wigner encoding.
    /// </summary>
    /// <param name="rawPhysicistFactory">A factory of raw physicist integrals ⟨pq|rs⟩ (see <c>rawPhysicistToWeightedFactory</c>).</param>
    /// <param name="n">The number of qubits/modes in the system.</param>
    let computeHamiltonianFromPhysicist (rawPhysicistFactory : string -> Complex option) n =
        computeHamiltonianFromPhysicistWith jordanWignerTerms rawPhysicistFactory n
