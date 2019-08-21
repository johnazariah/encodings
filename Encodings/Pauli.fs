namespace Encodings
[<AutoOpen>]
module Pauli =
    open System

    type Operator =
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
    | P1
    | M1
    | Pi
    | Mi
    with
        override this.ToString() =
            match this with
            | P1  -> "+"
            | M1  -> "-"
            | Pi  -> "+ i"
            | Mi  -> "- i"

        static member Unity = P1
        static member (*) (l : Phase, r : Phase) =
            match (l, r) with
            | (P1, s)
            | (s, P1) -> s
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

    type Pauli = {
        Op : Operator
        Ph : Phase
        Cf : Complex
    }
    with
        member this.Normalized =
            { Op = this.Op; Ph = P1; Cf = this.Cf * this.Ph }

        override this.ToString() =
            sprintf "(%A%A)" this.Normalized.Cf this.Normalized.Op

        static member Unity = { Op = Operator.Identity; Ph = Phase.Unity; Cf = Complex.Unity }

        // https://www.sciencedirect.com/topics/engineering/pauli-operator
        static member (*) (l : Pauli, r : Pauli) =
            match (l.Op, r.Op) with
            | (I, s)
            | (s, I) -> { Op = s; Ph = l.Ph * r.Ph; Cf = l.Cf * r.Cf}
            | (X, X)
            | (Y, Y)
            | (Z, Z) -> { Op = I; Ph = l.Ph * r.Ph; Cf = l.Cf * r.Cf}
            | (X, Y) -> { Op = Z; Ph = Pi * l.Ph * r.Ph; Cf = l.Cf * r.Cf}
            | (Y, X) -> { Op = Z; Ph = Mi * l.Ph * r.Ph; Cf = l.Cf * r.Cf}
            | (Y, Z) -> { Op = X; Ph = Pi * l.Ph * r.Ph; Cf = l.Cf * r.Cf}
            | (Z, Y) -> { Op = X; Ph = Mi * l.Ph * r.Ph; Cf = l.Cf * r.Cf}
            | (Z, X) -> { Op = Y; Ph = Pi * l.Ph * r.Ph; Cf = l.Cf * r.Cf}
            | (X, Z) -> { Op = Y; Ph = Mi * l.Ph * r.Ph; Cf = l.Cf * r.Cf}

    and OperatorRegister private (operators : Operator[], isLittleEndian, globalPhase) =
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
                let operators = Array.create<Operator> (int n) Operator.Identity
                new OperatorRegister (operators, true, coefficient |> Option.defaultValue Complex.Unity)

            static member BigEndianRegister (n : uint32, ?coefficient) =
                let operators = Array.create<Operator> (int n) Operator.Identity
                new OperatorRegister (operators, false, coefficient |> Option.defaultValue Complex.Unity)

            static member private FromList (coefficient, ops : Operator list) =
                new OperatorRegister (ops |> List.rev |> List.toArray, false, coefficient)

            static member FromString (ops : string) =
                ops
                |> Seq.choose (Operator.FromChar)
                |> Seq.toArray
                |> (fun rg -> new OperatorRegister (rg, false, Complex.Unity))

            member __.GlobalPhase = globalPhase
            member __.Size : int = operators.Length
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
                let result = Array.create<Pauli> (int n) Pauli.Unity
                for i in 0..(n-1) do
                    result.[i] <-
                        match (l.[i], r.[i]) with
                        | None, None -> Pauli.Unity
                        | Some x, None
                        | None, Some x -> { Pauli.Unity with Op = x }
                        | Some x, Some y -> { Pauli.Unity with Op = x } * { Pauli.Unity with Op = y }
                result
                |> Array.map (fun p -> (p.Normalized.Cf, p.Normalized.Op))
                |> Array.fold (fun (coeff, operators) (item_coeff, item_op) -> (coeff * item_coeff, item_op :: operators)) (Complex.Unity, [])
                |> OperatorRegister.FromList
        end


