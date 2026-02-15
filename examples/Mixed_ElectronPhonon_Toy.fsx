// Mixed_ElectronPhonon_Toy.fsx — Toy electron-phonon style symbolic workflow
//
// Constructs a simple mixed Hamiltonian-like expression with fermionic hopping
// and bosonic number terms, then canonicalizes to mixed normal form.

#r "../src/Encodings/bin/Debug/net8.0/Encodings.dll"

open Encodings
open System.Numerics

let hopping : P<IxOp<uint32, SectorLadderOperatorUnit>> =
    P<IxOp<uint32, SectorLadderOperatorUnit>>.Apply [|
        fermion Raise 1u
        fermion Lower 2u
    |]

let bosonNumber : P<IxOp<uint32, SectorLadderOperatorUnit>> =
    P<IxOp<uint32, SectorLadderOperatorUnit>>.Apply [|
        boson Raise 100u
        boson Lower 100u
    |]

let coupling = hopping * bosonNumber
let candidate = S<IxOp<uint32, SectorLadderOperatorUnit>>.Apply(Complex(0.7, 0.0), coupling)

printfn "Input mixed term: %O" candidate

match constructMixedNormalOrdered candidate with
| Some canonical ->
    printfn "\nCanonical mixed expression: %O" canonical
    canonical.ProductTerms.Value
    |> Array.iteri (fun i t ->
        let r = t.Reduce.Value
        printfn "  term[%d] coeff=%O | %O" i r.Coeff r)
| None ->
    printfn "No canonical form produced"
