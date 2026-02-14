namespace Encodings

/// <summary>
/// Defines the fundamental Pauli algebra types: Phase and Pauli operators.
/// </summary>
/// <remarks>
/// This module provides exact (no floating-point error) representations of:
/// <list type="bullet">
///   <item><description>Phase factors: the four roots of unity {+1, -1, +i, -i}</description></item>
///   <item><description>Single-qubit Pauli operators: I, X, Y, Z</description></item>
/// </list>
/// These are the building blocks for representing quantum operators in the Pauli basis.
/// </remarks>
[<AutoOpen>]
module IndexedPauli =
    /// <summary>
    /// The global phase factor attached to a Pauli product.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When multiplying Pauli matrices, the result is another Pauli matrix times a phase
    /// in {+1, -1, +i, -i}. For example: X · Y = iZ (phase = +i).
    /// </para>
    /// <para>
    /// Phases are tracked as a discriminated union enabling exact (no floating-point error)
    /// phase arithmetic. The multiplication table forms a cyclic group Z₄:
    /// </para>
    /// <code>
    ///   P1 * P1 = P1    Pi * Pi = M1
    ///   P1 * M1 = M1    Pi * Mi = P1
    ///   M1 * M1 = P1    Mi * Mi = M1
    /// </code>
    /// </remarks>
    type Phase =
        /// <summary>Phase factor +1 (identity phase).</summary>
        | P1    // +1
        /// <summary>Phase factor -1.</summary>
        | M1    // -1
        /// <summary>Phase factor +i (positive imaginary).</summary>
        | Pi    // +i
        /// <summary>Phase factor -i (negative imaginary).</summary>
        | Mi    // -i
    with
        /// <summary>
        /// Folds this phase into a complex global phase by multiplication.
        /// </summary>
        /// <param name="globalPhase">The complex number to multiply by this phase.</param>
        /// <returns>The product of globalPhase and this phase factor.</returns>
        /// <remarks>
        /// Converts the symbolic phase to its numeric effect:
        /// P1 → globalPhase, M1 → -globalPhase, Pi → i·globalPhase, Mi → -i·globalPhase.
        /// </remarks>
        member this.FoldIntoGlobalPhase (globalPhase : System.Numerics.Complex) =
            match this with
            | Pi -> globalPhase.TimesI
            | Mi -> -(globalPhase.TimesI)
            | P1 -> globalPhase
            | M1 -> -globalPhase

        /// <summary>
        /// Multiplies two phase factors exactly.
        /// </summary>
        /// <param name="l">The left phase factor.</param>
        /// <param name="r">The right phase factor.</param>
        /// <returns>The product phase factor in {P1, M1, Pi, Mi}.</returns>
        /// <remarks>
        /// Phase multiplication follows the cyclic group Z₄ structure:
        /// i² = -1, i · (-i) = 1, (-1)² = 1, etc.
        /// </remarks>
        static member (*) (l : Phase, r : Phase) =
            match (l, r) with
            | (P1, s)
            | (s, P1)  -> s
            | (M1, M1) -> P1
            | (M1, Pi)
            | (Pi, M1) -> Mi
            | (M1, Mi)
            | (Mi, M1) -> Pi
            | (Pi, Pi) -> M1
            | (Pi, Mi)
            | (Mi, Pi) -> P1
            | (Mi, Mi) -> M1

        /// <summary>
        /// Gets whether this phase has a positive real or imaginary part.
        /// </summary>
        /// <returns><c>true</c> for P1 (+1) and Pi (+i); <c>false</c> for M1 (-1) and Mi (-i).</returns>
        member this.IsPositive =
            match this with
            | P1 | Pi -> true
            | M1 | Mi -> false

        /// <summary>
        /// Gets whether this phase is purely imaginary.
        /// </summary>
        /// <returns><c>true</c> for Pi (+i) and Mi (-i); <c>false</c> for P1 (+1) and M1 (-1).</returns>
        member this.IsComplex =
            match this with
            | P1 | M1 -> false
            | Pi | Mi -> true

    /// <summary>
    /// Single-qubit Pauli operators forming the Pauli group.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The four single-qubit Pauli matrices form a basis for all 2×2 Hermitian matrices:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>I = identity matrix [[1,0],[0,1]]</description></item>
    ///   <item><description>X = bit-flip (NOT) [[0,1],[1,0]]</description></item>
    ///   <item><description>Y = bit+phase flip [[0,-i],[i,0]]</description></item>
    ///   <item><description>Z = phase-flip [[1,0],[0,-1]]</description></item>
    /// </list>
    /// <para>
    /// The multiplication table is cyclic: XY = iZ, YZ = iX, ZX = iY.
    /// Two distinct non-identity Paulis always anti-commute: XY = -YX.
    /// Each Pauli squares to identity: X² = Y² = Z² = I.
    /// </para>
    /// </remarks>
    type Pauli =
        /// <summary>Identity operator (no operation).</summary>
        | I
        /// <summary>Pauli-X (bit-flip, NOT gate, σₓ).</summary>
        | X
        /// <summary>Pauli-Y (bit and phase flip, σᵧ).</summary>
        | Y
        /// <summary>Pauli-Z (phase-flip, σᵤ).</summary>
        | Z
    with
        /// <summary>
        /// Gets the identity Pauli operator.
        /// </summary>
        static member Identity = I

        /// <summary>
        /// Parses a Pauli operator from its string representation.
        /// </summary>
        /// <param name="s">A string "I", "X", "Y", or "Z".</param>
        /// <returns>Some Pauli if valid; None otherwise.</returns>
        static member Apply = function
            | "I" -> Some I
            | "X" -> Some X
            | "Y" -> Some Y
            | "Z" -> Some Z
            | _ -> None

        /// <summary>
        /// Parses a Pauli operator from a single character.
        /// </summary>
        /// <param name="c">A character 'I', 'X', 'Y', or 'Z'.</param>
        /// <returns>Some Pauli if valid; None otherwise.</returns>
        static member FromChar = function
            | 'I' -> Some I
            | 'X' -> Some X
            | 'Y' -> Some Y
            | 'Z' -> Some Z
            | _ -> None

        /// <summary>
        /// Returns the string representation of this Pauli operator.
        /// </summary>
        /// <returns>"I", "X", "Y", or "Z".</returns>
        override this.ToString() =
            match this with
            | I -> "I"
            | X -> "X"
            | Y -> "Y"
            | Z -> "Z"

        /// <summary>
        /// Multiplies two Pauli operators, returning the result operator and phase.
        /// </summary>
        /// <param name="l">The left Pauli operator.</param>
        /// <param name="r">The right Pauli operator.</param>
        /// <returns>
        /// A tuple (resultOp, phase) where resultOp is the resulting Pauli operator
        /// and phase is the phase factor from the multiplication.
        /// </returns>
        /// <remarks>
        /// <para>Pauli multiplication follows these rules:</para>
        /// <list type="bullet">
        ///   <item><description>Any Pauli times I equals itself: P · I = I · P = P</description></item>
        ///   <item><description>Any Pauli squared is identity: X² = Y² = Z² = I</description></item>
        ///   <item><description>Cyclic products: XY = iZ, YZ = iX, ZX = iY</description></item>
        ///   <item><description>Anti-commutation: YX = -iZ, ZY = -iX, XZ = -iY</description></item>
        /// </list>
        /// </remarks>
        static member (*) (l, r) =
            match (l, r) with
            | (I, s)
            | (s, I) -> (s, P1)
            | (X, X)
            | (Y, Y)
            | (Z, Z) -> (I, P1)
            | (X, Y) -> (Z, Pi)
            | (Y, X) -> (Z, Mi)
            | (Y, Z) -> (X, Pi)
            | (Z, Y) -> (X, Mi)
            | (Z, X) -> (Y, Pi)
            | (X, Z) -> (Y, Mi)

        /// <summary>
        /// Parses an indexed Pauli operator from string format "P_n" (e.g., "X_0", "Z_3").
        /// </summary>
        /// <returns>A parser function for indexed Pauli strings.</returns>
        static member FromString =
            tryParseIxOpUint32 Pauli.Apply
