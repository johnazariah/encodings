let a = -1.8381893797015365
let d = -0.2463940993919009
let b = 0.69757824688281870
let trace = a + d
let det = a * d - b * b
let disc = sqrt(trace*trace - 4.0*det)
let l1 = (trace - disc) / 2.0
let l2 = (trace + disc) / 2.0
printfn "2x2 eigenvalues: %+.10f, %+.10f" l1 l2

// Also the 2x2 for |0110> and |1001>:
let a2 = -1.0646010495723739
let b2 = -0.69757824688281870
let trace2 = 2.0 * a2
let det2 = a2*a2 - b2*b2
let disc2 = sqrt(trace2*trace2 - 4.0*det2)
printfn "2x2 eigenvalues (cross): %+.10f, %+.10f" ((trace2 - disc2)/2.0) ((trace2 + disc2)/2.0)

// Full N_e=2 eigenvalues should be:
printfn ""
printfn "Expected N_e=2 eigenvalues (sorted):"
let evs = [| l1; l2; -1.7621792964551926; -1.7621792964551926; (trace2-disc2)/2.0; (trace2+disc2)/2.0 |] |> Array.sort
for ev in evs do printfn "  %+.16f" ev
