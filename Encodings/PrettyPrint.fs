namespace Encodings

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

    let inline prettyPrintIxOp< ^op
                        when ^op : equality
                        and  ^op : (static member InIndexOrder    : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> bool)
                        and  ^op : (static member InOperatorOrder : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> bool)
                        and  ^op : (static member ToIndexOrder    : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> C<IxOp<uint32, ^op >[]>[])
                        and  ^op : (static member ToOperatorOrder : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> C<IxOp<uint32, ^op >[]>[])
                        and  ^op : (static member NextIndexLocation : ^op * IxOp<uint32, ^op >[] -> uint32 option )
                        and  ^op : comparison>
        (this : IxOp<uint32, ^op>) = sprintf "(%O, %i)" this.Op this.Index

    let inline prettyPrintCIxOp< ^op
                        when ^op : equality
                        and  ^op : (static member InIndexOrder    : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> bool)
                        and  ^op : (static member InOperatorOrder : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> bool)
                        and  ^op : (static member ToIndexOrder    : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> C<IxOp<uint32, ^op >[]>[])
                        and  ^op : (static member ToOperatorOrder : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> C<IxOp<uint32, ^op >[]>[])
                        and  ^op : (static member NextIndexLocation : ^op * IxOp<uint32, ^op >[] -> uint32 option )
                        and  ^op : comparison>
        (this : CIxOp<uint32, ^op>) = prettyPrintIxOp< ^op > this.Unapply.Thunk

    let inline prettyPrintPIxOp< ^op
                        when ^op : equality
                        and  ^op : (member IsIdentity  : bool)
                        and  ^op : (static member InIndexOrder    : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> bool)
                        and  ^op : (static member InOperatorOrder : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> bool)
                        and  ^op : (static member ToIndexOrder    : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> C<IxOp<uint32, ^op >[]>[])
                        and  ^op : (static member ToOperatorOrder : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> C<IxOp<uint32, ^op >[]>[])
                        and  ^op : (static member NextIndexLocation : ^op * IxOp<uint32, ^op >[] -> uint32 option )
                        and ^op : comparison>
        (this : PIxOp<uint32, ^op>) =
        let isIdentity op = (^op : (member IsIdentity : bool)(op))

        if this.IsZero then
            ""
        else if isIdentity this.IndexedOps.[0].Op then
            sprintf "%s [1]" (prettyPrintPhase this.Coeff)
        else
            this.IndexedOps
            |> Array.filter (fun t -> not (isIdentity t.Op))
            |> Array.map prettyPrintIxOp< ^op >
            |> (fun rg -> System.String.Join (" | ", rg))
            |> sprintf "%s[%s]" (prettyPrintPhase this.Coeff)


    let inline prettyPrintPIxOps< ^op
                        when ^op : equality
                        and  ^op : (member IsIdentity  : bool)
                        and  ^op : (static member InIndexOrder    : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> bool)
                        and  ^op : (static member InOperatorOrder : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> bool)
                        and  ^op : (static member ToIndexOrder    : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> C<IxOp<uint32, ^op >[]>[])
                        and  ^op : (static member ToOperatorOrder : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> C<IxOp<uint32, ^op >[]>[])
                        and  ^op : (static member NextIndexLocation : ^op * IxOp<uint32, ^op >[] -> uint32 option )
                        and  ^op : comparison>
        (this : PIxOp<uint32, ^op>[]) =
        this
        |> Seq.map (prettyPrintPIxOp)
        |> (fun rg -> System.String.Join ("; ", rg))
        |> sprintf "{%s}"

    let inline prettyPrintSIxOp< ^op
                        when ^op : equality
                        and  ^op : (member IsIdentity  : bool)
                        and  ^op : (static member InIndexOrder    : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> bool)
                        and  ^op : (static member InOperatorOrder : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> bool)
                        and  ^op : (static member ToIndexOrder    : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> C<IxOp<uint32, ^op >[]>[])
                        and  ^op : (static member ToOperatorOrder : IxOp<uint32, ^op > -> IxOp<uint32, ^op > -> C<IxOp<uint32, ^op >[]>[])
                        and  ^op : (static member NextIndexLocation : ^op * IxOp<uint32, ^op >[] -> uint32 option )
                        and  ^op : comparison>
        (this : SIxOp<uint32, ^op>) =
        this.Terms
        |> Seq.map (prettyPrintPIxOp)
        |> (fun rg -> System.String.Join ("; ", rg))
        |> sprintf "{%s}"

    let inline prettyPrintSignature< ^op
                        when ^op : (static member Identity : ^op)
                        and  ^op : (static member Multiply : ^op -> ^op -> C< ^op >)
                        and ^op : equality>
        (this : R< ^op >) =
        if this.IsZero then
            ""
        else
            this.Units
            |> Seq.map (sprintf "%O")
            |> (fun rg -> System.String.Join ("", rg))
            |> sprintf "%s"

    let inline prettyPrintRegister< ^op
                        when ^op : (static member Identity : ^op)
                        and  ^op : (static member Multiply : ^op -> ^op -> C< ^op >)
                        and ^op : equality>
        (this : R< ^op >) =
        if this.IsZero then
            ""
        else
            this.Units
            |> Seq.map (sprintf "%O")
            |> (fun rg -> System.String.Join ("", rg))
            |> sprintf "%s%s" (prettyPrintPhase this.Coeff)

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

        if this.IsZero then
            ""
        else
            this.Terms
            |> Seq.fold buildString ""