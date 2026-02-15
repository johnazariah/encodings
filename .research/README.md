# Research Papers — Fermion-to-Qubit Encodings

Four papers exploring the structure, pedagogy, and emergent properties of
fermion-to-qubit encodings for quantum simulation.

## Papers

| # | Directory | Title | Target |
|---|-----------|-------|--------|
| 1 | `paper-tutorial/` | From Molecules to Qubits: A Complete Guide to Quantum Chemistry Simulation | AJP / Quantum (pedagogical) |
| 2 | `paper-software/` | Algebraic Encodings: A Typed Functional Framework for Fermion-to-Qubit Mappings | JOSS / SoftwareX |
| 2b | `paper-cookbook/` | FockMap Library Cookbook: A Progressive Tutorial for Symbolic Fock-Space Operator Algebra | Companion to Paper 2 (arXiv / JOSS supplement) |
| 3 | `paper-emergence/` | Emergent Structure in Fermion-to-Qubit Encodings: Trees, Locality, and the Geometry of Representation | PRA / PRResearch / Quantum |

## Shared Resources

- `tools/` — Verification & analysis scripts shared across all papers
  - Matrix-level eigenspectrum validation
  - Symmetry analysis (Z₂ stabilizer detection)
  - CNOT cost estimation
  - Encoding space explorer (random trees, phase diagram)
- `figures/` — Shared figure assets (tree diagrams, scaling plots)

## Dependencies

All scripts reference the compiled library at `../Encodings/bin/Debug/net10.0/Encodings.dll`.
Build the library first:

```bash
cd .. && dotnet build Encodings/Encodings.fsproj
```

## Status

| Paper | Outline | Plan | Draft | Figures | Verification |
|-------|---------|------|-------|---------|--------------|
| Tutorial   | ✅ | ✅ | ✅ v0.1 | 🔲 | ✅ MatrixVerification, ParityOperator |
| Software   | ✅ | ✅ | ✅ v0.1 | 🔲 | ✅ AnticommutationTest |
| Cookbook    | — | — | ✅ v0.1 | — | — (shares Software verification) |
| Emergence  | ✅ | ✅ | 🔲 (deferred) | 🔲 | ✅ MonotonicityCensus |

## Investigation Journal

See [JOURNAL.md](JOURNAL.md) for the running log of investigations,
discoveries, and open questions.
