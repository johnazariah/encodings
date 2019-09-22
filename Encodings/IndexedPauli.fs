namespace Encodings

[<AutoOpen>]
module IndexedPauli =
    type Pauli =
        | I
        | X
        | Y
        | Z
    with
        static member Apply = function
            | "I" -> I |> Some
            | "X" -> X |> Some
            | "Y" -> Y |> Some
            | "Z" -> Z |> Some
            | _ -> None

        member this.AsString =
            lazy
                match this with
                | I -> sprintf "I"
                | X -> sprintf "X"
                | Y -> sprintf "Y"
                | Z -> sprintf "Z"

        override this.ToString() = this.AsString.Value

        static member FromString =
            IndexedOpFromString Pauli.Apply
