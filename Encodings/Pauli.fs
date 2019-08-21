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

    type Operator = {
        Op : Pauli
        Ph : Phase
    }
    with
        static member Unity =
            { Op = Pauli.Identity; Ph = Phase.Unity }

        static member (*) (l : Operator, r : Operator) =
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

    and OperatorRegister private (operators : Pauli[], isLittleEndian, globalPhase) =
        class
            let fixupEndian i =
                if isLittleEndian then
                    operators.Length - (i + 1)
                else
                    i

            let bindAtIndex f = function
            | n when n < 0 -> None
            | n when n >= operators.Length -> None
            | n -> n |> fixupEndian |> f

            let mapAtIndex f = bindAtIndex (f >> Some)

            static member LittleEndianRegister (n : uint32, ?coefficient) =
                let operators = Array.create<Pauli> (int n) Pauli.Identity
                new OperatorRegister (operators, true, coefficient |> Option.defaultValue Complex.One)

            static member BigEndianRegister (n : uint32, ?coefficient) =
                let operators = Array.create<Pauli> (int n) Pauli.Identity
                new OperatorRegister (operators, false, coefficient |> Option.defaultValue Complex.One)

            static member internal FromList (coefficient, ops : Pauli list) =
                new OperatorRegister (ops |> List.rev |> List.toArray, false, coefficient)

            static member FromString (ops : string) =
                ops
                |> Seq.choose (Pauli.FromChar)
                |> Seq.toArray
                |> (fun rg -> new OperatorRegister (rg, false, Complex.One))

            member __.GlobalPhase = globalPhase
            member __.Size = operators.Length
            member __.IsLittleEndian = isLittleEndian
            member __.Item
                with get i =
                    mapAtIndex (fun idx -> operators.[idx]) i
                and set i v =
                    mapAtIndex (fun idx -> do operators.[idx] <- v) i
                    |> ignore

            override __.ToString() =
                operators
                |> Array.map (sprintf "%A")
                |> (fun rgstr -> System.String.Join("", rgstr))

            static member (*) (l : OperatorRegister, r : OperatorRegister) =
                let n = Math.Max (l.Size, r.Size)
                let result = Array.create<Operator> (int n) Operator.Unity
                for i in 0..(n-1) do
                    result.[i] <-
                        match (l.[i], r.[i]) with
                        | None, None -> Operator.Unity
                        | Some x, None
                        | None, Some x -> { Operator.Unity with Op = x }
                        | Some x, Some y -> { Operator.Unity with Op = x } * { Operator.Unity with Op = y }
                result
                |> Array.map (fun p -> (p.Ph, p.Op))
                |> Array.fold (fun (coeff, operators) (item_coeff, item_op) -> (coeff * item_coeff, item_op :: operators)) (Complex.One, [])
                |> OperatorRegister.FromList
        end


