// Test the Jacobi eigenvalue solver on the known N_e=2 block
open System

// Jacobi eigenvalue solver (copied from MatrixVerification.fsx)
let jacobiEigenvalues (size : int) (a : float[,]) =
    let mat = Array2D.copy a
    let maxIter = 200 * size * size
    let mutable iter = 0
    let mutable converged = false

    while not converged && iter < maxIter do
        let mutable maxVal = 0.0
        let mutable p = 0
        let mutable q = 1
        for i in 0 .. size-1 do
            for j in i+1 .. size-1 do
                if abs mat.[i, j] > maxVal then
                    maxVal <- abs mat.[i, j]
                    p <- i
                    q <- j

        if maxVal < 1e-14 then
            converged <- true
        else
            // Classic Jacobi: solve tan(2θ) = 2a_{pq}/(a_{pp} - a_{qq})
            // Use the stable formula: t = sgn(τ) / (|τ| + √(1 + τ²))
            // where τ = (a_{qq} - a_{pp}) / (2 a_{pq})
            let apq = mat.[p, q]
            let app = mat.[p, p]
            let aqq = mat.[q, q]
            let tau = (aqq - app) / (2.0 * apq)
            let t =
                if tau >= 0.0 then
                    1.0 / (tau + sqrt(1.0 + tau * tau))
                else
                    -1.0 / (-tau + sqrt(1.0 + tau * tau))
            let c = 1.0 / sqrt(1.0 + t * t)
            let s = t * c
            mat.[p, p] <- app - t * apq
            mat.[q, q] <- aqq + t * apq
            mat.[p, q] <- 0.0
            mat.[q, p] <- 0.0
            for r in 0 .. size-1 do
                if r <> p && r <> q then
                    let arp = mat.[r, p]
                    let arq = mat.[r, q]
                    mat.[r, p] <- c*arp - s*arq
                    mat.[p, r] <- mat.[r, p]
                    mat.[r, q] <- s*arp + c*arq
                    mat.[q, r] <- mat.[r, q]
            iter <- iter + 1

    printfn "  Jacobi: %d iterations, converged=%b" iter converged
    Array.init size (fun idx -> mat.[idx, idx]) |> Array.sort

// Test 1: 2x2 with known eigenvalues
printfn "=== Test 1: 2x2 ==="
let m1 = Array2D.init 2 2 (fun i j ->
    match i, j with
    | 0, 0 -> -1.8381893797015365
    | 0, 1 | 1, 0 -> 0.69757824688281870
    | 1, 1 -> -0.2463940993919009
    | _ -> 0.0)
let ev1 = jacobiEigenvalues 2 m1
for ev in ev1 do printfn "  %+.16f" ev
printfn "  Expected: -2.1006246097, +0.0160411306"
printfn ""

// Test 2: 6x6 N_e=2 block
printfn "=== Test 2: 6x6 N_e=2 block ==="
let m2 = Array2D.zeroCreate 6 6
m2.[0,0] <- -1.8381893797
m2.[0,5] <- 0.6975782469
m2.[5,0] <- 0.6975782469
m2.[1,1] <- -1.7621792965
m2.[2,2] <- -1.0646010496
m2.[2,3] <- -0.6975782469
m2.[3,2] <- -0.6975782469
m2.[3,3] <- -1.0646010496
m2.[4,4] <- -1.7621792965
m2.[5,5] <- -0.2463940994
let ev2 = jacobiEigenvalues 6 m2
for ev in ev2 do printfn "  %+.16f" ev
printfn "  Expected: -2.1006, -1.7622 (x3), -0.3670, +0.0160"
