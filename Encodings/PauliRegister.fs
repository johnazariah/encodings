namespace Encodings
[<AutoOpen>]
module PauliRegister =
    open System
    open System.Numerics
    open System.Collections.Generic

    type PauliRegister internal (operators : Pauli[], phase) =
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

            new (ops : Pauli seq, coefficient) =
                new PauliRegister (ops |> Seq.toArray, coefficient)

            member internal __.Operators = operators
            member internal __.Size = operators.Length

            member __.ResetPhase (p : Complex) = PauliRegister(operators |> Array.copy, p)

            member __.Coefficient      = phase
            member __.PhasePrefix      = phase.ToPhasePrefix
            member __.PhaseConjunction = phase.ToPhaseConjunction

            /// Get the Pauli operator at the given index.
            member __.Item
                with get i =
                    mapAtIndex (fun idx -> operators.[idx]) i

            /// Return a new PauliRegister with the operator at index i replaced.
            member __.WithOperatorAt (i : int) (op : Pauli) =
                let ops' = operators |> Array.copy
                if i >= 0 && i < ops'.Length then
                    ops'.[i] <- op
                PauliRegister(ops', phase)

            member __.Signature =
                operators
                |> Array.map (sprintf "%A")
                |> (fun rgstr -> System.String.Join("", rgstr))

            member private this.AsString =
                let phasePrefix = this.Coefficient.ToPhasePrefix
                sprintf "%s%s" (phasePrefix) (this.Signature)

            override this.ToString() =
                this.AsString

            static member (*) (l : PauliRegister, r : PauliRegister) =
                let buildOperatorListAndPhase (ops, globalPhase) (op, phase : Phase) =
                    (ops @ [op], phase.FoldIntoGlobalPhase globalPhase)

                let seed = ([], l.Coefficient * r.Coefficient)

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

    and PauliRegisterSequence private (bag : Dictionary<string, PauliRegister>, coefficient : Complex) =
        class
            static member private AddToDictionary (d : Dictionary<string, PauliRegister>) (r : PauliRegister) =
                let key = r.Signature
                match (d.TryGetValue key) with
                | (true, existingValue) ->
                    let newPhase = existingValue.Coefficient + r.Coefficient
                    if (newPhase = Complex.Zero) then
                        ignore <| d.Remove key
                    else
                        d.[key] <- existingValue.ResetPhase newPhase
                | (false, _) ->
                    d.[key] <- r
                d

            new () = PauliRegisterSequence (new Dictionary<string, PauliRegister>(), Complex.One)

            new (registers : PauliRegister[]) =
                let d =
                    registers
                    |> Array.fold PauliRegisterSequence.AddToDictionary (new Dictionary<string, PauliRegister>())
                new PauliRegisterSequence(d, Complex.One)

            new (registerSets : PauliRegisterSequence[]) =
                let addRegisterSetToDictionary result_d (rs : PauliRegisterSequence) =
                    rs.SummandTerms |> Array.fold PauliRegisterSequence.AddToDictionary result_d

                let d =
                    registerSets
                    |> Array.map (fun rs -> rs.DistributeCoefficient)
                    |> Array.fold addRegisterSetToDictionary (new Dictionary<string, PauliRegister>())
                PauliRegisterSequence (d, Complex.One)

            member this.DistributeCoefficient =
                this.SummandTerms
                |> Array.map (fun r -> r.ResetPhase (r.Coefficient * this.Coefficient))
                |> PauliRegisterSequence

            member val AsString =
                let buildString result (term : PauliRegister) =
                    let termStr = term.Signature

                    if String.IsNullOrWhiteSpace result then
                        let phasePrefix = term.PhasePrefix
                        sprintf "%s%s" phasePrefix termStr
                    else
                        let conjoiningPhase = term.PhaseConjunction
                        sprintf "%s%s%s" result conjoiningPhase termStr
                lazy
                    bag
                    |> Seq.sortBy (fun kvp -> kvp.Key)
                    |> Seq.map (fun kvp -> kvp.Value)
                    |> Seq.fold buildString ""

            override this.ToString() = this.AsString.Value

            member __.SummandTerms = bag.Values |> Seq.toArray

            member __.Coefficient = coefficient

            member __.Item
                with get key =
                    bag.TryGetValue key

            static member (*) (l : PauliRegisterSequence, r : PauliRegisterSequence) =
                let (l_normal, r_normal) = (l.DistributeCoefficient, r.DistributeCoefficient)
                [|
                    for lt in l_normal.SummandTerms do
                        for rt in r_normal.SummandTerms do
                            yield (lt * rt)
                |]
                |> PauliRegisterSequence
        end