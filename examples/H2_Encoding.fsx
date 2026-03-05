/// H₂ Molecule Encoding Example
/// ==============================
/// Encodes the H₂ electronic Hamiltonian using Jordan-Wigner transformation.
/// H₂ in STO-3G basis: 2 spatial orbitals → 4 spin-orbitals → 4 qubits.
///
/// H = Σ_{pq} h_{pq} a†_p a_q  +  ½ Σ_{pqrs} ⟨pq|rs⟩ a†_p a†_q a_s a_r
///
/// Reference: O'Malley et al., Phys. Rev. X 6, 031007 (2016)

#r "../src/Encodings/bin/Debug/net10.0/Encodings.dll"

open System.Numerics
open Encodings

// H₂ at R = 0.7414 Å, STO-3G basis
// Spin-orbitals: 0=σ_g↑, 1=σ_g↓, 2=σ_u↑, 3=σ_u↓
let nSpinOrbitals = 4u

// One-body integrals h_{pq}
let oneBodyIntegrals = Map.ofList [
    ("0,0", Complex(-1.2563390730032498, 0.0))
    ("1,1", Complex(-1.2563390730032498, 0.0))
    ("2,2", Complex(-0.4718960244306283, 0.0))
    ("3,3", Complex(-0.4718960244306283, 0.0))
]

// Two-body integrals ⟨pq|rs⟩
let twoBodyIntegrals = Map.ofList [
    ("0,0,0,0", Complex(0.6744887663049631, 0.0)); ("1,1,1,1", Complex(0.6744887663049631, 0.0))
    ("2,2,2,2", Complex(0.6973979494693556, 0.0)); ("3,3,3,3", Complex(0.6973979494693556, 0.0))
    ("0,0,1,1", Complex(0.6744887663049631, 0.0)); ("1,1,0,0", Complex(0.6744887663049631, 0.0))
    ("0,0,2,2", Complex(0.6636340478615040, 0.0)); ("2,2,0,0", Complex(0.6636340478615040, 0.0))
    ("0,0,3,3", Complex(0.6636340478615040, 0.0)); ("3,3,0,0", Complex(0.6636340478615040, 0.0))
    ("1,1,2,2", Complex(0.6636340478615040, 0.0)); ("2,2,1,1", Complex(0.6636340478615040, 0.0))
    ("1,1,3,3", Complex(0.6636340478615040, 0.0)); ("3,3,1,1", Complex(0.6636340478615040, 0.0))
    ("2,2,3,3", Complex(0.6973979494693556, 0.0)); ("3,3,2,2", Complex(0.6973979494693556, 0.0))
    ("0,2,2,0", Complex(0.6975782468828187, 0.0)); ("2,0,0,2", Complex(0.6975782468828187, 0.0))
    ("1,3,3,1", Complex(0.6975782468828187, 0.0)); ("3,1,1,3", Complex(0.6975782468828187, 0.0))
]

// Coefficient factory: returns Some(coeff) for valid index keys
let coefficientFactory (key : string) : Complex option =
    match key.Split(',').Length with
    | 2 -> oneBodyIntegrals.TryFind key
    | 4 -> twoBodyIntegrals.TryFind key
    | _ -> None

// Compute the Jordan-Wigner encoded Hamiltonian
printfn "Computing H₂ Hamiltonian with Jordan-Wigner encoding...\n"
let hamiltonian = computeHamiltonian coefficientFactory nSpinOrbitals
let terms = hamiltonian.SummandTerms

printfn "════════════════════════════════════════════════════════"
printfn " H₂ Qubit Hamiltonian (Jordan-Wigner Encoding)"
printfn "════════════════════════════════════════════════════════\n"
printfn " Number of Pauli terms: %d\n" terms.Length
printfn " Pauli terms (coefficient × Pauli string):"
printfn " ───────────────────────────────────────────"

for term in terms do
    let c = term.Coefficient
    let sign = if c.Real >= 0.0 then "+" else ""
    printfn "   %s%.6f  %s" sign c.Real term.Signature

printfn "\n════════════════════════════════════════════════════════"
