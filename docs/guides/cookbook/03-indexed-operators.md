# Operators on Specific Qubits

So far our operators have been "anonymous" — we haven't said which qubit
each Pauli acts on. The `IxOp` type tags each operator with a mode index:

```fsharp
// "X on qubit 0":
let x0 = IxOp<uint32, Pauli>.Apply(0u, X)

// "Z on qubit 3":
let z3 = IxOp<uint32, Pauli>.Apply(3u, Z)

printfn "%O" x0   // "(X, 0)"
printfn "%O" z3   // "(Z, 3)"
```

You can check whether indices are properly ordered:

```fsharp
IxOp<uint32, Pauli>.IndicesInOrder Ascending [x0; z3]   // true  (0 < 3)
IxOp<uint32, Pauli>.IndicesInOrder Descending [x0; z3]   // false
```

## Parsing from strings

Rather than constructing operators one-at-a-time, you can parse them:

```fsharp
// Single indexed Pauli:
let parsed = Pauli.FromString "(X, 2)"
// Some { Index = 2u; Op = X }

// Product of indexed Paulis:
let term = PIxOp<uint32, Pauli>.TryCreateFromString Pauli.Apply "[(X, 0)|(Z, 3)]"

// Full sum expression:
let expr = SIxOp<uint32, Pauli>.TryCreateFromString Pauli.Apply
               "{[(X, 0)|(Z, 1)]; [(Y, 0)|(I, 1)]}"
```

**Pattern:** `C`, `P`, and `S` all have `TryCreateFromString` methods.
The format uses `[...|...]` for products and `{...; ...}` for sums.
You pass a parser function for the underlying operator type.

---

**Next:** [Creation and Annihilation](04-creation-annihilation.html) — ladder operators for quantum chemistry
