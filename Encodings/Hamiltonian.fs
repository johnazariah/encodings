namespace Encodings

[<AutoOpen>]
module Hamiltonian =
    open System.Numerics

    type HamiltonianTerm =
    | Overlap  of OverlapTerm
    | Exchange of ExchangeTerm

    and OverlapTerm  = {i : uint32; j : uint32}
    with
        member private this.ToJordanWignerTerms n coeff =
            let crTerms = (Raise this.i).ToJordanWignerTerms n
            let anTerms = (Lower this.j).ToJordanWignerTerms n
            let result = (crTerms * anTerms)
            result.Coefficient <- result.Coefficient * coeff
            result

        static member internal ComputeTerms coefficientFactory n =
            [|
                for i in 0u .. n do
                    for j in 0u .. n do
                        let key = sprintf "%u%u" i j
                        match coefficientFactory key with
                        | Some hij ->
                            let term = {OverlapTerm.i = i; OverlapTerm.j = j}
                            yield term.ToJordanWignerTerms n hij
                        | _ -> ()
            |]
            |> PauliRegisterSequence

    and ExchangeTerm = {i : uint32; j : uint32; k : uint32; l : uint32}
    with
        member private this.ToJordanWignerTerms n coeff termCoefficient =
            let criTerms = (Raise this.i).ToJordanWignerTerms n
            let crjTerms = (Raise this.j).ToJordanWignerTerms n
            let ankTerms = (Lower this.k).ToJordanWignerTerms n
            let anlTerms = (Lower this.l).ToJordanWignerTerms n
            let result = (criTerms * crjTerms * ankTerms * anlTerms)
            result.Coefficient <- result.Coefficient * coeff
            result

        static member internal ComputeTerms coefficientFactory n =
            let termCoefficient = Complex (0.5, 0.)

            [|
                for i in 0u .. n do
                    for j in 0u .. n do
                        for k in 0u .. n do
                            for l in 0u .. n do
                                let key = sprintf "%u%u%u%u" i j k l
                                match coefficientFactory key with
                                | Some hijkl ->
                                    let term = {
                                        ExchangeTerm.i = i
                                        ExchangeTerm.j = j
                                        ExchangeTerm.k = k
                                        ExchangeTerm.l = l
                                    }
                                    yield term.ToJordanWignerTerms n hijkl termCoefficient
                                | _ -> ()
            |]
            |> PauliRegisterSequence


    let computeHamiltonian coefficientFactory n =
        [|
            yield OverlapTerm.ComputeTerms  coefficientFactory n
            yield ExchangeTerm.ComputeTerms coefficientFactory n
        |]
        |> PauliRegisterSequence
