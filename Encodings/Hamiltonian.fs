namespace Encodings

[<AutoOpen>]
module Hamiltonian =
    open System.Numerics

    /// A function that encodes a ladder operator into qubit Pauli strings.
    type EncoderFn = LadderOperatorUnit -> uint32 -> uint32 -> PauliRegisterSequence

    type HamiltonianTerm =
    | Overlap  of OverlapTerm
    | Exchange of ExchangeTerm

    and OverlapTerm  = {i : uint32; j : uint32}
    with
        member private this.ToEncodedTerms (encode : EncoderFn) n coeff =
            let product = (encode Raise this.i n) * (encode Lower this.j n)
            product.DistributeCoefficient
            |> fun prs ->
                prs.SummandTerms
                |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))
                |> PauliRegisterSequence

        member private this.ToJordanWignerTerms n coeff =
            this.ToEncodedTerms jordanWignerTerms n coeff

        static member internal ComputeTermsWith (encode : EncoderFn) coefficientFactory n =
            [|
                for i in 0u .. n do
                    for j in 0u .. n do
                        let key = sprintf "%u%u" i j
                        match coefficientFactory key with
                        | Some hij ->
                            let term = {OverlapTerm.i = i; OverlapTerm.j = j}
                            yield term.ToEncodedTerms encode n hij
                        | _ -> ()
            |]
            |> PauliRegisterSequence

        static member internal ComputeTerms coefficientFactory n =
            OverlapTerm.ComputeTermsWith jordanWignerTerms coefficientFactory n

    and ExchangeTerm = {i : uint32; j : uint32; k : uint32; l : uint32}
    with
        member private this.ToEncodedTerms (encode : EncoderFn) n coeff termCoefficient =
            let product =
                (encode Raise this.i n) * (encode Raise this.j n)
                * (encode Lower this.k n) * (encode Lower this.l n)
            product.DistributeCoefficient
            |> fun prs ->
                prs.SummandTerms
                |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coeff))
                |> PauliRegisterSequence

        member private this.ToJordanWignerTerms n coeff termCoefficient =
            this.ToEncodedTerms jordanWignerTerms n coeff termCoefficient

        static member internal ComputeTermsWith (encode : EncoderFn) coefficientFactory n =
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
                                    yield term.ToEncodedTerms encode n hijkl termCoefficient
                                | _ -> ()
            |]
            |> PauliRegisterSequence

        static member internal ComputeTerms coefficientFactory n =
            ExchangeTerm.ComputeTermsWith jordanWignerTerms coefficientFactory n


    /// Compute a qubit Hamiltonian from integral coefficients using Jordan-Wigner.
    let computeHamiltonian coefficientFactory n =
        [|
            yield OverlapTerm.ComputeTerms  coefficientFactory n
            yield ExchangeTerm.ComputeTerms coefficientFactory n
        |]
        |> PauliRegisterSequence

    /// Compute a qubit Hamiltonian from integral coefficients using any encoding.
    let computeHamiltonianWith (encode : EncoderFn) coefficientFactory n =
        [|
            yield OverlapTerm.ComputeTermsWith  encode coefficientFactory n
            yield ExchangeTerm.ComputeTermsWith encode coefficientFactory n
        |]
        |> PauliRegisterSequence
