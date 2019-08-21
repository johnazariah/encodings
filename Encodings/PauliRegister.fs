namespace Encodings
[<AutoOpen>]
module PauliRegister =
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

    and Phase =
        | P1    // +1
        | M1    // -1
        | Pi    // +i
        | Mi    // -i
    with
        member this.FoldIntoGlobalPhase (globalPhase : Complex) =
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

    and PauliRegister private (operators : Pauli[], globalPhase) =
        class
            let bindAtIndex f = function
            | n when n < 0 -> None
            | n when n >= operators.Length -> None
            | n -> n |> f

            let mapAtIndex f = bindAtIndex (f >> Some)

            new (n : uint32, ?coefficient) =
                let operators = Array.create<Pauli> (int n) Pauli.Identity
                new PauliRegister (operators, coefficient |> Option.defaultValue Complex.One)

            new (ops : string, coefficient) =
                let rg = ops |> Seq.choose (Pauli.FromChar) |> Seq.toArray
                new PauliRegister (rg, coefficient)

            new (ops : Pauli list, coefficient) =
                new PauliRegister (ops |> List.toArray, coefficient)

            member internal __.Operators =
                operators

            member __.GlobalPhase =
                globalPhase

            member __.Size =
                operators.Length

            member __.Item
                with get i =
                    mapAtIndex (fun idx -> operators.[idx]) i
                and set i v =
                    mapAtIndex (fun idx -> do operators.[idx] <- v) i
                    |> ignore

            member __.Signature =
                operators
                |> Array.map (sprintf "%A")
                |> (fun rgstr -> System.String.Join("", rgstr))

            override this.ToString() =
                sprintf "%s%s" (globalPhase.PhasePrefix) (this.Signature)

            static member (*) (l : PauliRegister, r : PauliRegister) =
                let buildOperatorListAndPhase (ops, globalPhase) (op, phase : Phase) =
                    (ops @ [op], phase.FoldIntoGlobalPhase globalPhase)

                let seed = ([], l.GlobalPhase * r.GlobalPhase)

                let combinePauli i =
                    match (l.[i], r.[i]) with
                    | None, None -> (I, P1)
                    | Some x, None
                    | None, Some x -> (x, P1)
                    | Some x, Some y -> x * y
                let n = Math.Max (l.Size, r.Size)
                [|
                    for i in 0..(n-1) do yield (combinePauli i)
                |]
                |> Array.fold buildOperatorListAndPhase seed
                |> PauliRegister
        end