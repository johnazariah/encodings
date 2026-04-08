namespace Tests

module CostAnalysis =
    open System.Numerics
    open Encodings
    open Encodings.CostAnalysis
    open Xunit

    let private prs (terms : (string * Complex) list) =
        terms
        |> List.map (fun (ops, coeff) -> PauliRegister(ops, coeff))
        |> List.toArray
        |> PauliRegisterSequence

    let private c x = Complex(x, 0.0)

    // ── hamiltonianCosts ────────────────────────────────────────────

    [<Fact>]
    let ``hamiltonianCosts: term count is correct`` () =
        let h = prs [ ("ZI", c 1.0); ("IZ", c 0.5); ("XX", c -0.25) ]
        let costs = hamiltonianCosts h
        Assert.Equal(3, costs.TermCount)

    [<Fact>]
    let ``hamiltonianCosts: qubit count is correct`` () =
        let h = prs [ ("ZII", c 1.0); ("IZI", c 1.0) ]
        let costs = hamiltonianCosts h
        Assert.Equal(3, costs.QubitCount)

    [<Fact>]
    let ``hamiltonianCosts: lambda norm is sum of magnitudes`` () =
        let h = prs [ ("ZI", c 1.0); ("IZ", c -0.5); ("XX", c 0.25) ]
        let costs = hamiltonianCosts h
        Assert.Equal(1.75, costs.LambdaNorm, 10)

    [<Fact>]
    let ``hamiltonianCosts: pauli weight stats are correct`` () =
        // ZI has weight 1, IZ has weight 1, XX has weight 2
        let h = prs [ ("ZI", c 1.0); ("IZ", c 1.0); ("XX", c 1.0) ]
        let costs = hamiltonianCosts h
        Assert.Equal(2, costs.MaxPauliWeight)
        Assert.Equal(4, costs.TotalPauliWeight)

    [<Fact>]
    let ``hamiltonianCosts: mean weight is correct`` () =
        // Weights: 1, 1, 2 → mean = 4/3
        let h = prs [ ("ZI", c 1.0); ("IZ", c 1.0); ("XX", c 1.0) ]
        let costs = hamiltonianCosts h
        Assert.Equal(4.0 / 3.0, costs.MeanPauliWeight, 10)

    [<Fact>]
    let ``hamiltonianCosts: identity coefficient extracted`` () =
        let h = prs [ ("II", c 3.5); ("ZI", c 1.0) ]
        let costs = hamiltonianCosts h
        Assert.Equal(3.5, costs.IdentityCoeff, 10)

    [<Fact>]
    let ``hamiltonianCosts: identity coefficient is zero when absent`` () =
        let h = prs [ ("ZI", c 1.0); ("XX", c 0.5) ]
        let costs = hamiltonianCosts h
        Assert.Equal(0.0, costs.IdentityCoeff, 10)

    [<Fact>]
    let ``hamiltonianCosts: empty hamiltonian has zero costs`` () =
        let h = PauliRegisterSequence()
        let costs = hamiltonianCosts h
        Assert.Equal(0, costs.TermCount)
        Assert.Equal(0.0, costs.LambdaNorm, 10)
        Assert.Equal(0, costs.MaxPauliWeight)

    // ── pauliWeight ─────────────────────────────────────────────────

    [<Fact>]
    let ``pauliWeight: all-identity has weight zero`` () =
        let r = PauliRegister("III", Complex.One)
        Assert.Equal(0, pauliWeight r)

    [<Fact>]
    let ``pauliWeight: counts non-identity positions`` () =
        let r = PauliRegister("XIZI", Complex.One)
        Assert.Equal(2, pauliWeight r)

    [<Fact>]
    let ``pauliWeight: all non-identity gives full weight`` () =
        let r = PauliRegister("XYZ", Complex.One)
        Assert.Equal(3, pauliWeight r)

    // ── qubitizationCosts ───────────────────────────────────────────

    [<Fact>]
    let ``qubitizationCosts: lambda matches hamiltonianCosts`` () =
        let h = prs [ ("ZI", c 1.0); ("IZ", c 0.5); ("XX", c -0.25) ]
        let qc = qubitizationCosts h
        let hc = hamiltonianCosts h
        Assert.Equal(hc.LambdaNorm, qc.Lambda, 10)

    [<Fact>]
    let ``qubitizationCosts: distinct unitaries equals term count`` () =
        let h = prs [ ("ZI", c 1.0); ("IZ", c 0.5); ("XX", c -0.25) ]
        let qc = qubitizationCosts h
        Assert.Equal(3, qc.DistinctUnitaries)

    [<Fact>]
    let ``qubitizationCosts: select ancillas is ceil log2 of term count`` () =
        let h = prs [ ("ZI", c 1.0); ("IZ", c 0.5); ("XX", c -0.25) ]
        let qc = qubitizationCosts h
        Assert.Equal(2, qc.SelectAncillas) // ceil(log2(3)) = 2

    [<Fact>]
    let ``qubitizationCosts: total qubits = system + ancilla`` () =
        let h = prs [ ("ZI", c 1.0); ("IZ", c 0.5); ("XX", c -0.25) ]
        let qc = qubitizationCosts h
        Assert.Equal(qc.SystemQubits + qc.SelectAncillas, qc.TotalQubits)

    [<Fact>]
    let ``qubitizationCosts: single term needs zero ancillas`` () =
        let h = prs [ ("ZZ", c 2.0) ]
        let qc = qubitizationCosts h
        Assert.Equal(0, qc.SelectAncillas)

    // ── qubitizationQueries ─────────────────────────────────────────

    [<Fact>]
    let ``qubitizationQueries: scales as lambda * t / epsilon`` () =
        let h = prs [ ("ZI", c 1.0); ("IZ", c 1.0) ]
        let qc = qubitizationCosts h
        // lambda = 2.0, t = 10.0, eps = 0.01 → ceil(2.0 * 10.0 / 0.01) = 2000
        let queries = qubitizationQueries qc 10.0 0.01
        Assert.Equal(2000, queries)

    [<Fact>]
    let ``qubitizationQueries: rounds up`` () =
        let h = prs [ ("ZI", c 1.0) ]
        let qc = qubitizationCosts h
        // lambda = 1.0, t = 1.0, eps = 0.3 → ceil(1/0.3) = ceil(3.33) = 4
        let queries = qubitizationQueries qc 1.0 0.3
        Assert.Equal(4, queries)

    // ── compareCosts ────────────────────────────────────────────────

    [<Fact>]
    let ``compareCosts: sorts by lambda ascending`` () =
        let h1 = prs [ ("ZI", c 3.0) ]               // lambda = 3.0
        let h2 = prs [ ("ZI", c 1.0); ("IZ", c 0.5) ] // lambda = 1.5
        let results = compareCosts [| ("A", h1); ("B", h2) |]
        Assert.Equal("B", fst results.[0]) // lower lambda first
        Assert.Equal("A", fst results.[1])

    // ── compareQubitizationCosts ─────────────────────────────────────

    [<Fact>]
    let ``compareQubitizationCosts: sorts by lambda ascending`` () =
        let h1 = prs [ ("ZI", c 3.0) ]
        let h2 = prs [ ("ZI", c 1.0) ]
        let results = compareQubitizationCosts [| ("High", h1); ("Low", h2) |]
        Assert.Equal("Low", fst results.[0])
        Assert.Equal("High", fst results.[1])
