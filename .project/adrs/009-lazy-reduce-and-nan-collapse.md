# ADR-009: Lazy Evaluation for Normalization; NaN/Inf Collapse to Zero

**Date:** —
**Status:** Accepted
**Source:** Code analysis — `Terms.fs` (Reduce), `TypeExtensions.fs` (Complex.Reduce)

## Context

Two related numerical hygiene decisions visible in the codebase:

### Lazy normalization

Normalizing a product `P<'unit>` requires folding all unit-level coefficients
into the product coefficient, checking for zeros, and potentially eliminating
dead terms. For a sum `S<'unit>` with hundreds of products, eager normalization
after every algebraic operation would be expensive. But without normalization,
like-term combination may miss matches (two products with different coefficient
distributions but identical normalized form).

### Non-finite coefficient handling

During Hamiltonian construction, numerical edge cases can produce `NaN` or
`Infinity` coefficients (e.g., division by zero in integral processing,
degenerate geometries). These non-finite values have no physical meaning
and must not propagate through the algebra.

## Decision

### Lazy<T> wrapper for Reduce

`P.Reduce` and `S.Reduce` return `Lazy<P<'unit>>` and `Lazy<S<'unit>>`.
Evaluation is deferred until `.Value` is called. Once forced, the result
is cached.

```fsharp
member this.Reduce =
    lazy
        this.Units
        |> Array.fold normalize (this.Coeff, [||])
        |> (fun (c, u) -> P<'unit>.Apply(c, u))
```

### NaN/Inf → Zero

`Complex.Reduce` (extension method) maps any non-finite value to
`Complex.Zero`. `Complex.IsZero` returns `true` for non-finite values.
This is applied pervasively at construction boundaries.

```fsharp
member this.Reduce =
    if isFinite then this else Complex.Zero
```

## Consequences

- **Performance**: Normalization cost is paid at most once, and only when
  needed. Intermediate expression building (chaining `*` and `+`) avoids
  unnecessary work.
- **API ergonomics**: Every caller must remember to access `.Value`. Forgetting
  to reduce before comparison is a latent bug. This trade-off is accepted
  because the library's hot paths (Hamiltonian construction, like-term
  combination) always reduce.
- **Silent NaN suppression**: A term with a non-finite coefficient is treated
  as zero — it vanishes from the sum. This is physically appropriate (an
  unphysical term should not contaminate the Hamiltonian) but could mask bugs
  in integral processing. The design choice is defensive: better to silently
  drop a bad term than to crash a long-running PES scan.

## Alternatives Considered

- **Eager normalization** — simpler API (no `.Value`), but O(n) cost after
  every operation. Rejected for performance.
- **Propagate NaN** — would surface bugs immediately but crash production
  workflows. Rejected in favour of defensive handling.
- **Throw on NaN** — explicit error reporting. Reasonable but the library
  is used in scripting contexts where exceptions are disruptive. Rejected.

## References

- [Terms.fs](../../src/Encodings/Terms.fs) — `P.Reduce`, `S.Reduce`
- [TypeExtensions.fs](../../src/Encodings/TypeExtensions.fs) — `Complex.Reduce`, `Complex.IsZero`
