namespace Tests

open System.Collections.Generic
open Encodings
open Xunit
open FsCheck.Xunit
open System.Numerics

[<Properties (Arbitrary = [|typeof<ComplexGenerator>|], QuietOnSuccess = true) >]
module Terms_SC =
    let verifyReduced (this : S<_>) =
        let coefficientIsUnity =
            this.Reduce.Value.Coeff = Complex.One

        let coefficientIsZeroWhenThereAreNoTerms =
            (this.Reduce.Value.Terms = [||]) &&
            (this.Reduce.Value.Coeff = Complex.Zero)

        coefficientIsZeroWhenThereAreNoTerms ||
        coefficientIsUnity

    [<Property>]
    let ``Coeff is Unity``(s : S<CChar>) =
        Assert.Equal (Complex.One, s.Coeff)

    [<Property>]
    let ``S <- 'coeff * 'terms``(c : Complex, terms : CChar []) =
        let actual = S<_>.Apply (c, terms)
        verifyReduced actual |> Assert.True

    [<Property>]
    let ``IsZero is only true if coefficient is zero or all terms are zero`` (candidate : S<CChar>) =
        let zeroCoeff = S<_>.Apply(Complex.Zero, candidate.Terms)
        Assert.True (zeroCoeff.IsZero)

        let zeroTerms = candidate.Terms |> Array.map (fun t -> t.ScaleCoefficient Complex.Zero)
        let allZeroTerms = S<_>.Apply(candidate.Coeff, zeroTerms)
        Assert.True (allZeroTerms.IsZero)

        let allTermsAreZero = candidate.Terms |> (Seq.exists (fun t -> not t.IsZero)) |> not
        if (candidate.Coeff = Complex.Zero || allTermsAreZero) then
            Assert.True (candidate.IsZero)
        else
            Assert.False (candidate.IsZero)

    [<Property>]
    let ``Reduce removes all terms if coeff is zero`` (candidate : S<CChar>) =
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
        let S = S<CChar>.Apply(Complex.One, [||])
        Assert.True(S.IsZero)
        Assert.Empty(S.Terms)

        let zc = S.ScaleCoefficient Complex.Zero
        Assert.True  (zc.IsZero)
        Assert.Empty (zc.Terms)
        Assert.Empty (zc.Reduce.Value.Terms)
        Assert.Equal (Complex.One, zc.Coeff)

    [<Property>]
    let ``Constructor coalesces coefficients for like terms``(coeffs : Complex[]) =
        let terms = coeffs |> Array.map (fun coeff -> CChar.Apply (coeff, 'a'))
        let S = S<_>.Apply(Complex.One, terms)
        if (S.IsZero) then
            Assert.Empty (S.Reduce.Value.Terms)
        else
            let found = Assert.Single(S.Terms)
            let expected = coeffs |> Array.fold (+) Complex.Zero
            let actual   = found.Coeff
            Assert.Equal(expected, actual)

    [<Fact>]
    let ``Constructor coalesces coefficients for like terms : Regression 1``() =
        let inputs = [| Complex.One; Complex.MinusOne |]
        ``Constructor coalesces coefficients for like terms`` inputs

    [<Property>]
    let ``Addition operator coalesces coefficients for like terms``(lcoeffs : Complex[], rcoeffs : Complex[]) =
        let (ls, rs) =
            [| lcoeffs; rcoeffs; |]
            |> Array.map (Array.map (fun coeff -> CChar.Apply (coeff, 'a')))
            |> Array.map ((curry S<_>.Apply) Complex.One)
            |> (fun rg -> (rg.[0], rg.[1]))

        let sum = ls + rs
        if (sum.IsZero) then
            Assert.Empty (sum.Reduce.Value.Terms)
        else
            let found = Assert.Single(sum.Terms)
            let lsum = lcoeffs |> Array.fold (+) Complex.Zero
            let rsum = rcoeffs |> Array.fold (+) Complex.Zero
            let expected = lsum + rsum
            let actual   = found.Coeff
            Assert.True(Complex.ApproximatelyEqual(expected, actual))

    [<Fact>]
    let ``Addition operator coalesces coefficients for like terms : Regression 1``() =
        let lterms = [||]
        let rterms = [|Complex(-0.5, -2.); Complex(0.5, -0.5)|]
        ``Addition operator coalesces coefficients for like terms`` (lterms, rterms)

    [<Property>]
    let ``Addition operator coalesces terms from both arguments``(lterms : char[], rterms: char[]) =
        let (ls, rs) =
            [| lterms; rterms; |]
            |> Array.map (HashSet)
            |> Array.map (Seq.map (CChar.New) >> Array.ofSeq)
            |> Array.map ((curry S<_>.Apply) Complex.One)
            |> (fun rg -> (rg.[0], rg.[1]))

        let S = ls + rs
        if (S.IsZero) then
            Assert.Empty (S.Terms)
        else
            let expected = HashSet (seq { yield! lterms; yield! rterms} )
            let actual   = S.Terms
            Assert.Equal(expected.Count, actual.Length)

    [<Property>]
    let ``Constructor coeff scales coefficient of all terms``(globalCoeff : Complex, terms : CChar[]) =
        let expected = S<_>.Apply(Complex.One, terms) |> (fun S -> S.ScaleCoefficient globalCoeff)
        let actual   = S<_>.Apply(globalCoeff, terms)

        Assert.Equal(expected.Terms.Length, actual.Terms.Length)
        Assert.Equal(expected.Coeff, actual.Coeff)
        Assert.Equal(Complex.One, actual.Coeff)

        let expectedTerms = expected.Terms |> Seq.sortBy(fun t -> t.Signature) |> List.ofSeq
        let actualTerms   = actual.Terms   |> Seq.sortBy(fun t -> t.Signature) |> List.ofSeq

        let rec allTermsEqual = function
        | [], [] -> true
        | (l : CChar) :: ls, (r : CChar) :: rs -> (Assert.True(Complex.ApproximatelyEqual(l.Coeff, r.Coeff)); Assert.Equal(l.Signature, r.Signature); true && allTermsEqual (ls, rs))
        | _, _ -> false
        Assert.True (allTermsEqual (expectedTerms, actualTerms))


    [<Fact>]
    let ``Constructor coeff scales coefficient of all terms : Regression 1``() =
        let globalCoeff = Complex.One
        let terms =
            [|
                CChar.Apply (Complex.Zero, 'n')
                CChar.Apply (Complex.Zero, 'a')
            |]
        ``Constructor coeff scales coefficient of all terms``(globalCoeff, terms)

    [<Fact>]
    let ``Constructor coeff scales coefficient of all terms : Regression 2``() =
        let globalCoeff = Complex(-2.588235294117647, -14.897435897435898)
        let terms =
            [|
                CChar.Apply (Complex(4.5, 4.0), 'a')
                CChar.Apply (Complex(1.0, 1.5), 'a')
            |]
        ``Constructor coeff scales coefficient of all terms``(globalCoeff, terms)

    [<Fact>]
    let ``Constructor coeff scales coefficient of all terms : Regression 3``() =
        let globalCoeff = Complex(11.666666666666666, 6.)
        let terms =
            [|
                CChar.Apply (Complex(-3., -3.5), 'a')
                CChar.Apply (Complex(1.8333333333333333, -0.19999999999999996), 'a')
            |]
        ``Constructor coeff scales coefficient of all terms``(globalCoeff, terms)

    [<Property>]
    let ``ScaleCoefficient scales coefficient of all terms``(terms : char[], initialCoeff : Complex, factor : Complex) =
        let ls =
            [| terms |]
            |> Array.map (HashSet)
            |> Array.map (Seq.map (curry CChar.Apply initialCoeff) >> Array.ofSeq)
            |> Array.map ((curry S<_>.Apply) Complex.One)
            |> (fun rg -> rg.[0])

        Assert.All(ls.Terms, (fun t -> Assert.True(Complex.ApproximatelyEqual(initialCoeff, t.Coeff))))
        let result = ls.ScaleCoefficient(factor)
        Assert.All(result.Terms, (fun t -> Assert.True(Complex.ApproximatelyEqual(initialCoeff * factor, t.Coeff))))

    [<Property>]
    let ``AddCoefficient adds coefficient to all terms``(terms : char[], initialCoeff : Complex, diff : Complex) =
        let ls =
            [| terms |]
            |> Array.map (HashSet)
            |> Array.map (Seq.map (curry CChar.Apply initialCoeff) >> Array.ofSeq)
            |> Array.map ((curry S<_>.Apply) Complex.One)
            |> (fun rg -> rg.[0])

        Assert.All(ls.Terms, (fun t -> Assert.True(Complex.ApproximatelyEqual(initialCoeff, t.Coeff))))
        let result = ls.AddCoefficient(diff)
        Assert.All(result.Terms, (fun t -> Assert.True(Complex.ApproximatelyEqual(initialCoeff + diff, t.Coeff))))

    // TODO: Multiply
