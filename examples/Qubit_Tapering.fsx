#if INTERACTIVE
#r "../src/Encodings/bin/Debug/net10.0/Encodings.dll"
#endif

open System.Numerics
open Encodings

let hamiltonian =
    [|
        PauliRegister("ZIZI", Complex(0.8, 0.0))
        PauliRegister("ZZII", Complex(-0.4, 0.0))
        PauliRegister("IIZZ", Complex(0.3, 0.0))
        PauliRegister("IZIZ", Complex(0.2, 0.0))
    |]
    |> PauliRegisterSequence

printfn "Original: %s" (hamiltonian.ToString())

let symQubits = diagonalZ2SymmetryQubits hamiltonian
printfn "Diagonal Z2 qubits: %A" symQubits

let tapered = taperDiagonalZ2 [ (1, 1); (3, -1) ] hamiltonian
printfn "Removed qubits: %A" tapered.RemovedQubits
printfn "Tapered  (%d -> %d qubits): %s"
    tapered.OriginalQubitCount
    tapered.TaperedQubitCount
    (tapered.Hamiltonian.ToString())
