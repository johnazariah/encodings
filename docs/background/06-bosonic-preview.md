# Bosonic Operators (Preview)

> **Status:** This feature is planned for FockMap v0.2.0. The content below provides a preview of what's coming.

So far, we've focused entirely on *fermionic* systems: particles that obey the Pauli exclusion principle and require anti-commutation relations. But many quantum systems involve *bosons*—particles like photons and phonons that can pile up arbitrarily in the same state.

## Bosons vs. Fermions

The key difference between bosons and fermions lies in their commutation relations:

**Fermions** (anti-commute):
$$
\{a_i, a^\dagger_j\} = a_i a^\dagger_j + a^\dagger_j a_i = \delta_{ij}
$$

**Bosons** (commute):
$$
[b_i, b^\dagger_j] = b_i b^\dagger_j - b^\dagger_j b_i = \delta_{ij}
$$

For fermions, each mode holds at most 1 particle (occupation $n_j \in \{0, 1\}$). For bosons, there's no limit—a single mode can hold 0, 1, 2, ... particles.

This creates a problem for qubit simulation: bosonic Hilbert space is *infinite-dimensional* per mode.

## Truncated Fock Space

The standard solution is to **truncate** each bosonic mode to at most $d$ particles:

$$
n_j \in \{0, 1, 2, \ldots, d-1\}
$$

Each mode then requires $\lceil \log_2 d \rceil$ qubits to represent. For $n$ modes with cutoff $d$, the total Hilbert space dimension is $d^n$.

Common choices:
- $d = 2$: Binary truncation (bosons behave like fermions)
- $d = 4$: Two qubits per mode
- $d = 8$: Three qubits per mode (common for photonic systems)

## Encoding Strategies

Several strategies exist for mapping truncated bosonic operators to qubits:

### Unary Encoding

The simplest approach uses $d$ qubits per mode in a "one-hot" representation:

$$
|n\rangle \mapsto |0\rangle^{\otimes n} \otimes |1\rangle \otimes |0\rangle^{\otimes (d-1-n)}
$$

Bosonic operators become shift operations on this register. Simple to implement, but uses many qubits.

### Gray Code Encoding

A more qubit-efficient approach uses Gray codes. The occupation number $n$ is encoded in $\lceil \log_2 d \rceil$ qubits using a Gray code, where successive values differ by exactly one bit.

This minimizes the Pauli weight of the bosonic ladder operators, similar to how Bravyi-Kitaev minimizes weight for fermions.

### Binary Encoding

Direct binary representation of occupation numbers. Requires fewer qubits than unary, but bosonic operators may have higher Pauli weight.

## Planned API

FockMap v0.2.0 will introduce:

```fsharp
// Truncated bosonic mode with cutoff d
type BosonicMode = { Index : int; Cutoff : int }

// Encoding schemes for bosons
let unaryBosonicScheme (d : int) : BosonicEncodingScheme
let grayCodeBosonicScheme (d : int) : BosonicEncodingScheme
let binaryBosonicScheme (d : int) : BosonicEncodingScheme

// Encode bosonic operators
let encodeBosonicOperator (scheme : BosonicEncodingScheme)
                          (op : BosonicOperator)
                          (j : int) (n : int) : PauliRegisterSequence
```

## Coming in v0.2.0

- Truncated bosonic Fock space representation
- Unary, Gray code, and binary encodings
- Creation/annihilation operators $b^\dagger_j$, $b_j$ for bosons
- Number operator $\hat{n}_j = b^\dagger_j b_j$
- Mixed fermion-boson systems (e.g., electron-phonon coupling)
- Documentation and tutorials for bosonic simulation

Stay tuned for updates, or follow the [GitHub repository](https://github.com/fockmap/fockmap) for development progress.

---

**Previous:** [Beyond Jordan-Wigner](05-beyond-jordan-wigner.html) — BK, trees, and O(log n) encodings
