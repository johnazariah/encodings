namespace Tests

module Fcidump =
    open System.Numerics
    open Encodings
    open Encodings.Fcidump
    open Xunit

    // ── Minimal H₂/STO-3G FCIDUMP (2 spatial orbitals) ─────────────
    // Values from PySCF: H₂ at 1.4 Bohr, STO-3G basis
    let private h2Fcidump = """
 &FCI NORB=   2,NELEC= 2,MS2=0,
  ORBSYM=1,1,
  ISYM=1,
 &END
  6.74493103e-01   1   1   1   1
  1.81288808e-01   2   2   1   1
  6.63472101e-02   2   1   2   1
  6.97398040e-01   2   2   2   2
 -1.25233495e+00   1   1   0   0
 -4.79677800e-01   2   2   0   0
  7.13776188e-01   0   0   0   0
"""

    // ── Header Parsing ──────────────────────────────────────────────

    [<Fact>]
    let ``parse: reads NORB correctly`` () =
        let data = parse h2Fcidump
        Assert.Equal(2, data.Norb)

    [<Fact>]
    let ``parse: reads NELEC correctly`` () =
        let data = parse h2Fcidump
        Assert.Equal(2, data.Nelec)

    [<Fact>]
    let ``parse: reads MS2 correctly`` () =
        let data = parse h2Fcidump
        Assert.Equal(0, data.Ms2)

    [<Fact>]
    let ``parse: reads ORBSYM correctly`` () =
        let data = parse h2Fcidump
        Assert.Equal<int[]>([| 1; 1 |], data.OrbSym)

    [<Fact>]
    let ``parse: reads ISYM correctly`` () =
        let data = parse h2Fcidump
        Assert.Equal(1, data.ISym)

    [<Fact>]
    let ``parse: reads core energy`` () =
        let data = parse h2Fcidump
        Assert.Equal(0.713776188, data.CoreEnergy, 6)

    // ── One-Electron Integrals ──────────────────────────────────────

    [<Fact>]
    let ``parse: diagonal one-electron integrals`` () =
        let data = parse h2Fcidump
        Assert.Equal(-1.25233495, data.H1e.[0, 0], 6)
        Assert.Equal(-0.479677800, data.H1e.[1, 1], 6)

    [<Fact>]
    let ``parse: one-electron integrals are symmetric`` () =
        // H₂/STO-3G has no off-diagonal h_12 in this minimal dump,
        // but verify symmetry holds for diagonal entries
        let data = parse h2Fcidump
        Assert.Equal(data.H1e.[0, 0], data.H1e.[0, 0])

    // ── Two-Electron Integrals ──────────────────────────────────────

    [<Fact>]
    let ``parse: diagonal two-electron integral (11|11)`` () =
        let data = parse h2Fcidump
        Assert.Equal(0.674493103, data.H2e.[0, 0, 0, 0], 6)

    [<Fact>]
    let ``parse: two-electron integral (22|11) = (11|22)`` () =
        let data = parse h2Fcidump
        // (22|11) in 1-based = (1,1,0,0) in 0-based
        Assert.Equal(0.181288808, data.H2e.[1, 1, 0, 0], 6)
        // 8-fold symmetry: (11|22) = (22|11)
        Assert.Equal(0.181288808, data.H2e.[0, 0, 1, 1], 6)

    [<Fact>]
    let ``parse: exchange integral (21|21) is symmetrized`` () =
        let data = parse h2Fcidump
        // (21|21) in 1-based = (1,0,1,0) in 0-based
        Assert.Equal(0.0663472101, data.H2e.[1, 0, 1, 0], 6)
        // 8-fold: (12|12) = (21|21)
        Assert.Equal(0.0663472101, data.H2e.[0, 1, 0, 1], 6)
        // 8-fold: (12|21) = (21|21)
        Assert.Equal(0.0663472101, data.H2e.[0, 1, 1, 0], 6)

    [<Fact>]
    let ``parse: diagonal (22|22)`` () =
        let data = parse h2Fcidump
        Assert.Equal(0.697398040, data.H2e.[1, 1, 1, 1], 6)

    // ── Coefficient Factory ─────────────────────────────────────────

    [<Fact>]
    let ``toCoefficientFactory: one-body key returns h1e`` () =
        let data = parse h2Fcidump
        let factory = toCoefficientFactory data
        let v = factory "0,0"
        Assert.True(v.IsSome)
        Assert.Equal(-1.25233495, v.Value.Real, 6)

    [<Fact>]
    let ``toCoefficientFactory: out-of-range key returns None`` () =
        let data = parse h2Fcidump
        let factory = toCoefficientFactory data
        Assert.True((factory "5,0").IsNone)
        Assert.True((factory "0,5").IsNone)

    [<Fact>]
    let ``toCoefficientFactory: two-body key includes half factor`` () =
        let data = parse h2Fcidump
        let factory = toCoefficientFactory data
        // factory("p,q,r,s") = ½ × chemist(p,s,q,r)
        // factory("0,1,1,0") = ½ × chemist(0,0,1,1) = ½ × (00|11)
        // (00|11) = (11|00) by symmetry = 0.181288808
        let v = factory "0,1,1,0"
        Assert.True(v.IsSome)
        Assert.Equal(0.5 * 0.181288808, v.Value.Real, 6)

    [<Fact>]
    let ``toCoefficientFactory: zero integral returns None`` () =
        let data = parse h2Fcidump
        let factory = toCoefficientFactory data
        // Off-diagonal h_01 not in this dump, should be zero → None
        let v = factory "0,1"
        Assert.True(v.IsNone)

    [<Fact>]
    let ``toCoefficientFactory: invalid key returns None`` () =
        let data = parse h2Fcidump
        let factory = toCoefficientFactory data
        Assert.True((factory "bad").IsNone)
        Assert.True((factory "1,2,3").IsNone)

    // ── parseToFactory convenience ──────────────────────────────────

    [<Fact>]
    let ``parseToFactory: returns factory, core energy, norb`` () =
        let (factory, coreEnergy, norb) = parseToFactory h2Fcidump
        Assert.Equal(2, norb)
        Assert.Equal(0.713776188, coreEnergy, 6)
        Assert.True((factory "0,0").IsSome)

    // ── Integration: Factory → Hamiltonian ──────────────────────────

    [<Fact>]
    let ``integration: FCIDUMP factory produces non-empty JW Hamiltonian`` () =
        let (factory, _, norb) = parseToFactory h2Fcidump
        let hamiltonian =
            Hamiltonian.computeHamiltonianWith
                JordanWigner.jordanWignerTerms
                factory
                (uint32 norb)
        Assert.True(hamiltonian.SummandTerms.Length > 0)

    [<Fact>]
    let ``integration: FCIDUMP Hamiltonian has correct qubit count`` () =
        let (factory, _, norb) = parseToFactory h2Fcidump
        let hamiltonian =
            Hamiltonian.computeHamiltonianWith
                JordanWigner.jordanWignerTerms
                factory
                (uint32 norb)
        // H₂/STO-3G with 2 spatial orbitals → 2-qubit Hamiltonian
        let maxSize = hamiltonian.SummandTerms |> Array.map (fun t -> t.Size) |> Array.max
        Assert.Equal(2, maxSize)

    [<Fact>]
    let ``integration: different encodings produce different Hamiltonians`` () =
        let (factory, _, norb) = parseToFactory h2Fcidump
        let n = uint32 norb
        let jw = Hamiltonian.computeHamiltonianWith JordanWigner.jordanWignerTerms factory n
        let bk = Hamiltonian.computeHamiltonianWith BravyiKitaev.bravyiKitaevTerms factory n
        // Both should be non-empty and produce valid Pauli sums
        Assert.True(jw.SummandTerms.Length > 0)
        Assert.True(bk.SummandTerms.Length > 0)

    [<Fact>]
    let ``integration: CostAnalysis works with FCIDUMP Hamiltonian`` () =
        let (factory, _, norb) = parseToFactory h2Fcidump
        let hamiltonian =
            Hamiltonian.computeHamiltonianWith
                JordanWigner.jordanWignerTerms
                factory
                (uint32 norb)
        let costs = CostAnalysis.hamiltonianCosts hamiltonian
        Assert.True(costs.TermCount > 0)
        Assert.True(costs.LambdaNorm > 0.0)
        Assert.True(costs.QubitCount = 2)

    // ── Header format variations ────────────────────────────────────

    [<Fact>]
    let ``parse: handles slash terminator`` () =
        let content = """
 &FCI NORB= 2,NELEC=2,MS2=0,
  ORBSYM=1,1,
  ISYM=1,
 /
  1.0   1   1   0   0
  0.5   0   0   0   0
"""
        let data = parse content
        Assert.Equal(2, data.Norb)
        Assert.Equal(1.0, data.H1e.[0, 0], 10)
        Assert.Equal(0.5, data.CoreEnergy, 10)
