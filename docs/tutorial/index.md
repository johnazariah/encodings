# From Molecules to Qubits

_A complete, step-by-step guide to fermion-to-qubit encoding for quantum chemistry simulation._

This tutorial walks through the **entire pipeline** from a molecular Schrödinger equation to a qubit Hamiltonian, using the hydrogen molecule (H₂) as a running example. Every integral, every sign, every coefficient is computed explicitly.

> **Prerequisites:** Linear algebra, introductory quantum mechanics (wavefunctions, the hydrogen atom), and basic chemistry (orbitals, bonds). No prior knowledge of second quantization, Fock space, or quantum computing is assumed — but we provide pointers to our [Background](../background/01-why-encodings.html) pages for those topics.

## The Pipeline

```
Molecule → Basis Set → Integrals → Second Quantization → Spin-Orbitals → Encoding → Qubit Hamiltonian
```

Each stage involves notation choices, sign conventions, and index manipulations that the research literature compresses into a few lines. This tutorial makes **every step explicit**.

## Chapters

| # | Chapter | What you'll learn |
|:--|:--------|:------------------|
| 1 | [The Electronic Structure Problem](01-electronic-structure.html) | Born–Oppenheimer, basis sets, and why H₂ has exactly 6 configurations |
| 2 | [The Notation Minefield](02-notation.html) | Chemist's vs. physicist's integrals — and the errors they cause |
| 3 | [From Spatial to Spin-Orbital Integrals](03-spin-orbitals.html) | Doubling the index space, cross-spin terms, and a complete integral table |
| 4 | [Building the H₂ Qubit Hamiltonian](04-building-hamiltonian.html) | The 15-term Jordan–Wigner Hamiltonian, term by term |
| 5 | [Checking Our Answer](05-verification.html) | Exact diagonalisation, eigenspectrum, and cross-encoding comparison |
| 6 | [What Comes Next](06-outlook.html) | VQE, QPE, and why encoding choice matters at scale |

## Background References

The theory behind each stage is covered in our Background pages:

- **Second quantization** → [Background: Second Quantization](../background/02-second-quantization.html)
- **Pauli algebra** → [Background: Pauli Algebra](../background/03-pauli-algebra.html)
- **Jordan–Wigner transform** → [Background: Jordan–Wigner](../background/04-jordan-wigner.html)
- **Alternative encodings** → [Background: Beyond Jordan–Wigner](../background/05-beyond-jordan-wigner.html)

## Companion Code

Every numerical result in this tutorial is reproduced by the FockMap library. See the interactive tutorials:

- [Encoding the H₂ Molecule](../tutorials/02-h2-molecule.html) — the complete calculation as executable F#
- [Compare Encodings](../tutorials/03-compare-encodings.html) — side-by-side cross-encoding comparison

## PDF Version

A TeX-typeset version of this tutorial is available as a [preprint (PDF)](https://github.com/johnazariah/encodings/blob/main/.research/paper-tutorial/paper.pdf) suitable for printing and citation.
