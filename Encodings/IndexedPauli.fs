namespace Encodings

[<AutoOpen>]
module IndexedPauli =
    /// Phase factors arising from Pauli multiplication.
    type Phase =
        | P1    // +1
        | M1    // -1
        | Pi    // +i
        | Mi    // -i
    with
        member this.FoldIntoGlobalPhase (globalPhase : System.Numerics.Complex) =
            match this with
            | Pi -> globalPhase.TimesI
            | Mi -> -(globalPhase.TimesI)
            | P1 -> globalPhase
            | M1 -> -globalPhase

        static member (*) (l : Phase, r : Phase) =
            match (l, r) with
            | (P1, s)
            | (s, P1)  -> s
            | (M1, M1) -> P1
            | (M1, Pi)
            | (Pi, M1) -> Mi
            | (M1, Mi)
            | (Mi, M1) -> Pi
            | (Pi, Pi) -> M1
            | (Pi, Mi)
            | (Mi, Pi) -> P1
            | (Mi, Mi) -> M1

        member this.IsPositive =
            match this with
            | P1 | Pi -> true
            | M1 | Mi -> false

        member this.IsComplex =
            match this with
            | P1 | M1 -> false
            | Pi | Mi -> true

    /// The four single-qubit Pauli operators.
    type Pauli =
        | I
        | X
        | Y
        | Z
    with
        static member Identity = I

        static member Apply = function
            | "I" -> Some I
            | "X" -> Some X
            | "Y" -> Some Y
            | "Z" -> Some Z
            | _ -> None

        static member FromChar = function
            | 'I' -> Some I
            | 'X' -> Some X
            | 'Y' -> Some Y
            | 'Z' -> Some Z
            | _ -> None

        override this.ToString() =
            match this with
            | I -> "I"
            | X -> "X"
            | Y -> "Y"
            | Z -> "Z"

        /// Pauli multiplication: returns (resultOp, phase).
        static member (*) (l, r) =
            match (l, r) with
            | (I, s)
            | (s, I) -> (s, P1)
            | (X, X)
            | (Y, Y)
            | (Z, Z) -> (I, P1)
            | (X, Y) -> (Z, Pi)
            | (Y, X) -> (Z, Mi)
            | (Y, Z) -> (X, Pi)
            | (Z, Y) -> (X, Mi)
            | (Z, X) -> (Y, Pi)
            | (X, Z) -> (Y, Mi)

        static member FromString =
            tryParseIxOpUint32 Pauli.Apply
