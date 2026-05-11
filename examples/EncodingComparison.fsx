#!/usr/bin/env dotnet fsi
/// ═══════════════════════════════════════════════════════════════════════
///  FockMap — Encoding Comparison Demo
///
///  Loads real molecular Hamiltonians from FCIDUMP files (PySCF / Psi4)
///  and compares all six built-in fermion-to-qubit encodings across
///  multiple cost metrics relevant to near-term and fault-tolerant
///  quantum simulation.
///
///  Usage:
///    dotnet fsi examples/EncodingComparison.fsx
///
///  Or with a custom FCIDUMP:
///    dotnet fsi examples/EncodingComparison.fsx path/to/your.fcidump
/// ═══════════════════════════════════════════════════════════════════════

#r "../src/Encodings/bin/Debug/net10.0/Encodings.dll"

open System
open System.IO
open System.Numerics
open Encodings
open Encodings.Hamiltonian
open Encodings.CostAnalysis
open Encodings.Optimization
open Encodings.Trotterization
open Encodings.Tapering
open Encodings.Fcidump

// ── Formatting Helpers ──────────────────────────────────────────────────

let private divider width = String('═', width)
let private thinDivider width = String('─', width)

let private centerText (width : int) (text : string) =
    let pad = max 0 (width - text.Length)
    let left = pad / 2
    String(' ', left) + text

let private formatFloat (decimals : int) (v : float) =
    v.ToString($"F{decimals}")

// ── Analysis for a Single Molecule ──────────────────────────────────────

type EncodingReport =
    { Name : string
      Hamiltonian : PauliRegisterSequence
      TermCount : int
      QubitCount : int
      LambdaNorm : float
      MaxWeight : int
      MeanWeight : float
      TotalWeight : int
      CnotCount : int
      CnotCount2 : int }

let analyzeEncoding (factory : string -> Complex option) (n : uint32) (name : string) (encoder : EncoderFn) =
    let hamiltonian = computeHamiltonianWithParallel encoder factory n
    let costs = hamiltonianCosts hamiltonian
    let step1 = firstOrderTrotter 1.0 hamiltonian
    let step2 = secondOrderTrotter 1.0 hamiltonian
    let cnots1 = trotterCnotCount step1
    let cnots2 = trotterCnotCount step2
    { Name = name
      Hamiltonian = hamiltonian
      TermCount = costs.TermCount
      QubitCount = costs.QubitCount
      LambdaNorm = costs.LambdaNorm
      MaxWeight = costs.MaxPauliWeight
      MeanWeight = costs.MeanPauliWeight
      TotalWeight = costs.TotalPauliWeight
      CnotCount = cnots1
      CnotCount2 = cnots2 }

let analyzeAllEncodings (factory : string -> Complex option) (n : uint32) =
    [| ("Jordan-Wigner",    JordanWigner.jordanWignerTerms)
       ("Bravyi-Kitaev",    BravyiKitaev.bravyiKitaevTerms)
       ("Parity",           MajoranaEncoding.parityTerms)
       ("Balanced Binary",  TreeEncoding.balancedBinaryTreeTerms)
       ("Balanced Ternary", TreeEncoding.ternaryTreeTerms)
       ("Vlasov",           TreeEncoding.vlasovTreeTerms) |]
    |> Array.map (fun (name, encoder) -> analyzeEncoding factory n name encoder)

// ── Pretty Printing ─────────────────────────────────────────────────────

let printMoleculeReport (title : string) (subtitle : string) (reports : EncodingReport[]) =
    let w = 102

    printfn ""
    printfn "  ╔%s╗" (divider (w - 4))
    printfn "  ║%s║" (centerText (w - 4) title)
    printfn "  ║%s║" (centerText (w - 4) subtitle)
    printfn "  ╠%s╣" (divider (w - 4))

    // Column headers
    printfn "  ║ %-18s │ %6s │ %5s │ %8s │ %6s │ %6s │ %9s │ %9s ║"
        "Encoding" "Qubits" "Terms" "λ-norm" "MaxWt" "MeanWt" "CNOT(S1)" "CNOT(S2)"
    printfn "  ╠%s╣" (thinDivider (w - 4))

    // Find the best (lowest) for each metric to mark with ★
    let minLambda = reports |> Array.minBy (fun r -> r.LambdaNorm) |> fun r -> r.LambdaNorm
    let minMaxWt  = reports |> Array.minBy (fun r -> r.MaxWeight) |> fun r -> r.MaxWeight
    let minCnot   = reports |> Array.minBy (fun r -> r.CnotCount) |> fun r -> r.CnotCount

    for r in reports do
        let markers =
            [ if r.LambdaNorm = minLambda then "λ"
              if r.MaxWeight = minMaxWt then "W"
              if r.CnotCount = minCnot then "C" ]
            |> String.concat ""
        let star = if markers.Length > 0 then sprintf " ★%s" markers else ""
        printfn "  ║ %-18s │ %6d │ %5d │ %8s │ %6d │ %6s │ %9d │ %9d%s"
            r.Name r.QubitCount r.TermCount
            (formatFloat 4 r.LambdaNorm) r.MaxWeight
            (formatFloat 2 r.MeanWeight) r.CnotCount r.CnotCount2
            (star.PadRight(3))

    printfn "  ╠%s╣" (thinDivider (w - 4))

    // Tapering analysis (reuse the already-computed JW Hamiltonian)
    let jwH = reports.[0].Hamiltonian
    let symCount = z2SymmetryCount jwH
    let taperingResult = taper defaultTaperingOptions jwH
    printfn "  ║ %-96s ║" (sprintf "Z₂ symmetries: %d detected → %d → %d qubits after tapering"
        symCount taperingResult.OriginalQubitCount taperingResult.TaperedQubitCount)

    // Qubitization resource estimate
    let qcosts = qubitizationCosts jwH
    let queries001 = qubitizationQueries qcosts 1.0 0.001
    printfn "  ║ %-96s ║" (sprintf "Qubitization:  λ = %.4f  │  Ancillas: %d  │  Total: %d qubits"
        qcosts.Lambda qcosts.SelectAncillas qcosts.TotalQubits)
    printfn "  ║ %-96s ║" (sprintf "QPE queries (ε=0.001, t=1):  %s"
        (queries001.ToString("N0")))

    // Trotter–Qubitization crossover analysis
    printfn "  ╠%s╣" (thinDivider (w - 4))
    printfn "  ║ %-96s ║" "Trotter vs Qubitization  (first-order, t=1)"
    printfn "  ║ %-96s ║" "  Ratio = Trotter CNOTs / QSP queries  (>1 ⇒ qubitization wins)"
    printfn "  ║ %-96s ║" ""

    let bestRatio = reports |> Array.minBy (fun r -> trotterQubitizationRatio r.Hamiltonian 1.0)
    let worstRatio = reports |> Array.maxBy (fun r -> trotterQubitizationRatio r.Hamiltonian 1.0)
    for r in reports do
        let ratio = trotterQubitizationRatio r.Hamiltonian 1.0
        let marker = if r.Name = bestRatio.Name then "  ← closest" else ""
        printfn "  ║ %-96s ║" (sprintf "  %-18s  ratio = %10s%s" r.Name (formatFloat 1 ratio) marker)

    let bestR = trotterQubitizationRatio bestRatio.Hamiltonian 1.0
    let worstR = trotterQubitizationRatio worstRatio.Hamiltonian 1.0
    let improvement = (1.0 - bestR / worstR) * 100.0
    printfn "  ║ %-96s ║" ""
    printfn "  ║ %-96s ║" (sprintf "  Best encoding reduces Trotter/QSP ratio by %.0f%% vs worst" improvement)

    // Post-tapering comparison
    printfn "  ╠%s╣" (thinDivider (w - 4))
    printfn "  ║ %-96s ║" "Post-Tapering Comparison  (diagonal Z₂, +1 sector)"
    printfn "  ║ %-96s ║" ""

    let taperResults =
        reports |> Array.map (fun r ->
            let tr = taperAllDiagonalZ2WithPositiveSector r.Hamiltonian
            let costs = hamiltonianCosts tr.Hamiltonian
            let step = firstOrderTrotter 1.0 tr.Hamiltonian
            let cnots = trotterCnotCount step
            (r.Name, tr.TaperedQubitCount, costs.TermCount, costs.LambdaNorm, costs.MaxPauliWeight, cnots))

    let minTapCnot = taperResults |> Array.minBy (fun (_, _, _, _, _, c) -> c) |> fun (_, _, _, _, _, c) -> c
    for (name, nq, nt, lam, mw, cnots) in taperResults do
        let mark = if cnots = minTapCnot then " ★" else ""
        printfn "  ║ %-96s ║" (sprintf "  %-18s  %2dq │ %5d terms │ λ=%8s │ MaxWt=%2d │ CNOT=%7d%s"
            name nq nt (formatFloat 4 lam) mw cnots mark)

    printfn "  ╠%s╣" (thinDivider (w - 4))
    printfn "  ║ %s ║" (("★λ = best λ-norm   ★W = best max weight   ★C = best CNOT count (S1)").PadRight(w - 6))
    printfn "  ╚%s╝" (divider (w - 4))
    printfn ""


// ── Main ────────────────────────────────────────────────────────────────

let runMolecule (name : string) (fcidumpPath : string) =
    let content = File.ReadAllText fcidumpPath
    let data = parse content
    let (factory, nso) = toSpinOrbitalFactory data
    let n = uint32 nso
    let subtitle = sprintf "%d spatial orbs → %d spin-orbitals │ %d electrons │ E_nuc = %.6f Ha"
                       data.Norb nso data.Nelec data.CoreEnergy

    let reports = analyzeAllEncodings factory n
    printMoleculeReport name subtitle reports


printfn ""
printfn "  ═══════════════════════════════════════════════════════════════════"
printfn "   FockMap — Fermion-to-Qubit Encoding Comparison"
printfn "   Symbolic operator algebra • Exact phase tracking • 6 encodings"
printfn "  ═══════════════════════════════════════════════════════════════════"

let examplesDir = Path.Combine(__SOURCE_DIRECTORY__)

let molecules =
    match fsi.CommandLineArgs |> Array.tail with
    | [||] ->
        // Default: run STO-3G benchmark set
        [| ("H₂ / STO-3G",   Path.Combine(examplesDir, "H2_STO-3G.fcidump"))
           ("LiH / STO-3G",  Path.Combine(examplesDir, "LiH_STO-3G.fcidump"))
           ("BeH₂ / STO-3G", Path.Combine(examplesDir, "BeH2_STO-3G.fcidump"))
           ("H₂O / STO-3G",  Path.Combine(examplesDir, "H2O_STO-3G.fcidump"))
           ("NH₃ / STO-3G",  Path.Combine(examplesDir, "NH3_STO-3G.fcidump")) |]
        |> Array.filter (fun (_, p) -> File.Exists p)
    | paths ->
        paths |> Array.map (fun p ->
            let name = Path.GetFileNameWithoutExtension(p).Replace("_", "/")
            (name, p))

if molecules.Length = 0 then
    printfn "\n  No FCIDUMP files found. Generate them with:"
    printfn "    python3 -c \"from pyscf import gto, scf, tools; ...\""
    printfn "  Or pass a path:  dotnet fsi examples/EncodingComparison.fsx my.fcidump"
else
    let sw = System.Diagnostics.Stopwatch.StartNew()
    for (name, path) in molecules do
        printfn "  ▸ Loading %s ..." name
        runMolecule name path
    sw.Stop()
    printfn "  Total time: %.1f seconds" sw.Elapsed.TotalSeconds
