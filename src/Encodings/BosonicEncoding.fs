namespace Encodings

/// <summary>
/// Bosonic-to-qubit encodings: Unary, Binary, and Gray code.
/// </summary>
/// <remarks>
/// <para>
/// Maps bosonic ladder operators (b†, b) acting on truncated Fock spaces
/// to qubit Pauli strings. Each bosonic mode is truncated to d occupation
/// levels (n ∈ {0, 1, …, d−1}) and encoded into qubits using one of three
/// strategies:
/// </para>
/// <list type="bullet">
///   <item><description><b>Unary (one-hot)</b>: d qubits per mode, Pauli weight ≤ 2.</description></item>
///   <item><description><b>Standard binary</b>: ⌈log₂ d⌉ qubits per mode, Pauli weight ≤ ⌈log₂ d⌉.</description></item>
///   <item><description><b>Gray code</b>: ⌈log₂ d⌉ qubits per mode, improved average Pauli weight.</description></item>
/// </list>
/// <para>
/// Reference: Sawaya, Menke, Kyaw, Johri, Aspuru-Guzik, Guerreschi —
/// "Resource-efficient digital quantum simulation of d-level systems
/// for photonic, vibrational, and spin-s Hamiltonians" (arXiv:1909.05820).
/// </para>
/// </remarks>
[<AutoOpen>]
module BosonicEncoding =
    open System
    open System.Numerics

    // ═════════════════════════════════════════════
    //  Types
    // ═════════════════════════════════════════════

    /// <summary>
    /// A function that encodes a single bosonic ladder operator into qubit Pauli strings.
    /// </summary>
    /// <remarks>
    /// Parameters: operator (Raise/Lower), mode index j, number of modes M, truncation cutoff d.
    /// Returns a <see cref="PauliRegisterSequence"/> of width M × (qubits per mode).
    /// </remarks>
    type BosonicEncoderFn = LadderOperatorUnit -> uint32 -> uint32 -> uint32 -> PauliRegisterSequence

    // ═════════════════════════════════════════════
    //  Matrix utilities
    // ═════════════════════════════════════════════

    /// <summary>
    /// Ceiling of log₂ d, with minimum 1.
    /// </summary>
    let internal ceilLog2 (d : int) : int =
        if d <= 1 then 1
        else
            let mutable q = 0
            let mutable power = 1
            while power < d do
                q <- q + 1
                power <- power * 2
            q

    /// <summary>
    /// The 2×2 matrix representation of a single-qubit Pauli operator.
    /// </summary>
    let internal pauliToMatrix (p : Pauli) : Complex[,] =
        match p with
        | Pauli.I -> array2D [| [| Complex.One;       Complex.Zero      |]
                                [| Complex.Zero;      Complex.One       |] |]
        | Pauli.X -> array2D [| [| Complex.Zero;      Complex.One       |]
                                [| Complex.One;       Complex.Zero      |] |]
        | Pauli.Y -> array2D [| [| Complex.Zero;      Complex(0., -1.)  |]
                                [| Complex(0., 1.);   Complex.Zero      |] |]
        | Pauli.Z -> array2D [| [| Complex.One;       Complex.Zero      |]
                                [| Complex.Zero;      Complex(-1., 0.)  |] |]

    /// <summary>
    /// Kronecker (tensor) product of two complex matrices.
    /// </summary>
    let internal kronecker (a : Complex[,]) (b : Complex[,]) : Complex[,] =
        let rb = Array2D.length1 b
        let cb = Array2D.length2 b
        Array2D.init
            (Array2D.length1 a * rb)
            (Array2D.length2 a * cb)
            (fun i j -> a.[i / rb, j / cb] * b.[i % rb, j % cb])

    /// <summary>
    /// Trace of the product A·B for two square complex matrices.
    /// Tr(AB) = Σᵢⱼ Aᵢⱼ Bⱼᵢ.
    /// </summary>
    let internal traceProduct (a : Complex[,]) (b : Complex[,]) : Complex =
        let n = Array2D.length1 a
        let mutable sum = Complex.Zero
        for i in 0 .. n - 1 do
            for j in 0 .. n - 1 do
                sum <- sum + a.[i, j] * b.[j, i]
        sum

    // ═════════════════════════════════════════════
    //  Pauli decomposition
    // ═════════════════════════════════════════════

    /// <summary>
    /// Enumerate all Pauli strings on q qubits (4^q strings).
    /// </summary>
    let internal allPauliStrings (q : int) : Pauli[][] =
        let paulis = [| Pauli.I; Pauli.X; Pauli.Y; Pauli.Z |]
        let rec generate remaining =
            if remaining = 0 then [| [||] |]
            else
                [| for p in paulis do
                       for r in generate (remaining - 1) do
                           yield Array.append [| p |] r |]
        generate q

    /// <summary>
    /// Build the 2^q × 2^q matrix for a tensor product of Pauli operators.
    /// </summary>
    let internal pauliTensorMatrix (paulis : Pauli[]) : Complex[,] =
        paulis
        |> Array.map pauliToMatrix
        |> Array.reduce kronecker

    /// <summary>
    /// Decompose a 2^q × 2^q operator matrix into Pauli strings.
    /// </summary>
    /// <param name="matrix">The operator matrix (must be 2^q × 2^q).</param>
    /// <param name="numQubits">The number of qubits q.</param>
    /// <returns>
    /// Array of (coefficient, Pauli[]) pairs with non-negligible coefficients.
    /// O = Σ_P (Tr(PO) / 2^q) P.
    /// </returns>
    let internal decomposeIntoPaulis (matrix : Complex[,]) (numQubits : int) : (Complex * Pauli[])[] =
        let size = 1 <<< numQubits
        let norm = 1.0 / float size
        allPauliStrings numQubits
        |> Array.choose (fun ps ->
            let pMat = pauliTensorMatrix ps
            let tr = traceProduct pMat matrix
            let coeff = Complex(tr.Real * norm, tr.Imaginary * norm)
            if coeff.Magnitude > 1e-12 then Some (coeff, ps)
            else None)

    // ═════════════════════════════════════════════
    //  Truncated bosonic operator matrices
    // ═════════════════════════════════════════════

    /// <summary>
    /// Build the d×d creation operator matrix b†.
    /// (b†)_{n+1,n} = √(n+1) for n = 0, …, d−2.
    /// </summary>
    let internal bosonicCreationMatrix (d : int) : Complex[,] =
        Array2D.init d d (fun row col ->
            if row = col + 1 then Complex(sqrt (float (col + 1)), 0.)
            else Complex.Zero)

    /// <summary>
    /// Build the d×d annihilation operator matrix b.
    /// (b)_{n,n+1} = √(n+1) for n = 0, …, d−2.
    /// </summary>
    let internal bosonicAnnihilationMatrix (d : int) : Complex[,] =
        Array2D.init d d (fun row col ->
            if col = row + 1 then Complex(sqrt (float (row + 1)), 0.)
            else Complex.Zero)

    /// <summary>
    /// Build the d×d number operator matrix n̂ = diag(0, 1, …, d−1).
    /// </summary>
    let internal bosonicNumberMatrix (d : int) : Complex[,] =
        Array2D.init d d (fun row col ->
            if row = col then Complex(float row, 0.)
            else Complex.Zero)

    // ═════════════════════════════════════════════
    //  Basis mappings
    // ═════════════════════════════════════════════

    /// <summary>
    /// Standard binary mapping: Fock state n → qubit state n (identity permutation).
    /// </summary>
    let internal binaryBasisMap (n : int) = n

    /// <summary>
    /// Gray code mapping: Fock state n → qubit state n ⊕ (n ≫ 1).
    /// Consecutive Fock states differ in exactly one qubit.
    /// </summary>
    let internal grayCodeBasisMap (n : int) = n ^^^ (n >>> 1)

    /// <summary>
    /// Embed a d×d operator matrix into a 2^q × 2^q matrix using a basis mapping.
    /// </summary>
    /// <param name="matrix">The d×d operator matrix.</param>
    /// <param name="numQubits">Number of qubits q (2^q ≥ d).</param>
    /// <param name="basisMap">Function mapping Fock state index to qubit basis state index.</param>
    let internal embedMatrix (matrix : Complex[,]) (numQubits : int) (basisMap : int -> int) : Complex[,] =
        let d = Array2D.length1 matrix
        let size = 1 <<< numQubits
        let result = Array2D.create size size Complex.Zero
        for m in 0 .. d - 1 do
            for n in 0 .. d - 1 do
                result.[basisMap m, basisMap n] <- matrix.[m, n]
        result

    // ═════════════════════════════════════════════
    //  Encoding implementations
    // ═════════════════════════════════════════════

    /// <summary>
    /// Generic matrix-based bosonic encoding.
    /// Builds the operator matrix, embeds via basis mapping, and decomposes into Pauli strings.
    /// </summary>
    let private matrixBosonTerms
        (basisMap : int -> int)
        (qubitsPerMode : int -> int)
        (op : LadderOperatorUnit)
        (j : uint32)
        (numModes : uint32)
        (d : uint32)
        : PauliRegisterSequence =
        match op with
        | Identity -> PauliRegisterSequence ()
        | _ when d <= 1u -> PauliRegisterSequence ()
        | _ when j >= numModes -> PauliRegisterSequence ()
        | _ ->
            let di = int d
            let qpm = qubitsPerMode di
            let totalQubits = int numModes * qpm
            let offset = int j * qpm
            let matrix =
                match op with
                | Raise -> bosonicCreationMatrix di
                | Lower -> bosonicAnnihilationMatrix di
                | _ -> failwith "unreachable"
            let embedded = embedMatrix matrix qpm basisMap
            let terms = decomposeIntoPaulis embedded qpm
            terms
            |> Array.map (fun (coeff, paulis) ->
                let ops = Array.create totalQubits Pauli.I
                for k in 0 .. paulis.Length - 1 do
                    ops.[offset + k] <- paulis.[k]
                PauliRegister(ops, coeff))
            |> PauliRegisterSequence

    /// <summary>
    /// Unary (one-hot) bosonic encoding.
    /// </summary>
    /// <remarks>
    /// <para>Uses d qubits per bosonic mode. Fock state |n⟩ is mapped to the
    /// qubit state with exactly one qubit (qubit n) in |1⟩.</para>
    /// <para>The creation operator is decomposed algebraically as:
    /// b† = Σₙ √(n+1) σ⁺_{n+1} σ⁻_n, giving weight-2 Pauli terms.</para>
    /// <para>Total Pauli terms per operator: 4(d−1).</para>
    /// <para>Maximum Pauli weight: 2.</para>
    /// </remarks>
    let unaryBosonTerms (op : LadderOperatorUnit) (j : uint32) (numModes : uint32) (d : uint32) : PauliRegisterSequence =
        match op with
        | Identity -> PauliRegisterSequence ()
        | _ when d <= 1u -> PauliRegisterSequence ()
        | _ when j >= numModes -> PauliRegisterSequence ()
        | _ ->
            let di = int d
            let totalQubits = int numModes * di
            let offset = int j * di
            [|
                for n in 0 .. di - 2 do
                    let amp = sqrt (float (n + 1))
                    let q0 = offset + n       // qubit for Fock state n
                    let q1 = offset + n + 1   // qubit for Fock state n+1

                    // b† : σ⁺_{q1} ⊗ σ⁻_{q0}
                    //    = ¼(X_{q0}X_{q1} - iX_{q0}Y_{q1} + iY_{q0}X_{q1} + Y_{q0}Y_{q1})
                    // b  : σ⁻_{q1} ⊗ σ⁺_{q0}
                    //    = ¼(X_{q0}X_{q1} + iX_{q0}Y_{q1} - iY_{q0}X_{q1} + Y_{q0}Y_{q1})
                    let xySign, yxSign =
                        match op with
                        | Raise -> (Complex(0., -1.), Complex(0.,  1.))
                        | Lower -> (Complex(0.,  1.), Complex(0., -1.))
                        | _     -> failwith "unreachable"

                    let quarter = amp / 4.0

                    // XX term
                    let xxOps = Array.create totalQubits Pauli.I
                    xxOps.[q0] <- Pauli.X; xxOps.[q1] <- Pauli.X
                    yield PauliRegister(xxOps, Complex(quarter, 0.))

                    // YY term
                    let yyOps = Array.create totalQubits Pauli.I
                    yyOps.[q0] <- Pauli.Y; yyOps.[q1] <- Pauli.Y
                    yield PauliRegister(yyOps, Complex(quarter, 0.))

                    // XY term (X at q0, Y at q1)
                    let xyOps = Array.create totalQubits Pauli.I
                    xyOps.[q0] <- Pauli.X; xyOps.[q1] <- Pauli.Y
                    yield PauliRegister(xyOps, Complex(xySign.Real * quarter, xySign.Imaginary * quarter))

                    // YX term (Y at q0, X at q1)
                    let yxOps = Array.create totalQubits Pauli.I
                    yxOps.[q0] <- Pauli.Y; yxOps.[q1] <- Pauli.X
                    yield PauliRegister(yxOps, Complex(yxSign.Real * quarter, yxSign.Imaginary * quarter))
            |]
            |> PauliRegisterSequence

    /// <summary>
    /// Standard binary bosonic encoding.
    /// </summary>
    /// <remarks>
    /// <para>Uses ⌈log₂ d⌉ qubits per bosonic mode. Fock state |n⟩ maps to the
    /// standard binary representation of n.</para>
    /// <para>The operator matrix is decomposed into Pauli strings via
    /// O = Σ_P (Tr(PO) / 2^q) P.</para>
    /// <para>Maximum Pauli weight: ⌈log₂ d⌉.</para>
    /// </remarks>
    let binaryBosonTerms : BosonicEncoderFn =
        matrixBosonTerms binaryBasisMap ceilLog2

    /// <summary>
    /// Gray code bosonic encoding.
    /// </summary>
    /// <remarks>
    /// <para>Uses ⌈log₂ d⌉ qubits per bosonic mode. Fock state |n⟩ maps to
    /// the reflected Gray code G(n) = n ⊕ (n ≫ 1).</para>
    /// <para>Consecutive Fock states differ in exactly one qubit, reducing
    /// the average Pauli weight of transition operators.</para>
    /// <para>Maximum Pauli weight: ⌈log₂ d⌉.</para>
    /// </remarks>
    let grayCodeBosonTerms : BosonicEncoderFn =
        matrixBosonTerms grayCodeBasisMap ceilLog2

    // ═════════════════════════════════════════════
    //  Qubit-count helpers
    // ═════════════════════════════════════════════

    /// <summary>
    /// Number of qubits used by the unary encoding for a given cutoff d.
    /// </summary>
    let unaryQubitsPerMode (d : int) = d

    /// <summary>
    /// Number of qubits used by the binary or Gray code encoding for a given cutoff d.
    /// </summary>
    let binaryQubitsPerMode (d : int) = ceilLog2 d
