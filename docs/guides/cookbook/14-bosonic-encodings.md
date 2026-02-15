# Bosonic-to-Qubit Encodings

Chapters 1–13 covered the fermionic world: creation and annihilation
operators obeying anti-commutation relations, mapped to qubit Pauli
strings via Jordan–Wigner, Bravyi–Kitaev, and friends.

But many physical models also involve **bosonic** modes — photons,
phonons, vibrations — where the occupation number of a single mode is
unbounded. To simulate these on a qubit device, we must **truncate** the
Fock space to a maximum of $d$ levels and then encode the resulting
$d$-level system as qubits.

FockMap provides three bosonic-to-qubit encodings, following Sawaya et
al. (2020): Unary, Standard Binary, and Gray Code.

## The problem: d levels → qubits

A bosonic mode truncated to $d$ levels lives in a $d$-dimensional
Hilbert space spanned by $|0\rangle, |1\rangle, \ldots, |d{-}1\rangle$.
The creation operator $b^\dagger$ and annihilation operator $b$ act as:

$$b^\dagger |n\rangle = \sqrt{n+1}\, |n{+}1\rangle, \qquad b |n\rangle = \sqrt{n}\, |n{-}1\rangle$$

We want to express $b^\dagger$ and $b$ as sums of Pauli strings acting
on qubits. The three encodings differ in how many qubits they use per
mode and the Pauli weight of the resulting terms.

## Encoding 1: Unary (one-hot)

The unary encoding uses **$d$ qubits per mode**. Each Fock state
$|n\rangle$ is represented by a one-hot qubit string where only the
$n$-th qubit is $|1\rangle$.

Each transition $|n\rangle \to |n{+}1\rangle$ becomes a two-qubit
operation $\sigma^+_{n+1}\sigma^-_n$, so the maximum Pauli weight is
always 2.

```fsharp
open Encodings

let d = 4u       // truncation: n ∈ {0, 1, 2, 3}
let M = 1u       // one mode

// Creation operator b† in unary encoding
let bDagger = unaryBosonTerms Raise 0u M d

printfn "Unary b† (d=%d): %d Pauli terms" d bDagger.SummandTerms.Length
for t in bDagger.SummandTerms do
    printfn "  %+.4f %+.4fi  %s" t.Coefficient.Real t.Coefficient.Imaginary t.Signature
```

The signature tells you exactly which type of encoding function to use:

```fsharp
// BosonicEncoderFn =
//   LadderOperatorUnit -> uint32 -> uint32 -> uint32 -> PauliRegisterSequence
//   operator              mode j    modes M   cutoff d
```

The first argument is `Raise` ($b^\dagger$) or `Lower` ($b$).

## Encoding 2: Standard binary

The binary encoding uses **$\lceil\log_2 d\rceil$ qubits per mode** —
much more compact. Fock state $|n\rangle$ maps to the binary
representation of $n$.

Internally, FockMap builds the $d \times d$ matrix for $b^\dagger$ (or
$b$), embeds it in a $2^q \times 2^q$ space, and decomposes it into
Pauli strings via the trace formula:

$$O = \sum_P \frac{1}{2^q} \operatorname{Tr}(PO)\, P$$

```fsharp
// Binary b† — only 2 qubits for d=4
let bDaggerBin = binaryBosonTerms Raise 0u M d

printfn "\nBinary b† (d=%d): %d Pauli terms" d bDaggerBin.SummandTerms.Length
for t in bDaggerBin.SummandTerms do
    printfn "  %+.4f %+.4fi  %s" t.Coefficient.Real t.Coefficient.Imaginary t.Signature
```

> **Trade-off:** Binary uses fewer qubits but typically produces
> higher-weight Pauli terms than unary.

## Encoding 3: Gray code

Like binary, the Gray code encoding uses $\lceil\log_2 d\rceil$ qubits.
The difference: consecutive Fock states differ in **exactly one qubit**,
which tends to reduce the average Pauli weight of transition operators.

```fsharp
let bDaggerGray = grayCodeBosonTerms Raise 0u M d

printfn "\nGray code b† (d=%d): %d Pauli terms" d bDaggerGray.SummandTerms.Length
for t in bDaggerGray.SummandTerms do
    printfn "  %+.4f %+.4fi  %s" t.Coefficient.Real t.Coefficient.Imaginary t.Signature
```

## Side-by-side comparison

All three encodings share the same function signature, so comparing them
is straightforward:

```fsharp
let cutoff = 4u
let numModes = 1u

let pauliWeight (reg : PauliRegister) =
    reg.Signature |> Seq.sumBy (fun c -> if c = 'I' then 0 else 1)

let encodings =
    [ ("Unary",      unaryBosonTerms,     unaryQubitsPerMode)
      ("Binary",     binaryBosonTerms,    binaryQubitsPerMode)
      ("Gray code",  grayCodeBosonTerms,  binaryQubitsPerMode) ]

printfn "%-12s  %6s  %6s  %9s" "Encoding" "Qubits" "Terms" "MaxWeight"

for (name, encoder, qpm) in encodings do
    let cr = encoder Raise 0u numModes cutoff
    let nTerms = cr.SummandTerms.Length
    let maxW =
        if nTerms > 0
        then cr.SummandTerms |> Array.map pauliWeight |> Array.max
        else 0
    printfn "%-12s  %6d  %6d  %9d" name (qpm (int cutoff)) nTerms maxW
```

Expected output for $d = 4$:

| Encoding | Qubits | Terms | MaxWeight |
|----------|:------:|:-----:|:---------:|
| Unary | 4 | 6 | 2 |
| Binary | 2 | 6 | 2 |
| Gray code | 2 | 6 | 2 |

At larger cutoffs, the differences become more pronounced — binary and
Gray code stay logarithmic in qubits while unary grows linearly.

## Building a Hamiltonian: harmonic oscillator

The simplest bosonic Hamiltonian is the harmonic oscillator
$H = \omega\, b^\dagger b$. We build it by multiplying the encoded
creation and annihilation operators:

```fsharp
open System.Numerics

let omega = 1.0

for (name, encoder, _) in encodings do
    let cr = encoder Raise 0u 1u cutoff
    let an = encoder Lower 0u 1u cutoff
    let numberOp = cr * an

    printfn "%s — b†b has %d Pauli terms:" name numberOp.SummandTerms.Length
    for term in numberOp.SummandTerms do
        let scaled = term.Coefficient * Complex(omega, 0.)
        printfn "  %+.4f  %s" scaled.Real term.Signature
    printfn ""
```

> **Verify:** For $d = 2$, the number operator $b^\dagger b$ reduces to
> $\tfrac{1}{2}(I - Z)$ — the standard qubit projection operator.

## Multi-mode systems: coupled bosonic modes

For multiple modes, each mode gets its own qubit register. The `mode`
and `numModes` parameters handle the embedding automatically:

```fsharp
let twoModes = 2u
let d = 3u

// Number operators for each mode
let n0 = (binaryBosonTerms Raise 0u twoModes d) * (binaryBosonTerms Lower 0u twoModes d)
let n1 = (binaryBosonTerms Raise 1u twoModes d) * (binaryBosonTerms Lower 1u twoModes d)

// Coupling: beam-splitter interaction b†₀ b₁ + b†₁ b₀
let coupling =
    let cr0 = binaryBosonTerms Raise 0u twoModes d
    let an1 = binaryBosonTerms Lower 1u twoModes d
    let cr1 = binaryBosonTerms Raise 1u twoModes d
    let an0 = binaryBosonTerms Lower 0u twoModes d
    (cr0 * an1) + (cr1 * an0)

// Full Hamiltonian: H = ω₁ n̂₁ + ω₂ n̂₂ + g(b†₀ b₁ + b†₁ b₀)
let qpm = binaryQubitsPerMode (int d)
printfn "Binary encoding: %d qubits/mode × %d modes = %d total qubits"
    qpm (int twoModes) (qpm * int twoModes)
printfn "n̂₀: %d terms,  n̂₁: %d terms,  coupling: %d terms"
    n0.SummandTerms.Length n1.SummandTerms.Length coupling.SummandTerms.Length
```

## Scaling: qubits and terms vs. cutoff

As the truncation cutoff $d$ grows, the three encodings scale
differently:

```fsharp
printfn "%-4s  %-16s  %-16s  %-16s" "d" "Unary (q,terms)" "Binary (q,terms)" "Gray (q,terms)"

for d in [2u; 3u; 4u; 6u; 8u] do
    let results =
        encodings |> List.map (fun (_, enc, qpm) ->
            let q = qpm (int d)
            let t = (enc Raise 0u 1u d).SummandTerms.Length
            sprintf "(%d, %d)" q t)
    printfn "%-4d  %-16s  %-16s  %-16s" d results.[0] results.[1] results.[2]
```

**Rule of thumb:**
- **Unary** is simplest (weight ≤ 2) but costs $d$ qubits per mode — impractical for large cutoffs.
- **Binary** and **Gray code** use $\lceil\log_2 d\rceil$ qubits, suitable for larger truncations.
- **Gray code** often has lower average Pauli weight than standard binary, reducing measurement overhead.

## Choosing an encoding

| Scenario | Recommended |
|----------|-------------|
| Small cutoff ($d \leq 4$), clarity matters | Unary |
| Qubit-limited device, moderate cutoff | Binary or Gray code |
| Minimizing measurement cost | Gray code |
| Mixed fermion–boson model | Any — output is `PauliRegisterSequence`, same as fermionic encodings |

## Runnable script

- `examples/Bosonic_Encoding.fsx` — all the above in one executable script

```bash
dotnet fsi examples/Bosonic_Encoding.fsx
```

---

**Back to:** [Cookbook index](index.html) — quick reference and further reading
