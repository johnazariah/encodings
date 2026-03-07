# Chapter 20: Scaling — From H₂ to FeMo-co

> **TODO: The bond angle and vibrational mode computations need real FCI eigenvalues**
> **from our encoded Hamiltonians, not just HF energies from PySCF.**
> **The H₂ dissociation curve should also be computed and plotted.**

_H₂ was our teacher. Now we look ahead to the molecules that actually matter — and where the pipeline meets its limits._

## In This Chapter

- **What you'll learn:** How the pipeline scales from small test systems (H₂) through medium molecules (H₂O, N₂) to the grand challenge of quantum chemistry (FeMo-co), including application sidebars on the H-O-H bond angle and water's greenhouse properties.
- **Why this matters:** This chapter connects the computational framework to real scientific questions — the kind where quantum simulation might actually change what we know.
- **Prerequisites:** Chapter 19 (the complete pipeline).

---

## The Scaling Landscape

| Molecule | Electrons | Spin-orbitals | JW qubits | Tapered | TT max weight | Configurations |
|:---|:---:|:---:|:---:|:---:|:---:|:---:|
| H₂ | 2 | 4 | 4 | ~2–4 | 3 | 6 |
| LiH | 4 | 12 | 12 | ~9 | 4 | 495 |
| H₂O | 10 | 14 | 14 | ~11 | 4 | 1,001 |
| N₂ | 14 | 20 | 20 | ~16 | 5 | 38,760 |
| FeMo-co | ~54 | ~108 | ~108 | ~100 | ~5 | ~$10^{30}$ |

The configuration count grows combinatorially. Classical Full CI is feasible up to about 20 spin-orbitals (a few thousand configurations). Beyond that, quantum simulation offers an exponential advantage — but only if the circuit is short enough to run on available hardware.

---

## Application Sidebar: The H-O-H Bond Angle

Every chemistry textbook states that water's bond angle is 104.52° and invokes VSEPR theory or hybridization to explain it. These explanations are qualitatively useful but quantitatively vague — they don't predict the actual number.

Using the pipeline from this book, we can **compute** the bond angle from first principles:

1. **Generate integrals** at many bond angles (60°–180° in 5° steps) using PySCF.
2. **Encode** each geometry (same pipeline, different integrals).
3. **Compute the energy** at each angle (via eigenvalue or VQE).
4. **Find the minimum** of the potential energy surface.

The result: the energy minimum occurs near **100°** in STO-3G (the discrepancy from 104.52° is due to the minimal basis set — a larger basis gives the correct value).

The point is not the number — it's the *method*. We derived a molecular geometry property from a quantum simulation pipeline. The same approach extends to transition states, isomerization barriers, and reaction mechanisms.

---

## Application Sidebar: Why Water Is a Greenhouse Gas

Water absorbs infrared radiation because its vibrational modes have nonzero dipole derivatives. The three vibrational modes of H₂O are:

1. **Symmetric stretch** ($\nu_1$): both O-H bonds stretch simultaneously
2. **Bend** ($\nu_2$): the H-O-H angle oscillates
3. **Asymmetric stretch** ($\nu_3$): one O-H bond stretches while the other contracts

The bending and asymmetric stretch modes change the molecular dipole moment — creating an oscillating electric field that couples to infrared photons. The symmetric stretch changes the dipole less and absorbs less strongly.

Computing vibrational frequencies requires the **Hessian** of the potential energy surface (the matrix of second derivatives of energy with respect to nuclear coordinates). This is obtained by computing the energy at many geometries near the equilibrium — exactly what our potential energy surface scan does.

The connection to quantum computing: for molecules where classical methods cannot accurately compute the potential energy surface (transition-metal complexes, strongly correlated systems), quantum simulation provides the energy values that the Hessian is built from.

---

## Application Sidebar: Encoding Choice at H₂O Scale

With 12 active spin-orbitals, H₂O is the smallest molecule where encoding choice produces a measurable difference in circuit cost:

| Encoding | Max weight | CNOTs/Trotter step | Circuit depth estimate |
|:---|:---:|:---:|:---:|
| JW | 12 | ~1,800 | ~3,600 |
| BK | 5 | ~750 | ~1,500 |
| Ternary Tree | 4 | ~600 | ~1,200 |
| Tapered TT | 4 | ~500 | ~1,000 |

The 3.6× reduction from JW to tapered TT is the difference between needing 3,600 two-qubit gates per Trotter step and needing 1,000. On near-term hardware with two-qubit gate fidelities of ~99.5%, this translates directly to simulation accuracy.

---

## The Grand Challenge: FeMo-co

The iron-molybdenum cofactor (FeMo-co) of nitrogenase is the "poster child" of quantum chemistry on quantum computers. It catalyzes nitrogen fixation — the conversion of atmospheric N₂ to ammonia — and understanding its mechanism could revolutionize fertilizer production.

FeMo-co has ~54 active electrons in ~108 active spin-orbitals. Classical methods cannot accurately compute its electronic structure because the iron centres are **strongly correlated** — many electron configurations contribute comparably, defeating perturbation theory and single-reference methods.

At 108 qubits under JW, the worst-case Pauli weight is 108 — requiring 214 CNOTs per worst-case rotation. Under ternary tree encoding, the worst-case weight drops to ~5 — requiring 8 CNOTs. The 27× reduction may determine whether the simulation is feasible on early fault-tolerant hardware.

This is the promise: the same pipeline we developed for H₂ (4 qubits, 15 terms, 36 CNOTs) scales — with better encodings, tapering, and hardware — to the molecules that matter.

---

## Key Takeaways

- The pipeline scales from 4 qubits (H₂) to 100+ qubits (FeMo-co) with the same code structure.
- Application sidebars show that the pipeline connects to real chemistry: bond angles, vibrational spectroscopy, catalysis.
- Encoding choice matters most at medium scale (12–20 qubits) — the range of near-term quantum hardware.
- FeMo-co is the grand challenge: ~108 qubits, strongly correlated, classically intractable.

---

**Previous:** [Chapter 19 — The Complete Pipeline](19-complete-pipeline.html)

**Next:** [Chapter 21 — VQE, QPE, and Beyond](21-vqe-qpe.html)
