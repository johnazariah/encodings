namespace Encodings

/// <summary>
/// The Jordan-Wigner fermion-to-qubit encoding (1928).
/// </summary>
/// <remarks>
/// Maps fermionic operators to qubit Pauli operators by inserting a chain
/// of Z operators to track the parity of all preceding modes:
///
///   cⱼ → Xⱼ ⊗ Zⱼ₋₁ ⊗ ... ⊗ Z₀
///   dⱼ → Yⱼ ⊗ Zⱼ₋₁ ⊗ ... ⊗ Z₀
///
/// where cⱼ = a†ⱼ + aⱼ and dⱼ = i(a†ⱼ − aⱼ) are Majorana operators.
///
/// The Z-chain grows linearly with mode index j, giving O(n) worst-case weight.
/// For O(log n) alternatives, see BravyiKitaev and TreeEncoding.
///
/// Reference: P. Jordan and E. Wigner, "Über das Paulische Äquivalenzverbot,"
/// Z. Phys. 47, 631 (1928).
/// </remarks>
[<AutoOpen>]
module JordanWigner =
    open System.Numerics

    /// <summary>
    /// Compute the Jordan-Wigner encoding of a single ladder operator.
    /// </summary>
    /// <param name="op">The ladder operator (Raise, Lower, or Identity).</param>
    /// <param name="j">The qubit/mode index for this operator.</param>
    /// <param name="n">The total number of qubits in the register.</param>
    /// <returns>A sequence of Pauli register terms representing the encoded operator.</returns>
    /// <remarks>
    /// For creation (Raise) and annihilation (Lower) operators, produces X and Y terms
    /// with Z-chains on all preceding qubits. The coefficients encode the ±½ and ±i/2
    /// factors from the Majorana decomposition. Returns empty sequence for Identity or
    /// if j >= n.
    /// </remarks>
    let jordanWignerTerms (op : LadderOperatorUnit) (j : uint32) (n : uint32) : PauliRegisterSequence =
        let jw_components () =
            let _zs_ = if j = 0u then "" else System.String ('Z', (int j))
            let _is_ = if j = n  then "" else System.String ('I', int (n - j - 1u))
            let jw_x = sprintf "%sX%s" _zs_ _is_
            let jw_y = sprintf "%sY%s" _zs_ _is_
            [|jw_x; jw_y|]

        match op with
        | Raise ->
            if (j >= n) then
                PauliRegisterSequence ()
            else
                [|Complex(0.5, 0.); (Complex(0., -0.5))|]
                |> Array.zip (jw_components ())
                |> Array.map (PauliRegister)
                |> PauliRegisterSequence
        | Lower ->
            if (j >= n) then
                PauliRegisterSequence ()
            else
                [|Complex(0.5, 0.); (Complex(0., 0.5))|]
                |> Array.zip (jw_components ())
                |> Array.map (PauliRegister)
                |> PauliRegisterSequence
        | Identity ->
            PauliRegisterSequence ()
