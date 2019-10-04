﻿namespace Encodings

open System

[<AutoOpen>]
module PrettyPrint =
    open System.Numerics

    let prettyPrintPhase (this : Complex) =
        if this = Complex.Zero then "(0)"
        else if this = Complex.One then ""
        else if this = Complex.MinusOne then " -"
        else if this = Complex.ImaginaryOne then "( i)"
        else if this = Complex.MinusI then "(-i)"
        else if this.Imaginary = 0.  then sprintf "(%O)"   this.Real
        else if this.Imaginary = 1.  then sprintf "(%O i)" this.Real
        else if this.Imaginary = -1. then sprintf "(-%Oi)" this.Real
        else sprintf "%O" this

    let prettyPrintC (this : C<'unit>) =
        let itemString = sprintf "%O" this.Thunk
        if this.Coeff = Complex.Zero then
            ""
        else if this.Coeff = Complex.One then
            sprintf "%s" itemString
        else if this.Coeff = Complex.MinusOne then
            sprintf "(- %s)" itemString
        else if this.Coeff = Complex.ImaginaryOne then
            sprintf "(i %s)" itemString
        else if this.Coeff = Complex.MinusI then
            sprintf "(-i %s)" itemString
        else if this.Coeff.Imaginary = 0. then
            sprintf "(%O %s)" this.Coeff.Real itemString
        else if this.Coeff.Imaginary = 1. then
            sprintf "(%Oi %s)" this.Coeff.Real itemString
        else if this.Coeff.Imaginary = -1. then
            sprintf "(-%Oi %s)" this.Coeff.Real itemString
        else
            sprintf "%O" this

    let inline prettyPrintIxOp< ^op when ^op : equality> (this : IxOp<uint32, ^op>) =
        sprintf "(%O, %i)" this.Op this.Index

    let inline prettyPrintCIxOp< ^op when ^op : equality> (this : CIxOp<uint32, ^op>) =
        prettyPrintIxOp< ^op > this.Unapply.Thunk

    let inline prettyPrintPIxOp< ^op when ^op : equality> (this : PIxOp<uint32, ^op>) =
        this.IndexedOps
        |> Array.map prettyPrintIxOp< ^op >
        |> (fun rg -> System.String.Join (" | ", rg))
        |> sprintf "[%s]"

    let inline prettyPrintSIxOp< ^op when ^op : equality> (this : SIxOp<uint32, ^op>) =
        this.Terms
        |> Seq.map (prettyPrintPIxOp)
        |> (fun rg -> System.String.Join ("; ", rg))
        |> sprintf "{%s}"

    let inline prettyPrintRegister< ^op
                        when ^op : (static member Identity : ^op)
                        and  ^op : (static member Multiply : ^op -> ^op -> C< ^op >)
                        and ^op : equality>
        (this : R< ^op >) =
        this.Units
        |> Seq.map (sprintf "%O")
        |> (fun rg -> System.String.Join ("", rg))
        |> sprintf "%s"


    let inline prettyPrintSR< ^op
                        when ^op : (static member Identity : ^op)
                        and  ^op : (static member Multiply : ^op -> ^op -> C< ^op >)
                        and ^op : equality>
        (this : SR< ^op>) =
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

        let buildString result (term : R< ^op >) =
            let termStr = term.Signature

            if String.IsNullOrWhiteSpace result then
                let phasePrefix = toPhasePrefix term.Coeff
                sprintf "%s%s" phasePrefix termStr
            else
                let conjoiningPhase = toPhaseConjunction term.Coeff
                sprintf "%s%s%s" result conjoiningPhase termStr

        this.Terms
        |> Seq.fold buildString ""