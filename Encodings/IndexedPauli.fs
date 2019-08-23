namespace Encodings

[<AutoOpen>]
module IndexedPauli =
    open System.Numerics

    type IndexedPauliOperatorUnit =
        | I of uint32
        | X of uint32
        | Y of uint32
        | Z of uint32
        static member FromStringAndIndex (op) (index) =
            match op with
            | "I" -> I index |> Some
            | "X" -> X index |> Some
            | "Y" -> Y index |> Some
            | "Z" -> Z index |> Some
            | _ -> None
        interface IIndexedOperatorUnit with
            member this.Index =
                match this with
                | I x -> x
                | X x -> x
                | Y x -> x
                | Z x -> x
            member this.AsString =
                lazy
                    match this with
                    | I x -> sprintf "(I %u)" x
                    | X x -> sprintf "(X %u)" x
                    | Y x -> sprintf "(Y %u)" x
                    | Z x -> sprintf "(Z %u)" x

        override this.ToString() = (this :> IIndexedOperatorUnit).AsString.Value