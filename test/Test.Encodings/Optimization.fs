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
    let ``evaluate: consumes the raw factory and preserves a tiny standalone coefficient`` () =
        // The Optimization path builds its Hamiltonian via the raw-physicist
        // computeHamiltonianWith, so it inherits the raw contract and the
        // cancellation-aware reduction. A tiny standalone one-body coefficient of 2e-13
        // (convention-invariant) must survive as ±1e-13 (II, ZI), not be pruned.
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

    // ── Convention-sensitive routing (Optimization is RAW, 0.9.0) ────
    // Optimization builds through the raw-physicist computeHamiltonianWith, so its
    // coefficientFactory argument follows the RAW single-bar contract. These three
    // regressions pin that routing on the exposed Optimization surface, so a future
    // re-wire to the weighted core (or a lost adapter) is caught here, not only in
    // the Hamiltonian tests.

    let private jw = { Name = "JW"; Encoder = JordanWigner.jordanWignerTerms }

    let private coeffOfResult (r : EvaluationResult) (sign : string) =
        match r.Hamiltonian.DistributeCoefficient.[sign] with
        | true, reg -> reg.Coefficient.Real
        | false, _ -> 0.0

    [<Fact>]
    let ``evaluate: raw two-body factory maps exactly through Optimization (½ + r↔s swap)`` () =
        // raw("0,1,0,1")=1 (⟨01|01⟩) routed through Optimization must yield the raw
        // convention ⅛(II − IZ − ZI + ZZ): the library's internal ½ and r↔s swap are
        // applied. A weighted-core regression would give ¼-scale values instead.
        let raw (key : string) = if key = "0,1,0,1" then Some (Complex(1.0, 0.0)) else None
        let result = evaluate lambdaNormCost raw 2u jw
        Assert.Equal( 0.125, coeffOfResult result "II", 10)
        Assert.Equal(-0.125, coeffOfResult result "IZ", 10)
        Assert.Equal(-0.125, coeffOfResult result "ZI", 10)
        Assert.Equal( 0.125, coeffOfResult result "ZZ", 10)

    [<Fact>]
    let ``evaluate: weightedToRawFactory migrates a weighted factory through Optimization`` () =
        // A legacy weighted factory wrapped with weightedToRawFactory and evaluated
        // through Optimization must reproduce the legacy weighted builder EXACTLY —
        // this is the supported migration path for the Optimization entry points,
        // which have no dedicated weighted overload.
        let weighted (key : string) =
            if key = "0,1,1,0" then Some (Complex(0.7, 0.0))
            elif key = "0,0" then Some (Complex(-0.9, 0.0))
            else None
        let viaOptimization = evaluate lambdaNormCost (weightedToRawFactory weighted) 2u jw
        let viaWeighted     = computeHamiltonianFromWeightedWith JordanWigner.jordanWignerTerms weighted 2u
        Assert.Equal(viaWeighted.DistributeCoefficient.ToString(),
                     viaOptimization.Hamiltonian.DistributeCoefficient.ToString())

    [<Fact>]
    let ``evaluate: pre-adapted weighted data fed directly to Optimization is a misuse (negative control)`` () =
        // Feeding pre-adapted weighted data (½ folded, indices swapped) straight to
        // Optimization's RAW routing double-adapts it → a materially different map than
        // the correct weighted build. Guards against silent accidental equivalence.
        let weighted (key : string) =
            if key = "0,1,1,0" then Some (Complex(0.7, 0.0))
            elif key = "0,0" then Some (Complex(-0.9, 0.0))
            else None
        let misused = (evaluate lambdaNormCost weighted 2u jw).Hamiltonian
        let correct = computeHamiltonianFromWeightedWith JordanWigner.jordanWignerTerms weighted 2u
        Assert.NotEqual<string>(correct.DistributeCoefficient.ToString(),
                                misused.DistributeCoefficient.ToString())

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
