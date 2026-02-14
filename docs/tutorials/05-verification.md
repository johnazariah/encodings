# Chapter 5: Checking Our Answer

_Exact diagonalisation, the eigenspectrum, and cross-encoding comparison confirm the Hamiltonian is correct._

## Exact Diagonalisation

To verify the qubit Hamiltonian, we construct its $16 \times 16$ matrix representation. Each Pauli string $\sigma_\alpha$ corresponds to a known matrix (the tensor product of its single-qubit Pauli matrices). The full Hamiltonian matrix is:

$$H = \sum_\alpha c_\alpha \cdot \sigma_\alpha$$

where $c_\alpha$ are the 15 coefficients from [Chapter 4](04-building-hamiltonian.html).

Diagonalising this matrix gives 16 eigenvalues, grouped by particle-number sector (since the Hamiltonian conserves particle number):

| Sector ($N_e$) | Dimension | Eigenvalues (Ha, electronic) |
|:---:|:---:|:---|
| 0 | 1 | $0$ |
| 1 | 4 | $-1.2563,\ -1.2563,\ -0.4719,\ -0.4719$ |
| 2 | 6 | $-1.8573,\ -1.3390,\ -0.9032,\ -0.9032,\ -0.6753,\ 0.0$ |
| 3 | 4 | $-1.7282,\ -1.7282,\ -0.9438,\ -0.9438$ |
| 4 | 1 | $-2.2001$ |

The ground state of the $N_e = 2$ sector is $E_0^\text{el} = -1.8573$ Ha. Adding nuclear repulsion:

$$E_0^\text{total} = E_0^\text{el} + V_{nn} = -1.8573 + 0.7151 = -1.1422 \text{ Ha}$$

## Comparison with Known Results

The **Hartree–Fock** energy (single determinant $|1100\rangle$) is:

$$E_\text{HF} = 2h_{00} + [00|00] = 2(-1.2563) + 0.6745 = -1.8382 \text{ Ha (electronic)}$$

The **Full CI** correlation energy is:

$$E_\text{corr} = E_\text{FCI} - E_\text{HF} = -1.8573 - (-1.8382) = -0.0191 \text{ Ha} \approx -12.0 \text{ kcal/mol}$$

This correlation energy — about 1% of the total energy but ~12 kcal/mol — is precisely what makes quantum simulation valuable. It captures the effect of electron–electron correlation that the single-determinant Hartree–Fock approximation misses, and it is this quantity that determines chemical accuracy in reaction energies, barrier heights, and binding affinities.

## Cross-Encoding Comparison

The same Hamiltonian encoded under all five transforms:

| Encoding | Terms | Max Weight | Avg Weight | Identity Coeff (Ha) |
|:---:|:---:|:---:|:---:|:---:|
| Jordan–Wigner | 15 | 4 | 2.13 | $-1.0704$ |
| Bravyi–Kitaev | 15 | 4 | 2.40 | $-1.0704$ |
| Parity | 15 | 4 | 2.27 | $-1.0704$ |
| Balanced Binary | 15 | 4 | 2.27 | $-1.0704$ |
| Balanced Ternary | 15 | 4 | 2.40 | $-1.0704$ |

**All encodings produce the same number of terms with the same identity coefficient** — as they must, since the identity coefficient equals $\text{Tr}(\hat{H})/2^n$, which is invariant under any unitary change of basis.

At $n = 4$, the encodings are too small for the weight advantages of BK and tree encodings to manifest. At $n = 16$ (8 spatial orbitals), the max single-operator weight would be 16 for JW but only 5 for BK and 4 for balanced ternary — a significant reduction.

**All five encodings produce the same eigenspectrum** to machine precision ($|\Delta\lambda| < 5 \times 10^{-16}$), confirming that the encoding is a unitary change of basis that preserves the physics exactly.

For more details on the encodings and their scaling properties, see [Beyond Jordan–Wigner](../background/05-beyond-jordan-wigner.html).

The [Compare Encodings](../labs/03-compare-encodings.html) interactive lab reproduces this comparison as executable F# code.

---

**Previous:** [Chapter 4 — Building the H₂ Qubit Hamiltonian](04-building-hamiltonian.html)
**Next:** [Chapter 6 — What Comes Next](06-outlook.html)
