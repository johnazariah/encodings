# Chapter 6: What Comes Next

_In this chapter, you'll connect the Hamiltonian to VQE/QPE workflows and practical scaling decisions._

## In This Chapter

- **What you'll learn:** How the encoded Hamiltonian is used in VQE and QPE, and why encoding choice affects scaling.
- **Why this matters:** Good encoding choices can be the difference between feasible and impractical circuits.
- **Try this next:** Jump into the [Compare Encodings lab](../labs/03-compare-encodings.fsx) and test scaling behavior yourself.

The 15-term qubit Hamiltonian from [Chapter 4](04-building-hamiltonian.html) is the input to quantum algorithms. Two families of algorithms can extract the ground-state energy:

## Variational Quantum Eigensolver (VQE)

VQE prepares a parameterised quantum state $\lvert\psi(\boldsymbol{\theta})\rangle$, measures $\langle\psi \mid\hat{H}\mid \psi\rangle$ by separately measuring each Pauli term, and uses a classical optimiser to minimise the energy over $\boldsymbol{\theta}$.

VQE is designed for **near-term noisy quantum hardware**: the circuits are short and the measurement overhead is manageable for small molecules. Experiments have already demonstrated VQE for H₂ and small molecules on real quantum computers.

## Quantum Phase Estimation (QPE)

QPE applies the time-evolution operator $e^{-i\hat{H}t}$ controlled on an ancilla register to extract eigenvalues directly. QPE requires **fault-tolerant quantum hardware** but provides exponential speedup over classical exact diagonalisation for large systems.

## Why Encoding Choice Matters at Scale

For H₂ with 4 qubits and 15 Pauli terms, both algorithms are trivially executable on current hardware. The challenge is **scaling** to chemically interesting molecules: LiH (12 spin-orbitals), H₂O (14), and the nitrogen fixation catalyst FeMo-co (~100 active spin-orbitals — the "poster child" of quantum chemistry on quantum computers).

The choice of encoding directly affects the scaling:

- **More Pauli terms → more shots.** Each term must be measured separately.
- **Higher Pauli weight → deeper CNOT ladders → more gate errors.**
- **The ternary tree encoding's $O(\log_3 n)$ weight scaling** means that for 100 modes, the deepest circuits are roughly 5 CNOTs instead of Jordan–Wigner's 100 — a difference that may determine whether the simulation is feasible on early fault-tolerant hardware.

| System | Spin-orbitals | JW max weight | Ternary tree max weight |
|:---:|:---:|:---:|:---:|
| H₂ | 4 | 4 | 2 |
| LiH | 12 | 12 | 4 |
| H₂O | 14 | 14 | 4 |
| N₂ | 20 | 20 | 5 |
| FeMo-co | ~100 | ~100 | ~5 |

## What You've Learned

If you've followed this tutorial from [Chapter 1](01-electronic-structure.html), you can now:

- **Understand** why fermions and qubits are algebraically different and why encoding is necessary
- **Construct** the Jordan–Wigner encoding by hand for any number of modes
- **Compute** the complete 15-term qubit Hamiltonian for H₂ and verify it by diagonalisation
- **Compare** five different encodings and explain why tree-based approaches matter for larger molecules

## Further Reading

- **The encoding framework:** [Beyond Jordan–Wigner](../theory/05-beyond-jordan-wigner.html) — how all encodings are unified under two composable abstractions
- **Hands-on code:** [Interactive labs](../labs/01-first-encoding.fsx) — executable F# labs
- **The library itself:** [API Reference](../reference/index.html) — all types and functions
- **The TeX preprint:** A typeset PDF version of this tutorial is available in the [repository](https://github.com/johnazariah/encodings/blob/main/.research/paper-tutorial/paper.pdf)

At this point you have the full end-to-end picture; the labs are the best next step to turn the concepts into intuition.

---

**Previous:** [Chapter 5 — Checking Our Answer](05-verification.html)
**Back to:** [Tutorial Index](index.html)
