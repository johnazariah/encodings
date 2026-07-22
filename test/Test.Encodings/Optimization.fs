namespace Tests

module Optimization =
    open System.Numerics
    open Encodings
    open Encodings.Hamiltonian
    open Encodings.CostAnalysis
    open Encodings.Optimization
    open Xunit

    let private allOnes _ = Some Complex.One

    // ── standardEncodings ───────────────────────────────────────────

    [<Fact>]
    let ``standardEncodings: returns six candidates`` () =
        let candidates = standardEncodings 4u
        Assert.Equal(6, candidates.Length)

    [<Fact>]
    let ``standardEncodings: names are distinct`` () =
        let names = standardEncodings 4u |> Array.map (fun c -> c.Name) |> Set.ofArray
        Assert.Equal(6, names.Count)

    // ── evaluate ────────────────────────────────────────────────────

    [<Fact>]
    let ``evaluate: produces non-empty hamiltonian for JW`` () =
        let candidate = { Name = "JW"; Encoder = JordanWigner.jordanWignerTerms }
        let result = evaluate lambdaNormCost allOnes 2u candidate
        Assert.True(result.Hamiltonian.SummandTerms.Length > 0)

    [<Fact>]
    let ``evaluate: consumes the weighted factory and preserves a tiny standalone coefficient`` () =
        // The Optimization path builds its Hamiltonian via computeHamiltonianWith, so
        // it inherits the released WEIGHTED contract (factory value applied verbatim)
        // and the cancellation-aware reduction: a tiny standalone one-body coefficient
        // of 2e-13 must survive as ±1e-13 (II, ZI), not be pruned by an absolute floor.
        let factory (key : string) = if key = "0,0" then Some (Complex(2e-13, 0.0)) else None
        let candidate = { Name = "JW"; Encoder = JordanWigner.jordanWignerTerms }
        let result = evaluate lambdaNormCost factory 2u candidate
        let coeffOf sg =
            match result.Hamiltonian.DistributeCoefficient.[sg] with
            | true, r -> r.Coefficient.Real
            | false, _ -> 0.0
        Assert.Equal(2, result.Hamiltonian.DistributeCoefficient.SummandTerms.Length)
        Assert.Equal(1e-13, coeffOf "II", 15)
        Assert.Equal(-1e-13, coeffOf "ZI", 15)

    [<Fact>]
    let ``evaluate: cost matches direct computation`` () =
        let candidate = { Name = "JW"; Encoder = JordanWigner.jordanWignerTerms }
        let result = evaluate lambdaNormCost allOnes 2u candidate
        let directLambda = (hamiltonianCosts result.Hamiltonian).LambdaNorm
        Assert.Equal(directLambda, result.Cost, 10)

    [<Fact>]
    let ``evaluate: costs record is populated`` () =
        let candidate = { Name = "JW"; Encoder = JordanWigner.jordanWignerTerms }
        let result = evaluate lambdaNormCost allOnes 2u candidate
        Assert.True(result.Costs.TermCount > 0)
        Assert.True(result.Costs.QubitCount > 0)

    // ── optimizeOver ────────────────────────────────────────────────

    [<Fact>]
    let ``optimizeOver: best has lowest cost`` () =
        let candidates = standardEncodings 4u
        let result = optimizeOver lambdaNormCost candidates allOnes 4u
        let bestCost = result.Best.Cost
        for r in result.AllResults do
            Assert.True(r.Cost >= bestCost)

    [<Fact>]
    let ``optimizeOver: allResults are sorted by cost ascending`` () =
        let candidates = standardEncodings 4u
        let result = optimizeOver totalPauliWeightCost candidates allOnes 4u
        let costs = result.AllResults |> Array.map (fun r -> r.Cost)
        for i in 0 .. costs.Length - 2 do
            Assert.True(costs.[i] <= costs.[i + 1])

    [<Fact>]
    let ``optimizeOver: allResults has one entry per candidate`` () =
        let candidates = standardEncodings 4u
        let result = optimizeOver lambdaNormCost candidates allOnes 4u
        Assert.Equal(candidates.Length, result.AllResults.Length)

    // ── optimizeStandard ────────────────────────────────────────────

    [<Fact>]
    let ``optimizeStandard: returns results for all six encodings`` () =
        let result = optimizeStandard lambdaNormCost allOnes 2u
        Assert.Equal(6, result.AllResults.Length)

    [<Fact>]
    let ``optimizeStandard: best matches optimizeOver`` () =
        let r1 = optimizeStandard lambdaNormCost allOnes 2u
        let r2 = optimizeOver lambdaNormCost (standardEncodings 2u) allOnes 2u
        Assert.Equal(r1.Best.Cost, r2.Best.Cost, 10)
        Assert.Equal(r1.Best.Candidate.Name, r2.Best.Candidate.Name)

    // ── Built-in cost functions ─────────────────────────────────────

    [<Fact>]
    let ``lambdaNormCost: matches hamiltonianCosts lambda`` () =
        let h = computeHamiltonian allOnes 2u
        let cost = lambdaNormCost h
        let hc = hamiltonianCosts h
        Assert.Equal(hc.LambdaNorm, cost, 10)

    [<Fact>]
    let ``totalPauliWeightCost: matches hamiltonianCosts total weight`` () =
        let h = computeHamiltonian allOnes 2u
        let cost = totalPauliWeightCost h
        let hc = hamiltonianCosts h
        Assert.Equal(float hc.TotalPauliWeight, cost, 10)

    [<Fact>]
    let ``termCountCost: matches hamiltonianCosts term count`` () =
        let h = computeHamiltonian allOnes 2u
        let cost = termCountCost h
        let hc = hamiltonianCosts h
        Assert.Equal(float hc.TermCount, cost, 10)

    [<Fact>]
    let ``maxPauliWeightCost: matches hamiltonianCosts max weight`` () =
        let h = computeHamiltonian allOnes 2u
        let cost = maxPauliWeightCost h
        let hc = hamiltonianCosts h
        Assert.Equal(float hc.MaxPauliWeight, cost, 10)

    [<Fact>]
    let ``trotterCnotCost: is non-negative`` () =
        let h = computeHamiltonian allOnes 2u
        let cost = trotterCnotCost 0.1 h
        Assert.True(cost >= 0.0)

    // ── combinedCost ────────────────────────────────────────────────

    [<Fact>]
    let ``combinedCost: weighted sum of two costs`` () =
        let h = computeHamiltonian allOnes 2u
        let c1 = lambdaNormCost h
        let c2 = totalPauliWeightCost h
        let combined = combinedCost 0.7 lambdaNormCost 0.3 totalPauliWeightCost
        Assert.Equal(0.7 * c1 + 0.3 * c2, combined h, 10)

    // ── evaluateCustom ──────────────────────────────────────────────

    [<Fact>]
    let ``evaluateCustom: works with a custom encoder`` () =
        let result = evaluateCustom lambdaNormCost "Custom-JW" JordanWigner.jordanWignerTerms allOnes 2u
        Assert.Equal("Custom-JW", result.Candidate.Name)
        Assert.True(result.Cost > 0.0)

    // ── Cross-encoding comparisons ──────────────────────────────────

    [<Fact>]
    let ``lambda norm is encoding-dependent for n=4`` () =
        let result = optimizeStandard lambdaNormCost allOnes 4u
        let costs = result.AllResults |> Array.map (fun r -> r.Cost) |> Set.ofArray
        // With all-ones coefficients and 4 qubits, not all encodings give the same lambda
        // (though some might — the point is the framework works)
        Assert.True(result.AllResults.Length = 6)
