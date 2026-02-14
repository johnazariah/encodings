# Chapter 1: The Electronic Structure Problem

_In this chapter, you'll turn the molecular problem into a finite spin-orbital model._

## In This Chapter

- **What you'll learn:** How H₂ goes from a continuous molecular Schrödinger equation to a finite spin-orbital model.
- **Why this matters:** This is the starting point for every encoding and Hamiltonian step that follows.
- **Try this next:** Continue to [Chapter 2 — The Notation Minefield](02-notation.html) to avoid common integral-convention mistakes.

## The Schrödinger Equation for Molecules

The full molecular Hamiltonian for a molecule with $M$ nuclei (charges $Z_A$, masses $M_A$, positions $\mathbf{R}_A$) and $N$ electrons (mass $m_e$, positions $\mathbf{r}_i$) is:

$$
\hat{H} = -\sum_{A=1}^{M} \frac{\hbar^2}{2M_A} \nabla_A^2
           -\sum_{i=1}^{N} \frac{\hbar^2}{2m_e} \nabla_i^2
           + \sum_{A<B} \frac{Z_A Z_B e^2}{|\mathbf{R}_A - \mathbf{R}_B|}
           - \sum_{i,A} \frac{Z_A e^2}{|\mathbf{r}_i - \mathbf{R}_A|}
           + \sum_{i<j} \frac{e^2}{|\mathbf{r}_i - \mathbf{r}_j|}
$$

For H₂, this means two protons ($A$ and $B$, separated by distance $R$) and two electrons (1 and 2). The Hamiltonian is a function of six electronic coordinates plus the internuclear distance $R$.

For the hydrogen *atom* (one electron, one proton), this equation can be solved analytically — giving the familiar $1s$, $2s$, $2p$, … orbitals. For two electrons, exact analytical solution is already impossible. The electron–electron repulsion term $e^2/\lvert \mathbf{r}_1 - \mathbf{r}_2 \rvert$ couples the two electrons, making the equation non-separable.

## The Born–Oppenheimer Approximation

Protons are roughly 1836 times heavier than electrons. On the timescale of electronic motion, the nuclei are nearly stationary. The Born–Oppenheimer approximation treats the nuclear positions $\{\mathbf{R}_A\}$ as fixed parameters rather than dynamical variables.

The result is the **electronic Hamiltonian**:

$$
\hat{H}_\text{el} = -\sum_{i=1}^{N} \frac{\hbar^2}{2m_e} \nabla_i^2
                     - \sum_{i,A} \frac{Z_A e^2}{|\mathbf{r}_i - \mathbf{R}_A|}
                     + \sum_{i<j} \frac{e^2}{|\mathbf{r}_i - \mathbf{r}_j|}
$$

The nuclear–nuclear repulsion $V_{nn} = Z_A Z_B e^2 / R$ is just a constant for fixed $R$.

For H₂ at the equilibrium bond length $R = 0.7414$ Å (= 1.401 Bohr):

$$V_{nn} = \frac{e^2}{R} = 0.7151 \text{ Ha}$$

> **Note:** The Born–Oppenheimer approximation is the standard starting point for essentially all electronic structure theory, classical and quantum alike. It is not limiting for our purposes.

## Basis Sets: Turning Continuous into Discrete

The electronic Hamiltonian acts on wavefunctions of $3N$ continuous variables. To make the problem finite-dimensional, we expand the molecular orbitals in a finite set of known functions — the **basis set**.

### Atomic Orbitals

The hydrogen atom eigenstates ($1s$, $2s$, $2p$, …) have exponential (Slater-type) radial dependence $e^{-\zeta r}$, but integrals involving products of exponentials on different centres are analytically intractable. The practical solution: approximate each Slater-type orbital by a sum of Gaussians $e^{-\alpha r^2}$. Gaussians have the wonderful property that the product of two Gaussians is another Gaussian.

### STO-3G

The "Slater-Type Orbital, 3 Gaussians" basis set approximates each atomic orbital by 3 Gaussian functions. It is the smallest meaningful basis set. For hydrogen, STO-3G provides one basis function per atom: the $1s$ orbital.

### Molecular Orbitals for H₂

With one $1s$ orbital on each hydrogen atom, the Linear Combination of Atomic Orbitals (LCAO) procedure gives two molecular orbitals:

$$\sigma_g = \frac{1s_A + 1s_B}{\sqrt{2(1+S)}} \qquad \text{(bonding)}$$

$$\sigma_u = \frac{1s_A - 1s_B}{\sqrt{2(1-S)}} \qquad \text{(antibonding)}$$

where $S = \langle 1s_A \mid 1s_B \rangle$ is the overlap integral. The bonding orbital $\sigma_g$ has lower energy (electron density concentrated between the nuclei), while the antibonding orbital $\sigma_u$ has a node at the midpoint.

With 2 molecular orbitals and 2 spin states ($\alpha$ = spin-up, $\beta$ = spin-down), we have $2 \times 2 = 4$ **spin-orbitals**.

## The Configuration Space

Two electrons distributed among 4 spin-orbitals can be arranged in $\binom{4}{2} = 6$ ways:

| Configuration | Notation | Description |
|:---:|:---:|:---|
| $\|1100\rangle$ | $\sigma_{g\alpha}\, \sigma_{g\beta}$ | Both in bonding orbital (ground state) |
| $\|1010\rangle$ | $\sigma_{g\alpha}\, \sigma_{u\alpha}$ | One in each, same spin |
| $\|1001\rangle$ | $\sigma_{g\alpha}\, \sigma_{u\beta}$ | One in each, opposite spin |
| $\|0110\rangle$ | $\sigma_{g\beta}\, \sigma_{u\alpha}$ | One in each, opposite spin |
| $\|0101\rangle$ | $\sigma_{g\beta}\, \sigma_{u\beta}$ | One in each, same spin |
| $\|0011\rangle$ | $\sigma_{u\alpha}\, \sigma_{u\beta}$ | Both in antibonding orbital |

The **exact** ground state of H₂ is a superposition of these six configurations. The Hartree–Fock approximation uses only the first ($\lvert1100\rangle$), capturing about 99% of the energy. The remaining 1% — the **correlation energy** — is what makes quantum simulation valuable.

> **Key observation:** These occupation vectors $\lvert n_0 n_1 n_2 n_3\rangle$ look exactly like qubit computational basis states $\lvert q_0 q_1 q_2 q_3\rangle$. This is not a coincidence — it is why quantum simulation of chemistry works. But the correspondence is not as simple as setting qubit $j$ = occupation of orbital $j$, because fermions and qubits obey different algebraic rules (see [Why Encodings?](../theory/01-why-encodings.html)).

## Second Quantization

Rather than tracking individual electrons, second quantization tracks which **orbitals** are occupied. The antisymmetry of the wavefunction (which would require $N!$ terms in a Slater determinant) is absorbed into the **operators**.

For a detailed treatment, see [Theory: Second Quantization](../theory/02-second-quantization.html).

The key result: the electronic Hamiltonian becomes

$$\hat{H} = \sum_{pq} h_{pq}\, a^\dagger_p a_q + \frac{1}{2}\sum_{pqrs} \langle pq \mid rs\rangle\, a^\dagger_p a^\dagger_q a_s a_r$$

where $h_{pq}$ are one-body integrals (kinetic energy + electron–nucleus attraction) and $\langle pq \mid rs\rangle$ are two-body integrals (electron–electron repulsion) in physicist's notation.

For H₂ in STO-3G, the non-zero one-body integrals are:

| Integral | Value (Ha) | Physical meaning |
|:---:|:---:|:---|
| $h_{00}$ | $-1.2563$ | $\sigma_g$ orbital energy |
| $h_{11}$ | $-0.4719$ | $\sigma_u$ orbital energy |

The off-diagonal elements $h_{01} = h_{10} = 0$ by symmetry.

With the physical model now in place, the next step is to make sure our integral notation is consistent before building any encoded Hamiltonian terms.

---

**Next:** [Chapter 2 — The Notation Minefield](02-notation.html)
