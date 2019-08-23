namespace Encodings
[<AutoOpen>]
module PauliRegister =
    open System
    open System.Numerics
    open System.Collections.Generic

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

    and PauliRegister internal (operators : Pauli[], phase) =
        class
            let toPhasePrefix (this : Complex) =
                match (this.Real, this.Imaginary) with
                | (+1., 0.) -> ""
                | (-1., 0.) -> " -"
                | (0., +1.) -> "( i) "
                | (0., -1.) -> "(-i) "
                | (r, 0.)   -> sprintf "%A " r
                | (0., i) -> sprintf "(%A i) " i
                | _ -> sprintf "%A" this

            let toPhaseConjunction (this : Complex) =
                match (this.Real, this.Imaginary) with
                | (+1., 0.) -> " + "
                | (-1., 0.) -> " - "
                | (0., +1.) -> " + i "
                | (0., -1.) -> " - i "
                | (r, 0.) when r >= 0. -> sprintf " + %A "     <| Math.Abs r
                | (r, 0.) when r <  0. -> sprintf " - %A "     <| Math.Abs r
                | (0., i) when i >= 0. -> sprintf " + (%A i) " <| Math.Abs i
                | (0., i) when i <  0. -> sprintf " - (%A i) " <| Math.Abs i
                | _ -> sprintf " + %A" this

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

            member internal __.Operators = operators
            member internal __.Size = operators.Length

            member __.ResetPhase (p : Complex) = PauliRegister(operators, p)

            member __.Coefficient      = phase
            member __.PhasePrefix      = phase |> toPhasePrefix
            member __.PhaseConjunction = phase |> toPhaseConjunction

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

            member private this.AsString =
                let phasePrefix = this.Coefficient |> toPhasePrefix
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

            member val Coefficient = coefficient
                with get, set

            member __.Item
                with get key =
                    bag.TryGetValue key
                and set key (newValue : PauliRegister) =
                    PauliRegisterSequence.AddToDictionary bag newValue |> ignore

            static member (*) (l : PauliRegisterSequence, r : PauliRegisterSequence) =
                let (l_normal, r_normal) = (l.DistributeCoefficient, r.DistributeCoefficient)
                [|
                    for lt in l_normal.SummandTerms do
                        for rt in r_normal.SummandTerms do
                            yield (lt * rt)
                |]
                |> PauliRegisterSequence
        end