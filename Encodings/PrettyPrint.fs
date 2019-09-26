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

    let prettyPrintIxOp (this : IxOp<uint32, _>) =
        sprintf "(%O, %i)" this.Op this.Index

    let prettyPrintCIxOp (this : CIxOp<uint32,_>) =
        prettyPrintIxOp this.Unapply.Item

    let prettyPrintPIxOp (this : PIxOp<uint32,_>) =
        this.IndexedOps
        |> Array.map prettyPrintIxOp
        |> (fun rg -> System.String.Join (" | ", rg))
        |> sprintf "[%s]"

    let prettyPrintSIxOp (this : SIxOp<uint32,_>) =
        this.Terms
        |> Seq.map (fun t -> prettyPrintPIxOp t.Item)
        |> (fun rg -> System.String.Join ("; ", rg))
        |> sprintf "{%s}"

