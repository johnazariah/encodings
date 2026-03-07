namespace Encodings

open System.Numerics

/// <summary>
/// Symbolic Trotter decomposition of Pauli Hamiltonians.
/// </summary>
/// <remarks>
/// <para>
/// Decomposes the time evolution operator exp(−iHΔt) into a sequence of
/// Pauli rotations exp(−iθₖPₖ), and further into elementary quantum gates
/// using the CNOT staircase construction. All operations are purely symbolic
/// — no matrix exponentiation or state-vector simulation is performed.
/// </para>
/// <para>
/// Supports first-order and second-order (symmetric) Suzuki–Trotter
/// decompositions, along with circuit resource estimation (CNOT count,
/// Pauli weight statistics).
/// </para>
/// </remarks>
module Trotterization =

    // ── Types ───────────────────────────────────────────────────────

    /// <summary>A single Pauli rotation exp(−iθP).</summary>
    /// <remarks>
    /// The operator P is a Pauli string with unit coefficient;
    /// the original Hamiltonian coefficient is absorbed into θ.
    /// </remarks>
    type PauliRotation =
        { /// <summary>The Pauli string P (coefficient = 1).</summary>
          Operator : PauliRegister
          /// <summary>Rotation angle θ in exp(−iθP).</summary>
          Angle : float }

    /// <summary>Trotter decomposition order.</summary>
    type TrotterOrder =
        /// <summary>
        /// First-order: exp(−iHΔt) ≈ Πₖ exp(−icₖPₖΔt). Error O(Δt²).
        /// </summary>
        | First
        /// <summary>
        /// Second-order symmetric: forward then reverse at half-angle. Error O(Δt³).
        /// </summary>
        | Second

    /// <summary>One complete Trotter step (a product of Pauli rotations).</summary>
    type TrotterStep =
        { /// <summary>Ordered sequence of Pauli rotations for this step.</summary>
          Rotations : PauliRotation[]
          /// <summary>Trotter decomposition order used.</summary>
          Order : TrotterOrder
          /// <summary>Time step size Δt.</summary>
          TimeStep : float }

    /// <summary>Elementary quantum gate for circuit synthesis.</summary>
    /// <remarks>
    /// Universal gate set used in CNOT staircase decomposition.
    /// Requires qualified access to avoid ambiguity with
    /// <see cref="CliffordGate"/> cases from the Tapering module.
    /// </remarks>
    [<RequireQualifiedAccess>]
    type Gate =
        /// <summary>Hadamard gate on a single qubit.</summary>
        | H of qubit: int
        /// <summary>Phase gate S on a single qubit (Z^½).</summary>
        | S of qubit: int
        /// <summary>Adjoint phase gate S† on a single qubit (Z^−½).</summary>
        | Sdg of qubit: int
        /// <summary>Rotation Rz(θ) = exp(−iθZ/2) on a single qubit.</summary>
        | Rz of qubit: int * angle: float
        /// <summary>Controlled-NOT gate.</summary>
        | CNOT of control: int * target: int

    /// <summary>Circuit resource statistics for a Trotter step.</summary>
    type CircuitStats =
        { /// <summary>Number of Pauli rotations.</summary>
          RotationCount : int
          /// <summary>Total CNOT gate count.</summary>
          CnotCount : int
          /// <summary>Total single-qubit gate count (H, S, S†, Rz).</summary>
          SingleQubitCount : int
          /// <summary>Total gate count.</summary>
          TotalGates : int
          /// <summary>Maximum Pauli weight across all rotations.</summary>
          MaxWeight : int
          /// <summary>Mean Pauli weight across all rotations.</summary>
          MeanWeight : float }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Pauli weight of a register: the number of non-identity positions.
    /// </summary>
    let pauliWeight (register : PauliRegister) =
        register.Operators
        |> Array.sumBy (fun p -> if p = I then 0 else 1)

    /// Non-identity qubit positions paired with their Pauli operators.
    let private nonIdentityPositions (register : PauliRegister) =
        register.Operators
        |> Array.indexed
        |> Array.filter (fun (_, p) -> p <> I)

    /// Build a PauliRotation from a Hamiltonian term and time step.
    let private termToRotation (dt : float) (term : PauliRegister) =
        { Operator = term.ResetPhase Complex.One
          Angle    = term.Coefficient.Real * dt }

    // ── Trotter Decomposition ───────────────────────────────────────

    /// <summary>
    /// First-order Trotter decomposition.
    /// </summary>
    /// <remarks>
    /// exp(−iHΔt) ≈ Πₖ exp(−icₖPₖΔt) where H = Σₖ cₖPₖ.
    /// </remarks>
    let firstOrderTrotter (dt : float) (hamiltonian : PauliRegisterSequence) =
        let rotations =
            hamiltonian.DistributeCoefficient.SummandTerms
            |> Array.map (termToRotation dt)
        { Rotations = rotations; Order = First; TimeStep = dt }

    /// <summary>
    /// Second-order (symmetric) Trotter decomposition.
    /// </summary>
    /// <remarks>
    /// Forward pass at half-angle followed by reverse pass at half-angle.
    /// The resulting step is palindromic.
    /// </remarks>
    let secondOrderTrotter (dt : float) (hamiltonian : PauliRegisterSequence) =
        let terms = hamiltonian.DistributeCoefficient.SummandTerms
        let forward = terms |> Array.map (termToRotation (dt / 2.0))
        let reverse = forward |> Array.rev
        { Rotations = Array.append forward reverse
          Order     = Second
          TimeStep  = dt }

    /// <summary>Trotter decomposition dispatching on order.</summary>
    let trotterize (order : TrotterOrder) =
        match order with
        | First  -> firstOrderTrotter
        | Second -> secondOrderTrotter

    // ── Gate Decomposition (CNOT Staircase) ─────────────────────────

    /// <summary>
    /// Decompose a single Pauli rotation exp(−iθP) into elementary gates.
    /// </summary>
    /// <remarks>
    /// <para>For weight-w Pauli string P on qubits {q₁,…,qw}:</para>
    /// <list type="number">
    ///   <item><description>Basis change: H on X positions, S†H on Y positions</description></item>
    ///   <item><description>CNOT staircase: w−1 CNOTs linking non-identity qubits</description></item>
    ///   <item><description>Rz(2θ) on the last qubit</description></item>
    ///   <item><description>Reverse CNOT staircase</description></item>
    ///   <item><description>Reverse basis change</description></item>
    /// </list>
    /// <para>Total: 2(w−1) CNOTs + 1 Rz + basis-change single-qubit gates.</para>
    /// </remarks>
    let decomposeRotation (rotation : PauliRotation) : Gate[] =
        let positions = nonIdentityPositions rotation.Operator
        if Array.isEmpty positions then [||]
        else
            let qubits = positions |> Array.map fst
            [|
                // Basis change into Z basis
                for idx, pauli in positions do
                    match pauli with
                    | X -> Gate.H idx
                    | Y ->
                        Gate.Sdg idx
                        Gate.H idx
                    | _ -> ()

                // CNOT staircase (forward)
                for i in 0 .. qubits.Length - 2 do
                    Gate.CNOT(qubits.[i], qubits.[i + 1])

                // Rz rotation on last qubit
                Gate.Rz(Array.last qubits, 2.0 * rotation.Angle)

                // CNOT staircase (reverse)
                for i in qubits.Length - 2 .. -1 .. 0 do
                    Gate.CNOT(qubits.[i], qubits.[i + 1])

                // Restore from Z basis
                for idx, pauli in positions do
                    match pauli with
                    | X -> Gate.H idx
                    | Y ->
                        Gate.H idx
                        Gate.S idx
                    | _ -> ()
            |]

    /// <summary>Decompose a full Trotter step into elementary gates.</summary>
    let decomposeTrotterStep (step : TrotterStep) : Gate[] =
        step.Rotations |> Array.collect decomposeRotation

    // ── Cost Analysis ───────────────────────────────────────────────

    /// <summary>
    /// Quick CNOT count from Pauli weights (no gate decomposition needed).
    /// Each weight-w rotation contributes 2(w−1) CNOTs.
    /// </summary>
    let trotterCnotCount (step : TrotterStep) =
        step.Rotations
        |> Array.sumBy (fun r ->
            let w = pauliWeight r.Operator
            if w <= 1 then 0 else 2 * (w - 1))

    /// <summary>Compute full circuit resource statistics for a Trotter step.</summary>
    let trotterStepStats (step : TrotterStep) : CircuitStats =
        let gates   = decomposeTrotterStep step
        let weights = step.Rotations |> Array.map (fun r -> pauliWeight r.Operator)
        let cnots   = gates |> Array.sumBy (function Gate.CNOT _ -> 1 | _ -> 0)
        { RotationCount    = step.Rotations.Length
          CnotCount        = cnots
          SingleQubitCount = gates.Length - cnots
          TotalGates       = gates.Length
          MaxWeight        = if Array.isEmpty weights then 0 else Array.max weights
          MeanWeight       = if Array.isEmpty weights then 0.0 else Array.averageBy float weights }

    /// <summary>
    /// Compare CNOT costs across multiple encoded Hamiltonians.
    /// </summary>
    /// <param name="encodings">Array of (name, Hamiltonian) pairs.</param>
    /// <param name="dt">Time step size for the comparison.</param>
    let compareTrotterCosts (encodings : (string * PauliRegisterSequence)[]) (dt : float) =
        encodings
        |> Array.map (fun (name, h) ->
            let step = firstOrderTrotter dt h
            name, trotterStepStats step)
