// Mixed_HybridPipeline.fsx — Hybrid mixed-system pipeline demo
//
// Demonstrates a practical workflow where:
//   1) mixed boson+fermion expressions are canonicalized,
//   2) fermionic subsectors are encoded to Pauli strings,
//   3) bosonic subsectors remain symbolic for separate truncation/encoding.

#r "../src/Encodings/bin/Debug/net10.0/Encodings.dll"

open System.Numerics
open Encodings

let nQubits = 6u

let candidate : S<IxOp<uint32, SectorLadderOperatorUnit>> =
    P<IxOp<uint32, SectorLadderOperatorUnit>>.Apply [|
        boson Lower 100u
        fermion Lower 2u
        fermion Raise 1u
        boson Raise 100u
    |]
    |> S<IxOp<uint32, SectorLadderOperatorUnit>>.Apply

let encodeFermionProduct (n : uint32) (units : IxOp<uint32, LadderOperatorUnit>[]) =
    let identity = PauliRegisterSequence [| PauliRegister(n) |]
    units
    |> Array.fold (fun acc unit -> acc * encodeOperator jordanWignerScheme unit.Op unit.Index n) identity

let scaleSequence (factor : Complex) (sequence : PauliRegisterSequence) =
    sequence.DistributeCoefficient.SummandTerms
    |> Array.map (fun term -> term.ResetPhase(term.Coefficient * factor))
    |> PauliRegisterSequence

printfn "Input mixed expression: %O" candidate

match constructMixedNormalOrdered candidate with
| None ->
    printfn "No canonical expression produced"
| Some canonical ->
    printfn "\nCanonical mixed expression: %O" canonical

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

        let fermionPauli =
            encodeFermionProduct nQubits fermionUnits
            |> scaleSequence reduced.Coeff

        let bosonSymbolic =
            if bosonUnits.Length = 0 then "<none>"
            else
                bosonUnits
                |> Array.map (fun u -> sprintf "(%O, %u)" u.Op u.Index)
                |> String.concat " "

        printfn "\nterm[%d]" i
        printfn "  fermionic units: %A" (fermionUnits |> Array.map (fun u -> (u.Op, u.Index)))
        printfn "  bosonic units:   %s" bosonSymbolic
        printfn "  encoded fermion Pauli: %O" fermionPauli)
