namespace Tests

open System.Collections.Generic
open Encodings
open Xunit
open FsCheck.Xunit
open System.Numerics

[<Properties (Arbitrary = [|typeof<ComplexGenerator>|], QuietOnSuccess = true) >]
module Terms_SC =
    let verifyReduced (this : SC<_>) =
        let coefficientIsUnity =
            this.Reduce.Value.Coeff = Complex.One

        let coefficientIsZeroWhenThereAreNoTerms =
            (this.Reduce.Value.Terms = [||]) &&
            (this.Reduce.Value.Coeff = Complex.Zero)

        coefficientIsZeroWhenThereAreNoTerms ||
        coefficientIsUnity

    [<Property>]
    let ``Coeff is Unity``(s : SC<CChar>) =
        Assert.Equal (Complex.One, s.Coeff)

    [<Property>]
    let ``S <- 'coeff * 'terms``(c : Complex, terms : C<CChar> []) =
        let actual = SC<_>.Apply (c, terms)
        verifyReduced actual |> Assert.True

    [<Property>]
    let ``IsZero is only true if coefficient is zero or all terms are zero`` (candidate : SC<CChar>) =
        let zeroCoeff = SC<_>.Apply(Complex.Zero, candidate.Terms)
        Assert.True (zeroCoeff.IsZero)

        let zeroTerms = candidate.Terms |> Array.map (fun t -> t.ScaleCoefficient Complex.Zero)
        let allZeroTerms = SC<_>.Apply(candidate.Coeff, zeroTerms)
        Assert.True (allZeroTerms.IsZero)

        let allTermsAreZero = candidate.Terms |> (Seq.exists (fun t -> not t.IsZero)) |> not
        if (candidate.Coeff = Complex.Zero || allTermsAreZero) then
            Assert.True (candidate.IsZero)
        else
            Assert.False (candidate.IsZero)

    [<Property>]
    let ``Reduce removes all terms if coeff is zero`` (candidate : SC<CChar>) =
        if (candidate.Coeff = Complex.Zero) then
            Assert.False (true, "Zero coefficient?")
        else if (candidate.Terms = [||]) then
            Assert.Empty (candidate.Terms)
            Assert.Empty (candidate.Reduce.Value.Terms)
            Assert.Equal (Complex.One, candidate.Coeff)
            Assert.Equal (Complex.One, candidate.Reduce.Value.Coeff)
        else
            let zc = candidate.ScaleCoefficient Complex.Zero
            Assert.NotEmpty (candidate.Terms)
            Assert.True     (zc.IsZero)
            Assert.NotEmpty (zc.Terms)
            Assert.Empty    (zc.Reduce.Value.Terms)
            Assert.Equal    (Complex.One, candidate.Coeff)
            Assert.Equal    (Complex.One, zc.Reduce.Value.Coeff)

    [<Fact>]
    let ``Reduce works on empty array``() =
        let sc = SC<CChar>.Apply(Complex.One, [||])
        Assert.True(sc.IsZero)
        Assert.Empty(sc.Terms)

        let zc = sc.ScaleCoefficient Complex.Zero
        Assert.True  (zc.IsZero)
        Assert.Empty (zc.Terms)
        Assert.Empty (zc.Reduce.Value.Terms)
        Assert.Equal (Complex.One, zc.Coeff)

    [<Property>]
    let ``Constructor coalesces coefficients for like terms``(coeffs : Complex[]) =
        let terms = coeffs |> Array.map (fun coeff -> C<_>.Apply(coeff, CC 'a'))
        let sc = SC<_>.Apply(Complex.One, terms)
        if (sc.IsZero) then
            Assert.Empty (sc.Reduce.Value.Terms)
        else
            let found = Assert.Single(sc.Terms)
            let expected = coeffs |> Array.fold (+) Complex.Zero
            let actual   = found.C
            Assert.Equal(expected, actual)

    [<Fact>]
    let ``Constructor coalesces coefficients for like terms : Regression 1``() =
        let inputs = [| Complex.One; Complex.MinusOne |]
        ``Constructor coalesces coefficients for like terms`` inputs

    [<Property>]
    let ``Addition operator coalesces coefficients for like terms``(lcoeffs : Complex[], rcoeffs : Complex[]) =
        let (ls, rs) =
            [| lcoeffs; rcoeffs; |]
            |> Array.map (Array.map (fun coeff -> C<_>.Apply (coeff, CC 'a')))
            |> Array.map ((curry SC<_>.Apply) Complex.One)
            |> (fun rg -> (rg.[0], rg.[1]))

        let sc = ls + rs
        if (sc.IsZero) then
            Assert.Empty (sc.Reduce.Value.Terms)
        else
            let found = Assert.Single(sc.Terms)
            let lsum = lcoeffs |> Array.fold (+) Complex.Zero
            let rsum = rcoeffs |> Array.fold (+) Complex.Zero
            let expected = lsum + rsum
            let actual   = found.C
            Assert.True(Complex.ApproximatelyEqual(expected, actual))

    [<Property>]
    let ``Addition operator coalesces terms from both arguments``(lterms : char[], rterms: char[]) =
        let (ls, rs) =
            [| lterms; rterms; |]
            |> Array.map (HashSet)
            |> Array.map (Seq.map (fun term -> C<_>.Apply(Complex.One, CC term)) >> Array.ofSeq)
            |> Array.map ((curry SC<_>.Apply) Complex.One)
            |> (fun rg -> (rg.[0], rg.[1]))

        let sc = ls + rs
        if (sc.IsZero) then
            Assert.Empty (sc.Terms)
        else
            let expected = HashSet (seq { yield! lterms; yield! rterms} )
            let actual   = sc.Terms |> Array.map (fun t -> t.U)
            Assert.Equal(expected.Count, actual.Length)

    [<Property>]
    let ``Constructor coeff scales coefficient of all terms``(globalCoeff : Complex, terms : C<CChar>[]) =
        let expected = SC<_>.Apply(Complex.One, terms) |> (fun sc -> sc.ScaleCoefficient globalCoeff)
        let actual   = SC<_>.Apply(globalCoeff, terms)

        Assert.Equal(expected.Terms.Length, actual.Terms.Length)
        Assert.Equal(expected.Coeff, actual.Coeff)
        Assert.Equal(Complex.One, actual.Coeff)

        let expectedTerms = expected.Terms |> Seq.sortBy(fun t -> t.U) |> List.ofSeq
        let actualTerms   = actual.Terms   |> Seq.sortBy(fun t -> t.U) |> List.ofSeq

        let rec allTermsEqual = function
        | [], [] -> true
        | (l : C<CChar>) :: ls, (r : C<CChar>) :: rs -> (Assert.True(Complex.ApproximatelyEqual(l.C, r.C)); Assert.Equal(l.U, r.U); true && allTermsEqual (ls, rs))
        | _, _ -> false
        Assert.True (allTermsEqual (expectedTerms, actualTerms))


    [<Fact>]
    let ``Constructor coeff scales coefficient of all terms : Regression 0``() =
        let globalCoeff = Complex.One
        let terms =
            [|
                {C = Complex.Zero; U = CC 'n'}
                {C = Complex.Zero; U = CC 'a'}
            |]
        ``Constructor coeff scales coefficient of all terms``(globalCoeff, terms)

    [<Fact>]
    let ``Constructor coeff scales coefficient of all terms : Regression 1``() =
        let globalCoeff = Complex(-2.588235294117647, -14.897435897435898)
        let terms =
            [|
                {C = Complex(4.5, 4.0); U = CC 'a'}
                {C = Complex(1.0, 1.5); U = CC 'a'}
            |]
        ``Constructor coeff scales coefficient of all terms``(globalCoeff, terms)

    [<Fact>]
    let ``Constructor coeff scales coefficient of all terms : Regression 2``() =
        let globalCoeff = Complex(11.666666666666666, 6.)
        let terms =
            [|
                {C = Complex(-3., -3.5); U = CC 'a'}
                {C = Complex(1.8333333333333333, -0.19999999999999996); U = CC 'a'}
            |]
        ``Constructor coeff scales coefficient of all terms``(globalCoeff, terms)

    [<Property>]
    let ``ScaleCoefficient scales coefficient of all terms``(terms : char[], initialCoeff : Complex, factor : Complex) =
        let ls =
            [| terms |]
            |> Array.map (HashSet)
            |> Array.map (Seq.map (fun term -> C<_>.Apply(initialCoeff, CC term)) >> Array.ofSeq)
            |> Array.map ((curry SC<_>.Apply) Complex.One)
            |> (fun rg -> rg.[0])

        Assert.All(ls.Terms, (fun t -> Assert.True(Complex.ApproximatelyEqual(initialCoeff, t.C))))
        let result = ls.ScaleCoefficient(factor)
        Assert.All(result.Terms, (fun t -> Assert.True(Complex.ApproximatelyEqual(initialCoeff * factor, t.C))))

    [<Property>]
    let ``AddCoefficient adds coefficient to all terms``(terms : char[], initialCoeff : Complex, diff : Complex) =
        let ls =
            [| terms |]
            |> Array.map (HashSet)
            |> Array.map (Seq.map (fun term -> C<_>.Apply(initialCoeff, CC term)) >> Array.ofSeq)
            |> Array.map ((curry SC<_>.Apply) Complex.One)
            |> (fun rg -> rg.[0])

        Assert.All(ls.Terms, (fun t -> Assert.True(Complex.ApproximatelyEqual(initialCoeff, t.C))))
        let result = ls.AddCoefficient(diff)
        Assert.All(result.Terms, (fun t -> Assert.True(Complex.ApproximatelyEqual(initialCoeff + diff, t.C))))

    // TODO: Multiply
