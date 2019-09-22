namespace Tests

module Terms_SC =
    open Encodings
    open Xunit
    open FsCheck.Xunit
    open System.Numerics

    let verifyReduced (this : SC<_>) =
        let coefficientIsUnity =
            this.Reduce.Value.Coeff = Complex.One

        let coefficientIsZeroWhenThereAreNoTerms =
            (this.Reduce.Value.Terms = [||]) &&
            (this.Reduce.Value.Coeff = Complex.Zero)

        coefficientIsZeroWhenThereAreNoTerms ||
        coefficientIsUnity

    [<Property (Arbitrary = [|typeof<ComplexGenerator>|]) >]
    let ``Coeff is Unity``(s : SC<_>) =
        Assert.Equal (Complex.One, s.Coeff)

    [<Property (Arbitrary = [|typeof<ComplexGenerator>|]) >]
    let ``S <- 'coeff * 'terms``(c : Complex, terms : C<char> []) =
        let actual = SC<_>.Apply (c, terms)
        verifyReduced actual |> Assert.True

    [<Property (Arbitrary = [|typeof<ComplexGenerator>|]) >]
    let ``IsZero is only true if coefficient is zero or all terms are zero`` (candidate : SC<char>) =
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
(*
    [<Property>]
    let ``S <- C[]``(units : C<int>[]) =
        let actual = SCSC.Apply (units)
        verifyReduced actual.Reduce.Value |> Assert.True

    [<Property>]
    let ``S <- 'coeff * C``(c : Complex, unit : C<int>) =
        let actual = SCSC.Apply (c, unit)
        verifyReduced actual.Reduce.Value |> Assert.True

    [<Property>]
    let ``S <- 'coeff * C[]``(c : Complex, units : C<int>[]) =
        let actual = SCSC.Apply (c, units)
        verifyReduced actual.Reduce.Value |> Assert.True

    [<Property>]
    let ``S <- 'coeff * P``(c : Complex, unit : P<int>) =
        let actual = SCSC.Apply (c, unit)
        verifyReduced actual.Reduce.Value |> Assert.True

    [<Property>]
    let ``S <- 'coeff * P[]``(c : Complex, units : P<int>[]) =
        let actual = SCSC.Apply (c, units)
        verifyReduced actual.Reduce.Value |> Assert.True
*)