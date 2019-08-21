namespace Encodings
[<AutoOpen>]
module Pauli =
    open System
    open System.Numerics

    type Pauli =
    | I
    | X
    | Y
    | Z
    with
        static member Identity = I
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

    type Phase =
    | P1    // +1
    | M1    // -1
    | Pi    // +i
    | Mi    // -i
    with
        static member Unity = P1
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

        static member (*) (l : Complex, r : Phase) =
            match r with
            | Pi -> l.TimesI
            | Mi -> -(l.TimesI)
            | P1 -> l
            | M1 -> -l
        member this.IsPositive =
            match this with
            | P1
            | Pi -> true
            | M1
            | Mi -> false
        member this.IsComplex =
            match this with
            | P1
            | M1 -> false
            | Pi
            | Mi -> true

    type PauliOperator = {
        Op : Pauli
        Ph : Phase
    }
    with
        static member Unity =
            { Op = Pauli.Identity; Ph = Phase.Unity }

        static member (*) (l : PauliOperator, r : PauliOperator) =
            match (l.Op, r.Op) with
            | (I, s)
            | (s, I) -> { Op = s; Ph =      l.Ph * r.Ph }
            | (X, X)
            | (Y, Y)
            | (Z, Z) -> { Op = I; Ph =      l.Ph * r.Ph }
            | (X, Y) -> { Op = Z; Ph = Pi * l.Ph * r.Ph }
            | (Y, X) -> { Op = Z; Ph = Mi * l.Ph * r.Ph }
            | (Y, Z) -> { Op = X; Ph = Pi * l.Ph * r.Ph }
            | (Z, Y) -> { Op = X; Ph = Mi * l.Ph * r.Ph }
            | (Z, X) -> { Op = Y; Ph = Pi * l.Ph * r.Ph }
            | (X, Z) -> { Op = Y; Ph = Mi * l.Ph * r.Ph }


