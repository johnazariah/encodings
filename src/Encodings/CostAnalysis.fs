namespace Encodings

open System
open System.Numerics

/// <summary>
/// Unified cost metrics for encoded Hamiltonians, including qubitization resource estimation.
/// </summary>
/// <remarks>
/// <para>
/// Provides cost functions that characterise a Hamiltonian in the Pauli basis
/// independently of any particular simulation strategy. Metrics include the
/// LCU 1-norm (λ), Pauli weight statistics, and term counts — all of which
/// feed into resource estimates for Trotterization, qubitization, and
/// measurement programs.
/// </para>
/// <para>
/// The qubitization resource estimate follows Low &amp; Chuang (2019): the query
/// complexity of quantum signal processing scales as O(λ / ε), where λ = Σ|cₖ|
/// is the LCU 1-norm and ε is the target precision.
/// </para>
/// </remarks>
module CostAnalysis =

    // ── Hamiltonian-Level Cost Metrics ───────────────────────────────

    /// <summary>
    /// Comprehensive cost metrics for a Hamiltonian in Pauli basis.
    /// </summary>
    /// <remarks>
    /// These metrics are encoding-dependent — comparing them across encodings
    /// reveals which encoding minimises a given cost for a particular molecule.
    /// </remarks>
    type HamiltonianCosts =
        { /// <summary>Number of distinct Pauli terms (after like-term combination).</summary>
          TermCount : int
          /// <summary>Number of qubits the Hamiltonian acts on.</summary>
          QubitCount : int
          /// <summary>LCU 1-norm: Σ|cₖ|. Governs qubitization query complexity.</summary>
          LambdaNorm : float
          /// <summary>Maximum Pauli weight (non-identity count) across all terms.</summary>
          MaxPauliWeight : int
          /// <summary>Mean Pauli weight across all terms.</summary>
          MeanPauliWeight : float
          /// <summary>Sum of Pauli weights across all terms. Proxy for total CNOT cost in Trotter.</summary>
          TotalPauliWeight : int
          /// <summary>Coefficient of the all-identity term (global energy offset).</summary>
          IdentityCoeff : float }

    /// <summary>
    /// Compute the Pauli weight (number of non-identity positions) of a register.
    /// </summary>
    let pauliWeight (register : PauliRegister) =
        register.Operators
        |> Array.sumBy (fun p -> if p = I then 0 else 1)

    /// <summary>
    /// Compute comprehensive cost metrics for a Hamiltonian.
    /// </summary>
    /// <param name="hamiltonian">The Hamiltonian as a sum of Pauli strings.</param>
    /// <returns>A <see cref="HamiltonianCosts"/> record with all metrics.</returns>
    let hamiltonianCosts (hamiltonian : PauliRegisterSequence) : HamiltonianCosts =
        let terms = hamiltonian.DistributeCoefficient.SummandTerms

        let qubitCount =
            terms
            |> Array.map (fun t -> t.Size)
            |> Array.fold max 0

        let weights = terms |> Array.map pauliWeight

        let lambdaNorm =
            terms |> Array.sumBy (fun t -> t.Coefficient.Magnitude)

        let identityCoeff =
            let identitySig = String.replicate qubitCount "I"
            match hamiltonian.DistributeCoefficient.Item identitySig with
            | true, reg -> reg.Coefficient.Real
            | false, _  -> 0.0

        { TermCount        = terms.Length
          QubitCount        = qubitCount
          LambdaNorm        = lambdaNorm
          MaxPauliWeight    = if Array.isEmpty weights then 0 else Array.max weights
          MeanPauliWeight   = if Array.isEmpty weights then 0.0 else Array.averageBy float weights
          TotalPauliWeight  = Array.sum weights
          IdentityCoeff     = identityCoeff }

    // ── Qubitization Resource Estimation ─────────────────────────────

    /// <summary>
    /// Qubitization resource estimate for a Hamiltonian expressed as an LCU.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In qubitization (quantum signal processing / quantum walk), the
    /// Hamiltonian is decomposed as H = Σₖ αₖ Uₖ where each Uₖ is unitary
    /// (a Pauli string). The key cost parameters are:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>λ = Σ|αₖ| — the LCU 1-norm, governing query complexity</description></item>
    ///   <item><description>L = number of unitaries — governs ancilla width for SELECT/PREPARE</description></item>
    ///   <item><description>Query complexity: O(λ · t / ε) for time evolution to precision ε</description></item>
    /// </list>
    /// </remarks>
    type QubitizationCosts =
        { /// <summary>LCU 1-norm λ = Σ|αₖ|.</summary>
          Lambda : float
          /// <summary>Number of distinct Pauli unitaries in the LCU decomposition.</summary>
          DistinctUnitaries : int
          /// <summary>Ancilla qubits for PREPARE/SELECT: ⌈log₂ L⌉.</summary>
          SelectAncillas : int
          /// <summary>Total qubits: system + ancillas.</summary>
          TotalQubits : int
          /// <summary>System (data) qubits.</summary>
          SystemQubits : int }

    /// <summary>
    /// Compute qubitization resource estimates for a Hamiltonian.
    /// </summary>
    /// <param name="hamiltonian">The Hamiltonian as a sum of Pauli strings.</param>
    /// <returns>A <see cref="QubitizationCosts"/> record.</returns>
    let qubitizationCosts (hamiltonian : PauliRegisterSequence) : QubitizationCosts =
        let terms = hamiltonian.DistributeCoefficient.SummandTerms

        let systemQubits =
            terms
            |> Array.map (fun t -> t.Size)
            |> Array.fold max 0

        let lambda =
            terms |> Array.sumBy (fun t -> t.Coefficient.Magnitude)

        let distinctUnitaries = terms.Length

        let selectAncillas =
            if distinctUnitaries <= 1 then 0
            else int (ceil (Math.Log(float distinctUnitaries, 2.0)))

        { Lambda            = lambda
          DistinctUnitaries = distinctUnitaries
          SelectAncillas    = selectAncillas
          TotalQubits       = systemQubits + selectAncillas
          SystemQubits      = systemQubits }

    /// <summary>
    /// Estimate the number of queries to the block-encoding oracle needed
    /// for time evolution exp(−iHt) to precision ε.
    /// </summary>
    /// <remarks>
    /// Based on the quantum signal processing bound: the number of queries
    /// scales as O(λt/ε + log(1/ε)), where λ is the LCU 1-norm.
    /// The leading term dominates for chemically relevant precisions.
    /// </remarks>
    /// <param name="costs">Pre-computed qubitization costs.</param>
    /// <param name="time">Evolution time t.</param>
    /// <param name="epsilon">Target precision ε.</param>
    /// <returns>Estimated query count (rounded up).</returns>
    let qubitizationQueries (costs : QubitizationCosts) (time : float) (epsilon : float) : int =
        // QSP query complexity: ⌈λt/ε⌉ + O(log(1/ε))
        // We use the leading term as the practical estimate.
        int (ceil (costs.Lambda * time / epsilon))

    // ── Comparison ──────────────────────────────────────────────────

    /// <summary>
    /// Compare cost metrics across multiple encoded Hamiltonians.
    /// </summary>
    /// <param name="encodings">Array of (name, Hamiltonian) pairs.</param>
    /// <returns>Array of (name, costs) pairs sorted by λ-norm ascending.</returns>
    let compareCosts (encodings : (string * PauliRegisterSequence)[]) : (string * HamiltonianCosts)[] =
        encodings
        |> Array.map (fun (name, h) -> name, hamiltonianCosts h)
        |> Array.sortBy (fun (_, c) -> c.LambdaNorm)

    /// <summary>
    /// Compare qubitization costs across multiple encoded Hamiltonians.
    /// </summary>
    /// <param name="encodings">Array of (name, Hamiltonian) pairs.</param>
    /// <returns>Array of (name, costs) pairs sorted by λ ascending.</returns>
    let compareQubitizationCosts (encodings : (string * PauliRegisterSequence)[]) : (string * QubitizationCosts)[] =
        encodings
        |> Array.map (fun (name, h) -> name, qubitizationCosts h)
        |> Array.sortBy (fun (_, c) -> c.Lambda)
