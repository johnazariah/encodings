# Encoding Internals

Every encoding in FockMap follows the same recipe. Understanding it
lets you create your own.

## Majorana decomposition

A ladder operator is first split into two **Majorana operators**:

$$a^\dagger_j = \tfrac{1}{2}(c_j - i \cdot d_j), \qquad a_j = \tfrac{1}{2}(c_j + i \cdot d_j)$$

The Majorana operators are then built from three **index sets** that
determine which qubits get X, Y, or Z:

$$c_j = X_{U(j) \cup \{j\}} \cdot Z_{P(j)}$$
$$d_j = Y_j \cdot X_{U(j)} \cdot Z_{(P(j) \oplus \text{Occ}(j)) \setminus \{j\}}$$

Different choices of these three functions yield different encodings.

## The EncodingScheme record

```fsharp
type EncodingScheme =
    { Update     : int -> int -> Set<int>    // U(j, n)
      Parity     : int -> Set<int>           // P(j)
      Occupation : int -> Set<int> }         // Occ(j)
```

FockMap provides three built-in schemes:

```fsharp
// Jordan-Wigner: U = ∅,  P = {0..j−1},  Occ = {j}
encodeOperator jordanWignerScheme Raise 2u 4u

// Bravyi-Kitaev: index sets from Fenwick tree
encodeOperator bravyiKitaevScheme Raise 2u 4u

// Parity: dual of Jordan-Wigner
encodeOperator parityScheme Raise 2u 4u
```

## Custom encoding in 5 lines

This is where FockMap shines. Let's recreate Jordan-Wigner from scratch
to prove it's just three functions:

```fsharp
let myJW : EncodingScheme =
    { Update     = fun _ _ -> Set.empty
      Parity     = fun j   -> set [ for k in 0 .. j-1 -> k ]
      Occupation = fun j   -> set [j] }

let myResult = encodeOperator myJW Raise 2u 4u
// Identical to jordanWignerTerms Raise 2u 4u!
```

Compare that to the ~200 lines you'd need in other frameworks.

## Peeking inside: Majorana assignments

For debugging and learning, you can inspect the raw Majorana construction:

```fsharp
// c-Majorana assignments for mode 2 on 4 qubits:
let cAssign = cMajorana jordanWignerScheme 2 4
// [(0, Z); (1, Z); (2, X)]

// d-Majorana assignments:
let dAssign = dMajorana jordanWignerScheme 2 4
// [(0, Z); (1, Z); (2, Y)]

// Turn sparse assignments into a full PauliRegister:
let cReg = pauliOfAssignments 4 cAssign Complex.One
// PauliRegister("ZZXI", 1.0)
```

---

**Next:** [Trees and Fenwick Trees](09-trees.html) — tree-shaped parity structures
