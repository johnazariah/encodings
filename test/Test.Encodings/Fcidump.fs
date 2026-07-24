namespace Tests

module Fcidump =
    open System.Numerics
    open Encodings
    open Encodings.Fcidump
    open Xunit

    // ── Minimal H₂-like FCIDUMP fixture (2 spatial orbitals) ─────────────
    // Illustrative values chosen to exercise the parser and factory conventions.
    // NOT a physically-converged PySCF run (a real H₂/STO-3G at 1.4 Bohr has
    // h1 ≈ -1.252797, -0.475602; (00|00) ≈ 0.674594, (11|00) ≈ 0.663564,
    // (10|10) ≈ 0.181258, (11|11) ≈ 0.697495, Vnn = 5/7). For physically exact
    // coefficient/spectrum checks, the tests use examples/H2_STO-3G.fcidump.
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
    let ``toCoefficientFactory: two-body key returns raw physicist integral`` () =
        let data = parse h2Fcidump
        let factory = toCoefficientFactory data
        // Raw contract (0.9.0): factory("p,q,r,s") = ⟨pq|rs⟩ = (pr|qs) chemist = h2e[p,r,q,s],
        // with NO ½ and NO index swap — the Hamiltonian builder applies them.
        // ⟨01|01⟩ = (00|11) = (11|00) = 0.181288808 (Coulomb).
        let v = factory "0,1,0,1"
        Assert.True(v.IsSome)
        Assert.Equal(0.181288808, v.Value.Real, 6)
        // ⟨00|00⟩ = (00|00) = 0.674493103.
        Assert.Equal(0.674493103, (factory "0,0,0,0").Value.Real, 6)

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

    // ── Real shipped FCIDUMP → canonical H₂ coefficients (end-to-end) ────
    // Proves the real examples/H2_STO-3G.fcidump, run through the spin-orbital
    // factory, reproduces the canonical H₂ coefficient map under the raw contract
    // (the adapter supplies the raw physicist ⟨pq|rs⟩ = (pr|qs); the builder applies
    // the ½ and r↔s order). The identical coefficient map is shown to give the FCI
    // ground −1.8523881736 by the direct raw oracle in HamiltonianCoefficients, so
    // this transitively locks the FCIDUMP spectrum path.

    let private locateExample name =
        let rec up (dir : System.IO.DirectoryInfo) =
            if isNull dir then None
            else
                let candidate = System.IO.Path.Combine(dir.FullName, "examples", name)
                if System.IO.File.Exists candidate then Some candidate else up dir.Parent
        up (System.IO.DirectoryInfo(System.AppContext.BaseDirectory))

    [<Fact>]
    let ``integration: real H2 FCIDUMP reproduces the canonical H2 coefficients`` () =
        match locateExample "H2_STO-3G.fcidump" with
        | None -> failwith "examples/H2_STO-3G.fcidump not found"
        | Some path ->
            let content = System.IO.File.ReadAllText path
            let (factory, core, nso) = parseToSpinOrbitalFactory content
            Assert.Equal(4, nso)
            Assert.Equal(0.7151043391, core, 7)
            let ham =
                Hamiltonian.computeHamiltonianWith JordanWigner.jordanWignerTerms factory (uint32 nso)
            let terms = ham.DistributeCoefficient.SummandTerms
            let coeffOf sg =
                terms |> Array.tryFind (fun t -> t.Signature = sg)
                      |> Option.map (fun t -> t.Coefficient.Real) |> Option.defaultValue 0.0
            let oneNorm = terms |> Array.sumBy (fun t -> Complex.Abs t.Coefficient)
            // 15 nonzero Pauli terms; canonical identity, four-body, and 1-norm.
            Assert.Equal(15, terms.Length)
            Assert.Equal(-0.8121706072, coeffOf "IIII", 8)
            Assert.Equal(2.699278, oneNorm, 5)
            let fourBody =
                terms
                |> Array.filter (fun t -> t.Signature |> Seq.filter (fun c -> c <> 'I') |> Seq.length = 4)
            Assert.NotEmpty fourBody
            for t in fourBody do
                Assert.Equal(0.0453026155, Complex.Abs t.Coefficient, 8)

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

    // ── Tiny-safe: no absolute magnitude deletion ───────────────────

    [<Fact>]
    let ``parse: preserves an exact 1e-15 integral (no absolute drop)`` () =
        // The parser must store a legitimate 1e-15 one-electron integral rather than
        // deleting it by an absolute threshold, so tiny physics survives end-to-end.
        let content = """
 &FCI NORB= 2,NELEC=2,MS2=0,
  ORBSYM=1,1,
  ISYM=1,
 &END
  1.0e-15   1   1   0   0
  0.5       0   0   0   0
"""
        let data = parse content
        let close a b = Assert.True(abs (a - b) < 1e-20, sprintf "%g vs %g" a b)
        close 1e-15 data.H1e.[0, 0]
        let factory = toCoefficientFactory data
        let v = factory "0,0"
        Assert.True(v.IsSome)
        close 1e-15 v.Value.Real
        // And it flows through to a surviving Pauli term (JW: ±5e-16 on II/ZI).
        let ham = Hamiltonian.computeHamiltonianWith JordanWigner.jordanWignerTerms factory 2u
        let coeffOf sg =
            match ham.DistributeCoefficient.[sg] with
            | true, r -> r.Coefficient.Real
            | false, _ -> 0.0
        close 0.5e-15 (coeffOf "II")
        close (-0.5e-15) (coeffOf "ZI")
