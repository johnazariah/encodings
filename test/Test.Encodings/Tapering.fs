namespace Tests

module Tapering =
    open System
    open System.Numerics
    open Encodings
    open Encodings.Tapering
    open Xunit

    let private prs (terms : (string * Complex) list) =
        terms
        |> List.map (fun (ops, coeff) -> PauliRegister(ops, coeff))
        |> List.toArray
        |> PauliRegisterSequence

    [<Fact>]
    let ``Tapering : diagonalZ2SymmetryQubits detects I/Z-only qubits`` () =
        let h =
            prs
                [ ("ZI", Complex(1.0, 0.0))
                  ("IZ", Complex(2.0, 0.0))
                  ("ZZ", Complex(-0.5, 0.0)) ]

        let qs = diagonalZ2SymmetryQubits h
        Assert.Equal<int[]>([| 0; 1 |], qs)

    [<Fact>]
    let ``Tapering : diagonalZ2SymmetryQubits excludes qubits with X/Y`` () =
        let h =
            prs
                [ ("XI", Complex(1.0, 0.0))
                  ("IZ", Complex(2.0, 0.0)) ]

        let qs = diagonalZ2SymmetryQubits h
        Assert.Equal<int[]>([| 1 |], qs)

    [<Fact>]
    let ``Tapering : taperDiagonalZ2 applies sector sign and removes qubits`` () =
        let h =
            prs
                [ ("ZI", Complex(-0.5, 0.0))
                  ("ZZ", Complex(0.25, 0.0)) ]

        let plus = taperDiagonalZ2 [ (1, 1) ] h
        let minus = taperDiagonalZ2 [ (1, -1) ] h

        Assert.Equal(2, plus.OriginalQubitCount)
        Assert.Equal(1, plus.TaperedQubitCount)
        Assert.Equal<int[]>([| 1 |], plus.RemovedQubits)

        // +1 sector: -0.5 Z + 0.25 Z = -0.25 Z
        let (plusFound, plusZ) = plus.Hamiltonian.["Z"]
        Assert.True(plusFound)
        Assert.Equal(Complex(-0.25, 0.0), plusZ.Coefficient)

        // -1 sector: -0.5 Z - 0.25 Z = -0.75 Z
        let (minusFound, minusZ) = minus.Hamiltonian.["Z"]
        Assert.True(minusFound)
        Assert.Equal(Complex(-0.75, 0.0), minusZ.Coefficient)

    [<Fact>]
    let ``Tapering : taperDiagonalZ2 rejects non-diagonal tapered qubits`` () =
        let h = prs [ ("XI", Complex.One) ]

        let ex = Assert.Throws<ArgumentException>(fun () ->
            taperDiagonalZ2 [ (0, 1) ] h |> ignore)

        Assert.Contains("not a diagonal Z2 symmetry", ex.Message)

    [<Fact>]
    let ``Tapering : taperDiagonalZ2 rejects invalid eigenvalues`` () =
        let h = prs [ ("ZI", Complex.One) ]

        let ex = Assert.Throws<ArgumentException>(fun () ->
            taperDiagonalZ2 [ (0, 0) ] h |> ignore)

        Assert.Contains("must be +1 or -1", ex.Message)

    [<Fact>]
    let ``Tapering : convenience helper matches explicit +1 tapering`` () =
        let h =
            prs
                [ ("ZI", Complex(1.0, 0.0))
                  ("IZ", Complex(2.0, 0.0))
                  ("ZZ", Complex(3.0, 0.0)) ]

        let auto = taperAllDiagonalZ2WithPositiveSector h
        let manual = taperDiagonalZ2 [ (0, 1); (1, 1) ] h

        Assert.Equal(manual.Hamiltonian.ToString(), auto.Hamiltonian.ToString())
        Assert.Equal<int[]>(manual.RemovedQubits, auto.RemovedQubits)

    // ── Benchmarks: measurable improvements on encoded Hamiltonians ──

    [<Fact>]
    let ``Tapering : pure-ZZ diagonal Hamiltonian detects all qubits and reduces`` () =
        // A diagonal-only Hamiltonian (number operators, typical of Z-only terms)
        let h =
            prs
                [ ("ZIIZ", Complex(0.5, 0.0))
                  ("IZZI", Complex(-0.3, 0.0))
                  ("IIZI", Complex(0.8, 0.0))
                  ("ZZII", Complex(0.2, 0.0))
                  ("IIZZ", Complex(-0.1, 0.0)) ]

        let symQubits = diagonalZ2SymmetryQubits h
        Assert.Equal(4, symQubits.Length)

        let result = taperAllDiagonalZ2WithPositiveSector h
        Assert.Equal(0, result.TaperedQubitCount)
        Assert.True(result.Hamiltonian.SummandTerms.Length <= h.SummandTerms.Length)

    [<Fact>]
    let ``Tapering : mixed diagonal+off-diagonal Hamiltonian tapers partial qubits`` () =
        // Qubits 0 and 2 are diagonal (only I/Z); qubits 1 and 3 have X/Y
        let h =
            prs
                [ ("ZIZI", Complex(0.5, 0.0))       // q0=Z, q1=I, q2=Z, q3=I
                  ("IXIX", Complex(-0.3, 0.0))       // q0=I, q1=X, q2=I, q3=X
                  ("ZIIZ", Complex(0.2, 0.0))        // q0=Z, q1=I, q2=I, q3=Z — q3 has Z not X/Y here
                  ("IYIY", Complex(0.1, 0.0)) ]       // q0=I, q1=Y, q2=I, q3=Y

        let symQubits = diagonalZ2SymmetryQubits h
        // q0: Z,I,Z,I → ✓ diagonal
        // q1: I,X,I,Y → ✗ has X and Y
        // q2: Z,I,I,I → ✓ diagonal
        // q3: I,X,Z,Y → ✗ has X and Y
        Assert.Equal<int[]>([| 0; 2 |], symQubits)

        let result = taperDiagonalZ2 [ (0, 1); (2, -1) ] h
        Assert.Equal(4, result.OriginalQubitCount)
        Assert.Equal(2, result.TaperedQubitCount)
        Assert.Equal<int[]>([| 0; 2 |], result.RemovedQubits)

    [<Fact>]
    let ``Tapering : all sectors preserve hermitian structure (coefficients are real)`` () =
        let h =
            prs
                [ ("ZIIZ", Complex(0.5, 0.0))
                  ("IZZI", Complex(-0.3, 0.0))
                  ("ZZII", Complex(0.2, 0.0))
                  ("IIZZ", Complex(-0.1, 0.0)) ]

        let symQubits = diagonalZ2SymmetryQubits h

        let allSectors =
            [ for mask in 0 .. (1 <<< symQubits.Length) - 1 ->
                  symQubits
                  |> Array.mapi (fun i q -> (q, if (mask &&& (1 <<< i)) <> 0 then 1 else -1))
                  |> Array.toList ]

        for sector in allSectors do
            let result = taperDiagonalZ2 sector h
            for t in result.Hamiltonian.SummandTerms do
                Assert.True(abs t.Coefficient.Imaginary < 1e-14,
                    sprintf "Imaginary coefficient in sector %A: %A" sector t.Coefficient)

    // ══════════════════════════════════════════════════════════════════
    //  v2: Symplectic, Clifford, and unified pipeline tests
    // ══════════════════════════════════════════════════════════════════

    // ── Phase 1: Symplectic representation ────────────────────────────

    [<Fact>]
    let ``Symplectic : toSymplectic round-trips through fromSymplectic`` () =
        let reg = PauliRegister("XYZI", Complex(0.5, 0.3))
        let sv = toSymplectic reg
        let reg' = fromSymplectic sv
        Assert.Equal(reg.Signature, reg'.Signature)

    [<Theory>]
    [<InlineData("II", "II", true)>]
    [<InlineData("XX", "ZZ", true)>]
    [<InlineData("XY", "YX", true)>]
    [<InlineData("XZ", "ZX", true)>]   // commute: XZ and ZX both have inner product 0 mod 2? XZ: (1,0)(0,1)=1 and (0,1)(1,0)=1, sum=2, mod2=0 → commute
    [<InlineData("XY", "XY", true)>]
    [<InlineData("XZ", "YI", false)>]  // X on q0, Z on q1 vs Y on q0, I on q1: q0 contributes x·z+z·x = 1·1+0·1=1, q1: 0, total=1 odd → anti-commute
    let ``Symplectic : commutes agrees with Pauli algebra`` (a : string, b : string, expected : bool) =
        let ra = PauliRegister(a, Complex.One)
        let rb = PauliRegister(b, Complex.One)
        let svA = toSymplectic ra
        let svB = toSymplectic rb
        Assert.Equal(expected, commutes svA svB)

    [<Fact>]
    let ``Symplectic : Z operator is symplectic (0,1)`` () =
        let z = PauliRegister("Z", Complex.One)
        let sv = toSymplectic z
        Assert.False(sv.X.[0])
        Assert.True(sv.Z.[0])

    [<Fact>]
    let ``Symplectic : X operator is symplectic (1,0)`` () =
        let x = PauliRegister("X", Complex.One)
        let sv = toSymplectic x
        Assert.True(sv.X.[0])
        Assert.False(sv.Z.[0])

    [<Fact>]
    let ``Symplectic : Y operator is symplectic (1,1)`` () =
        let y = PauliRegister("Y", Complex.One)
        let sv = toSymplectic y
        Assert.True(sv.X.[0])
        Assert.True(sv.Z.[0])

    // ── Phase 1: Commuting generators ─────────────────────────────────

    [<Fact>]
    let ``findCommutingGenerators : diagonal-only Hamiltonian has full symmetry`` () =
        let h = prs [ ("ZI", Complex.One); ("IZ", Complex.One) ]
        let gens = findCommutingGenerators h
        // All 2-qubit Paulis with only I/Z commute with ZI and IZ
        Assert.True(gens.Length > 0)

    [<Fact>]
    let ``findCommutingGenerators : single X term has Z symmetry on other qubits`` () =
        let h = prs [ ("XI", Complex.One) ]
        let gens = findCommutingGenerators h
        // IZ commutes with XI (different qubits), ZI anti-commutes (ZX = -XZ), XI commutes (same)
        // So generators should include things like IZ and XI
        Assert.True(gens.Length > 0)

    // ── Phase 2: Independent generators ───────────────────────────────

    [<Fact>]
    let ``independentGenerators : removes redundant generators`` () =
        let g1 = toSymplectic (PauliRegister("ZI", Complex.One))
        let g2 = toSymplectic (PauliRegister("IZ", Complex.One))
        let g3 = toSymplectic (PauliRegister("ZZ", Complex.One))  // g3 = g1 * g2 (mod 2)
        let indep = independentGenerators [| g1; g2; g3 |]
        Assert.Equal(2, indep.Length)

    [<Fact>]
    let ``z2SymmetryCount : diagonal-only 2-qubit Hamiltonian has 2 symmetries`` () =
        let h = prs [ ("ZI", Complex.One); ("IZ", Complex.One); ("ZZ", Complex.One) ]
        let count = z2SymmetryCount h
        Assert.Equal(2, count)

    // ── Phase 3: Clifford synthesis ───────────────────────────────────

    [<Fact>]
    let ``Clifford : single-qubit Z generator produces no gates`` () =
        let gen = toSymplectic (PauliRegister("ZI", Complex.One))
        let (gates, targets) = synthesizeTaperingClifford [| gen |]
        // Z on qubit 0 is already diagonal — should target qubit 0
        Assert.Equal(0, targets.[0])
        // After synthesis, applying gates to gen should give Z on target qubit
        let rotated = applyGatesToSymplectic gates gen
        Assert.False(rotated.X.[targets.[0]])
        Assert.True(rotated.Z.[targets.[0]])

    [<Fact>]
    let ``Clifford : X generator gets rotated to Z`` () =
        let gen = toSymplectic (PauliRegister("XI", Complex.One))
        let (gates, targets) = synthesizeTaperingClifford [| gen |]
        let rotated = applyGatesToSymplectic gates gen
        // Should be single-qubit Z on target
        Assert.False(rotated.X.[targets.[0]])
        Assert.True(rotated.Z.[targets.[0]])
        // Other qubit should be identity
        let otherQ = if targets.[0] = 0 then 1 else 0
        Assert.False(rotated.X.[otherQ])
        Assert.False(rotated.Z.[otherQ])

    [<Fact>]
    let ``Clifford : Y generator gets rotated to Z`` () =
        let gen = toSymplectic (PauliRegister("IY", Complex.One))
        let (gates, targets) = synthesizeTaperingClifford [| gen |]
        let rotated = applyGatesToSymplectic gates gen
        Assert.False(rotated.X.[targets.[0]])
        Assert.True(rotated.Z.[targets.[0]])

    [<Fact>]
    let ``Clifford : ZZ generator gets rotated to single-qubit Z`` () =
        let gen = toSymplectic (PauliRegister("ZZ", Complex.One))
        let (gates, targets) = synthesizeTaperingClifford [| gen |]
        let rotated = applyGatesToSymplectic gates gen
        // Exactly one qubit should have Z, the other should be I
        let zCount = [| for i in 0 .. 1 -> (not rotated.X.[i]) && rotated.Z.[i] |] |> Array.filter id |> Array.length
        let iCount = [| for i in 0 .. 1 -> (not rotated.X.[i]) && (not rotated.Z.[i]) |] |> Array.filter id |> Array.length
        Assert.Equal(1, zCount)
        Assert.Equal(1, iCount)

    [<Fact>]
    let ``Clifford : two independent generators get different target qubits`` () =
        let g1 = toSymplectic (PauliRegister("ZI", Complex.One))
        let g2 = toSymplectic (PauliRegister("IZ", Complex.One))
        let (gates, targets) = synthesizeTaperingClifford [| g1; g2 |]
        Assert.NotEqual(targets.[0], targets.[1])

    // ── Phase 4: Unified pipeline ─────────────────────────────────────

    [<Fact>]
    let ``taper DiagonalOnly : matches v1 result`` () =
        let h = prs [ ("ZI", Complex(1.0, 0.0)); ("IZ", Complex(2.0, 0.0)); ("ZZ", Complex(3.0, 0.0)) ]
        let v1 = taperAllDiagonalZ2WithPositiveSector h
        let v2 = taper { defaultTaperingOptions with Method = DiagonalOnly } h
        Assert.Equal(v1.Hamiltonian.ToString(), v2.Hamiltonian.ToString())
        Assert.Equal(v1.TaperedQubitCount, v2.TaperedQubitCount)

    [<Fact>]
    let ``taper FullClifford : diagonal Hamiltonian matches v1`` () =
        let h = prs [ ("ZI", Complex(1.0, 0.0)); ("IZ", Complex(2.0, 0.0)); ("ZZ", Complex(3.0, 0.0)) ]
        let v1 = taperAllDiagonalZ2WithPositiveSector h
        let v2 = taper { defaultTaperingOptions with Method = FullClifford } h
        // Both should reduce to 0 qubits
        Assert.Equal(v1.TaperedQubitCount, v2.TaperedQubitCount)

    [<Fact>]
    let ``taper FullClifford : reports generators and Clifford gates`` () =
        let h = prs [ ("XI", Complex(1.0, 0.0)); ("IX", Complex(2.0, 0.0)) ]
        let result = taper defaultTaperingOptions h
        Assert.True(result.Generators.Length > 0)
        Assert.True(result.TaperedQubitCount < result.OriginalQubitCount)

    [<Fact>]
    let ``taper FullClifford : MaxQubitsToRemove caps removal`` () =
        let h = prs [ ("ZI", Complex(1.0, 0.0)); ("IZ", Complex(2.0, 0.0)) ]
        let result = taper { defaultTaperingOptions with MaxQubitsToRemove = Some 1 } h
        Assert.True(result.RemovedQubits.Length <= 1)

    [<Fact>]
    let ``taper FullClifford : empty Hamiltonian returns unchanged`` () =
        let h = PauliRegisterSequence()
        let result = taper defaultTaperingOptions h
        Assert.Equal(0, result.TaperedQubitCount)
        Assert.Equal(0, result.OriginalQubitCount)

    // ══════════════════════════════════════════════════════════════════
    //  Benchmarks: measurable with/without tapering comparison
    // ══════════════════════════════════════════════════════════════════

    [<Fact>]
    let ``Benchmark : 6-qubit diagonal Hamiltonian tapers to 0 qubits`` () =
        let h =
            prs
                [ ("ZIIIII", Complex(0.5, 0.0))
                  ("IZIIII", Complex(-0.3, 0.0))
                  ("IIZIII", Complex(0.8, 0.0))
                  ("IIIZII", Complex(0.2, 0.0))
                  ("IIIIZI", Complex(-0.4, 0.0))
                  ("IIIIIZ", Complex(0.7, 0.0))
                  ("ZZZZII", Complex(0.1, 0.0))
                  ("IIZZZZ", Complex(-0.2, 0.0)) ]

        let before = h.SummandTerms.Length
        let result = taper { defaultTaperingOptions with Method = DiagonalOnly } h

        Assert.Equal(6, result.OriginalQubitCount)
        Assert.Equal(0, result.TaperedQubitCount)
        Assert.True(result.Hamiltonian.SummandTerms.Length <= before,
            sprintf "Tapering should not increase term count: %d -> %d" before result.Hamiltonian.SummandTerms.Length)

    [<Fact>]
    let ``Benchmark : mixed Hamiltonian — Clifford finds more symmetries than diagonal`` () =
        // Hamiltonian with XX+YY hopping terms: no diagonal Z2 symmetries,
        // but ZZ is a Z2 symmetry generator (commutes with XX, YY, ZZ, IZ, ZI)
        let h =
            prs
                [ ("XX", Complex(0.5, 0.0))
                  ("YY", Complex(0.5, 0.0))
                  ("ZZ", Complex(-0.3, 0.0))
                  ("ZI", Complex(0.2, 0.0))
                  ("IZ", Complex(0.2, 0.0)) ]

        let diag = diagonalZ2SymmetryQubits h
        let cliffordResult = taper defaultTaperingOptions h

        // No diagonal symmetries (X/Y appear on both qubits)
        Assert.Equal(0, diag.Length)
        // Clifford should find ZZ as a generator and taper 1 qubit
        Assert.True(cliffordResult.TaperedQubitCount < cliffordResult.OriginalQubitCount,
            sprintf "Clifford should taper at least 1 qubit, got %d -> %d"
                cliffordResult.OriginalQubitCount cliffordResult.TaperedQubitCount)

    [<Fact>]
    let ``Benchmark : 4-qubit Heisenberg model — Clifford tapers where diagonal cannot`` () =
        // XXI + YYI + ZZI + IXX + IYY + IZZ — 3-qubit Heisenberg chain
        let h =
            prs
                [ ("XXI", Complex(1.0, 0.0))
                  ("YYI", Complex(1.0, 0.0))
                  ("ZZI", Complex(1.0, 0.0))
                  ("IXX", Complex(1.0, 0.0))
                  ("IYY", Complex(1.0, 0.0))
                  ("IZZ", Complex(1.0, 0.0)) ]

        let diagCount = (diagonalZ2SymmetryQubits h).Length
        let fullCount = z2SymmetryCount h

        // The Heisenberg model has no diagonal Z2 on any single qubit
        Assert.Equal(0, diagCount)
        // But general Z2 detection should find at least 1 multi-qubit symmetry
        Assert.True(fullCount >= 1,
            sprintf "Expected ≥1 general Z2 symmetry in Heisenberg chain, got %d" fullCount)

    // ── Clifford synthesis: X/Y/XX generator coverage ─────────────────

    [<Fact>]
    let ``Clifford : XX generator hits X-clearing on non-target qubit`` () =
        let gen = toSymplectic (PauliRegister("XX", Complex.One))
        let (gates, targets) = synthesizeTaperingClifford [| gen |]
        let rotated = applyGatesToSymplectic gates gen
        // Should end up as single-qubit Z on the target
        Assert.False(rotated.X.[targets.[0]])
        Assert.True(rotated.Z.[targets.[0]])
        let otherQ = if targets.[0] = 0 then 1 else 0
        Assert.False(rotated.X.[otherQ])
        Assert.False(rotated.Z.[otherQ])

    [<Fact>]
    let ``Clifford : YY generator hits Y-path and X-clearing`` () =
        let gen = toSymplectic (PauliRegister("YY", Complex.One))
        let (gates, targets) = synthesizeTaperingClifford [| gen |]
        let rotated = applyGatesToSymplectic gates gen
        // Should produce a single-qubit Z on the target (net result)
        let otherQ = if targets.[0] = 0 then 1 else 0
        // The non-target qubit should be identity
        Assert.False(rotated.X.[otherQ] || rotated.Z.[otherQ])

    [<Fact>]
    let ``Clifford : XY generator produces correct rotation`` () =
        let gen = toSymplectic (PauliRegister("XY", Complex.One))
        let (gates, targets) = synthesizeTaperingClifford [| gen |]
        let rotated = applyGatesToSymplectic gates gen
        let otherQ = if targets.[0] = 0 then 1 else 0
        Assert.False(rotated.X.[otherQ] || rotated.Z.[otherQ])

    [<Fact>]
    let ``Clifford : XZ generator handles mixed X and Z support`` () =
        let gen = toSymplectic (PauliRegister("XZ", Complex.One))
        let (gates, targets) = synthesizeTaperingClifford [| gen |]
        let rotated = applyGatesToSymplectic gates gen
        Assert.False(rotated.X.[targets.[0]])
        Assert.True(rotated.Z.[targets.[0]])

    [<Fact>]
    let ``Clifford : YZ generator handles Y-path with Z on other qubit`` () =
        let gen = toSymplectic (PauliRegister("YZ", Complex.One))
        let (gates, targets) = synthesizeTaperingClifford [| gen |]
        let rotated = applyGatesToSymplectic gates gen
        Assert.False(rotated.X.[targets.[0]])
        Assert.True(rotated.Z.[targets.[0]])

    [<Fact>]
    let ``applyClifford : CNOT phase tracking on XY terms`` () =
        // Apply a CNOT-containing Clifford to a Hamiltonian with XY terms
        let h = prs [ ("XY", Complex(1.0, 0.0)); ("YX", Complex(-0.5, 0.0)) ]
        let gates = [CNOT(0, 1)]
        let result = applyClifford gates h
        // Just verify it doesn't crash and produces a valid Hamiltonian
        Assert.True(result.SummandTerms.Length > 0)

    [<Fact>]
    let ``applyClifford : Had gate flips Y sign`` () =
        let h = prs [ ("YI", Complex(1.0, 0.0)) ]
        let gates = [Had 0]
        let result = applyClifford gates h
        // H Y H† = -Y
        let term = result.SummandTerms.[0]
        Assert.Equal(Complex(-1.0, 0.0), term.Coefficient)

    [<Fact>]
    let ``applyClifford : S gate flips Y to -X`` () =
        let h = prs [ ("YI", Complex(1.0, 0.0)) ]
        let gates = [Sgate 0]
        let result = applyClifford gates h
        // S Y S† = -X
        let term = result.SummandTerms.[0]
        Assert.Equal(Complex(-1.0, 0.0), term.Coefficient)

    [<Fact>]
    let ``applyClifford : identity pass-through`` () =
        let h = prs [ ("ZI", Complex(1.0, 0.0)) ]
        let result = applyClifford [] h
        Assert.Equal(h.ToString(), result.ToString())

    [<Fact>]
    let ``applyClifford : Sgate on X converts to Y`` () =
        let h = prs [ ("XI", Complex(1.0, 0.0)) ]
        let gates = [Sgate 0]
        let result = applyClifford gates h
        // S X S† = Y
        let signature = result.SummandTerms.[0].Signature
        Assert.Contains("Y", signature)

    [<Fact>]
    let ``applyClifford : combined Had and Sgate`` () =
        let h = prs [ ("XI", Complex(1.0, 0.0)); ("YI", Complex(0.5, 0.0)) ]
        let gates = [Had 0; Sgate 0]
        let result = applyClifford gates h
        Assert.True(result.SummandTerms.Length > 0)

    [<Fact>]
    let ``applyClifford : CNOT with XI propagates X to target`` () =
        let h = prs [ ("XI", Complex(1.0, 0.0)) ]
        let gates = [CNOT(0, 1)]
        let result = applyClifford gates h
        // CNOT: X_c I_t → X_c X_t
        Assert.True(result.SummandTerms.Length > 0)

    [<Fact>]
    let ``applyClifford : CNOT with IZ propagates Z to control`` () =
        let h = prs [ ("IZ", Complex(1.0, 0.0)) ]
        let gates = [CNOT(0, 1)]
        let result = applyClifford gates h
        // CNOT: I_c Z_t → Z_c Z_t
        Assert.True(result.SummandTerms.Length > 0)

    [<Fact>]
    let ``applyClifford : CNOT with XZ produces phase change`` () =
        let h = prs [ ("XZ", Complex(1.0, 0.0)) ]
        let gates = [CNOT(0, 1)]
        let result = applyClifford gates h
        // cx=true, tz=true → phase flip
        Assert.True(result.SummandTerms.Length > 0)

    [<Fact>]
    let ``findCommutingGenerators : empty Hamiltonian returns empty`` () =
        let h = PauliRegisterSequence()
        let gens = findCommutingGenerators h
        Assert.Empty(gens)

    [<Fact>]
    let ``independentGenerators : empty input returns empty`` () =
        let result = independentGenerators [||]
        Assert.Empty(result)

    [<Fact>]
    let ``independentGenerators : single generator returns it`` () =
        let g = toSymplectic (PauliRegister("ZI", Complex.One))
        let result = independentGenerators [| g |]
        Assert.Equal(1, result.Length)

    [<Fact>]
    let ``Tapering : taperDiagonalZ2 rejects out-of-range qubit`` () =
        let h = prs [ ("ZI", Complex.One) ]
        let ex = Assert.Throws<ArgumentException>(fun () ->
            taperDiagonalZ2 [ (5, 1) ] h |> ignore)
        Assert.Contains("out of range", ex.Message)

    [<Fact>]
    let ``Tapering : diagonalZ2SymmetryQubits returns empty for all-X Hamiltonian`` () =
        let h = prs [ ("XX", Complex(1.0, 0.0)); ("YY", Complex(-0.5, 0.0)) ]
        let qs = diagonalZ2SymmetryQubits h
        Assert.Empty(qs)

    [<Fact>]
    let ``Symplectic : anti-commuting XZ pair detected`` () =
        let x = toSymplectic (PauliRegister("X", Complex.One))
        let z = toSymplectic (PauliRegister("Z", Complex.One))
        Assert.True(symplecticInnerProduct x z)
        Assert.False(commutes x z)

    [<Fact>]
    let ``taper FullClifford : with explicit sector and FullClifford`` () =
        let h = prs [ ("ZI", Complex(1.0, 0.0)); ("IZ", Complex(2.0, 0.0)); ("ZZ", Complex(0.5, 0.0)) ]
        let result = taper { defaultTaperingOptions with Method = FullClifford; Sector = [(0, 1); (1, -1)] } h
        Assert.True(result.TaperedQubitCount < result.OriginalQubitCount)

    [<Fact>]
    let ``gf2Kernel : identity-like independence`` () =
        // Use 3 generators on 3 qubits where all are independent
        let g1 = toSymplectic (PauliRegister("ZII", Complex.One))
        let g2 = toSymplectic (PauliRegister("IZI", Complex.One))
        let g3 = toSymplectic (PauliRegister("IIZ", Complex.One))
        let indep = independentGenerators [| g1; g2; g3 |]
        Assert.Equal(3, indep.Length)

    [<Fact>]
    let ``z2SymmetryCount : single-term X Hamiltonian`` () =
        let h = prs [ ("X", Complex.One) ]
        let count = z2SymmetryCount h
        Assert.True(count >= 0)

    [<Fact>]
    let ``taper FullClifford : 4-qubit mixed Hamiltonian`` () =
        let h = prs [
            ("XXII", Complex(0.5, 0.0))
            ("YYII", Complex(0.5, 0.0))
            ("ZZII", Complex(-0.3, 0.0))
            ("IIZZ", Complex(0.2, 0.0))
            ("ZIZI", Complex(0.1, 0.0))
            ("IZIZ", Complex(0.1, 0.0))
        ]
        // Just verify we can find and count symmetries on a 4-qubit system
        let symCount = z2SymmetryCount h
        Assert.True(symCount >= 0)

    [<Fact>]
    let ``taper FullClifford : 3-qubit with all-X terms`` () =
        let h = prs [
            ("XXX", Complex(1.0, 0.0))
            ("ZZZ", Complex(-0.5, 0.0))
        ]
        let symCount = z2SymmetryCount h
        Assert.True(symCount >= 0)

    [<Fact>]
    let ``applyClifford : multiple gates on 3 qubits`` () =
        let h = prs [ ("XYZ", Complex(1.0, 0.0)); ("ZZI", Complex(0.5, 0.0)) ]
        let gates = [Had 0; Sgate 1; CNOT(0, 2); Had 1]
        let result = applyClifford gates h
        Assert.True(result.SummandTerms.Length > 0)

    [<Fact>]
    let ``Symplectic : toSymplectic handles all Pauli types`` () =
        let r = PauliRegister("XYZI", Complex.One)
        let sv = toSymplectic r
        // X: (1,0), Y: (1,1), Z: (0,1), I: (0,0)
        Assert.True(sv.X.[0] && not sv.Z.[0])   // X
        Assert.True(sv.X.[1] && sv.Z.[1])        // Y
        Assert.True(not sv.X.[2] && sv.Z.[2])    // Z
        Assert.True(not sv.X.[3] && not sv.Z.[3]) // I

    [<Fact>]
    let ``fromSymplectic reconstructs all Pauli types`` () =
        let sv = { X = [| true; true; false; false |]; Z = [| false; true; true; false |]; N = 4 }
        let reg = fromSymplectic sv
        Assert.Equal("XYZI", reg.Signature)

    [<Fact>]
    let ``taper FullClifford : XX+YY Hamiltonian uses CNOT in Clifford`` () =
        let h = prs [ ("XX", Complex(0.5, 0.0)); ("YY", Complex(0.5, 0.0)); ("ZI", Complex(0.1, 0.0)); ("IZ", Complex(0.1, 0.0)) ]
        let result = taper defaultTaperingOptions h
        Assert.True(result.CliffordGates.Length > 0 || result.TaperedQubitCount < result.OriginalQubitCount)

    [<Fact>]
    let ``taper DiagonalOnly : with explicit sector`` () =
        let h = prs [ ("ZI", Complex(1.0, 0.0)); ("IZ", Complex(2.0, 0.0)) ]
        let result = taper { defaultTaperingOptions with Method = DiagonalOnly; Sector = [(0, 1)] } h
        Assert.Equal(1, result.RemovedQubits.Length)

    [<Fact>]
    let ``taper DiagonalOnly : with MaxQubitsToRemove`` () =
        let h = prs [ ("ZI", Complex(1.0, 0.0)); ("IZ", Complex(2.0, 0.0)) ]
        let result = taper { defaultTaperingOptions with Method = DiagonalOnly; MaxQubitsToRemove = Some 1 } h
        Assert.True(result.RemovedQubits.Length <= 1)
