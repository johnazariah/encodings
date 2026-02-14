# Chapter 2: The Notation Minefield

_In this chapter, you'll align integral notation so every later coefficient and sign is correct._

## In This Chapter

- **What you'll learn:** How chemist's and physicist's notations differ, and how to convert safely.
- **Why this matters:** Small index-order mistakes can produce believable-but-wrong Hamiltonians.
- **Try this next:** Move to [Chapter 3 — From Spatial to Spin-Orbital Integrals](03-spin-orbitals.html) to build the actual spin-orbital tables used in encoding.

This section exists to save the reader weeks of debugging.

## Chemist's Notation

Chemist's notation (Mulliken notation, charge-density notation) groups indices by **spatial coordinate**:

$$[pq \mid rs] = \iint \phi_p^*(\mathbf{r}_1)\phi_q(\mathbf{r}_1)\, \frac{1}{r_{12}}\, \phi_r^*(\mathbf{r}_2)\phi_s(\mathbf{r}_2)\, d\mathbf{r}_1\, d\mathbf{r}_2$$

The bracket $[pq\mid$ refers to electron 1 (at $\mathbf{r}_1$), and $\mid rs]$ refers to electron 2. Within each bracket, the first index is the complex conjugate (bra) and the second is the ket.

## Physicist's Notation

Physicist's notation (Dirac notation — confusingly, not the same as bra-ket notation for states) groups indices by **particle**:

$$\langle pq \mid rs\rangle = \iint \phi_p^*(\mathbf{r}_1)\phi_q^*(\mathbf{r}_2)\, \frac{1}{r_{12}}\, \phi_r(\mathbf{r}_1)\phi_s(\mathbf{r}_2)\, d\mathbf{r}_1\, d\mathbf{r}_2$$

Here $p$ and $r$ belong to electron 1, while $q$ and $s$ belong to electron 2. Convention: bra indices on the left ($p, q$), ket indices on the right ($r, s$).

## The Conversion

Comparing the two definitions:

$$\boxed{\langle pq \mid rs\rangle_\text{physicist} = [pr \mid qs]_\text{chemist}}$$

The indices get **shuffled**: the physicist's bra-ket pairs $(p, r)$ and $(q, s)$ become the chemist's coordinate pairs, but the positions within each bracket change.

For H₂, this means:
- $\langle 00 \mid 11\rangle = [01 \mid 01]$ — these are *different integrals* with different values!
- $[00 \mid 11]$ is the Coulomb integral $J_{01}$ (density–density repulsion)
- $\langle 00 \mid 11\rangle = [01 \mid 01]$ is an exchange-type integral

## Which Notation for the Hamiltonian?

The second-quantized Hamiltonian uses **physicist's** notation:

$$\hat{H} = \sum_{pq} h_{pq}\, a^\dagger_p a_q + \frac{1}{2}\sum_{pqrs} \langle pq \mid rs\rangle\, a^\dagger_p a^\dagger_q a_s a_r$$

If you have integrals in chemist's notation (which most quantum chemistry codes output), you must convert before plugging into this formula.

Equivalently, the Hamiltonian in chemist's notation is:

$$\hat{H} = \sum_{pq} h_{pq}\, a^\dagger_p a_q + \frac{1}{2}\sum_{pqrs} [pr \mid qs]\, a^\dagger_p a^\dagger_q a_s a_r$$

## Common Errors

> **Error 1:** Using chemist's integrals $[pq \mid rs]$ directly in the physicist's formula (or vice versa). This permutes the indices and gives wrong coefficients.
>
> **Error 2:** Forgetting the $\frac{1}{2}$ prefactor on the two-body term. This double-counts electron–electron interactions.
>
> **Error 3:** Writing the operator ordering as $a^\dagger_p a^\dagger_q a_r a_s$ instead of $a^\dagger_p a^\dagger_q a_s a_r$. The $r$ and $s$ are reversed relative to the integral. Getting this wrong flips signs on certain terms.

All three errors produce Hamiltonians that "look right" — they have plausible structure, correct symmetry, and even the right number of terms. But the eigenvalues are wrong. The only reliable way to catch them is to verify against known results (see [Chapter 5: Checking Our Answer](05-verification.html)).

Now that notation is pinned down, we can safely expand the spatial integrals into the spin-orbital form used by the encoding pipeline.

---

**Previous:** [Chapter 1 — The Electronic Structure Problem](01-electronic-structure.html)
**Next:** [Chapter 3 — From Spatial to Spin-Orbital Integrals](03-spin-orbitals.html)
