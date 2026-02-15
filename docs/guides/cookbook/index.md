# Library Cookbook

_A hands-on guide that teaches FockMap from the ground up._

Each chapter introduces a concept through worked examples, building toward
a complete molecular Hamiltonian by the end.

> **Prerequisites:** .NET 10 installed ([setup guide](../cross-platform.html)),
> basic comfort with F#. Prior quantum computing knowledge is helpful but not required —
> we explain the physics as we go.

## Chapters

| # | Chapter | What you'll learn |
|:--|:--------|:------------------|
| 1 | [Hello, Qubit](01-hello-qubit.html) | Pauli operators and exact symbolic phases |
| 2 | [Building Expressions](02-building-expressions.html) | The `C` / `P` / `S` type hierarchy |
| 3 | [Operators on Specific Qubits](03-indexed-operators.html) | `IxOp` and string parsing |
| 4 | [Creation and Annihilation](04-creation-annihilation.html) | Ladder operators and product terms |
| 5 | [Normal Ordering](05-normal-ordering.html) | CAR / CCR rewriting and pluggable algebras |
| 6 | [Your First Encoding](06-first-encoding.html) | Jordan–Wigner step by step |
| 7 | [Five Encodings, One Interface](07-five-encodings.html) | Drop-in encoder comparison |
| 8 | [Encoding Internals](08-encoding-internals.html) | Majorana decomposition and `EncodingScheme` |
| 9 | [Trees and Fenwick Trees](09-trees.html) | Tree-shaped parity structures |
| 10 | [Building a Real Hamiltonian](10-building-hamiltonian.html) | End-to-end H₂ pipeline |
| 11 | [Mixed Bosonic–Fermionic Systems](11-mixed-systems.html) | Sector tags, mixed normal ordering, and hybrid workflows |
| 12 | [The Utility Belt](12-utilities.html) | Complex extensions, map helpers, currying |
| 13 | [Grand Finale](13-grand-finale.html) | Three encodings, one molecule — a capstone script |

## Quick Reference

### Encoding functions

| Function | Scaling | Best for |
|----------|:-------:|---------|
| `jordanWignerTerms` | $O(n)$ | Small systems, simplicity |
| `bravyiKitaevTerms` | $O(\log_2 n)$ | General-purpose logarithmic |
| `parityTerms` | $O(n)$ | When parity is the natural basis |
| `balancedBinaryTreeTerms` | $O(\log_2 n)$ | Logarithmic via binary tree |
| `ternaryTreeTerms` | $O(\log_3 n)$ | Best asymptotic scaling |
| `encodeOperator scheme` | Varies | Your custom scheme |
| `encodeWithTernaryTree tree` | Varies | Your custom tree shape |

### Type cheat sheet

| Type | What it represents |
|------|--------------------|
| `Pauli` | Single-qubit operator: `I`, `X`, `Y`, `Z` |
| `Phase` | Exact phase: `P1` (+1), `M1` (−1), `Pi` (+i), `Mi` (−i) |
| `C<'T>` | Coefficient × single operator |
| `P<'T>` | Ordered product of operators |
| `S<'T>` | Sum of products (the Hamiltonian shape) |
| `IxOp<'idx,'op>` | Operator tagged with a mode index |
| `LadderOperatorUnit` | `Raise` ($a^\dagger$) / `Lower` ($a$) / `Identity` |
| `PauliRegister` | Fixed-width Pauli string with coefficient |
| `PauliRegisterSequence` | Sum of Pauli strings (encoding output) |
| `EncodingScheme` | Three index-set functions → custom encoding |
| `EncodingTree` | Tree shape for tree-based encodings |
| `FenwickTree<'a>` | Immutable binary indexed tree |
| `SectorLadderOperatorUnit` | Sector-tagged ladder operator (mixed systems) |

## Where to Go Next

- [Architecture Guide](../architecture.html) — the two-framework design in depth
- [Interactive Labs](../../labs/01-first-encoding.html) — focused hands-on exercises
- [From Molecules to Qubits](../../from-molecules-to-qubits/index.html) — complete H₂ pipeline with real integrals
- [Theory: Why Encodings?](../../theory/01-why-encodings.html) — background on the problem encodings solve

## PDF Version

A TeX-typeset version of this cookbook is available as a [companion preprint (PDF)](https://github.com/johnazariah/encodings/blob/main/.research/paper-cookbook/paper.pdf) suitable for printing and citation.
