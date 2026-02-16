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
       │               └─ JW, BK, Parity, BinTree, TerTree, Custom
       └─ Fermionic (CAR) or Bosonic (CCR)
```

**Two encoding abstractions**: Index-set schemes (JW, BK, Parity) and path-based tree encodings (arbitrary trees).

---

## Critical Files

| File | Purpose |
|------|---------|
| `src/Encodings/Terms.fs` | Core types: `C`, `P`, `S` (coefficient, product, sum) |
| `src/Encodings/MajoranaEncoding.fs` | Universal path-based encoding |
| `src/Encodings/IndexSetEncoding.fs` | Index-set encoding schemes |
| `src/Encodings/TreeEncoding.fs` | Tree-based encoding infrastructure |
| `src/Encodings/Hamiltonian.fs` | Hamiltonian assembly |
| `src/Encodings/Encodings.fsproj` | Project file, version, metadata |

---

## Development Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Build in Release mode
dotnet build -c Release

# Run examples
dotnet fsi examples/H2_Encoding.fsx

# Build documentation
dotnet build && dotnet fsdocs build --clean --strict

# Build papers
make -C .project/research clean && make -C .project/research all
```

---

## Planning Documents

| Document | Purpose |
|----------|---------|
| [.project/plans/](.project/plans/) | Cleanup and improvement plans |
| [.project/research/JOURNAL.md](.project/research/JOURNAL.md) | Research investigation journal |
| [.project/research/README.md](.project/research/README.md) | Paper status and overview |
| [.project/release-checklist.md](.project/release-checklist.md) | Pre-release verification |
| [.project/test-register.md](.project/test-register.md) | Test documentation and coverage |

**Always check the research JOURNAL** to understand recent discoveries.

**Always update the JOURNAL** after completing research work.

---

## Research Papers

| Paper | Target | Status |
|-------|--------|--------|
| Tutorial (Paper 1) | AJP / Quantum | Draft v0.1 |
| Software (Paper 2) | JOSS | Draft v0.1 |
| Cookbook (Paper 2b) | arXiv / JOSS supplement | Draft v0.1 |
| Emergence (Paper 3) | PRA / PRResearch | Scaffold, major revision |

Papers live in `.project/research/paper-*/`.

---

## Code Patterns

### Discriminated Unions for Algebra

```fsharp
type C<'u> = C of complex * 'u    // Coefficient × unit
type P<'u> = P of C<'u> list      // Product of coefficients
type S<'u> = S of P<'u> list      // Sum of products
```

### Encoding via Index Sets

```fsharp
let jordanWignerScheme n : EncodingScheme = ...
let bravyiKitaevScheme n : EncodingScheme = ...
```

### Encoding via Trees

```fsharp
let balancedTernaryTreeTerms n : MajoranaTerms = ...
```

---

## Testing

- 270+ tests with FsCheck property testing
- Test register at `.project/test-register.md`
- Run `dotnet test` before committing

---

## Current Focus

Check `.project/research/JOURNAL.md` for current priorities, but likely:

1. **Paper 3 (Emergence)** — Major revision incorporating star-tree discovery
2. **Paper 2 (Software)** — JOSS submission preparation
3. **Documentation** — Cookbook chapters, API docs
4. **Bosonic extensions** — Mixed fermion-boson systems

---

## Tips

1. **Ask about context** if unsure — read plans and journal first
2. **Use immutable types** — F# discriminated unions, records
3. **Write tests** — especially property tests with FsCheck
4. **Update documentation** — XML doc comments required on all public APIs
5. **Update journal** — record what was done each session
6. **Check CI** — all commits must pass build + tests on all platforms
7. **Test register** — update `.project/test-register.md` when tests change
8. **Use prompts** — `commit`, `release`, `paper`, `coach`, `pick-next-work` etc.
