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

    let inline prettyPrintIxOp< ^op when ^op : equality> (this : IxOp<uint32, ^op>) =
        sprintf "(%O, %i)" this.Op this.Index

    let inline prettyPrintCIxOp< ^op when ^op : equality> (this : CIxOp<uint32, ^op>) =
        prettyPrintIxOp< ^op > this.Unapply.Item

    let inline prettyPrintPIxOp< ^op when ^op : equality> (this : PIxOp<uint32, ^op>) =
        this.IndexedOps
        |> Array.map prettyPrintIxOp< ^op >
        |> (fun rg -> System.String.Join (" | ", rg))
        |> sprintf "[%s]"

    let inline prettyPrintSIxOp< ^op when ^op : equality> (this : SIxOp< uint32, ^op>) =
        this.Terms
        |> Seq.map (fun t -> prettyPrintPIxOp< ^op > t.Item)
        |> (fun rg -> System.String.Join ("; ", rg))
        |> sprintf "{%s}"

