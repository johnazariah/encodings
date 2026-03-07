namespace Tests

module VariationalCircuits =
    open System.Numerics
    open Encodings
    open Encodings.VariationalCircuits
    open Xunit

    let private prs (terms : (string * Complex) list) =
        terms
        |> List.map (fun (ops, coeff) -> PauliRegister(ops, coeff))
        |> List.toArray
        |> PauliRegisterSequence

    let private c x = Complex(x, 0.0)

    // ── qubitWiseCommutes ───────────────────────────────────────────

    [<Fact>]
    let ``identical Pauli strings qubit-wise commute`` () =
        let a = PauliRegister("XYZ", Complex.One)
        let b = PauliRegister("XYZ", Complex.One)
        Assert.True(qubitWiseCommutes a b)

    [<Fact>]
    let ``II commutes with everything`` () =
        let id = PauliRegister("II", Complex.One)
        let xz = PauliRegister("XZ", Complex.One)
        Assert.True(qubitWiseCommutes id xz)
        Assert.True(qubitWiseCommutes xz id)

    [<Fact>]
    let ``XZ and ZX do NOT qubit-wise commute`` () =
        let xz = PauliRegister("XZ", Complex.One)
        let zx = PauliRegister("ZX", Complex.One)
        Assert.False(qubitWiseCommutes xz zx)

    [<Fact>]
    let ``ZI and ZZ qubit-wise commute`` () =
        let zi = PauliRegister("ZI", Complex.One)
        let zz = PauliRegister("ZZ", Complex.One)
        Assert.True(qubitWiseCommutes zi zz)

    [<Fact>]
    let ``XI and ZI do NOT qubit-wise commute`` () =
        let xi = PauliRegister("XI", Complex.One)
        let zi = PauliRegister("ZI", Complex.One)
        Assert.False(qubitWiseCommutes xi zi)

    // ── groupCommutingTerms ─────────────────────────────────────────

    [<Fact>]
    let ``all-Z Hamiltonian goes into one measurement group`` () =
        let h = prs [ ("ZI", c 0.5); ("IZ", c 0.3); ("ZZ", c -0.2) ]
        let program = groupCommutingTerms h
        Assert.Equal(1, program.GroupCount)

    [<Fact>]
    let ``non-commuting X and Z on same qubit get separate groups`` () =
        let h = prs [ ("XI", c 1.0); ("ZI", c 1.0) ]
        let program = groupCommutingTerms h
        Assert.True(program.GroupCount >= 2)

    [<Fact>]
    let ``grouping reduces group count below total terms for H2-like Hamiltonian`` () =
        // A small H₂-like Hamiltonian with some commuting terms
        let h = prs [
            ("IIZI", c 0.17)
            ("ZZII", c -0.22)
            ("IIIZ", c 0.17)
            ("ZIZI", c 0.12)
            ("IZIZ", c 0.12)
            ("XXYY", c 0.04)
            ("YYXX", c 0.04)
        ]
        let program = groupCommutingTerms h
        Assert.True(program.GroupCount < program.TotalTerms)

    [<Fact>]
    let ``MeasurementProgram total terms matches Hamiltonian term count`` () =
        let h = prs [ ("ZI", c 0.5); ("IZ", c 0.3); ("XX", c 0.1); ("YY", c -0.2) ]
        let program = groupCommutingTerms h
        Assert.Equal(4, program.TotalTerms)
        let totalInBases = program.Bases |> Array.sumBy (fun b -> b.Terms.Length)
        Assert.Equal(program.TotalTerms, totalInBases)

    // ── estimateShots ───────────────────────────────────────────────

    [<Fact>]
    let ``estimateShots scales as O(1/epsilon^2)`` () =
        let h = prs [ ("ZI", c 1.0); ("IZ", c 1.0) ]
        let shots1 = estimateShots 0.1 h
        let shots2 = estimateShots 0.01 h
        // Reducing ε by 10× should increase shots by ~100×
        let ratio = float shots2 / float shots1
        Assert.True(ratio > 80.0 && ratio < 120.0,
            sprintf "Expected ~100× ratio, got %.1f" ratio)

    [<Fact>]
    let ``estimateShots with single unit-coefficient term`` () =
        let h = prs [ ("ZI", c 1.0) ]
        // (Σ|c|)² / ε² = 1 / 0.1² = 100
        let shots = estimateShots 0.1 h
        Assert.Equal(100, shots)

    // ── qpeResources ────────────────────────────────────────────────

    [<Fact>]
    let ``QPE ancilla count equals precision bits`` () =
        let h = prs [ ("ZI", c 1.0); ("IZ", c -0.5) ]
        let est = qpeResources 4 h 0.1
        Assert.Equal(4, est.AncillaQubits)
        Assert.Equal(4, est.PrecisionBits)

    [<Fact>]
    let ``QPE system qubits matches register size`` () =
        let h = prs [ ("XZYI", c 1.0) ]
        let est = qpeResources 3 h 0.1
        Assert.Equal(4, est.SystemQubits)

    [<Fact>]
    let ``QPE total CNOTs increase with precision bits`` () =
        let h = prs [ ("XZ", c 1.0) ]
        let est3 = qpeResources 3 h 0.1
        let est5 = qpeResources 5 h 0.1
        Assert.True(est5.TotalCnots > est3.TotalCnots)
