# Creation and Annihilation

Quantum chemistry doesn't work with Pauli operators directly — it works with
**ladder operators**: creation ($a^\dagger$) and annihilation ($a$).
These add or remove a particle from a specific orbital:

```fsharp
// The three ladder operator cases:
let create  = Raise     // a† — adds an electron
let destroy = Lower     // a  — removes an electron
let nothing = Identity  // I  — does nothing

// Parse from short names:
LadderOperatorUnit.Apply "u"    // Some Raise  ("up")
LadderOperatorUnit.Apply "d"    // Some Lower  ("down")
```

## Indexed ladder operators

In practice, each operator targets a specific **mode** (orbital):

```fsharp
// a†₂ — create an electron in orbital 2:
let adag2 = LadderOperatorUnit.FromUnit(true, 2u)
// { Index = 2u; Op = Raise }

// a₁ — annihilate an electron in orbital 1:
let a1 = LadderOperatorUnit.FromUnit(false, 1u)
// { Index = 1u; Op = Lower }
```

## Product terms: multi-operator strings

A typical quantum chemistry term involves several ladder operators.
For example, $a^\dagger_0 a^\dagger_1 a_1 a_0$ (a two-body interaction):

```fsharp
let twoBody = LadderOperatorProductTerm.FromUnits [|
    (true,  0u)    // a†₀
    (true,  1u)    // a†₁
    (false, 1u)    // a₁
    (false, 0u)    // a₀
|]

// Is this in "normal order" (all creates before annihilates)?
twoBody.IsInNormalOrder    // true ✓

// Is it in "index order" (create indices ascending, annihilate descending)?
twoBody.IsInIndexOrder     // true ✓
```

You can also build from explicit tuples or parse from strings:

```fsharp
let oneBody = LadderOperatorProductTerm.FromTuples [|
    (Raise, 2u); (Lower, 3u)
|]

let parsed = LadderOperatorProductTerm.TryCreateFromString "[(u, 0)|(d, 1)]"
```

And sum expressions work the same way:

```fsharp
let expr = LadderOperatorSumExpression.TryCreateFromString
               "{[(u, 0)|(d, 1)]; [(u, 2)|(d, 3)]}"
// Two terms: a†₀ a₁  +  a†₂ a₃
```

---

**Next:** [Normal Ordering](05-normal-ordering.html) — making physics legal with CAR and CCR
