namespace Encodings

/// <summary>
/// Bosonic ladder-operator expressions and normal-ordering utilities.
/// </summary>
/// <remarks>
/// This module exposes a bosonic specialization of <c>LadderOperatorSumExpr</c>
/// using <see cref="T:Encodings.BosonicAlgebra"/>. It shares the same indexed
/// operator representation as the fermionic path, but uses CCR instead of CAR
/// when constructing normal order.
/// </remarks>
[<AutoOpen>]
module Bosonic =

    /// <summary>
    /// A bosonic ladder-operator sum expression.
    /// </summary>
    type BosonicLadderOperatorSumExpression = LadderOperatorSumExpr<BosonicAlgebra>

    /// <summary>
    /// Constructs a bosonic normal-ordered sum expression.
    /// </summary>
    /// <param name="candidate">The candidate sum expression.</param>
    /// <returns>A normal-ordered bosonic expression if construction succeeds.</returns>
    let constructBosonicNormalOrdered (candidate : S<IxOp<uint32, LadderOperatorUnit>>) =
        BosonicLadderOperatorSumExpression.ConstructNormalOrdered candidate

    /// <summary>
    /// Constructs a bosonic index-ordered sum expression.
    /// </summary>
    /// <param name="candidate">The candidate sum expression.</param>
    /// <returns>An index-ordered bosonic expression if construction succeeds.</returns>
    let constructBosonicIndexOrdered (candidate : S<IxOp<uint32, LadderOperatorUnit>>) =
        BosonicLadderOperatorSumExpression.ConstructIndexOrdered candidate
