namespace Encodings
[<AutoOpen>]
module PauliOperatorRegister =
    open System
    open System.Numerics

    type PauliOperatorRegister private (operators : Pauli[], isLittleEndian, globalPhase) =
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
                new PauliOperatorRegister (operators, true, coefficient |> Option.defaultValue Complex.One)

            static member BigEndianRegister (n : uint32, ?coefficient) =
                let operators = Array.create<Pauli> (int n) Pauli.Identity
                new PauliOperatorRegister (operators, false, coefficient |> Option.defaultValue Complex.One)

            static member FromString (ops : string, coefficient) =
                ops
                |> Seq.choose (Pauli.FromChar)
                |> Seq.toArray
                |> (fun rg -> new PauliOperatorRegister (rg, false, coefficient))

            member internal __.Operators = operators
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
                |> sprintf "%s%s" (globalPhase.PhasePrefix)

            static member (*) (l : PauliOperatorRegister, r : PauliOperatorRegister) =
                let n = Math.Max (l.Size, r.Size)
                [|
                    for i in 0..(n-1) do
                        let p =
                            match (l.[i], r.[i]) with
                            | None, None -> PauliOperator.Unity
                            | Some x, None
                            | None, Some x -> { PauliOperator.Unity with Op = x }
                            | Some x, Some y -> { PauliOperator.Unity with Op = x } * { PauliOperator.Unity with Op = y }
                        yield (p.Ph, p.Op)
                |]
                |> Array.fold (fun (coeff, operators) (item_coeff, item_op) -> (coeff * item_coeff, operators @ [item_op])) (l.GlobalPhase * r.GlobalPhase, [])
                |> (fun (coefficient, ops) ->
                    PauliOperatorRegister (ops |> List.toArray, false, coefficient))
        end