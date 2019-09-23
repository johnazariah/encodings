namespace Encodings

[<AutoOpen>]
module PrettyPrint =
    open System.Numerics

    let prettyPrintC (this : C<'unit>) =
        let itemString = sprintf "%O" this.Item
        if this.Coeff = Complex.Zero then
            ""
        else if this.Coeff = Complex.One then
            sprintf "%s" itemString
        else if this.Coeff = - Complex.One then
            sprintf "(- %s)" itemString
        else if this.Coeff = Complex.ImaginaryOne then
            sprintf "(i %s)" itemString
        else if this.Coeff = - Complex.ImaginaryOne then
            sprintf "(-i %s)" itemString
        else if this.Coeff.Imaginary = 0. then
            sprintf "(%O %s)" this.Coeff.Real itemString
        else if this.Coeff.Imaginary = 1. then
            sprintf "(%Oi %s)" this.Coeff.Real itemString
        else if this.Coeff.Imaginary = -1. then
            sprintf "(-%Oi %s)" this.Coeff.Real itemString
        else
            sprintf "%O" this

    let prettyPrintI (this : IxOp<_,_>) =
        sprintf "(%s, %i)" this.Op this.Index

    let prettyPrintCIxOp (this : CIxOp<_,_>) =
        prettyPrintI this.Unapply.Item

    let prettyPrintPIxOp (this : PIxOp<_,_>) =
        this.Units
        |> Array.map prettyPrintCIxOp
        |> (fun rg -> System.String.Join (" | ", rg))
        |> sprintf "[%s]"

    let prettyPrintSIxOp (this : SIxOp<_,_>) =
        this.Terms
        |> Array.map prettyPrintPIxOp
        |> (fun rg -> System.String.Join ("; ", rg))
        |> sprintf "{%s}"

