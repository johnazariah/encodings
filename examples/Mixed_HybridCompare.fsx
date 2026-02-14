// Mixed_HybridCompare.fsx — Advanced hybrid comparison workflow
//
// Canonicalize mixed boson+fermion expressions, encode only fermionic blocks,
// and compare Jordan-Wigner vs Bravyi-Kitaev outputs while keeping bosons symbolic.

#r "../src/Encodings/bin/Debug/net10.0/Encodings.dll"

open System
open System.Numerics
open Encodings

let nQubits = 8u

let candidate : S<IxOp<uint32, SectorLadderOperatorUnit>> =
    P<IxOp<uint32, SectorLadderOperatorUnit>>.Apply [|
        boson Lower 100u
        fermion Lower 3u
        fermion Raise 1u
        boson Raise 100u
        fermion Lower 2u
        fermion Raise 4u
    |]
    |> S<IxOp<uint32, SectorLadderOperatorUnit>>.Apply

let encodeFermionProductWith
    (encoder : LadderOperatorUnit -> uint32 -> uint32 -> PauliRegisterSequence)
    (n : uint32)
    (units : IxOp<uint32, LadderOperatorUnit>[]) =
    let identity = PauliRegisterSequence [| PauliRegister(n) |]
    units
    |> Array.fold (fun acc unit -> acc * (encoder unit.Op unit.Index n)) identity

let scaleSequence (factor : Complex) (sequence : PauliRegisterSequence) =
    sequence.DistributeCoefficient.SummandTerms
    |> Array.map (fun term -> term.ResetPhase(term.Coefficient * factor))
    |> PauliRegisterSequence

let stringifyBosons (units : IxOp<uint32, SectorLadderOperatorUnit>[]) =
    if units.Length = 0 then
        "<none>"
    else
        units
        |> Array.map (fun u -> sprintf "(%O, %u)" u.Op u.Index)
        |> String.concat " "

printfn "Input mixed expression:\n  %O" candidate

match constructMixedNormalOrdered candidate with
| None ->
    printfn "No canonical expression produced"
| Some canonical ->
    printfn "\nCanonical mixed expression:\n  %O" canonical

    canonical.ProductTerms.Value
    |> Array.iteri (fun i term ->
        let reduced = term.Reduce.Value

        let fermionUnits =
            reduced.Units
            |> Array.choose (fun c ->
                if c.Item.Op.Sector = Fermionic then
                    Some (IxOp<uint32, LadderOperatorUnit>.Apply(c.Item.Index, c.Item.Op.Operator))
                else None)
            |> Array.filter (fun u -> u.Op <> Identity)

        let bosonUnits =
            reduced.Units
            |> Array.choose (fun c ->
                if c.Item.Op.Sector = Bosonic then Some c.Item else None)

        let jw =
            encodeFermionProductWith jordanWignerTerms nQubits fermionUnits
            |> scaleSequence reduced.Coeff

        let bk =
            encodeFermionProductWith bravyiKitaevTerms nQubits fermionUnits
            |> scaleSequence reduced.Coeff

        printfn "\nterm[%d]" i
        printfn "  coeff: %O" reduced.Coeff
        printfn "  fermions: %A" (fermionUnits |> Array.map (fun u -> (u.Op, u.Index)))
        printfn "  bosons:   %s" (stringifyBosons bosonUnits)
        printfn "  JW terms: %d" jw.SummandTerms.Length
        printfn "  BK terms: %d" bk.SummandTerms.Length
        printfn "  JW: %O" jw
        printfn "  BK: %O" bk)
