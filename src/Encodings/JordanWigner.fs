namespace Encodings

/// <summary>
/// The Jordan-Wigner fermion-to-qubit encoding (1928).
/// </summary>
/// <remarks>
/// Maps fermionic operators to qubit Pauli operators by inserting a chain
/// of Z operators to track the parity of all preceding modes:
///
///   cⱼ → Z₀ ⊗ Z₁ ⊗ ... ⊗ Zⱼ₋₁ ⊗ Xⱼ ⊗ Iⱼ₊₁ ⊗ ... ⊗ Iₙ₋₁
///   dⱼ → Z₀ ⊗ Z₁ ⊗ ... ⊗ Zⱼ₋₁ ⊗ Yⱼ ⊗ Iⱼ₊₁ ⊗ ... ⊗ Iₙ₋₁
///
/// where cⱼ = a†ⱼ + aⱼ and dⱼ = i(a†ⱼ − aⱼ) are Majorana operators.
///
/// <para>
/// <b>Qubit ordering.</b> In this library the position of a Pauli in a
/// <see cref="T:Encodings.PauliRegister"/> string maps directly to the qubit index:
/// mode/qubit 0 is the <b>leftmost</b> character. So cⱼ produces the string
/// "Z…ZX I…I" — a Z on qubits 0…j−1, an X on qubit j, and I on the rest — read
/// left to right. For example a†₂ (n=4) = ½·ZZXI − (i/2)·ZZYI, and the number
/// operator nⱼ = a†ⱼaⱼ = ½(I − Zⱼ) carries its Z at string position j
/// (n₀ = ½·IIII − ½·ZIII, n₃ = ½·IIII − ½·IIIZ).
/// </para>
/// <para>
/// <b>Occupation-number basis.</b> Occupation integers weight mode j by 2ʲ
/// (mode 0 is the least-significant bit): the H₂ Hartree–Fock state with modes 0
/// and 1 occupied is the integer 3 = 0b0011. To evaluate a FockMap Pauli string in
/// this occupation basis so that mode j maps to bit 2ʲ, <b>reverse the string</b>
/// before forming the Kronecker product (mode 0, the leftmost character, becomes
/// the least-significant bit). That same reversal converts a FockMap label to a
/// Qiskit-style Pauli-label string, where mode 0 is the <b>rightmost</b> character:
/// FockMap "ZXII" (Z₀X₁I₂I₃) ≡ Qiskit-style label "IIXZ". (Prefer this concrete
/// rule over "big/little-endian": FockMap mode 0 is leftmost; occupation integers
/// and Qiskit-style Pauli labels put mode 0 rightmost / at bit 2⁰. OpenQASM has no
/// Pauli-label string — it addresses qubits as q[i] explicitly.)
/// </para>
///
/// The Z-chain grows linearly with mode index j, giving O(n) worst-case weight.
/// For O(log n) alternatives, see BravyiKitaev and TreeEncoding.
///
/// Reference: P. Jordan and E. Wigner, "Über das Paulische Äquivalenzverbot,"
/// Z. Phys. 47, 631 (1928).
/// </remarks>
/// <seealso cref="T:Encodings.EncodingScheme">Index-set scheme abstraction shared by JW, BK, and Parity encodings.</seealso>
/// <seealso cref="T:Encodings.FenwickTree">Fenwick tree used by the Bravyi-Kitaev encoding for O(log n) weight.</seealso>
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
    /// <example>
    /// Encode a†₀ in a 2-qubit register:
    /// <code>
    /// jordanWignerTerms Raise 0u 2u
    /// // X term: "XI" with coefficient  0.5
    /// // Y term: "YI" with coefficient -0.5i
    /// </code>
    /// </example>
    /// <seealso cref="T:Encodings.EncodingScheme">For the equivalent index-set formulation of Jordan-Wigner.</seealso>
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
