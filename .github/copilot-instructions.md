# Copilot Instructions

*Quick context for AI assistants working on this project.*

---

## Working Style

**Default mode: Discuss, don't code.**

- When asked a question, **discuss ideas, design options, and trade-offs first**
- Only write code when explicitly asked to implement something
- Brainstorm and explore alternatives before jumping to implementation
- Ask clarifying questions rather than making assumptions
- For complex tasks, propose a plan and get approval before coding

**When to code:**
- User says "implement", "write", "code", "fix", "add", or similar action words
- User approves a proposed design/plan
- User explicitly asks for a code sample

---

## Project Overview

**FockMap** is an F# library for symbolic operator algebra on Fock space — fermionic and bosonic — with fermion-to-qubit encodings. The core task is mapping fermionic/bosonic ladder operators to Pauli strings via multiple encoding schemes.

**Name**: "FockMap" — maps Fock-space operators to qubit representations

**Status**: Published on NuGet, actively developed

---

## Key Concepts

| Term | Definition |
|------|------------|
| **Fermion-to-qubit encoding** | Map from anticommuting ladder operators to Pauli strings |
| **CAR** | Canonical Anticommutation Relations — {a†_i, a_j} = δ_ij |
| **CCR** | Canonical Commutation Relations — [b_i, b†_j] = δ_ij |
| **Pauli weight** | Number of non-identity Pauli operators in a string |
| **Index-set scheme** | Encoding defined by Update/Parity/Occupation sets |
| **Path-based encoding** | Encoding defined by labelled rooted tree |
| **Majorana operators** | Hermitian operators c_j = a†_j + a_j, d_j = i(a†_j - a_j) |

---

## Architecture

```
Ladder Operators → Encoding → Pauli Strings → Hamiltonian Assembly
       │               │            │                │
       │               │            │                └─ Symbolic sum, coefficients
       │               │            └─ Exact phase tracking, no floats
       │               └─ JW, BK, Parity, BinTree, TerTree, Vlasov, Custom
       └─ Fermionic (CAR) or Bosonic (CCR)
```

**Two encoding abstractions**: Index-set schemes (JW, BK, Parity) and path-based tree encodings (balanced binary, balanced ternary, Vlasov, arbitrary trees).

---

## Critical Files

| File | Purpose |
|------|---------|
| `src/Encodings/Terms.fs` | Core types: `C`, `P`, `S` (coefficient, product, sum) |
| `src/Encodings/TypeExtensions.fs` | Complex number extensions (`Reduce`, `IsZero`, `IsNonZero`) |
| `src/Encodings/MajoranaEncoding.fs` | Index-set encoding (`EncodingScheme`) and Majorana operator construction |
| `src/Encodings/TreeEncoding.fs` | Tree-based encoding infrastructure and path-based Majorana strings |
| `src/Encodings/BravyiKitaev.fs` | Bravyi-Kitaev encoding via Fenwick tree |
| `src/Encodings/JordanWigner.fs` | Jordan-Wigner encoding |
| `src/Encodings/Hamiltonian.fs` | Hamiltonian assembly |
| `src/Encodings/Encodings.fsproj` | Project file, version, metadata, and **compilation order** |

---

## Development Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run a single test project
dotnet test test/Test.Encodings/Test.Encodings.fsproj

# Run a single test by name (xunit filter syntax)
dotnet test --filter "FullyQualifiedName~SomeTestName"

# Run tests with verbose output
dotnet test --logger "console;verbosity=detailed"

# Build in Release mode
dotnet build -c Release

# Run examples
dotnet fsi examples/H2_Encoding.fsx

# Build documentation
dotnet build && dotnet fsdocs build --clean --strict
```

---

## Planning Documents

| Document | Purpose |
|----------|---------|
| [.project/adrs/](.project/adrs/) | Architectural decision records |
| [.project/test-register.md](.project/test-register.md) | Test documentation and coverage |
| [joss/paper.md](joss/paper.md) | JOSS software paper |

> Research planning and paper drafts live in the private `encodings-research` repo. This repo focuses on the public software.

---

## Code Patterns

### Discriminated Unions for Algebra

```fsharp
type C<'u> = C of complex * 'u    // Coefficient × unit
type P<'u> = P of C<'u> list      // Product of coefficients
type S<'u> = S of P<'u> list      // Sum of products
```

`S<'unit>` stores terms in a `Map<string, P<'unit>>` — two products are "like terms" if their `ToString()` representations match. The `Reduce` member on `P` and `S` uses **lazy evaluation** to defer expensive normalization.

### `[<AutoOpen>]` Modules

Every source file is an `[<AutoOpen>]` module inside `namespace Encodings`. All symbols are available without explicit module qualification throughout the library.

### F# Compilation Order

Files in `Encodings.fsproj` must be listed in **strict dependency order** — F# requires this. When adding a new file, insert it in the correct position within the `<ItemGroup>` compile list, after all its dependencies.

### Encoding via Index Sets

```fsharp
let jordanWignerScheme n : EncodingScheme = ...
let bravyiKitaevScheme n : EncodingScheme = ...
```

`EncodingScheme` is defined in `MajoranaEncoding.fs` with three index-set functions: `Update`, `Parity`, `Occupation`.

### Encoding via Trees

```fsharp
let balancedTernaryTreeTerms n : MajoranaTerms = ...
let vlasovTreeTerms n : MajoranaTerms = ...
```

### Exact Phase Tracking

Coefficients are `System.Numerics.Complex` — no floats used directly for phase. The `Complex.Reduce` extension sanitizes non-finite values to zero.

---

## Testing

- 270+ tests using **xunit** and **FsCheck.Xunit** (property testing)
- Test register at `.project/test-register.md` — update it when tests change
- Pre-commit gate: `dotnet build Encodings.sln && dotnet test Encodings.sln`

---

## Current Focus

1. **JOSS submission** — Software paper at `joss/paper.md`
2. **Documentation** — Cookbook chapters, API docs
3. **Bosonic extensions** — Mixed fermion-boson systems

---

## Tips

1. **Ask about context** if unsure — read ADRs and test register first
2. **Use immutable types** — F# discriminated unions, records
3. **Write tests** — especially property tests with FsCheck
4. **Update documentation** — XML doc comments required on all public APIs
5. **Check CI** — all commits must pass build + tests on all platforms
6. **Test register** — update `.project/test-register.md` when tests change
7. **Use prompts** — `commit`, `release`, `paper`, `coach`, `pick-next-work` etc.
