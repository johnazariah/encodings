# References

Literature referenced during development of this fermion-to-qubit encoding library.

---

## Core Encoding Theory

1. **Jordan, P. & Wigner, E.** (1928).
   "Über das Paulische Äquivalenzverbot."
   *Zeitschrift für Physik*, 47(9–10), 631–651.
   — Original Jordan-Wigner transformation mapping fermionic operators to Pauli strings with O(n) Z-chains.

2. **Bravyi, S. & Kitaev, A.** (2002).
   "Fermionic quantum computation."
   *Annals of Physics*, 298(1), 210–226.
   [arXiv:quant-ph/0003137](https://arxiv.org/abs/quant-ph/0003137)
   — Introduces the Bravyi-Kitaev encoding using a Fenwick-tree structure to achieve O(log n) Pauli weight.

3. **Seeley, J. T., Richard, M. J., & Love, P. J.** (2012).
   "The Bravyi-Kitaev transformation for quantum computation of electronic structure."
   *Journal of Chemical Physics*, 137(22), 224109.
   [arXiv:1208.5986](https://arxiv.org/abs/1208.5986)
   — Practical formulation of the BK transform for quantum chemistry; defines update, parity, and remainder sets via the Fenwick tree; Majorana-operator decomposition used in this library.

4. **Tranter, A., Sofia, S., Sherrill, C. D., et al.** (2015).
   "The Bravyi–Kitaev transformation: Properties and applications."
   *International Journal of Quantum Chemistry*, 115(19), 1431–1441.
   [arXiv:1502.04563](https://arxiv.org/abs/1502.04563)
   — Comparative study of JW vs BK; analysis of Pauli weight scaling and gate-count implications.

5. **Steudtner, M. & Wehner, S.** (2018).
   "Fermion-to-qubit mappings with varying resource requirements."
   *New Journal of Physics*, 20, 063010.
   [arXiv:1712.07067](https://arxiv.org/abs/1712.07067)
   — Unifying framework for fermion-to-qubit encodings via ternary trees; shows JW, BK, and Parity are special cases of a general tree-based construction. Directly inspired the `EncodingScheme` abstraction in this library.

6. **Havlíček, V., Troyer, M., & Whitfield, J. D.** (2017).
   "Operator locality in the quantum simulation of fermionic models."
   *Physical Review A*, 95(3), 032332.
   [arXiv:1701.07072](https://arxiv.org/abs/1701.07072)
   — Analyzes operator locality across different encodings; proves lower bounds on encoding overhead.

## Symmetry Reduction & Qubit Tapering

7. **Bravyi, S., Gambetta, J. M., Mezzacapo, A., & Temme, K.** (2017).
   "Tapering off qubits to simulate fermionic Hamiltonians."
   [arXiv:1701.08213](https://arxiv.org/abs/1701.08213)
   — Technique for exploiting Z₂ symmetries in qubit Hamiltonians to reduce qubit count. The Parity encoding makes particle-number parity manifest, enabling direct tapering of the last qubit.

## Molecular Hamiltonian & Quantum Chemistry

8. **O'Malley, P. J. J., Babbush, R., Kivlichan, I. D., et al.** (2016).
   "Scalable quantum simulation of molecular energies."
   *Physical Review X*, 6(3), 031007.
   [arXiv:1512.06860](https://arxiv.org/abs/1512.06860)
   — Experimental H₂ simulation on superconducting qubits; provides STO-3G integral values used in the H₂ demo script.

9. **Whitfield, J. D., Biamonte, J., & Aspuru-Guzik, A.** (2011).
   "Simulation of electronic structure Hamiltonians using quantum computers."
   *Molecular Physics*, 109(5), 735–750.
   [arXiv:1001.3855](https://arxiv.org/abs/1001.3855)
   — Foundational reference for second-quantized Hamiltonian construction and Jordan-Wigner encoding in quantum chemistry simulation.

## Encoding Optimization (Future Directions)

10. **Loaiza, I., Marefat Khah, A., Wiersema, R., et al.** (2023).
    "Reducing the molecular electronic Hamiltonian encoding costs on quantum computers by symmetry shifts."
    [arXiv:2304.13772](https://arxiv.org/abs/2304.13772)
    — Clifford-rotation-based optimization of qubit Hamiltonian 1-norm; relevant to Hamiltonian-adapted encoding search.

11. **Goings, J. J., Zhao, L., Jakowski, J., Morris, T., & Pooser, R.** (2023).
    "Molecular symmetry in VQE: A dual approach for trapped-ion simulations of benzene."
    [arXiv:2308.00667](https://arxiv.org/abs/2308.00667)
    — Encoding-aware circuit optimization exploiting molecular symmetry.

## Software References

12. **McClean, J. R., Sung, K. J., Kivlichan, I. D., et al.** (2020).
    "OpenFermion: The electronic structure package for quantum computers."
    *Quantum Science and Technology*, 5(3), 034014.
    [arXiv:1710.07629](https://arxiv.org/abs/1710.07629) ·
    [GitHub: quantumlib/OpenFermion](https://github.com/quantumlib/OpenFermion)
    — Open-source library implementing JW, BK, and other transforms; used as a cross-reference for correctness of the Fenwick-tree index sets and Majorana decomposition.

13. **Fenwick, P. M.** (1994).
    "A new data structure for cumulative frequency tables."
    *Software: Practice and Experience*, 24(3), 327–336.
    — Original binary indexed tree (Fenwick tree) data structure. The parent/child relationships define the update, parity, and occupation sets used in the Bravyi-Kitaev encoding.
