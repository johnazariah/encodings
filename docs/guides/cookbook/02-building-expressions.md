# Building Expressions: The C / P / S Hierarchy

Real quantum operators aren't single Paulis — they're **sums of products**
of operators, each with a coefficient. FockMap represents this with three
nested types. Think of them as atoms → words → sentences:

| Type | Role | Analogy |
|------|------|---------|
| `C<'T>` | Coefficient × single operator | A letter with emphasis |
| `P<'T>` | Product of operators | A word (ordered sequence) |
| `S<'T>` | Sum of products | A sentence (the whole expression) |

These types are **generic** — they work with any operator type, not just Paulis.

## C — A single weighted operator

```fsharp
// Create X with coefficient 1:
let one_x = C<Pauli>.Apply X              // 1 · X

// Create 0.5 · Y:
let half_y = C<Pauli>.Apply(Complex(0.5, 0.0), Y)

printfn "%O" one_x      // "X"
printfn "%O" half_y      // "(0.5 Y)"
```

## P — An ordered product (tensor product)

When you multiply two `C` values you get a `P` — a product of operators
that represents a multi-qubit string:

```fsharp
// Build X ⊗ Y by multiplying two C values:
let xy = one_x * half_y
// P<Pauli> with Coeff = 0.5, Units = [X; Y]

// Or build directly from an array:
let xzy = P<Pauli>.Apply [| X; Z; Y |]
// 1 · (X ⊗ Z ⊗ Y)

// Products compose by concatenation:
let big = xzy * xzy
// X ⊗ Z ⊗ Y ⊗ X ⊗ Z ⊗ Y

// Scale the coefficient:
let scaled = xzy.ScaleCoefficient(Complex(3.0, 0.0))
// 3 · (X ⊗ Z ⊗ Y)
```

Reduction normalises all internal coefficients into the single overall
coefficient — critical for comparing and combining terms:

```fsharp
let mixed = P<Pauli>.Apply(Complex(2.0, 0.0), [| half_y; one_x |])
// Coeff = 2.0, Units = [(0.5 Y); X]

let clean = mixed.Reduce.Value
// Coeff = 1.0, Units = [Y; X]  — the 2 × 0.5 folded together
```

## S — A sum of products (the Hamiltonian shape)

Most quantum operators are **sums** of terms. `S<'T>` collects product
terms and automatically combines like terms:

```fsharp
// Two terms:
let s1 = S<Pauli>.Apply(P<Pauli>.Apply [| X; Z |])
let s2 = S<Pauli>.Apply(P<Pauli>.Apply [| Y; I |])

// Add them: H = 1·(X⊗Z) + 1·(Y⊗I)
let hamiltonian = s1 + s2

// Like terms combine automatically:
let doubled = s1 + s1
// → 2·(X⊗Z)

// Multiplication distributes: (A + B)(C + D) = AC + AD + BC + BD
let distributed = (s1 + s2) * (s1 + s2)
```

You can inspect the terms:

```fsharp
for term in hamiltonian.ProductTerms.Value do
    printfn "%O" term
// [X | Z]
// [Y | I]
```

## How like-term combination works

`S<'T>` stores its terms in a `Map<string, P<'T>>`, keyed by the string
representation of the product's operator sequence. When two terms share the
same key, their coefficients are summed automatically. This is what makes
`s1 + s1` produce `2·(X⊗Z)` instead of two separate entries.

## Coefficient hygiene

Every level has a `Reduce` method that sanitizes coefficients. `NaN` and
infinity values are replaced with zero, preventing numerical corruption
from silently propagating through your computation:

```fsharp
member this.Reduce = { this with Coeff = this.Coeff.Reduce }
```

## Zero propagation

A product containing any zero-coefficient unit becomes the zero product.
This is checked eagerly so that downstream code never wastes time on
terms that contribute nothing:

```fsharp
member this.IsZero =
    (not this.Coeff.IsNonZero) ||
    (this.Units |> Seq.exists (fun item -> item.IsZero))
```

**Why this matters:** Every Hamiltonian in quantum chemistry is an `S<'T>`.
The type system ensures you can't accidentally mix up a single term with
a full expression.

---

**Next:** [Operators on Specific Qubits](03-indexed-operators.html) — tagging operators with qubit indices
