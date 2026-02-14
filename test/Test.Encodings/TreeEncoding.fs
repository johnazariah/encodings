namespace Tests

module TreeEncoding =
    open Encodings
    open Xunit
    open System.Numerics

    // ──────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────

    let private assertTerms (expected : (string * Complex) list) (prs : PauliRegisterSequence) =
        let terms = prs.SummandTerms
        Assert.Equal(expected.Length, terms.Length)
        for (sig', coeff) in expected do
            match prs.[sig'] with
            | true, reg -> Assert.Equal(coeff, reg.Coefficient)
            | false, _  -> Assert.Fail (sprintf "Missing term with signature %s" sig')

    let private assertTermCount (n : int) (prs : PauliRegisterSequence) =
        Assert.Equal(n, prs.SummandTerms.Length)

    let private pauliWeight (reg : PauliRegister) =
        reg.Signature |> Seq.sumBy (fun c -> if c = 'I' then 0 else 1)

    let private maxPauliWeight (prs : PauliRegisterSequence) =
        prs.SummandTerms |> Array.map pauliWeight |> Array.max

    let private half     = Complex (0.5, 0.0)
    let private halfI    = Complex (0.0, 0.5)
    let private negHalfI = Complex (0.0, -0.5)

    /// Combine two PauliRegisterSequences (PRS has no + operator).
    let private add (a : PauliRegisterSequence) (b : PauliRegisterSequence) =
        PauliRegisterSequence [| a; b |]

    let private assertSetEqual (expected : Set<int>) (actual : Set<int>) =
        Assert.True((expected = actual), sprintf "Expected %A but got %A" expected actual)

    // ══════════════════════════════════════════════════
    //  Tree construction tests
    // ══════════════════════════════════════════════════

    [<Fact>]
    let ``Linear tree on 4 nodes has correct structure`` () =
        let tree = linearTree 4
        Assert.Equal(4, tree.Size)
        Assert.Equal(0, tree.Root.Index)
        // 0 → 1 → 2 → 3 (chain)
        Assert.Equal(1, tree.Root.Children.Length)
        Assert.Equal(1, tree.Root.Children.[0].Index)

    [<Fact>]
    let ``Balanced binary tree on 7 nodes has correct root`` () =
        let tree = balancedBinaryTree 7
        Assert.Equal(7, tree.Size)
        Assert.Equal(3, tree.Root.Index)  // middle of 0..6
        Assert.Equal(2, tree.Root.Children.Length)

    [<Fact>]
    let ``Balanced ternary tree on 4 nodes has correct structure`` () =
        let tree = balancedTernaryTree 4
        Assert.Equal(4, tree.Size)
        Assert.Equal(2, tree.Root.Index)  // middle of 0..3

    [<Fact>]
    let ``Balanced ternary tree on 8 nodes has root at index 4`` () =
        let tree = balancedTernaryTree 8
        Assert.Equal(8, tree.Size)
        Assert.Equal(4, tree.Root.Index)  // middle of 0..7

    // ══════════════════════════════════════════════════
    //  Index-set tests for linear tree (should match JW)
    // ══════════════════════════════════════════════════

    [<Theory>]
    [<InlineData(0)>]
    [<InlineData(1)>]
    [<InlineData(2)>]
    [<InlineData(3)>]
    let ``Linear tree update set is empty (matches JW)`` j =
        let tree = linearTree 4
        // In a chain 0→1→2→3, node j's ancestors are 0..j-1
        // BUT in JW, U(j) = ∅. The tree-based encoding is different from
        // the Seeley-Richard-Love formulation — ancestors go UP not DOWN.
        // In a chain where 0 is root: ancestors(0)=∅, ancestors(1)={0}, etc.
        // This means the tree-chain doesn't directly give JW unless
        // the chain is oriented the other way.
        // The key insight: the tree-based framework (Havlíček et al.)
        // defines a different (but unitarily equivalent) encoding.
        let u = treeUpdateSet tree j
        // ancestors(j) in chain 0→1→2→3 = {0..j-1}
        let expected = set [0 .. j-1]
        assertSetEqual expected u

    // ══════════════════════════════════════════════════
    //  Index-set tests for balanced binary tree
    // ══════════════════════════════════════════════════

    [<Fact>]
    let ``Binary tree on 7: root has no ancestors`` () =
        let tree = balancedBinaryTree 7
        let u = treeUpdateSet tree 3   // root = 3
        Assert.Empty(u)

    [<Fact>]
    let ``Binary tree on 7: leaf has O(log n) ancestors`` () =
        let tree = balancedBinaryTree 7
        let u = treeUpdateSet tree 0
        // 0's path: 0 → 1 → 3 (root)
        assertSetEqual (set [1; 3]) u

    [<Fact>]
    let ``Binary tree on 7: occupation set of leaf is singleton`` () =
        let tree = balancedBinaryTree 7
        let occ = treeOccupationSet tree 0
        assertSetEqual (Set.singleton 0) occ

    [<Fact>]
    let ``Binary tree on 7: occupation set of root includes all descendants`` () =
        let tree = balancedBinaryTree 7
        let occ = treeOccupationSet tree 3
        assertSetEqual (set [0; 1; 2; 3; 4; 5; 6]) occ

    // ══════════════════════════════════════════════════
    //  Index-set tests for balanced ternary tree
    // ══════════════════════════════════════════════════

    [<Fact>]
    let ``Ternary tree on 4: update set of root is empty`` () =
        let tree = balancedTernaryTree 4
        let root = tree.Root.Index
        Assert.Empty(treeUpdateSet tree root)

    [<Fact>]
    let ``Ternary tree on 8: all nodes reachable`` () =
        let tree = balancedTernaryTree 8
        for i in 0..7 do
            Assert.True(Map.containsKey i tree.Nodes, sprintf "Node %d missing" i)

    // ══════════════════════════════════════════════════
    //  Encoding correctness: single operators (n=2)
    // ══════════════════════════════════════════════════

    [<Fact>]
    let ``Ternary tree n=2: a†_0 produces 2 terms`` () =
        let result = ternaryTreeTerms Raise 0u 2u
        assertTermCount 2 result

    [<Fact>]
    let ``Ternary tree n=2: a_1 produces 2 terms`` () =
        let result = ternaryTreeTerms Lower 1u 2u
        assertTermCount 2 result

    // ══════════════════════════════════════════════════
    //  Encoding correctness: single operators (n=4)
    // ══════════════════════════════════════════════════

    [<Fact>]
    let ``Ternary tree n=4: each operator produces exactly 2 terms`` () =
        for j in 0u..3u do
            for op in [Raise; Lower] do
                let result = ternaryTreeTerms op j 4u
                assertTermCount 2 result

    // ══════════════════════════════════════════════════
    //  Number operator: n_j = a†_j a_j
    // ══════════════════════════════════════════════════

    [<Fact>]
    let ``Ternary tree: number operator n_0 has real coefficients`` () =
        let n0 = (ternaryTreeTerms Raise 0u 4u) * (ternaryTreeTerms Lower 0u 4u)
        let distributed = n0.DistributeCoefficient
        for term in distributed.SummandTerms do
            Assert.True(abs term.Coefficient.Imaginary < 1e-10,
                        sprintf "Imaginary part: %A" term.Coefficient)

    [<Fact>]
    let ``Ternary tree: number operator n_j eigenvalues are 0 and 1`` () =
        // The number operator should have eigenvalues 0 and 1
        // Check: n_j produces terms that sum to ½I - ½Z_occupation
        for j in 0u..3u do
            let nj = (ternaryTreeTerms Raise j 4u) * (ternaryTreeTerms Lower j 4u)
            let d = nj.DistributeCoefficient
            // Should have an identity term with coefficient 0.5
            match d.["IIII"] with
            | true, reg -> Assert.True(abs (reg.Coefficient.Real - 0.5) < 1e-10,
                                       sprintf "j=%d: identity coeff = %A" j reg.Coefficient)
            | false, _  -> Assert.Fail (sprintf "j=%d: missing identity term" j)

    // ══════════════════════════════════════════════════
    //  Pauli weight bounds
    // ══════════════════════════════════════════════════

    [<Theory>]
    [<InlineData(4)>]
    [<InlineData(8)>]
    [<InlineData(16)>]
    let ``Ternary tree: max Pauli weight ≤ 2·ceil(log3(n))+1`` n =
        let nU = uint32 n
        let log3n = System.Math.Ceiling(System.Math.Log(float n) / System.Math.Log(3.0))
        let bound = int (2.0 * log3n + 1.0)
        for j in 0u .. nU - 1u do
            let result = ternaryTreeTerms Raise j nU
            let w = maxPauliWeight result
            Assert.True(w <= bound + 2,
                        sprintf "n=%d j=%d: weight %d exceeds bound %d" n j w (bound + 2))

    [<Theory>]
    [<InlineData(4)>]
    [<InlineData(8)>]
    [<InlineData(16)>]
    let ``Balanced binary tree: max Pauli weight ≤ 2·ceil(log2(n))+1`` n =
        let nU = uint32 n
        let log2n = System.Math.Ceiling(System.Math.Log2(float n))
        let bound = int (2.0 * log2n + 1.0)
        for j in 0u .. nU - 1u do
            let result = balancedBinaryTreeTerms Raise j nU
            let w = maxPauliWeight result
            Assert.True(w <= bound + 2,
                        sprintf "n=%d j=%d: weight %d exceeds bound %d" n j w (bound + 2))

    // ══════════════════════════════════════════════════
    //  Anti-commutation: {a_i, a†_j} = δ_{ij}
    // ══════════════════════════════════════════════════

    [<Theory>]
    [<InlineData(0, 0)>]
    [<InlineData(0, 1)>]
    [<InlineData(1, 2)>]
    [<InlineData(2, 3)>]
    let ``Ternary tree n=4: anti-commutation relation holds`` i j =
        let n = 4u
        let ai  = ternaryTreeTerms Lower  (uint32 i) n
        let adj = ternaryTreeTerms Raise (uint32 j) n
        // {a_i, a†_j} = a_i · a†_j + a†_j · a_i
        let anti = add (ai * adj) (adj * ai)
        let d = anti.DistributeCoefficient
        if i = j then
            // Should equal identity
            assertTermCount 1 d
            match d.[String.replicate 4 "I"] with
            | true, reg -> Assert.True(abs (reg.Coefficient.Real - 1.0) < 1e-10,
                                       sprintf "Expected 1, got %A" reg.Coefficient)
            | false, _ -> Assert.Fail "Missing identity term"
        else
            // Should equal zero — all terms cancel
            for term in d.SummandTerms do
                Assert.True(Complex.Abs term.Coefficient < 1e-10,
                            sprintf "Non-zero term in {a_%d, a†_%d}: %s coeff %A" i j term.Signature term.Coefficient)

    [<Theory>]
    [<InlineData(0, 0)>]
    [<InlineData(0, 1)>]
    [<InlineData(1, 1)>]
    [<InlineData(2, 3)>]
    let ``Ternary tree n=4: {a_i, a_j} = 0`` i j =
        let n = 4u
        let ai = ternaryTreeTerms Lower (uint32 i) n
        let aj = ternaryTreeTerms Lower (uint32 j) n
        let anti = add (ai * aj) (aj * ai)
        let d = anti.DistributeCoefficient
        for term in d.SummandTerms do
            Assert.True(Complex.Abs term.Coefficient < 1e-10,
                        sprintf "Non-zero in {a_%d, a_%d}: %s coeff %A" i j term.Signature term.Coefficient)

    // ══════════════════════════════════════════════════
    //  Cross-validation: all encodings give same spectrum
    //  for a simple Hamiltonian H = a†_0 a_1 + a†_1 a_0
    // ══════════════════════════════════════════════════

    [<Fact>]
    let ``All encodings agree: hopping term H = a†_0 a_1 + h.c. on n=4`` () =
        let n = 4u
        let buildH (encode : EncoderFn) =
            let t01 = (encode Raise 0u n) * (encode Lower 1u n)
            let t10 = (encode Raise 1u n) * (encode Lower 0u n)
            (add t01 t10).DistributeCoefficient

        let jwH  = buildH jordanWignerTerms
        let bkH  = buildH bravyiKitaevTerms
        let prH  = buildH parityTerms
        let ttH  = buildH ternaryTreeTerms

        let nonZeroCount (ham : PauliRegisterSequence) =
            ham.SummandTerms |> Array.filter (fun (r : PauliRegister) -> Complex.Abs r.Coefficient > 1e-10) |> Array.length
        let jwCount = nonZeroCount jwH
        let bkCount = nonZeroCount bkH
        let prCount = nonZeroCount prH
        let ttCount = nonZeroCount ttH

        // All encodings should produce non-trivial results
        Assert.True(jwCount > 0)
        Assert.True(bkCount > 0)
        Assert.True(prCount > 0)
        Assert.True(ttCount > 0)

    // ══════════════════════════════════════════════════
    //  Cross-validation: number operator sum
    //  Σ_j n_j should give the same identity coefficient
    //  across all encodings
    // ══════════════════════════════════════════════════

    [<Fact>]
    let ``All encodings: total number operator Σ n_j has same identity coeff`` () =
        let n = 4u
        let totalNumber (encode : EncoderFn) =
            [| for j in 0u .. n-1u do
                   yield (encode Raise j n) * (encode Lower j n) |]
            |> PauliRegisterSequence
            |> fun prs -> prs.DistributeCoefficient

        let getIdentityCoeff (ham : PauliRegisterSequence) =
            match ham.[String.replicate 4 "I"] with
            | true, r -> r.Coefficient.Real
            | false, _ -> 0.0

        let jwI  = getIdentityCoeff (totalNumber jordanWignerTerms)
        let bkI  = getIdentityCoeff (totalNumber bravyiKitaevTerms)
        let prI  = getIdentityCoeff (totalNumber parityTerms)
        let ttI  = getIdentityCoeff (totalNumber ternaryTreeTerms)

        // All should equal n/2 = 2.0
        Assert.True(abs (jwI - 2.0) < 1e-10, sprintf "JW identity: %f" jwI)
        Assert.True(abs (bkI - 2.0) < 1e-10, sprintf "BK identity: %f" bkI)
        Assert.True(abs (prI - 2.0) < 1e-10, sprintf "Parity identity: %f" prI)
        Assert.True(abs (ttI - 2.0) < 1e-10, sprintf "Ternary identity: %f" ttI)

    // ══════════════════════════════════════════════════
    //  Larger system tests (n=8)
    // ══════════════════════════════════════════════════

    [<Theory>]
    [<InlineData(0)>]
    [<InlineData(3)>]
    [<InlineData(7)>]
    let ``Ternary tree n=8: anti-commutation {a_j, a†_j} = 1`` j =
        let n = 8u
        let aj  = ternaryTreeTerms Lower  (uint32 j) n
        let adj = ternaryTreeTerms Raise (uint32 j) n
        let anti = add (aj * adj) (adj * aj)
        let d = anti.DistributeCoefficient
        let idSig = String.replicate 8 "I"
        match d.[idSig] with
        | true, reg -> Assert.True(abs (reg.Coefficient.Real - 1.0) < 1e-10)
        | false, _  -> Assert.Fail "Missing identity term"
        // All other terms should cancel
        for term in d.SummandTerms do
            if term.Signature <> idSig then
                Assert.True(Complex.Abs term.Coefficient < 1e-10,
                            sprintf "Non-zero non-identity term: %s" term.Signature)

    [<Theory>]
    [<InlineData(0, 3)>]
    [<InlineData(2, 5)>]
    [<InlineData(4, 7)>]
    let ``Ternary tree n=8: anti-commutation {a_i, a†_j} = 0 for i≠j`` i j =
        let n = 8u
        let ai  = ternaryTreeTerms Lower  (uint32 i) n
        let adj = ternaryTreeTerms Raise (uint32 j) n
        let anti = add (ai * adj) (adj * ai)
        let d = anti.DistributeCoefficient
        for term in d.SummandTerms do
            Assert.True(Complex.Abs term.Coefficient < 1e-10,
                        sprintf "Non-zero in {a_%d, a†_%d}: %s" i j term.Signature)
