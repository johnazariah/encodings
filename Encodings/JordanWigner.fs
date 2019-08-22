namespace Encodings

[<AutoOpen>]
module JordanWigner =
    open System.Numerics

    type LadderOperatorUnit
    with
        member this.ToJordanWignerTerms (n : uint32) =
            let jw_components j =
                let _zs_ = if j = 0u then "" else System.String ('Z', (int j))
                let _is_ = if j = n  then "" else System.String ('I', int (n - j - 1u))
                let jw_x = sprintf "%sX%s" _zs_ _is_
                let jw_y = sprintf "%sY%s" _zs_ _is_
                [|jw_x; jw_y|]

            match this with
            | Raise j ->
                if (j >= n) then
                    PauliRegisterSequence ()
                else
                    [|Complex(0.5, 0.); (Complex(0., -0.5))|]
                    |> Array.zip (jw_components j)
                    |> Array.map (PauliRegister)
                    |> PauliRegisterSequence
            | Lower j ->
                if (j >= n) then
                    PauliRegisterSequence ()
                else
                    [|Complex(0.5, 0.); (Complex(0., 0.5))|]
                    |> Array.zip (jw_components j)
                    |> Array.map (PauliRegister)
                    |> PauliRegisterSequence
