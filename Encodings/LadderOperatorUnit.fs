namespace Encodings

[<AutoOpen>]
module LadderOperator =
    open System.Numerics

    type LadderOperatorUnit =
        | Raise of uint32
        | Lower of uint32
        static member FromStringAndIndex op index =
            match op with
            | "u" -> Raise index |> Some
            | "d" -> Lower index |> Some
            | _ -> None
        interface IIndexedOperatorUnit with
            member this.Index =
                match this with
                | Raise x -> x
                | Lower x -> x
            member this.AsString =
                lazy
                    match this with
                    | Raise n -> sprintf "(u, %u)" n
                    | Lower n -> sprintf "(d, %u)" n

        override this.ToString() =
            (this :> IIndexedOperatorUnit).AsString.Value

        static member FromString =
            TryCreateIndexedOperatorUnit LadderOperatorUnit.FromStringAndIndex

        /// Convert a tuple of the form (isRaisingOperator : bool, index : uint32) to a LadderOperatorUnit
        static member FromTuple : (bool * uint32 -> LadderOperatorUnit) = function
            | true,  index -> Raise index
            | false, index -> Lower index
