---
title: 'FockMap: A Composable Functional Framework for Symbolic Fock-Space Operator Algebra and Fermion-to-Qubit Encodings in F#'
tags:
  - F#
  - quantum computing
  - fermion-to-qubit encoding
  - symbolic algebra
  - Pauli operators
  - bosonic encoding
  - Hamiltonian simulation
authors:
  - name: John Azariah
    orcid: 0009-0007-9870-1970
    corresponding: true
    affiliation: 1
affiliations:
  - name: University of Technology Sydney, Australia
    index: 1
    ror: 03f0f6041
date: 15 February 2026
bibliography: paper.bib
---

# Summary

Simulating fermionic systems on quantum hardware requires a mapping from
fermionic ladder operators to qubit Pauli operators.  This
fermion-to-qubit encoding step strongly influences Pauli weight,
measurement cost, and circuit depth.  Although widely used encodings
(Jordan--Wigner, Bravyi--Kitaev, Parity, and tree-based variants) share
substantial algebraic structure, they are often implemented in existing
software as isolated transformations rather than as instances of a common
formal interface.

`FockMap` is an open-source F# library that formalizes this shared
structure through two composable abstractions: *index-set schemes*,
defined by three set-valued functions (`Update`, `Parity`,
`Occupation`), and *path-based tree encodings*, in which any rooted
labelled tree induces a valid encoding.  The index-set abstraction
expresses Jordan--Wigner [@jordanwigner1928], Bravyi--Kitaev
[@bravyikitaev2002; @seeley2012], and Parity in 3--5 lines each, while
the path-based abstraction supports arbitrary tree topologies, including
balanced binary and balanced ternary trees with optimal $O(\log n)$
asymptotic Pauli weight [@jiang2020].

The operator-processing pipeline is implemented symbolically: Pauli
strings are multiplied exactly with algebraic phase tracking, without
constructing operator matrices and without introducing floating-point
error in intermediate steps.  Numerical coefficients are introduced only
at Hamiltonian assembly.

In addition to the fermionic canonical anti-commutation workflow,
`FockMap` includes a bosonic canonical commutation algebra for symbolic
normal ordering of ladder-operator expressions, as well as three
bosonic-to-qubit truncation encodings (Unary, Standard Binary, and Gray
Code) that map $d$-level bosonic modes to qubit Pauli strings
[@sawaya2020].  This extends the same typed expression pipeline to both
fermionic and bosonic sectors, supporting mixed-statistics model assembly
in a single representation.

The library is implemented with algebraic data types and pure functions,
includes a persistent Fenwick tree ADT, and is validated by an extensive
xUnit + FsCheck test suite (497 passing tests), including both
property-based algebraic checks and targeted edge-case regressions.  A
complete H~2~/STO-3G example is provided and reproduced across all five
built-in encodings.

# Statement of need

Quantum simulation of molecular electronic structure is widely regarded
as one of the most promising near-term applications of quantum computing
[@feynman1982; @aspuruguzik2005].  Between the molecular Hamiltonian in
second quantization and the measurements performed on quantum hardware
lies a critical middleware step: the fermion-to-qubit encoding.  The
choice of encoding determines the Pauli weight of each operator (and
hence circuit depth), the number of measurement terms, and ultimately
whether a simulation is feasible on a given device.

Current tools for this step---OpenFermion [@mcclean2020], Qiskit Nature
[@qiskit2023], and PennyLane [@bergholm2022]---implement each encoding
as a monolithic function mapping `FermionOperator` $\to$
`QubitOperator`.  Adding a new encoding requires writing hundreds of
lines of bespoke code.  The mathematical structure shared across
encodings (Majorana decomposition, parity tracking, update sets) is
duplicated rather than abstracted.  There is no mechanism for users to
define, compose, or compare custom encodings programmatically.

`FockMap` addresses this gap by representing an encoding as a *value* (a
record of three functions) rather than as an opaque class hierarchy.
This design enables:

- **Exploration:** researchers can define and test novel encodings in
  3--5 lines of code.
- **Comparison:** all encodings share the same verification pipeline
  (anti-commutation tests, eigenspectrum comparison).
- **Pedagogy:** the implementation remains close to the formal
  definitions in the literature; an `EncodingScheme` directly encodes
  the mathematical specification.

Many contemporary models also require bosonic modes (for example,
vibrational and photonic degrees of freedom), where canonical commutation
relations govern symbolic rewriting.  A practical software stack
therefore needs both fermionic and bosonic ladder-operator
normal-ordering support, together with bosonic-to-qubit truncation
encodings, before downstream circuit synthesis can proceed.  Existing
libraries provide neither bosonic-to-qubit encodings nor a unified
framework for mixed fermion--boson workflows.

The library serves quantum computing researchers exploring
encoding-aware circuit synthesis, students learning the algebraic
structure of encodings, and developers building simulation pipelines who
need a correct and composable symbolic operator layer.

# State of the field

The three most widely used fermion-to-qubit libraries are OpenFermion
[@mcclean2020], Qiskit Nature [@qiskit2023], and PennyLane
[@bergholm2022].

OpenFermion is the most comprehensive existing tool, offering extensive
support for operator manipulation and circuit synthesis.  Qiskit Nature
integrates tightly with IBM quantum hardware.  PennyLane excels at
differentiable quantum computing and includes bosonic canonical
commutation relation (CCR) support for symbolic rewriting.

All three implement encodings as black-box functions; none expose the
underlying algebraic structure (update, parity, and occupation sets) as a
composable interface.  None provide a mechanism for users to define
custom encodings or custom tree topologies.  None offer bosonic-to-qubit
truncation encodings.

\autoref{tab:comparison} summarizes the feature landscape.

: Feature comparison with existing fermion-to-qubit and bosonic encoding
libraries.  `FockMap` is the only library providing both fermionic and
bosonic-to-qubit encodings within a single symbolic algebra framework.
[]{label="tab:comparison"}

| Feature                       | OpenFermion       | Qiskit Nature     | PennyLane     | FockMap           |
|-------------------------------|:-----------------:|:-----------------:|:-------------:|:-----------------:|
| JW / BK / Parity              | ✓/✓/✓            | ✓/✓/✓            | ✓/✓/---       | ✓/✓/✓            |
| Tree encodings                | Steiner ext.      | ---               | ---           | Binary, Ternary   |
| Bosonic CCR algebra            | ---               | ---               | ✓             | ✓                 |
| Bosonic-to-qubit encodings     | ---               | ---               | ---           | ✓                 |
| Mixed fermion--boson ordering  | ---               | ---               | ---           | ✓                 |
| User-defined encodings         | ---               | ---               | ---           | ✓                 |
| User-defined trees             | ---               | ---               | ---           | ✓                 |
| Generic encoding abstraction   | ---               | ---               | ---           | ✓                 |
| Symbolic Pauli algebra         | ---               | Partial           | ✓             | ✓                 |
| Typed / functional             | ---               | ---               | ---           | ✓                 |
| Persistent Fenwick tree        | ---               | ---               | ---           | ✓                 |

`FockMap` does not compete on scope; it provides the *framework*
abstraction that these libraries lack, enabling systematic exploration
and comparison of encodings.  It was built as a new library rather than a
contribution to an existing project because its core design premise---
encodings as algebraic values, not class hierarchies---requires a
fundamentally different type system and host language (F# algebraic data
types and pattern matching) than the Python object model used by the
existing tools.

# Software design

## Encodings as data (Index-set framework)

The `EncodingScheme` record type captures the three index-set functions
that define a fermion-to-qubit encoding.  Three concrete schemes are
provided: `jordanWignerScheme`, `bravyiKitaevScheme`, and
`parityScheme`.  User-defined schemes are first-class values of the same
type:

```fsharp
let myScheme : EncodingScheme =
    { Update     = fun j n -> set [ j + 1 .. n - 1 ]
      Parity     = fun j   -> if j > 0 then Set.singleton (j - 1)
                               else Set.empty
      Occupation  = fun j   -> if j > 0 then set [j-1; j]
                               else Set.singleton j }
```

The framework automatically constructs Majorana operators $c_j$ and
$d_j$ from these three functions, then derives ladder operators
$a^\dagger_j$ and $a_j$ by linear combination.

## Tree encodings (Path-based framework)

Any rooted labelled tree defines a fermion-to-qubit encoding.  The
library provides `balancedBinaryTree` and `balancedTernaryTree`
constructors; users can build arbitrary trees from `TreeNode` values.
The path-based encoding function traverses the tree to construct Majorana
operators without requiring an index-set monotonicity constraint, making
it strictly more general than the index-set framework.

## Symbolic algebra engine

A distinguishing feature of `FockMap` is that operator multiplication is
entirely symbolic.  The `PauliRegister` type represents a Pauli string
(e.g., $XZIY$) together with an exact `Phase` drawn from $\{+1, -1, +i,
-i\}$.  Multiplying two Pauli registers applies the single-qubit
multiplication table ($X \cdot Y = iZ$, etc.) position-wise and
accumulates the phase algebraically; no $2^n \times 2^n$ matrices are
ever constructed.  Correctness can be verified symbolically: the
anti-commutation tests check $\{a_i, a^\dagger_j\} = \delta_{ij}$ by
Pauli string cancellation, not by matrix eigenvalue comparison.

## Bosonic-to-qubit encodings

For bosonic modes truncated to $d$ occupation levels, the library
provides three encoding strategies that map bosonic ladder operators
$b^\dagger$ and $b$ to qubit Pauli strings [@sawaya2020]:

- **Unary (one-hot):** $d$ qubits per mode, maximum Pauli weight 2.
  Each transition $|n\rangle \to |n{+}1\rangle$ is decomposed
  algebraically as $\sigma^+_{n+1}\sigma^-_n$.
- **Standard binary:** $\lceil\log_2 d\rceil$ qubits per mode.  The
  $d \times d$ operator matrix is embedded in a $2^q \times 2^q$ space
  and decomposed via
  $O = \sum_P \tfrac{1}{2^q}\operatorname{Tr}(PO)\,P$.
- **Gray code:** $\lceil\log_2 d\rceil$ qubits per mode.  Consecutive
  Fock states differ in exactly one qubit, reducing the average Pauli
  weight of transition operators.

All three encodings produce `PauliRegisterSequence` values, making them
interchangeable with the fermionic encoding output and enabling mixed
fermion--boson Hamiltonian assembly in a single pipeline.

## Key design principles

- **Symbolic over numerical.**  Operators are typed Pauli strings with
  exact algebraic phases, not sparse matrices or opaque integer-keyed
  coefficient dictionaries.  Numerical coefficients enter only at
  Hamiltonian assembly.
- **Pure functions, no mutation.**  All data structures are immutable:
  persistent Fenwick trees, recursive tree ADTs, and Pauli register
  sequences.  The library has zero mutation and no side effects in its
  core modules.
- **Two complementary frameworks.**  The index-set framework
  (`MajoranaEncoding.fs`) is fast and algebraically transparent but
  requires a monotonicity condition on ancestor indices.  The path-based
  framework (`TreeEncoding.fs`) works for *any* tree topology.  Both
  produce the same output type (`PauliRegisterSequence`), so downstream
  code is encoding-agnostic.

## Verification suite

The verification strategy combines unit tests, property-based tests, and
cross-encoding consistency checks.  At submission time, 497 tests pass,
covering six categories: anti-commutation ($\{a_i, a^\dagger_j\} =
\delta_{ij}$ verified symbolically), commutation ($[b_i, b^\dagger_j] =
\delta_{ij}$), number conservation, cross-encoding eigenspectrum
agreement for H~2~ (to $5 \times 10^{-16}$), bosonic encoding
correctness (70 tests covering Pauli-weight bounds, number-operator
roundtrip, and cross-encoding consistency), and parser and ordering
robustness.

# Research impact statement

`FockMap` has been developed as research infrastructure for
encoding-aware quantum simulation and is accompanied by extensive
reproducible materials:

- A 14-chapter progressive tutorial (*Library Cookbook*) covering every
  public type, function, and workflow, available as both hosted
  documentation and a companion preprint [@cookbook2026].
- A pedagogical walkthrough of the complete H~2~ pipeline (*From
  Molecules to Qubits*), also available as web documentation and a
  standalone preprint [@tutorial2026].
- Seven theory pages on second quantization, Pauli algebra, and encoding
  theory.
- Six runnable F# interactive lab scripts with guided exercises.

The library's typed, symbolic approach has enabled the discovery of a
previously undocumented structural constraint: the index-set framework's
monotonicity requirement is satisfied only by star-shaped trees.  This
finding motivated the path-based framework as a universal alternative
and is explored further in a companion paper.

`FockMap` is packaged on NuGet for cross-platform use (.NET 8.0), tested
on Linux, macOS, and Windows via GitHub Actions CI, and designed for
integration into quantum simulation pipelines.

# AI usage disclosure

Generative AI tools (GitHub Copilot, powered by Claude) were used during
the development of this software and the preparation of this manuscript.
Specifically:

- **Code generation and refactoring:** AI assisted with implementing
  encoding functions, test scaffolding, and boilerplate code.
- **Documentation and paper drafting:** AI assisted with drafting
  documentation pages, cookbook chapters, and sections of this paper.
- **Test generation:** AI assisted with generating property-based and
  unit test cases.

All AI-generated outputs were reviewed, edited, and validated by the
human author, who made all core design decisions including the
index-set/path-based dual-framework architecture, the symbolic algebra
approach, the type system design, and the choice of bosonic encoding
strategies.

# Acknowledgements

This work is dedicated to Dr. Guang Hao Low, whose early encouragement
to study Bravyi--Kitaev encodings motivated the development of this
symbolic algebra framework.  The author also acknowledges the F# Software
Foundation and the .NET open-source community for the language and
runtime ecosystem supporting this work.

# References
