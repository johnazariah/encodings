# The Utility Belt

A few helpers that round out the library.

## Complex number extensions

FockMap extends `System.Numerics.Complex` with properties you'll use
constantly:

```fsharp
open System.Numerics
open Encodings
let c = Complex(1.0, 2.0)

c.IsNonZero       // true
c.TimesI          // Complex(-2.0, 1.0) — multiplied by i
c.Reduce          // returns self if finite, zero if NaN/Inf

// (.NET 10 provides Complex.IsFinite as a built-in static method:)
Complex.IsFinite c   // true

// Fermionic sign factor: (−1)ⁿ
Complex.SwapSignMultiple 3 Complex.One    // −1 (odd swaps)
Complex.SwapSignMultiple 4 Complex.One    // +1 (even swaps)
```

## Map extensions

```fsharp
let m = Map [ ("a", 1); ("b", 2) ]
m.Keys       // [| "a"; "b" |]
m.Values     // [| 1; 2 |]
```

## Currying utilities

```fsharp
let add = fun x y -> x + y
let addTupled = uncurry add      // (int * int) -> int
let addCurried = curry addTupled // int -> int -> int
```

---

**Next:** [Grand Finale](13-grand-finale.html) — three encodings, one molecule
