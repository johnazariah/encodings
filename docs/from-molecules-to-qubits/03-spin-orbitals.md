# Chapter 3: From Spatial to Spin-Orbital Integrals

_In this chapter, you'll expand spatial integrals into the spin-orbital form used for encoding._

## In This Chapter

- **What you'll learn:** How to expand spatial-orbital integrals into spin-orbital one-body and two-body terms.
- **Why this matters:** Encoding operates on spin-orbital ladder operators, so this conversion is mandatory.
- **Try this next:** Continue to [Chapter 4 — Building the H₂ Qubit Hamiltonian](04-building-hamiltonian.html) to encode term-by-term.

The molecular integrals computed by quantum chemistry codes are in the **spatial orbital** basis (2 orbitals for H₂). But the fermionic operators act on **spin-orbitals** (4 for H₂), because each spatial orbital can hold one electron of each spin.

## Spin-Orbital Indexing

Each spatial orbital $p$ gives rise to two spin-orbitals: $p\alpha$ (spin up) and $p\beta$ (spin down).

We use **interleaved** indexing:

| Spin-orbital index | Spatial orbital | Spin |
|:---:|:---:|:---:|
| 0 | 0 ($\sigma_g$) | $\alpha$ |
| 1 | 0 ($\sigma_g$) | $\beta$ |
| 2 | 1 ($\sigma_u$) | $\alpha$ |
| 3 | 1 ($\sigma_u$) | $\beta$ |

The conversion rules:
- Spatial orbital index $= \lfloor j/2 \rfloor$ (integer division)
- Spin $= j \bmod 2$ (0 = $\alpha$, 1 = $\beta$)

## One-Body Expansion

The spin-orbital one-body integral is:

$$h^\text{spin}_{pq} = h^\text{spatial}_{p/2,\, q/2} \times \delta(\sigma_p, \sigma_q)$$

In words: the integral equals the spatial integral if the spins match, and zero otherwise. An electron cannot change its spin through one-body interactions (in the non-relativistic limit).

For H₂, this gives 4 non-zero entries — all diagonal:

| $p$ | $q$ | $h^\text{spin}_{pq}$ (Ha) | Origin |
|:---:|:---:|:---:|:---|
| $0\alpha$ | $0\alpha$ | $-1.2563$ | $h^\text{spatial}_{00}$, same spin |
| $0\beta$  | $0\beta$  | $-1.2563$ | $h^\text{spatial}_{00}$, same spin |
| $1\alpha$ | $1\alpha$ | $-0.4719$ | $h^\text{spatial}_{11}$, same spin |
| $1\beta$  | $1\beta$  | $-0.4719$ | $h^\text{spatial}_{11}$, same spin |

## Two-Body Expansion

The spin-orbital two-body integral in physicist's notation is:

$$\langle pq \mid rs\rangle_\text{spin} = \left[\frac{p}{2}\frac{r}{2}\bigg\mid\frac{q}{2}\frac{s}{2}\right]_\text{spatial} \times \delta(\sigma_p, \sigma_r) \times \delta(\sigma_q, \sigma_s)$$

Both electrons must independently conserve spin: electron 1 (indices $p, r$) keeps its spin, and electron 2 (indices $q, s$) keeps its spin.

This generates more non-zero integrals than one might expect, because **cross-spin** terms are allowed. For example, $\langle 0\alpha\, 1\beta \mid 0\alpha\, 1\beta\rangle$ is non-zero: electron 1 stays spin-$\alpha$ and electron 2 stays spin-$\beta$.

> **Common error:** If you include only same-spin blocks ($\alpha\alpha$ and $\beta\beta$) and omit the cross-spin blocks ($\alpha\beta$ and $\beta\alpha$), your Hamiltonian will contain only Z-type (diagonal) Pauli terms and no XX/YY excitation terms. The eigenvalues will be wrong. This was our actual first-implementation bug.

For H₂, there are 32 non-zero spin-orbital two-body integrals (before symmetry reduction).

## The Complete Spin-Orbital Hamiltonian

Combining one-body (4 terms) and two-body (32 terms, with $\frac{1}{2}$ prefactor), plus the nuclear repulsion constant:

$$\hat{H} = V_{nn}\cdot\hat{I} + \sum_{pq} h^\text{spin}_{pq}\, a^\dagger_p a_q + \frac{1}{2}\sum_{pqrs} \langle pq \mid rs\rangle_\text{spin}\, a^\dagger_p a^\dagger_q a_s a_r$$

with $V_{nn} = 0.7151$ Ha.

## Integral Tables

### Molecular Parameters

| Parameter | Value |
|:---:|:---:|
| Bond length $R$ | 0.7414 Å = 1.401 Bohr |
| Nuclear repulsion $V_{nn}$ | 0.7151043391 Ha |
| Spatial orbitals | 2 ($\sigma_g$, $\sigma_u$) |
| Spin-orbitals | 4 |
| Electrons | 2 |

### Spatial One-Body Integrals $h_{pq}$ (Ha)

|  | $q = 0$ ($\sigma_g$) | $q = 1$ ($\sigma_u$) |
|:---:|:---:|:---:|
| $p = 0$ | $-1.2563390730$ | $0$ |
| $p = 1$ | $0$ | $-0.4718960244$ |

### Spatial Two-Body Integrals $[pq \mid rs]$ (Ha)

| Integral | Value |
|:---:|:---:|
| $[00\|00]$ | $0.6744887663$ |
| $[11\|11]$ | $0.6973979495$ |
| $[00\|11] = [11\|00]$ | $0.6636340479$ |
| $[01\|10] = [10\|01] = [01\|01] = [10\|10]$ | $0.6975782469$ |

All other elements are zero by symmetry. These integrals are reproduced by the companion code — see the [H₂ Molecule lab](../labs/02-h2-molecule.html).

With the spin-orbital tables ready, we can now do the core step: encode each fermionic term into Pauli strings and assemble the full qubit Hamiltonian.

---

**Previous:** [Chapter 2 — The Notation Minefield](02-notation.html)
**Next:** [Chapter 4 — Building the H₂ Qubit Hamiltonian](04-building-hamiltonian.html)
