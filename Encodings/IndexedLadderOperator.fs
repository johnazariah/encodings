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

        static member FromString =
            Ix<_>.TryCreateFromString LadderOperatorUnit.Apply

        //static member FromTuple : (bool * uint32 -> C<I<LadderOperatorUnit>>) = function
        //    | true,  index -> Raise index
        //    | false, index -> Lower index
