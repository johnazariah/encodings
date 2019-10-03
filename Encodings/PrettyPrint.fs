﻿namespace Encodings

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
        let itemString = sprintf "%O" this.U
        if this.C = Complex.Zero then
            ""
        else if this.C = Complex.One then
            sprintf "%s" itemString
        else if this.C = Complex.MinusOne then
            sprintf "(- %s)" itemString
        else if this.C = Complex.ImaginaryOne then
            sprintf "(i %s)" itemString
        else if this.C = Complex.MinusI then
            sprintf "(-i %s)" itemString
        else if this.C.Imaginary = 0. then
            sprintf "(%O %s)" this.C.Real itemString
        else if this.C.Imaginary = 1. then
            sprintf "(%Oi %s)" this.C.Real itemString
        else if this.C.Imaginary = -1. then
            sprintf "(-%Oi %s)" this.C.Real itemString
        else
            sprintf "%O" this

    let inline prettyPrintIxOp< ^op when ^op : equality> (this : IxOp<uint32, ^op>) =
        sprintf "(%O, %i)" this.Op this.Index

    let inline prettyPrintCIxOp< ^op when ^op : equality> (this : CIxOp<uint32, ^op>) =
        prettyPrintIxOp< ^op > this.Unapply.U

    let inline prettyPrintPIxOp< ^op when ^op : equality> (this : PIxOp<uint32, ^op>) =
        this.IndexedOps
        |> Array.map prettyPrintIxOp< ^op >
        |> (fun rg -> System.String.Join (" | ", rg))
        |> sprintf "[%s]"

    let inline prettyPrintSIxOp< ^op when ^op : equality> (this : SIxOp<uint32, ^op>) =
        this.Terms
        |> Seq.map (fun t -> prettyPrintPIxOp< ^op > t.U)
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
        this.Terms
        |> Seq.map (fun t -> sprintf "%s%s" (prettyPrintPhase t.C) (prettyPrintRegister< ^op > t.U))
        |> (fun rg -> System.String.Join (" + ", rg))
        |> sprintf "%s"