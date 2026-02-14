# Chapter 4: Building the H₂ Qubit Hamiltonian

_This is the payoff. We take the spin-orbital Hamiltonian, apply the Jordan–Wigner encoding, and construct the qubit Hamiltonian term by term._

For background on why encoding is necessary (fermions anti-commute, qubits commute), see [Why Encodings?](../background/01-why-encodings.html). For the Jordan–Wigner transform itself, see [Background: Jordan–Wigner](../background/04-jordan-wigner.html).

## The Recipe

1. For each non-zero one-body integral $h_{pq}$: encode $a^\dagger_p$ and $a_q$ as Pauli strings, multiply them, multiply by $h_{pq}$.
2. For each non-zero two-body integral $\langle pq|rs\rangle$: encode all four ladder operators, multiply the four Pauli strings, multiply by $\frac{1}{2}\langle pq|rs\rangle$.
3. Sum all terms, collecting Pauli strings with the same signature and adding their coefficients.
4. Add $V_{nn} \cdot IIII$ (nuclear repulsion as a constant offset).

## One-Body Terms

The non-zero one-body integrals for H₂ are all diagonal: $h_{00}$, $h_{11}$, $h_{22}$, $h_{33}$ (in the spin-orbital basis). These are number operators $\hat{n}_j = a^\dagger_j a_j$.

Under Jordan–Wigner:

$$\hat{n}_j = a^\dagger_j a_j = \frac{1}{2}(c_j - id_j)\cdot\frac{1}{2}(c_j + id_j) = \frac{1}{2}(I - Z_j)$$

(The $Z$-chains cancel because both $c_j$ and $d_j$ have the same chain.)

So the one-body contribution is:

$$\hat{H}_1 = \sum_j h_{jj} \cdot \frac{1}{2}(I - Z_j)$$

$$= \frac{1}{2}(h_{00} + h_{11} + h_{22} + h_{33})\cdot IIII - \frac{h_{00}}{2}\, IIIZ - \frac{h_{11}}{2}\, IIZI - \frac{h_{22}}{2}\, IZII - \frac{h_{33}}{2}\, ZIII$$

Substituting $h_{00} = h_{11} = -1.2563$ and $h_{22} = h_{33} = -0.4719$:

| Term | Coefficient (Ha) |
|:---:|:---:|
| $IIII$ | $-1.7282$ |
| $IIIZ$ | $+0.6282$ |
| $IIZI$ | $+0.6282$ |
| $IZII$ | $+0.2359$ |
| $ZIII$ | $+0.2359$ |

## Two-Body Terms

The two-body terms are more involved. Consider one representative term:

$$\frac{1}{2}\langle 0\alpha\, 0\beta | 0\alpha\, 0\beta\rangle\, a^\dagger_0 a^\dagger_1 a_1 a_0$$

This describes two electrons in $\sigma_g$ (one spin-up, one spin-down) repelling each other.

Encoding each operator under JW:
- $a^\dagger_0 \to \frac{1}{2}(XIII - iYIII)$
- $a^\dagger_1 \to \frac{1}{2}(ZXII - iZYII)$
- $a_1 \to \frac{1}{2}(ZXII + iZYII)$
- $a_0 \to \frac{1}{2}(XIII + iYIII)$

The product of four Pauli strings, after simplification and coefficient tracking, contributes to $IIII$, $IIIZ$, $IIZI$, and $IIZZ$ terms.

## The Complete 15-Term Hamiltonian

After processing all 32 non-zero two-body integrals and combining like terms:

| # | Pauli String | Coefficient (Ha) |
|:---:|:---:|:---:|
| 1 | $IIII$ | $-1.0704$ |
| 2 | $IIIZ$ | $-0.0958$ |
| 3 | $IIZI$ | $-0.0958$ |
| 4 | $IZII$ | $+0.3021$ |
| 5 | $ZIII$ | $+0.3021$ |
| 6 | $IIZZ$ | $+0.1743$ |
| 7 | $IZIZ$ | $-0.0085$ |
| 8 | $IZZI$ | $+0.1659$ |
| 9 | $ZIIZ$ | $+0.1659$ |
| 10 | $ZIZI$ | $-0.0085$ |
| 11 | $ZZII$ | $+0.1686$ |
| 12 | $XXYY$ | $-0.1744$ |
| 13 | $XYYX$ | $+0.1744$ |
| 14 | $YXXY$ | $+0.1744$ |
| 15 | $YYXX$ | $-0.1744$ |

## Interpreting the Terms

**Z-only terms (rows 2–11):** These represent classical electrostatic interactions — Coulomb repulsion and orbital energies. They are diagonal in the computational basis, meaning they can be measured by preparing a computational basis state and reading out.

**XXYY-type terms (rows 12–15):** These represent **quantum exchange** — a fundamentally non-classical effect arising from the indistinguishability of electrons. Without these terms, the Hamiltonian would have no off-diagonal elements in the computational basis and could not produce entanglement.

The exchange terms are what makes quantum simulation **necessary**: they create superpositions between different electron configurations, and capturing their contribution accurately is where classical approximations break down.

## Reproducing This With FockMap

The companion library computes this entire Hamiltonian in a few lines:

```fsharp
open Encodings

let hamiltonian = computeHamiltonian jordanWigner oneBodyIntegrals twoBodyIntegrals 4u
// → 15 Pauli terms with coefficients matching the table above
```

See the [H₂ Molecule lab](../labs/02-h2-molecule.html) for the full executable example.

---

**Previous:** [Chapter 3 — From Spatial to Spin-Orbital Integrals](03-spin-orbitals.html)
**Next:** [Chapter 5 — Checking Our Answer](05-verification.html)
