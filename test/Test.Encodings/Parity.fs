namespace Tests

module Parity =
    open Encodings
    open Xunit
    open System.Numerics

    // ──────────────────────────────────────────────────
    //  Helpers — same order-independent assertions as BK tests
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

    let private assertCoeff (sig' : string) (expected : Complex) (prs : PauliRegisterSequence) =
        match prs.[sig'] with
        | true, reg -> Assert.Equal(expected, reg.Coefficient)
        | false, _  -> Assert.Fail (sprintf "Missing term with signature %s" sig')

    let private pauliWeight (reg : PauliRegister) =
        reg.Signature |> Seq.sumBy (fun c -> if c = 'I' then 0 else 1)

    let private maxPauliWeight (prs : PauliRegisterSequence) =
        prs.SummandTerms |> Array.map pauliWeight |> Array.max

    let private half     = Complex (0.5, 0.0)
    let private halfI    = Complex (0.0, 0.5)
    let private negHalfI = Complex (0.0, -0.5)
    let private negHalf  = Complex (-0.5, 0.0)

    // ──────────────────────────────────────────────────
    //  Parity index-set verification
    // ──────────────────────────────────────────────────

    //  Parity scheme:
    //    U(j, n) = {j+1 … n−1}
    //    P(j)    = {j−1}  if j > 0, else ∅
    //    Occ(j)  = {j−1, j} if j > 0, else {j}

    [<Theory>]
    [<InlineData(0, 4)>]
    [<InlineData(1, 4)>]
    [<InlineData(3, 4)>]
    let ``Parity update set is {j+1..n-1}`` (j : int, n : int) =
        let expected = set [ j + 1 .. n - 1 ]
        Assert.Equal<Set<int>>(expected, parityScheme.Update j n)

    [<Fact>]
    let ``Parity parity set for j=0 is empty`` () =
        Assert.Equal<Set<int>>(Set.empty, parityScheme.Parity 0)

    [<Fact>]
    let ``Parity parity set for j=3 is {2}`` () =
        Assert.Equal<Set<int>>(set [2], parityScheme.Parity 3)

    [<Fact>]
    let ``Parity occupation set for j=0 is {0}`` () =
        Assert.Equal<Set<int>>(set [0], parityScheme.Occupation 0)

    [<Fact>]
    let ``Parity occupation set for j=3 is {2,3}`` () =
        Assert.Equal<Set<int>>(set [2; 3], parityScheme.Occupation 3)

    // ──────────────────────────────────────────────────
    //  n = 2  hand-verified cases
    // ──────────────────────────────────────────────────

    //  j = 0, n = 2:  U = {1},  P = {},  Occ = {0}
    //    c: X on {0,1} → XX      d: Y on 0, X on {1} → YX
    //    (same as BK for n=2)

    [<Fact>]
    let ``Parity n=2 j=0 creation`` () =
        parityTerms Raise 0u 2u |> assertTerms [ "XX", half; "YX", negHalfI ]

    [<Fact>]
    let ``Parity n=2 j=1 creation`` () =
        //  j = 1, n = 2:  U = {},  P = {0},  Occ = {0,1}
        //    c: X on {1}, Z on {0} → ZX       d: Y on 1 → IY
        parityTerms Raise 1u 2u |> assertTerms [ "ZX", half; "IY", negHalfI ]

    // ──────────────────────────────────────────────────
    //  n = 4  creation operator signatures
    // ──────────────────────────────────────────────────

    //  j = 0:  U = {1,2,3},  P = {},  Occ = {0}
    //    c: X on {0,1,2,3} → XXXX
    //    d: Y on 0, X on {1,2,3} → YXXX

    //  j = 1:  U = {2,3},  P = {0},  Occ = {0,1}
    //    c: X on {1,2,3}, Z on {0} → ZXXX
    //    P⊕Occ = {1}, \{1} = {}
    //    d: Y on 1, X on {2,3} → IYXX

    //  j = 2:  U = {3},  P = {1},  Occ = {1,2}
    //    c: X on {2,3}, Z on {1} → IZXX
    //    P⊕Occ = {2}, \{2} = {}
    //    d: Y on 2, X on {3} → IIYX

    //  j = 3:  U = {},  P = {2},  Occ = {2,3}
    //    c: X on {3}, Z on {2} → IIZX
    //    P⊕Occ = {3}, \{3} = {}
    //    d: Y on 3 → IIIY

    [<Theory>]
    [<InlineData(0, "XXXX", "YXXX")>]
    [<InlineData(1, "ZXXX", "IYXX")>]
    [<InlineData(2, "IZXX", "IIYX")>]
    [<InlineData(3, "IIZX", "IIIY")>]
    let ``Parity n=4 creation has correct Majorana signatures`` (j : int, cSig : string, dSig : string) =
        let result = parityTerms Raise (uint32 j) 4u
        result |> assertTermCount 2
        result |> assertCoeff cSig half
        result |> assertCoeff dSig negHalfI

    [<Theory>]
    [<InlineData(0, "XXXX", "YXXX")>]
    [<InlineData(1, "ZXXX", "IYXX")>]
    [<InlineData(2, "IZXX", "IIYX")>]
    [<InlineData(3, "IIZX", "IIIY")>]
    let ``Parity n=4 annihilation has correct Majorana signatures`` (j : int, cSig : string, dSig : string) =
        let result = parityTerms Lower (uint32 j) 4u
        result |> assertTermCount 2
        result |> assertCoeff cSig half
        result |> assertCoeff dSig halfI

    // ──────────────────────────────────────────────────
    //  Parity encoding has O(n) weight (X chain runs to the right)
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``Parity weight for j=0 is n`` () =
        let n = 8u
        let w = maxPauliWeight (parityTerms Raise 0u n)
        Assert.Equal(int n, w)

    [<Fact>]
    let ``Parity weight for j=n-1 is small`` () =
        // The last mode has U = {}, so only 1-2 non-identity operators
        let n = 8u
        let w = maxPauliWeight (parityTerms Raise (n - 1u) n)
        Assert.True(w <= 2)

    // ──────────────────────────────────────────────────
    //  Number operators  n_j = a†_j a_j
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``Parity n=2 number operator j=0`` () =
        let num = (parityTerms Raise 0u 2u) * (parityTerms Lower 0u 2u)
        // Same as BK for n=2: 0.5 II - 0.5 ZI
        num |> assertTerms [ "II", half; "ZI", negHalf ]

    [<Fact>]
    let ``Parity n=2 number operator j=1`` () =
        let num = (parityTerms Raise 1u 2u) * (parityTerms Lower 1u 2u)
        // Same as BK: 0.5 II - 0.5 ZZ
        num |> assertTerms [ "II", half; "ZZ", negHalf ]

    [<Theory>]
    [<InlineData(4, 0)>]
    [<InlineData(4, 1)>]
    [<InlineData(4, 2)>]
    [<InlineData(4, 3)>]
    let ``Parity number operator is two-term`` (n : uint32, j : uint32) =
        let num = (parityTerms Raise j n) * (parityTerms Lower j n)
        num |> assertTermCount 2
        let idSig = System.String ('I', int n)
        num |> assertCoeff idSig half

    // ──────────────────────────────────────────────────
    //  Generic encoding:  JW scheme matches original jordanWignerTerms
    // ──────────────────────────────────────────────────

    [<Theory>]
    [<InlineData(4, 0)>]
    [<InlineData(4, 1)>]
    [<InlineData(4, 2)>]
    [<InlineData(4, 3)>]
    let ``JW scheme matches jordanWignerTerms`` (n : uint32, j : uint32) =
        let original = jordanWignerTerms Raise j n
        let generic  = encodeOperator jordanWignerScheme Raise j n
        // Same number of terms
        Assert.Equal(original.SummandTerms.Length, generic.SummandTerms.Length)
        // Each term matches by signature and coefficient
        for term in original.SummandTerms do
            generic |> assertCoeff term.Signature term.Coefficient

    [<Theory>]
    [<InlineData(4, 0)>]
    [<InlineData(4, 1)>]
    [<InlineData(4, 2)>]
    [<InlineData(4, 3)>]
    let ``BK scheme matches bravyiKitaevTerms`` (n : uint32, j : uint32) =
        let original = bravyiKitaevTerms Raise j n
        let generic  = encodeOperator bravyiKitaevScheme Raise j n
        Assert.Equal(original.SummandTerms.Length, generic.SummandTerms.Length)
        for term in original.SummandTerms do
            generic |> assertCoeff term.Signature term.Coefficient

    // ──────────────────────────────────────────────────
    //  Edge cases
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``Parity out of range returns empty`` () =
        parityTerms Raise 5u 4u |> assertTermCount 0

    [<Fact>]
    let ``Parity Identity returns empty`` () =
        parityTerms Identity 0u 4u |> assertTermCount 0

    // ──────────────────────────────────────────────────
    //  All three number operators have same term count for n=4
    // ──────────────────────────────────────────────────

    [<Theory>]
    [<InlineData(4, 0)>]
    [<InlineData(4, 1)>]
    [<InlineData(4, 2)>]
    [<InlineData(4, 3)>]
    let ``All three encodings produce 2-term number operators`` (n : uint32, j : uint32) =
        let jwNum = (jordanWignerTerms Raise j n)  * (jordanWignerTerms Lower j n)
        let bkNum = (bravyiKitaevTerms Raise j n)  * (bravyiKitaevTerms Lower j n)
        let prNum = (parityTerms Raise j n)        * (parityTerms Lower j n)
        Assert.Equal(2, jwNum.SummandTerms.Length)
        Assert.Equal(2, bkNum.SummandTerms.Length)
        Assert.Equal(2, prNum.SummandTerms.Length)
