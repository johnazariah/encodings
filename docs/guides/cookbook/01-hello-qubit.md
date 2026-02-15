# Hello, Qubit

Everything in quantum computing eventually becomes a **Pauli operator**.
These are 2×2 matrices that act on a single qubit:

$$I = \begin{pmatrix}1&0\\0&1\end{pmatrix},\quad
X = \begin{pmatrix}0&1\\1&0\end{pmatrix},\quad
Y = \begin{pmatrix}0&{-i}\\i&0\end{pmatrix},\quad
Z = \begin{pmatrix}1&0\\0&{-1}\end{pmatrix}$$

In FockMap they're a simple discriminated union. Let's start a script and play with them:

```fsharp
#r "nuget: FockMap"
open Encodings
open System.Numerics

// The four Pauli operators — nothing more than labels
let identity = I
let bitFlip  = X
let combined = Y
let phase    = Z
```

Multiplying two Paulis always gives another Pauli **times a phase**.
The algebra is exact — no floating point involved:

```fsharp
let (result, phase) = X * Y
// result = Z,  phase = Pi    because XY = iZ

let (result2, _) = Y * X
// result2 = Z, phase = Mi    because YX = −iZ  (anti-commutation!)

let (result3, _) = X * X
// result3 = I, phase = P1    every Pauli squares to identity
```

Notice that `X * Y ≠ Y * X` — they differ by a sign. This is the
**anti-commutation** property, and it's fundamental to quantum mechanics.

## Phases without floating point

The four phase values $\{+1, -1, +i, -i\}$ live in their own type:

```fsharp
// Phase is a discriminated union: P1 (+1), M1 (−1), Pi (+i), Mi (−i)
Pi * Pi      // M1    because i × i = −1
M1 * Mi      // Pi    because (−1) × (−i) = +i
P1 * M1      // M1    the identity doesn't change anything
```

When you need to fold a phase into a complex number:

```fsharp
let c = Complex(2.0, 0.0)
Pi.FoldIntoGlobalPhase c     // Complex(0.0, 2.0) — multiplied by i
M1.FoldIntoGlobalPhase c     // Complex(-2.0, 0.0)
```

**Key insight:** FockMap tracks phases symbolically using `Phase` and only
converts to floating-point `Complex` at the boundaries. This eliminates
the rounding errors that plague naïve Pauli algebra implementations.

---

**Next:** [Building Expressions](02-building-expressions.html) — the `C` / `P` / `S` hierarchy
