/// Shared verification and analysis tools for all three papers.
///
/// Tools:
///   1. MatrixVerification.fsx  — eigenspectrum equivalence across encodings
///   2. SymmetryAnalysis.fsx    — Z₂ stabiliser detection, parity operator weight
///   3. CnotCost.fsx            — circuit cost estimation from Pauli weights
///   4. EncodingSpace.fsx       — random tree generation, monotonicity census
///   5. FigureData.fsx          — data generation for paper figures

#r "../../src/Encodings/bin/Debug/net10.0/Encodings.dll"

// This file is a placeholder. Each tool will be a separate .fsx script.
// Run `dotnet build ../../Encodings/Encodings.fsproj` before using any tool.

printfn "Shared tools loaded. Build the library first:"
printfn "  dotnet build ../../Encodings/Encodings.fsproj"
