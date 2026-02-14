// Mixed_NormalOrdering.fsx — Sector-aware mixed boson/fermion normal ordering
//
// Demonstrates canonical block ordering (fermions left, bosons right)
// and sector-specific CAR/CCR rewrites via constructMixedNormalOrdered.

#r "../src/Encodings/bin/Debug/net10.0/Encodings.dll"

open Encodings

let show title (expr : S<IxOp<uint32, SectorLadderOperatorUnit>>) =
    printfn "\n=== %s ===" title
    printfn "Input:    %O" expr
    let canonical = constructMixedNormalOrdered expr
    match canonical with
    | Some c ->
        printfn "Canonical:%O" c
        c.ProductTerms.Value
        |> Array.iteri (fun i t ->
            let reduced = t.Reduce.Value
            printfn "  term[%d] coeff=%O blockOrdered=%b" i reduced.Coeff (isSectorBlockOrdered reduced))
    | None ->
        printfn "Canonical: <none>"

let mixed1 : S<IxOp<uint32, SectorLadderOperatorUnit>> =
    P<IxOp<uint32, SectorLadderOperatorUnit>>.Apply [|
        boson Lower 100u
        fermion Lower 3u
        boson Raise 100u
        fermion Raise 1u
    |]
    |> S<IxOp<uint32, SectorLadderOperatorUnit>>.Apply

let mixed2 : S<IxOp<uint32, SectorLadderOperatorUnit>> =
    P<IxOp<uint32, SectorLadderOperatorUnit>>.Apply [|
        fermion Lower 5u
        boson Lower 200u
        fermion Raise 5u
        boson Raise 200u
    |]
    |> S<IxOp<uint32, SectorLadderOperatorUnit>>.Apply

show "Cross-sector block ordering + in-sector CAR/CCR" mixed1
show "Same-index rewrites in both sectors" mixed2
