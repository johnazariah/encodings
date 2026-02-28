# Spec: Trotterization Module for FockMap

**Status**: Proposed
**Date**: 2026-02-27
**Author**: John S Azariah

---

## 1. Motivation

FockMap currently terminates at the symbolic Hamiltonian level: a
`PauliRegisterSequence` representing $H = \sum_k c_k P_k$.  The natural
next step is Trotterization — decomposing $e^{-iHt}$ into a sequence
of Pauli rotations that can be compiled to a quantum circuit.

This module would:
- Complete the pipeline from chemistry integrals to executable circuits
- Make Pauli weight savings concrete (measurable CNOT counts)
- Produce output in Q# or OpenQASM for real hardware execution
- Remain purely symbolic — no matrix simulation

---

## 2. Architecture

```
PauliRegisterSequence                     (existing — end of current pipeline)
        │
        ▼
┌─────────────────────┐
│  Trotter Decomposer │    ← NEW: this spec
│                     │
│  • Term ordering    │
│  • Step size        │
│  • Order (1st/2nd)  │
└─────────────────────┘
        │
        ▼
TrotterCircuit                            (NEW type: ordered Pauli rotations)
        │
        ├──▶ CircuitStats        (gate counts, depth, CNOT count)
        ├──▶ OpenQASM 3.0        (text output)
        └──▶ Q# operations       (text output)
```

---

## 3. New Types

### 3.1 `PauliRotation`

A single rotation $e^{-i\theta P}$ where $P$ is a Pauli string.

```fsharp
type PauliRotation = {
    /// The Pauli string to rotate around
    Operator : PauliRegister
    /// Rotation angle θ (so the gate is e^{-iθP})
    Angle    : float
}
```

**Design note**: The `PauliRegister` here should have coefficient = 1
(the coefficient gets absorbed into the angle).  We factor
$c_k P_k \Delta t$ into angle $= \text{Re}(c_k) \cdot \Delta t$ and
pure Pauli string $P_k$.

### 3.2 `TrotterStep`

One complete Trotter step (a product of Pauli rotations).

```fsharp
type TrotterStep = {
    /// Ordered sequence of Pauli rotations for this step
    Rotations : PauliRotation list
    /// Trotter order (1 = first-order, 2 = second-order symmetric)
    Order     : int
}
```

### 3.3 `TrotterCircuit`

A complete Trotterized circuit for time evolution.

```fsharp
type TrotterCircuit = {
    /// Number of qubits
    NumQubits  : int
    /// Number of Trotter steps (repetitions)
    NumSteps   : int
    /// Total simulation time
    Time       : float
    /// Step size Δt = Time / NumSteps
    StepSize   : float
    /// The single Trotter step (repeated NumSteps times)
    Step       : TrotterStep
    /// The source Hamiltonian (for reference)
    Hamiltonian : PauliRegisterSequence
}
```

### 3.4 `CircuitStats`

Summary statistics for resource estimation.

```fsharp
type CircuitStats = {
    /// Total number of Pauli rotations per step
    RotationsPerStep   : int
    /// Total CNOT gates per step
    CNOTsPerStep       : int
    /// Total single-qubit gates per step
    SingleQubitPerStep : int
    /// Circuit depth per step (assuming no parallelism)
    DepthPerStep       : int
    /// Maximum Pauli weight across all rotations
    MaxWeight          : int
    /// Mean Pauli weight across all rotations
    MeanWeight         : float
    /// Total gates for full simulation (per step × NumSteps)
    TotalCNOTs         : int
}
```

---

## 4. Core Functions

### 4.1 Trotter Decomposition

```fsharp
module Trotterization =

    /// First-order Trotter decomposition.
    /// Produces rotations in the given term order.
    val firstOrder :
        hamiltonian : PauliRegisterSequence ->
        stepSize    : float ->
        TrotterStep

    /// Second-order (symmetric) Trotter decomposition.
    /// Produces forward-then-reverse rotation sequence.
    val secondOrder :
        hamiltonian : PauliRegisterSequence ->
        stepSize    : float ->
        TrotterStep

    /// Build a complete Trotter circuit for time evolution.
    val trotterize :
        hamiltonian : PauliRegisterSequence ->
        time        : float ->
        numSteps    : int ->
        order       : int ->
        TrotterCircuit

    /// Compute resource estimates for a Trotter circuit.
    val stats : TrotterCircuit -> CircuitStats
```

### 4.2 Term Ordering Strategies

The order of Pauli rotations within a Trotter step affects both
the Trotter error and opportunities for gate cancellation.

```fsharp
module TermOrdering =

    /// Lexicographic order by Pauli signature string
    val lexicographic : PauliRegisterSequence -> PauliRegister list

    /// Sorted by decreasing |coefficient| (largest first)
    val byMagnitude : PauliRegisterSequence -> PauliRegister list

    /// Group mutually commuting terms (fewer groups = lower error)
    val commuteGrouped : PauliRegisterSequence -> PauliRegister list list
```

### 4.3 Circuit Synthesis (Pauli Rotation → Gates)

A single Pauli rotation $e^{-i\theta P}$ where
$P = \sigma_{i_1} \otimes \sigma_{i_2} \otimes \cdots$ of weight $w$
is implemented as:

1. Basis rotation: $H$ on each $X$ qubit, $S^\dagger H$ on each $Y$ qubit
2. CNOT staircase: chain of CNOTs connecting the $w$ non-identity qubits
3. $R_z(2\theta)$ on the last qubit in the chain
4. Reverse CNOT staircase
5. Reverse basis rotation

```fsharp
module CircuitSynthesis =

    /// Abstract gate type for circuit output
    type Gate =
        | H      of qubit: int
        | S      of qubit: int
        | Sdg    of qubit: int            // S†
        | Rz     of qubit: int * angle: float
        | CNOT   of control: int * target: int

    /// Synthesize a single Pauli rotation into a gate sequence.
    val synthesizeRotation : PauliRotation -> Gate list

    /// Synthesize an entire Trotter step into a gate sequence.
    val synthesizeStep : TrotterStep -> Gate list

    /// Synthesize a full circuit (all steps).
    val synthesizeCircuit : TrotterCircuit -> Gate list
```

### 4.4 Code Generation

```fsharp
module CodeGen =

    /// Emit OpenQASM 3.0 for a Trotter circuit.
    val toOpenQASM : TrotterCircuit -> string

    /// Emit Q# operation for a Trotter circuit.
    val toQSharp : TrotterCircuit -> string

    /// Emit a gate count summary table (Markdown).
    val toMarkdownTable : CircuitStats -> string
```

---

## 5. Pipeline Integration

### 5.1 End-to-End Example

```fsharp
open FockMap.Hamiltonian
open FockMap.Trotterization
open FockMap.CodeGen

// Step 1: Build Hamiltonian (existing pipeline)
let H = computeHamiltonian coeffFactory 4u

// Step 2: Trotterize (NEW)
let circuit = trotterize H 1.0 100 2  // t=1.0, 100 steps, 2nd order

// Step 3: Resource estimation (NEW)
let s = stats circuit
printfn "CNOTs per step: %d" s.CNOTsPerStep
printfn "Total CNOTs:    %d" s.TotalCNOTs
printfn "Max weight:     %d" s.MaxWeight

// Step 4: Code generation (NEW)
let qasm = toOpenQASM circuit
let qs   = toQSharp circuit
```

### 5.2 Encoding Comparison Example

```fsharp
// Compare CNOT cost across encodings
let encodings = [
    "JW",      jordanWignerScheme 12
    "BK",      bravyiKitaevScheme 12
    "Parity",  parityScheme 12
    "BinTree", binaryTreeScheme 12
    "TerTree", ternaryTreeScheme 12
]

for name, scheme in encodings do
    let H = computeHamiltonianWith (encodeWithScheme scheme) coeffFactory 12u
    let circuit = trotterize H 1.0 1 1
    let s = stats circuit
    printfn "%s: %d rotations, %d CNOTs/step, max weight %d"
        name s.RotationsPerStep s.CNOTsPerStep s.MaxWeight
```

---

## 6. File Structure

```
src/Encodings/
├── ...existing files...
├── Trotterization.fs       ← Trotter decomposition + stats
├── CircuitSynthesis.fs     ← Gate-level synthesis
└── CodeGen.fs              ← OpenQASM / Q# emitters
```

Add to `Encodings.fsproj` after `Hamiltonian.fs`:
```xml
<Compile Include="Trotterization.fs" />
<Compile Include="CircuitSynthesis.fs" />
<Compile Include="CodeGen.fs" />
```

---

## 7. Testing Strategy

| Test | Type | What it verifies |
|------|------|------------------|
| Rotation count | Unit | Number of rotations = number of Hamiltonian terms |
| CNOT count formula | Unit | CNOTs per rotation = 2(w−1) where w = Pauli weight |
| Second-order symmetry | Property | 2nd-order step is palindromic (forward = reverse) |
| Identity recovery | Property | Trotterize H=0 → empty rotation list |
| Coefficient→angle | Unit | Angle = Re(coeff) × Δt, imaginary part warned/rejected |
| OpenQASM syntax | Unit | Output parses with a QASM validator |
| Q# syntax | Unit | Output compiles (or at minimum, well-formed string) |
| Encoding comparison | Integration | JW has higher CNOT count than BK for n≥4 |
| H₂ end-to-end | Integration | Full pipeline from integrals to QASM, verify gate count |

---

## 8. Design Decisions

### 8.1 Symbolic vs Numeric

The module stays **symbolic** — it produces gate lists and code strings,
not matrix simulations.  This is consistent with FockMap's philosophy:
no matrices, exact where possible, numerical only for coefficients.

### 8.2 Coefficient Handling

`PauliRegisterSequence` stores `Complex` coefficients.  For
Trotterization, only the real part of $c_k$ contributes to the
rotation angle (Hermitian Hamiltonians have real coefficients in the
Pauli basis).  The module should:
- Warn if any $|\text{Im}(c_k)| > \epsilon$
- Use $\theta_k = \text{Re}(c_k) \cdot \Delta t$

### 8.3 Q# Output Format

Generate standalone Q# operations using `Microsoft.Quantum.Intrinsic`:

```qsharp
operation TrotterStep(qs : Qubit[]) : Unit is Adj + Ctl {
    // Rotation 1: e^{-i(0.123) XZYI}
    H(qs[0]);
    Adjoint S(qs[2]);
    H(qs[2]);
    CNOT(qs[0], qs[1]);
    CNOT(qs[1], qs[2]);
    Rz(0.246, qs[2]);
    CNOT(qs[1], qs[2]);
    CNOT(qs[0], qs[1]);
    H(qs[2]);
    S(qs[2]);
    H(qs[0]);
    // ...
}
```

### 8.4 What This Module Does NOT Do

- **Circuit optimisation**: no gate cancellation, commutation-based
  reordering, or T-count minimisation.  Those are separate concerns.
- **Matrix simulation**: no state vectors or density matrices.
- **Error analysis**: no Trotter error bounds (though `CircuitStats`
  enables external analysis).
- **Hardware mapping**: no qubit routing, connectivity constraints,
  or transpilation.  That's Qiskit/Cirq territory.

---

## 9. Future Extensions

| Extension | Description | Priority |
|-----------|-------------|----------|
| Gate cancellation | Cancel adjacent CNOT pairs across rotations | Medium |
| Commuting groups | Simultaneous diagonalisation for commuting Paulis | Medium |
| Bosonic Trotter | Extend to bosonic-encoded Hamiltonians | Low |
| Cirq / Qiskit output | Python code generation | Low |
| Error bounds | Trotter error estimates from commutator norms | Low |
| Hardware-aware ordering | Optimise term order for connectivity | Low |

---

## 10. Dependencies

- No new NuGet dependencies required
- Q# code generation is string-based (no Q# compiler dependency)
- OpenQASM output is string-based (no external parser needed)
- All new code is pure F# with no side effects
