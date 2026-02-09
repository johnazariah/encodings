---
title: 'Fermion2Qubit: A Composable Functional Framework for Fermion-to-Qubit Encodings in F#'
tags:
  - quantum computing
  - quantum chemistry
  - fermion-to-qubit encoding
  - F#
  - functional programming
authors:
  - name: John Googleplex Aziz
    orcid: 0000-0000-0000-0000
    affiliation: 1
affiliations:
  - name: TBD
    index: 1
date: 2024
bibliography: paper.bib
---

**Draft v0.1 — Working draft, not for circulation**

---

# Summary

Simulating fermionic systems on quantum computers requires mapping
fermionic creation and annihilation operators to qubit (Pauli) operators
— the fermion-to-qubit encoding problem.  Different encodings (Jordan-
Wigner, Bravyi-Kitaev, Parity, tree-based) trade off Pauli weight,
circuit depth, and implementational complexity.  Existing libraries
implement each encoding as a separate, non-composable transform, obscuring
the shared mathematical structure.

We present `Fermion2Qubit`, an open-source F# library that unifies all
known fermion-to-qubit encodings under two composable abstractions:
*index-set schemes*, in which an encoding is specified by three
set-valued functions (`Update`, `Parity`, `Occupation`), and *path-based
tree encodings*, in which any rooted labelled tree automatically induces
a valid encoding.  The index-set framework captures Jordan-Wigner,
Bravyi-Kitaev, and Parity in 3–5 lines each; the path-based framework
handles arbitrary tree topologies including balanced binary and balanced
ternary trees that achieve provably optimal $O(\log n)$ Pauli weight.

The library is built entirely from algebraic data types and pure
functions, features a persistent Fenwick tree ADT, includes 303 unit
and property-based tests, and provides a running example encoding the
complete H₂ STO-3G Hamiltonian across all five built-in encodings.

# Statement of Need

Quantum simulation of molecular electronic structure is widely regarded as
one of the most promising near-term applications of quantum computing
[@feynman1982; @aspuruguzik2005].  Between the molecular Hamiltonian in
second quantization and the measurements performed on quantum hardware lies
a critical middleware step: the fermion-to-qubit encoding.  The choice of
encoding determines the Pauli weight of each operator (and hence circuit
depth), the number of measurement terms, and ultimately whether a
simulation is feasible on a given device.

Current tools for this step — OpenFermion [@mcclean2020], Qiskit Nature
[@qiskit2023], and PennyLane [@bergholm2022] — implement each encoding as a
monolithic function mapping `FermionOperator → QubitOperator`.  Adding a
new encoding requires writing hundreds of lines of bespoke code.  The
mathematical structure shared across encodings (Majorana decomposition,
parity tracking, update sets) is duplicated rather than abstracted.  There
is no mechanism for users to define, compose, or compare custom encodings
programmatically.

`Fermion2Qubit` addresses this gap.  An encoding is a *value* — a record
of three functions — not a class hierarchy.  This design enables:

- **Exploration:** researchers can define and test novel encodings in
  3–5 lines of code.
- **Comparison:** all encodings share the same verification pipeline
  (anti-commutation tests, eigenspectrum comparison).
- **Pedagogy:** the code is the mathematics — an `EncodingScheme` is the
  literal definition from the literature, not an opaque implementation of
  it.

The library serves quantum computing researchers exploring encoding-aware
circuit synthesis, students learning the algebraic structure of encodings,
and developers building quantum chemistry simulation pipelines who need
a correct and composable encoding layer.

# Functionality

## Encoding Schemes (Index-Set Framework)

The `EncodingScheme` record type captures the three index-set functions
that define a fermion-to-qubit encoding.  Three concrete schemes are
provided: `jordanWignerScheme`, `bravyiKitaevScheme`, and `parityScheme`.
User-defined schemes are first-class values of the same type:

```fsharp
let myScheme : EncodingScheme =
    { Update     = fun j n -> set [ j + 1 .. n - 1 ]
      Parity     = fun j   -> if j > 0 then Set.singleton (j - 1)
                               else Set.empty
      Occupation  = fun j   -> if j > 0 then set [j-1; j]
                               else Set.singleton j }
```

The framework automatically constructs Majorana operators $c_j$ and $d_j$
from these three functions, then derives ladder operators $a^\dagger_j$ and
$a_j$ by linear combination.

## Tree Encodings (Path-Based Framework)

Any rooted labelled tree defines a fermion-to-qubit encoding.  The library
provides `balancedBinaryTree` and `balancedTernaryTree` constructors; users
can build arbitrary trees from `TreeNode` values.  The path-based encoding
function `encodeWithTernaryTree` traverses the tree to construct Majorana
operators without requiring the index-set monotonicity constraint, making
it strictly more general than the index-set framework.

## Data Structures and Algebra

The library includes: a persistent `FenwickTree<'a>` (parameterised over
any monoid); immutable `PauliRegister` and `PauliRegisterSequence` types
with exact symbolic multiplication (including phase tracking); and a
`Hamiltonian` module for constructing molecular Hamiltonians from
one-body and two-body integrals.

## Verification Suite

All 303 tests pass across three categories:
- **Anti-commutation:** $\{a_i, a^\dagger_j\} = \delta_{ij}$ verified
  symbolically for all mode pairs.
- **Number conservation:** $a^\dagger_j a_j$ produces diagonal Pauli
  operators.
- **Cross-encoding agreement:** all five encodings produce isospectral
  Hamiltonians for H₂ (eigenvalue agreement to $5 \times 10^{-16}$).

# Design Principles

**Encodings as data.**  An `EncodingScheme` is a value, not a class
hierarchy.  Jordan-Wigner, Bravyi-Kitaev, and Parity are different values
of the same type.  This enables algebraic reasoning: one can ask whether
two schemes agree on a given mode without running a full encoding.

**Two complementary frameworks.**  The index-set framework
(`MajoranaEncoding.fs`) is fast and algebraically transparent but requires
a monotonicity condition on ancestor indices.  The path-based framework
(`TreeEncoding.fs`) works for *any* tree topology.  Both produce the same
output type (`PauliRegisterSequence`), so downstream code is
encoding-agnostic.

**Pure functions, no mutation.**  All data structures are immutable:
persistent Fenwick trees, recursive tree ADTs, and Pauli register
sequences.  The library has zero mutation and no side effects in its
core modules.

**Discovered constraints.**  Implementation and testing revealed that the
index-set framework's monotonicity requirement (ancestor indices must
exceed descendant indices) is satisfied only by star-shaped trees — a
structural constraint not previously documented in the literature.  This
discovery motivated the path-based framework as a universal alternative
and is explored further in a companion paper [Paper 3].

# Comparison with Related Software

| Feature | OpenFermion | Qiskit Nature | PennyLane | **Fermion2Qubit** |
|:--------|:-----------:|:------------:|:---------:|:---------:|
| JW / BK / Parity | ✅ / ✅ / ✅ | ✅ / ✅ / ✅ | ✅ / ✅ / ❌ | ✅ / ✅ / ✅ |
| Tree encodings | Steiner ext. | ❌ | ❌ | ✅ (binary, ternary) |
| User-defined encodings | ❌ | ❌ | ❌ | ✅ |
| User-defined trees | ❌ | ❌ | ❌ | ✅ |
| Generic encoding abstraction | ❌ | ❌ | ❌ | ✅ |
| Typed / functional | ❌ | ❌ | ❌ | ✅ |
| Persistent Fenwick tree | ❌ | ❌ | ❌ | ✅ |

OpenFermion [@mcclean2020] is the most comprehensive existing tool,
offering extensive support for operator manipulation and circuit synthesis.
Qiskit Nature [@qiskit2023] integrates tightly with IBM quantum hardware.
PennyLane [@bergholm2022] excels at differentiable quantum computing.
`Fermion2Qubit` does not compete on scope; it provides the *framework*
abstraction that these libraries lack, enabling systematic exploration and
comparison of encodings.

# Acknowledgements

[TBD]

# References
