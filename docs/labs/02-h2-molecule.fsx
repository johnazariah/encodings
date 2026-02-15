(**
# Encoding the H₂ Molecule

This tutorial walks through encoding the electronic Hamiltonian of molecular
hydrogen (H₂) using the Jordan-Wigner transformation. This is the canonical
example for quantum chemistry on quantum computers.

## Physical Setup

We use the STO-3G minimal basis set, which gives:
- **2 spatial orbitals**: σg (bonding) and σu (antibonding)
- **4 spin-orbitals**: σg↑, σg↓, σu↑, σu↓
- **4 qubits** after encoding

The electronic Hamiltonian in second quantization is:

$$H = \sum_{pq} h_{pq} \, a^\dagger_p a_q + \frac{1}{2} \sum_{pqrs} \langle pq | rs \rangle \, a^\dagger_p a^\dagger_q a_s a_r$$

where:
- $h_{pq}$ are one-body (kinetic + nuclear) integrals
- $\langle pq | rs \rangle$ are two-body (electron-electron) integrals

## Setup
*)

#r "../../src/Encodings/bin/Debug/net8.0/Encodings.dll"

open System.Numerics
open Encodings

(**
## Molecular Integrals

These are the STO-3G integrals for H₂ at the equilibrium bond length
R = 0.7414 Å, computed from standard quantum chemistry packages.

### Spin-Orbital Indexing

| Index | Spin-Orbital |
|-------|--------------|
| 0     | σg↑          |
| 1     | σg↓          |
| 2     | σu↑          |
| 3     | σu↓          |

### One-Body Integrals h_{pq}

These include kinetic energy and electron-nuclear attraction.
In a spin-orbital basis with real spatial orbitals, only diagonal-in-spin
terms are nonzero:
*)

let nSpinOrbitals = 4u

let oneBodyIntegrals = Map.ofList [
    // Diagonal elements (same spatial orbital)
    ("00", Complex(-1.2563390730032498, 0.0))  // h₀₀ = ⟨σg↑|h|σg↑⟩
    ("11", Complex(-1.2563390730032498, 0.0))  // h₁₁ = ⟨σg↓|h|σg↓⟩
    ("22", Complex(-0.4718960244306283, 0.0))  // h₂₂ = ⟨σu↑|h|σu↑⟩
    ("33", Complex(-0.4718960244306283, 0.0))  // h₃₃ = ⟨σu↓|h|σu↓⟩
]

(**
### Two-Body Integrals ⟨pq|rs⟩

These are the electron-electron repulsion integrals in physicist's notation.
Symmetries reduce the number of unique values significantly.
*)

let twoBodyIntegrals = Map.ofList [
    // Direct Coulomb: electrons in same orbital
    ("0000", Complex(0.6744887663049631, 0.0))
    ("1111", Complex(0.6744887663049631, 0.0))
    ("2222", Complex(0.6973979494693556, 0.0))
    ("3333", Complex(0.6973979494693556, 0.0))

    // Coulomb: σg-σg interactions
    ("0011", Complex(0.6744887663049631, 0.0))
    ("1100", Complex(0.6744887663049631, 0.0))

    // Coulomb: σg-σu interactions
    ("0022", Complex(0.6636340478615040, 0.0))
    ("2200", Complex(0.6636340478615040, 0.0))
    ("0033", Complex(0.6636340478615040, 0.0))
    ("3300", Complex(0.6636340478615040, 0.0))
    ("1122", Complex(0.6636340478615040, 0.0))
    ("2211", Complex(0.6636340478615040, 0.0))
    ("1133", Complex(0.6636340478615040, 0.0))
    ("3311", Complex(0.6636340478615040, 0.0))

    // Coulomb: σu-σu interactions
    ("2233", Complex(0.6973979494693556, 0.0))
    ("3322", Complex(0.6973979494693556, 0.0))

    // Exchange integrals
    ("0220", Complex(0.1809312433852046, 0.0))
    ("2002", Complex(0.1809312433852046, 0.0))
    ("1331", Complex(0.1809312433852046, 0.0))
    ("3113", Complex(0.1809312433852046, 0.0))
]

(**
## Coefficient Lookup Function

The `computeHamiltonian` function needs a way to look up coefficients
for arbitrary index combinations. We provide a factory function:
*)

let coefficientFactory (key : string) : Complex option =
    match key.Length with
    | 2 -> oneBodyIntegrals.TryFind key   // One-body: 2-digit key
    | 4 -> twoBodyIntegrals.TryFind key   // Two-body: 4-digit key
    | _ -> None

(**
## Computing the Hamiltonian

Now we call `computeHamiltonian` with Jordan-Wigner encoding (the default).
This function:
1. Iterates over all one-body index pairs (p,q)
2. Iterates over all two-body index quadruples (p,q,r,s)
3. Looks up coefficients using our factory
4. Encodes each term using Jordan-Wigner
5. Combines and simplifies the result
*)

printfn "Computing H₂ Hamiltonian with Jordan-Wigner encoding...\n"

let hamiltonian = computeHamiltonian coefficientFactory nSpinOrbitals
let terms = hamiltonian.SummandTerms

(**
## Examining the Results

Let's display the qubit Hamiltonian:
*)

printfn "════════════════════════════════════════════════════════════"
printfn " H₂ Qubit Hamiltonian (Jordan-Wigner Encoding)"
printfn "════════════════════════════════════════════════════════════\n"
printfn " Number of Pauli terms: %d\n" terms.Length

printfn " Pauli terms (coefficient × Pauli string):"
printfn " ───────────────────────────────────────────"

for term in terms do
    let c = term.Coefficient
    let sign = if c.Real >= 0.0 then "+" else ""
    printfn "   %s%.6f  %s" sign c.Real term.Signature

(**
## Understanding the Output

The H₂ Hamiltonian typically produces 15 unique Pauli terms:

| Pattern | Physical Origin |
|---------|-----------------|
| `IIII`  | Constant energy offset |
| `ZZII`, `IZZI`, etc. | Number operators from h_{pp} a†_p a_p |
| `XXYY`, `YYXX`, etc. | Hopping terms from h_{pq} a†_p a_q |
| `ZZZZ` patterns | Two-body Coulomb repulsion |

## Why This Matters

The qubit Hamiltonian H can be measured on a quantum computer:
1. Each Pauli string is a tensor product of single-qubit observables
2. Measure each term separately (or use grouping strategies)
3. Combine measurements weighted by coefficients

This is the foundation for the Variational Quantum Eigensolver (VQE)
algorithm, which finds the ground state energy of molecules.

## Next Steps

Try modifying this tutorial to:
- Change the bond length and see how coefficients change
- Use `computeHamiltonianWith` with different encodings
- Compare the number of terms across encodings
*)

printfn "\n════════════════════════════════════════════════════════════"
printfn " Tutorial complete! H₂ encoded into %d Pauli terms." terms.Length
printfn "════════════════════════════════════════════════════════════"
