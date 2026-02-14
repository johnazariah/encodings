namespace Tests

module BravyiKitaev =
    open Encodings
    open Xunit
    open System.Numerics

    // ──────────────────────────────────────────────────
    //  Helpers — order-independent assertions on Pauli sums
    // ──────────────────────────────────────────────────

    /// Assert that prs contains exactly the given set of (signature, coefficient) pairs.
    let private assertTerms (expected : (string * Complex) list) (prs : PauliRegisterSequence) =
        let terms = prs.SummandTerms
        Assert.Equal(expected.Length, terms.Length)
        for (sig', coeff) in expected do
            match prs.[sig'] with
            | true, reg -> Assert.Equal(coeff, reg.Coefficient)
            | false, _  -> Assert.Fail (sprintf "Missing expected term with signature %s" sig')

    /// Assert that prs contains exactly the given number of terms.
    let private assertTermCount (n : int) (prs : PauliRegisterSequence) =
        Assert.Equal(n, prs.SummandTerms.Length)

    /// Look up a term by signature and assert its coefficient.
    let private assertCoeff (sig' : string) (expected : Complex) (prs : PauliRegisterSequence) =
        match prs.[sig'] with
        | true, reg -> Assert.Equal(expected, reg.Coefficient)
        | false, _  -> Assert.Fail (sprintf "Missing term with signature %s" sig')

    /// Pauli weight = number of non-I operators in a register.
    let private pauliWeight (reg : PauliRegister) =
        reg.Signature |> Seq.sumBy (fun c -> if c = 'I' then 0 else 1)

    let private maxPauliWeight (prs : PauliRegisterSequence) =
        prs.SummandTerms |> Array.map pauliWeight |> Array.max

    let private half      = Complex (0.5, 0.0)
    let private halfI     = Complex (0.0, 0.5)
    let private negHalfI  = Complex (0.0, -0.5)
    let private negHalf   = Complex (-0.5, 0.0)

    // ──────────────────────────────────────────────────
    //  n = 2  hand-verified cases
    // ──────────────────────────────────────────────────

    //  j = 0:  U = {1},  P = {},  Occ = {0}
    //    c : X on {0,1}, Z on {}        → XX,  coeff  0.5
    //    d : Y on 0, X on {1}, Z on {}  → YX,  coeff −0.5i (raise) / +0.5i (lower)

    [<Fact>]
    let ``BK n=2 j=0 creation`` () =
        let result = bravyiKitaevTerms Raise 0u 2u
        result |> assertTerms [ "XX", half; "YX", negHalfI ]

    [<Fact>]
    let ``BK n=2 j=0 annihilation`` () =
        let result = bravyiKitaevTerms Lower 0u 2u
        result |> assertTerms [ "XX", half; "YX", halfI ]

    //  j = 1:  U = {},  P = {0},  Occ = {0,1},  P⊕Occ = {1}, \{1} = {}
    //    c : X on {1}, Z on {0}         → ZX,  coeff  0.5
    //    d : Y on 1, X on {}, Z on {}   → IY,  coeff −0.5i / +0.5i

    [<Fact>]
    let ``BK n=2 j=1 creation`` () =
        let result = bravyiKitaevTerms Raise 1u 2u
        result |> assertTerms [ "ZX", half; "IY", negHalfI ]

    [<Fact>]
    let ``BK n=2 j=1 annihilation`` () =
        let result = bravyiKitaevTerms Lower 1u 2u
        result |> assertTerms [ "ZX", half; "IY", halfI ]

    // ──────────────────────────────────────────────────
    //  n = 8  creation operators — verify both Pauli signatures
    //  and their coefficients, order-independently
    // ──────────────────────────────────────────────────

    [<Theory>]
    [<InlineData(0, "XXIXIIIX", "YXIXIIIX")>]
    [<InlineData(1, "ZXIXIIIX", "IYIXIIIX")>]
    [<InlineData(2, "IZXXIIIX", "IZYXIIIX")>]
    [<InlineData(3, "IZZXIIIX", "IIIYIIIX")>]
    [<InlineData(4, "IIIZXXIX", "IIIZYXIX")>]
    [<InlineData(5, "IIIZZXIX", "IIIZIYIX")>]
    [<InlineData(6, "IIIZIZXX", "IIIZIZYX")>]
    [<InlineData(7, "IIIZIZZX", "IIIIIIIY")>]
    let ``BK n=8 creation has correct c and d Majorana signatures`` (j : int, cSig : string, dSig : string) =
        let result = bravyiKitaevTerms Raise (uint32 j) 8u
        result |> assertTermCount 2
        result |> assertCoeff cSig half
        result |> assertCoeff dSig negHalfI

    [<Theory>]
    [<InlineData(0, "XXIXIIIX", "YXIXIIIX")>]
    [<InlineData(1, "ZXIXIIIX", "IYIXIIIX")>]
    [<InlineData(2, "IZXXIIIX", "IZYXIIIX")>]
    [<InlineData(3, "IZZXIIIX", "IIIYIIIX")>]
    [<InlineData(4, "IIIZXXIX", "IIIZYXIX")>]
    [<InlineData(5, "IIIZZXIX", "IIIZIYIX")>]
    [<InlineData(6, "IIIZIZXX", "IIIZIZYX")>]
    [<InlineData(7, "IIIZIZZX", "IIIIIIIY")>]
    let ``BK n=8 annihilation has correct c and d Majorana signatures`` (j : int, cSig : string, dSig : string) =
        let result = bravyiKitaevTerms Lower (uint32 j) 8u
        result |> assertTermCount 2
        result |> assertCoeff cSig half
        result |> assertCoeff dSig halfI

    // ──────────────────────────────────────────────────
    //  Number operators   n_j = a†_j a_j
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``BK n=2 number operator j=0 is 0.5 II − 0.5 ZI`` () =
        let num = (bravyiKitaevTerms Raise 0u 2u) * (bravyiKitaevTerms Lower 0u 2u)
        num |> assertTerms [ "II", half; "ZI", negHalf ]

    [<Fact>]
    let ``BK n=2 number operator j=1 is 0.5 II − 0.5 ZZ`` () =
        let num = (bravyiKitaevTerms Raise 1u 2u) * (bravyiKitaevTerms Lower 1u 2u)
        num |> assertTerms [ "II", half; "ZZ", negHalf ]

    [<Theory>]
    [<InlineData(8, 0)>]
    [<InlineData(8, 1)>]
    [<InlineData(8, 2)>]
    [<InlineData(8, 3)>]
    [<InlineData(8, 4)>]
    [<InlineData(8, 5)>]
    [<InlineData(8, 6)>]
    [<InlineData(8, 7)>]
    let ``BK number operator is two-term`` (n : uint32, j : uint32) =
        let num = (bravyiKitaevTerms Raise j n) * (bravyiKitaevTerms Lower j n)
        num |> assertTermCount 2
        // One term is always 0.5 × identity string
        let idSig = System.String ('I', int n)
        num |> assertCoeff idSig half

    // ──────────────────────────────────────────────────
    //  BK vs JW:  Pauli weight comparison
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``BK has lower Pauli weight than JW for large registers`` () =
        let n = 16u
        let j = n - 1u
        let jwWeight = maxPauliWeight (jordanWignerTerms Raise j n)
        let bkWeight = maxPauliWeight (bravyiKitaevTerms Raise j n)
        Assert.True(bkWeight < jwWeight,
            sprintf "BK weight %d should be less than JW weight %d for j=%d, n=%d" bkWeight jwWeight j n)

    // ──────────────────────────────────────────────────
    //  BK vs JW:  number operators match structurally
    // ──────────────────────────────────────────────────

    [<Theory>]
    [<InlineData(4, 0)>]
    [<InlineData(4, 1)>]
    [<InlineData(4, 2)>]
    [<InlineData(4, 3)>]
    let ``BK and JW number operators have same term count`` (n : uint32, j : uint32) =
        let bkNum = (bravyiKitaevTerms Raise j n) * (bravyiKitaevTerms Lower j n)
        let jwNum = (jordanWignerTerms  Raise j n) * (jordanWignerTerms  Lower j n)
        Assert.Equal(jwNum.SummandTerms.Length, bkNum.SummandTerms.Length)

    [<Fact>]
    let ``BK n=2 j=0 number operator matches JW`` () =
        let jwNum = (jordanWignerTerms  Raise 0u 2u) * (jordanWignerTerms  Lower 0u 2u)
        let bkNum = (bravyiKitaevTerms Raise 0u 2u) * (bravyiKitaevTerms Lower 0u 2u)
        for term in jwNum.SummandTerms do
            bkNum |> assertCoeff term.Signature term.Coefficient

    // ──────────────────────────────────────────────────
    //  Edge cases
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``BK out of range returns empty`` () =
        bravyiKitaevTerms Raise 5u 4u |> assertTermCount 0

    [<Fact>]
    let ``BK Identity returns empty`` () =
        bravyiKitaevTerms Identity 0u 4u |> assertTermCount 0

    // ──────────────────────────────────────────────────
    //  Overlap integral  a†_0 a_1
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``BK product term for overlap integral is non-trivial`` () =
        let product = (bravyiKitaevTerms Raise 0u 4u) * (bravyiKitaevTerms Lower 1u 4u)
        Assert.True(product.SummandTerms.Length > 0)

    // ──────────────────────────────────────────────────
    //  JW reference tests
    // ──────────────────────────────────────────────────

    [<Fact>]
    let ``JW n=2 number operator j=0 for reference`` () =
        let num = (jordanWignerTerms Raise 0u 2u) * (jordanWignerTerms Lower 0u 2u)
        num |> assertTerms [ "II", half; "ZI", negHalf ]

    [<Fact>]
    let ``JW n=2 number operator j=1 for reference`` () =
        let num = (jordanWignerTerms Raise 1u 2u) * (jordanWignerTerms Lower 1u 2u)
        num |> assertTerms [ "II", half; "IZ", negHalf ]
