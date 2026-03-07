# Chapter 1: Why Tapering?

_In this chapter, you'll understand why encoded Hamiltonians have removable qubits and what that means for quantum simulation._

## In This Chapter

- **What you'll learn:** How fermion-to-qubit encoding creates symmetries that allow safe qubit removal
- **Why this matters:** Fewer qubits = smaller circuits = less noise = larger problems solvable on near-term hardware
- **Try this next:** Move to [Chapter 2 — The Diagonal Z₂ Approach](02-diagonal-z2-approach.html) to see how to detect and exploit these symmetries.

## From Encoding to Symmetry

Recall the encoding pipeline:

$$\text{Fermionic operators} \xrightarrow{\text{Jordan-Wigner, BK, etc.}} \text{Pauli strings on } n \text{ qubits}$$

Each fermionic ladder operator $a_i^\dagger$ or $a_i$ maps to a sum of Pauli strings. When you assemble a molecular Hamiltonian from these encoded operators, the result is a Pauli sum:

$$\hat{H}_\text{qubit} = \sum_{\alpha} c_\alpha \sigma_\alpha$$

where each $\sigma_\alpha$ is a Pauli string (product of single-qubit Paulis I, X, Y, Z).

### The Symmetry Observation

In many practical systems — especially those respecting **particle-number conservation** — the encoded Hamiltonian exhibits a special structure:

**For certain qubit indices $j$, every term in the Hamiltonian has only I or Z on qubit $j$, never X or Y.**

This is a **diagonal Z₂ symmetry**. It means:
- The measurement outcome on qubit $j$ is an eigenvalue of the Z operator ($+1$ or $-1$), not a superposition.
- The physics of the system is independent of whether qubit $j$ is measured to be spin-up or spin-down — **you can fix it permanently to one value**.
- Fixing that value **eliminates the qubit entirely without changing the spectrum**.

### Concrete Illustration

Consider this toy 4-qubit Hamiltonian:

$$\hat{H} = 0.8 \,\mathrm{ZIZI} - 0.4 \,\mathrm{ZZII} + 0.3 \,\mathrm{IIZZ}$$

Look at qubit index 1 (second column):
- In $\mathrm{ZIZI}$: position 1 is **I**
- In $\mathrm{ZZII}$: position 1 is **Z**
- In $\mathrm{IIZZ}$: position 1 is **I**

**Every term has I or Z at qubit 1.** Qubit 1 is a diagonal Z₂ symmetry.

The same is true for qubits 0, 2, and 3. In fact, all four qubits are diagonal Z₂ symmetric in this Hamiltonian!

### What "Fixing" a Qubit Means

If we decide that qubit 1 will have eigenvalue $+1$ (spin-up), then:
- Terms with $\mathrm{Z}$ on qubit 1 pick up a coefficient factor of $+1$
- Terms with $\mathrm{I}$ on qubit 1 are unaffected
- Qubit 1 can be **removed** from the tensor product

Example: fixing qubits 1 and 3 to the $+1$ sector:

$$\hat{H}' = (0.8 \cdot 1) \,\mathrm{ZI} + (-0.4 \cdot 1) \,\mathrm{ZI} + (0.3) \,\mathrm{II}$$
$$= 0.4 \,\mathrm{ZI} + 0.3 \,\mathrm{II}$$

The result is a **2-qubit Hamiltonian** (qubits 0 and 2), and it has exactly the same eigenvalues as the original 4-qubit system would have in the $(+1, +1)$ sector on qubits $(1, 3)$.

### Why It Matters: Circuit Depth

Quantum circuits for simulating a Hamiltonian scale with:

- **Number of qubits:** Every additional qubit doubles the Hilbert space dimension
- **Pauli weight:** Each term with weight $w$ requires $\sim 2(w-1)$ two-qubit gates

**Removing even one qubit halves the state space.** For a 12-qubit molecular Hamiltonian where 3 qubits are taperable, tapering gives you a 9-qubit system — an 8-fold reduction in Hilbert space.

### Scope: v1 Diagonal Z₂

FockMap's **v1 tapering** handles the "diagonal Z₂" case: generators of the form $Z_j$ where each term is diagonal on qubit $j$ (only I or Z). This covers:

- **Particle-number symmetries** (the most common and important)
- Single-qubit-only conservation laws
- Spin-projection symmetries (in spin-adapted encodings)

**v2 direction** (planned): General Z₂ generators (not just single-qubit), combined with **Clifford tapering** for multi-qubit stabiliser generators.

---

**Next:** [Chapter 2 — The Diagonal Z₂ Approach](02-diagonal-z2-approach.html)
