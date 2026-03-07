# Chapter 2: The Diagonal Z₂ Approach

_In this chapter, you'll learn to detect diagonal Z₂ symmetries and understand the machinery behind sector fixing._

## In This Chapter

- **What you'll learn:** How to algorithmically detect diagonal Z₂ qubits, what sectors are, and how fixing a sector modifies the Hamiltonian
- **Why this matters:** Before you can taper, you must know which qubits are safe to remove and what choices you have
- **Try this next:** Jump to [Chapter 3 — FockMap Implementation](03-fockmap-implementation.html) to code it up.

## Detection: Which Qubits Are Z₂ Symmetric?

Given a Hamiltonian $\hat{H} = \sum_\alpha c_\alpha \sigma_\alpha$, we say qubit $j$ is **diagonal Z₂ symmetric** if:

$$\forall \alpha: \sigma_\alpha[j] \in \{\mathrm{I}, \mathrm{Z}\}$$

In words: on every single Pauli term $\sigma_\alpha$, the Pauli operator at position $j$ is either the identity $\mathrm{I}$ or $\mathrm{Z}$, never $\mathrm{X}$ or $\mathrm{Y}$.

### Algorithmic Check

For each qubit index $j = 0, 1, \ldots, n-1$:
1. Inspect every term in the Hamiltonian
2. Extract the Pauli operator at position $j$
3. If all operators are I or Z, then $j$ is diagonal Z₂ symmetric
4. If any operator is X or Y, then $j$ is **not** diagonal Z₂ symmetric

**Example:** In the Hamiltonian $\hat{H} = 0.8 \,\mathrm{ZIZI} - 0.4 \,\mathrm{ZZII} + 0.3 \,\mathrm{IIZZ}$:

| Qubit | Position 0 | Position 1 | Position 2 | Position 3 | Diagonal? |
|:---:|:---:|:---:|:---:|:---:|:---|
| Term 1: ZIZI | Z | I | Z | I | — |
| Term 2: ZZII | Z | Z | I | I | — |
| Term 3: IIZZ | I | I | Z | Z | — |
| **Result** | Z, Z, I → **✓** | I, Z, I → **✓** | Z, I, Z → **✓** | I, I, Z → **✓** | **All 4 diagonal** |

All qubits are candidates for removal.

## Sectors: Choosing Eigenvalues

For a diagonal Z₂ symmetric qubit $j$, we can **fix its Z eigenvalue** to be either $+1$ (spin-up) or $-1$ (spin-down).

A **sector** is a choice of eigenvalue for each qubit we plan to remove.

Mathematically, a sector is a list of pairs:

$$\text{sector} = \{(j_1, \lambda_1), (j_2, \lambda_2), \ldots\}$$

where $j_i$ is a qubit index and $\lambda_i \in \{+1, -1\}$ is the chosen eigenvalue of $Z_{j_i}$.

**Example sectors for the 4-qubit Hamiltonian:**

- `sector = [(0, +1), (1, +1)]` — "Fix qubits 0 and 1 to spin-up; leave 2 and 3"
- `sector = [(1, -1), (3, +1)]` — "Fix qubit 1 to spin-down, qubit 3 to spin-up; leave 0 and 2"
- `sector = [(0, +1), (1, +1), (2, +1), (3, +1)]` — "Fix all four to spin-up; reduce to 0 qubits (scalar)"

## Modification: How Fixing a Sector Changes Terms

When you fix a sector, each Pauli term is modified as follows:

1. For each $(j, \lambda)$ in the sector:
   - If term $\sigma_\alpha$ has **I** at position $j$: no change
   - If term $\sigma_\alpha$ has **Z** at position $j$: multiply the coefficient by $\lambda$

2. Remove qubit $j$ from the tensor product (delete its position from every Pauli string)

### Worked Example

Start with:
$$\hat{H} = 0.8 \,\mathrm{ZIZI} - 0.4 \,\mathrm{ZZII} + 0.3 \,\mathrm{IIZZ}$$

Fix sector `[(1, +1), (3, -1)]` — qubits 1 and 3 to eigenvalues +1 and −1 respectively.

**Process term by term:**

| Original | Qubit 1 | Factor | Qubit 3 | Factor | New Coeff | Remaining |
|:---:|:---:|:---:|:---:|:---:|:---:|:---|
| $0.8 \mathrm{ZIZI}$ | I | ×1 | I | ×1 | $0.8$ | $\mathrm{ZZ}$ |
| $-0.4 \mathrm{ZZII}$ | Z | ×(+1) | I | ×1 | $-0.4$ | $\mathrm{ZI}$ |
| $0.3 \mathrm{IIZZ}$ | I | ×1 | Z | ×(−1) | $-0.3$ | $\mathrm{IZ}$ |

**Result:**
$$\hat{H}' = 0.8 \,\mathrm{ZZ} - 0.4 \,\mathrm{ZI} - 0.3 \,\mathrm{IZ}$$

## Multiple Sectors: Different Physics in Each

For a system with $k$ removable qubits, there are $2^k$ possible sectors. Each sector gives a **valid, eigenvalue-preserving tapered Hamiltonian**, but they may represent different **quantum numbers** or **molecular states**.

To recover the ground state of the full problem:

1. Compute eigenvalues in **all** $2^k$ sectors
2. Find the global minimum across sectors

## Sign Conventions and Validation

To avoid bugs:

1. **Always validate before tapering** — check that all qubits in your sector list are actually diagonal Z₂ symmetric
2. **Check sector eigenvalues** — must be exactly $+1$ or $-1$, not other values
3. **Trace coefficients carefully** — when a Z eigenvalue is $-1$, that factor multiplies the coefficient. This is not a sign error; it is the physics.

---

**Previous:** [Chapter 1 — Why Tapering?](01-why-tapering.html)

**Next:** [Chapter 3 — FockMap Implementation](03-fockmap-implementation.html)
