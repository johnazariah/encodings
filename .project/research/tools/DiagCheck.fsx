/// Quick diagnostic: verify E_HF manually
#r "../../src/Encodings/bin/Debug/net8.0/Encodings.dll"

open System.Numerics
open Encodings

let h00 = -1.2563390730032498
let g0000 = 0.6744887663049631

// RHF for H₂: E = 2*h[0,0] + [00|00]
// In spin-orbital form with 2 electrons in 0α and 0β:
// E = h[0α,0α] + h[0β,0β] + ½ Σ (⟨pq|pq⟩ - ⟨pq|qp⟩)
// The only non-vanishing antisymmetrized integral for {0α,0β} is ⟨0α 0β|0α 0β⟩
// because exchange ⟨0α 0β|0β 0α⟩ = 0 (spin mismatch δ(α,β)=0)
// So: E = 2*h00 + ½*2*g0000 = 2*h00 + g0000
let ehf_spatial = 2.0 * h00 + g0000
printfn "E_HF (spatial formula, electronic) = %+.16f" ehf_spatial
printfn "E_HF + V_nn                        = %+.16f" (ehf_spatial + 0.7151043390810812)
printfn ""

// Now let's see what the FCI diagonal element gives
// ⟨0011| H_full |0011⟩ where 0011 means bits 0,1 occupied (spin-orbitals 0α, 0β)
// This should equal E_HF for a single determinant

// One-body contribution: Σ_{p occupied} h[p,p] = h[0α,0α] + h[0β,0β] = 2*h00
let oneBody = 2.0 * h00
printfn "One-body: %+.16f" oneBody

// Two-body contribution: ½ Σ_{p,q,r,s} ⟨pq|rs⟩ ⟨0011|a†p a†q as ar|0011⟩
// For diagonal, only terms where applying operators returns |0011⟩ contribute.
// This means we need a†p a†q as ar |0011⟩ = |0011⟩
// So {r,s} must be a subset of occupied orbitals, and {p,q} must fill exactly those.
// Since occupied = {0,1}: (p,q,r,s) must have {p,q} = {0,1} and the annihilations
// of r,s from |0011⟩ must leave a state that creating p,q returns to |0011⟩.
//
// Possible: a†0 a†1 a1 a0 |0011⟩ and a†1 a†0 a0 a1 |0011⟩
// and a†0 a†1 a0 a1 |0011⟩ and a†1 a†0 a1 a0 |0011⟩
// Note: a†p a†q as ar means right-to-left: ar first, then as, then a†q, then a†p

// Let's just compute by brute force:
let nSpin = 4
let occupied s j = (s >>> j) &&& 1 = 1

let parityBelow s j =
    let mutable count = 0
    for k in 0 .. j-1 do
        if occupied s k then count <- count + 1
    if count % 2 = 0 then 1.0 else -1.0

let create j s =
    if occupied s j then None
    else Some (s ||| (1 <<< j), parityBelow s j)

let annihilate j s =
    if not (occupied s j) then None
    else Some (s ^^^ (1 <<< j), parityBelow s j)

let h1_spatial = Array2D.init 2 2 (fun p q ->
    if p = q then [| -1.2563390730032498; -0.4718960244306283 |].[p] else 0.0)

let h2_spatial = Array4D.init 2 2 2 2 (fun p q r s ->
    match (p,q,r,s) with
    | (0,0,0,0) -> 0.6744887663049631
    | (1,1,1,1) -> 0.6973979494693556
    | (0,0,1,1) | (1,1,0,0) -> 0.6636340478615040
    | (0,1,1,0) | (1,0,0,1) | (0,1,0,1) | (1,0,1,0) -> 0.6975782468828187
    | _ -> 0.0)

let h1_spin =
    [| for p in 0 .. nSpin-1 do
           for q in 0 .. nSpin-1 do
               if p%2 = q%2 then
                   let v = h1_spatial.[p/2, q/2]
                   if abs v > 1e-15 then yield (p, q, v) |]

let h2_spin =
    [| for p in 0 .. nSpin-1 do
           for q in 0 .. nSpin-1 do
               for r in 0 .. nSpin-1 do
                   for s in 0 .. nSpin-1 do
                       if p%2 = r%2 && q%2 = s%2 then
                           let v = h2_spatial.[p/2, r/2, q/2, s/2]
                           if abs v > 1e-15 then yield (p, q, r, s, v) |]

let hfState = 3  // bits 0 and 1 occupied = |0α, 0β⟩

// Compute ⟨HF|H|HF⟩ explicitly
let mutable diagE = 0.0

// One-body
for (p, q, v) in h1_spin do
    match annihilate q hfState with
    | Some (s1, ph1) ->
        match create p s1 with
        | Some (s2, ph2) when s2 = hfState ->
            let contrib = v * ph1 * ph2
            printfn "  1-body h[%d,%d] = %+.10f, phase = %.0f, contrib = %+.10f" p q v (ph1*ph2) contrib
            diagE <- diagE + contrib
        | _ -> ()
    | None -> ()

printfn "One-body total: %+.16f" diagE

let mutable twoBodyDiag = 0.0
// Two-body
for (p, q, r, s, v) in h2_spin do
    let coeff = 0.5 * v
    match annihilate r hfState with
    | Some (s1, ph1) ->
        match annihilate s s1 with
        | Some (s2, ph2) ->
            match create q s2 with
            | Some (s3, ph3) ->
                match create p s3 with
                | Some (s4, ph4) when s4 = hfState ->
                    let phase = ph1 * ph2 * ph3 * ph4
                    let contrib = coeff * phase
                    printfn "  2-body [%d%d%d%d] v=%.10f, ½v=%.10f, phase=%.0f, contrib=%+.10f" p q r s v coeff phase contrib
                    twoBodyDiag <- twoBodyDiag + contrib
                | _ -> ()
            | _ -> ()
        | _ -> ()
    | None -> ()

printfn "Two-body total: %+.16f" twoBodyDiag
printfn ""
printfn "Total diagonal (electronic): %+.16f" (diagE + twoBodyDiag)
printfn ""

// Build the full 16×16 FCI matrix and extract N_e=2 block
let dim = 1 <<< nSpin
let fci = Array2D.zeroCreate<float> dim dim

for (p, q, v) in h1_spin do
    for st in 0 .. dim-1 do
        match annihilate q st with
        | Some (s1, ph1) ->
            match create p s1 with
            | Some (s2, ph2) -> fci.[s2, st] <- fci.[s2, st] + v * ph1 * ph2
            | None -> ()
        | None -> ()

for (p, q, r, s, v) in h2_spin do
    let coeff = 0.5 * v
    for st in 0 .. dim-1 do
        match annihilate r st with
        | Some (s1, ph1) ->
            match annihilate s s1 with
            | Some (s2, ph2) ->
                match create q s2 with
                | Some (s3, ph3) ->
                    match create p s3 with
                    | Some (s4, ph4) ->
                        fci.[s4, st] <- fci.[s4, st] + coeff * ph1 * ph2 * ph3 * ph4
                    | None -> ()
                | None -> ()
            | None -> ()
        | None -> ()

printfn "Full 16x16 FCI diagonal:"
for i in 0 .. dim-1 do
    let nPart = [0..nSpin-1] |> List.filter (fun j -> occupied i j) |> List.length
    let bits = System.Convert.ToString(i, 2).PadLeft(nSpin, '0') |> Seq.rev |> System.String.Concat
    printfn "  |%s> (N=%d): %+.10f" bits nPart fci.[i,i]

// N_e=2 states
let ne2 = [| for s in 0 .. dim-1 do
                let n = [0..nSpin-1] |> List.filter (fun j -> occupied s j) |> List.length
                if n = 2 then yield s |]
printfn ""
printfn "N_e=2 block (6x6):"
for i in ne2 do
    for j in ne2 do
        printf "%+.8f  " fci.[i,j]
    printfn ""

// Check: is the block Hermitian?
printfn ""
printfn "N_e=2 block diagonal:"
for i in ne2 do
    let bits = System.Convert.ToString(i, 2).PadLeft(nSpin, '0') |> Seq.rev |> System.String.Concat
    printfn "  |%s> : %+.16f" bits fci.[i,i]
