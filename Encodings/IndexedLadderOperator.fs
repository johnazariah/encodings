namespace Encodings

[<AutoOpen>]
module IndexedLadderOperator =
    type LadderOperatorUnit =
        | Raise
        | Lower
    with
        static member Apply = function
            | "u" -> Raise |> Some
            | "d" -> Lower |> Some
            | _ -> None

        member this.AsString =
            match this with
            | Raise -> "u"
            | Lower -> "d"

        override this.ToString() = this.AsString

        static member TryCreateFromString =
            IndexedOperator<LadderOperatorUnit>.TryCreateFromString LadderOperatorUnit.Apply

        static member FromUnit : (bool * uint32 -> IndexedOperator<LadderOperatorUnit>) = function
            | true,  index -> IndexedOperator<LadderOperatorUnit>.Apply Raise index
            | false, index -> IndexedOperator<LadderOperatorUnit>.Apply Lower index

        static member FromTuple : (LadderOperatorUnit * uint32 -> IndexedOperator<LadderOperatorUnit>) =
            uncurry IndexedOperator<LadderOperatorUnit>.Apply
