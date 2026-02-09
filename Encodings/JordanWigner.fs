namespace Encodings

[<AutoOpen>]
module JordanWigner =
    open System.Numerics

    /// Compute the Jordan-Wigner encoding of a single ladder operator
    /// at qubit index j within a register of n qubits.
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
