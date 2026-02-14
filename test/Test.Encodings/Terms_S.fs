namespace Tests

module Terms_S =
    open Encodings
    open Xunit
    open FsCheck.Xunit
    open System.Numerics


    let verifyReduced (this : S<_>) =
        let termCoefficientIsUnity =
            this.Coeff = Complex.One

        let termCoefficientIsZeroWhenThereAreNoProductTerms =
            (this.ProductTerms.Value = [||]) &&
            (this.Coeff = Complex.Zero)

        termCoefficientIsZeroWhenThereAreNoProductTerms ||
        termCoefficientIsUnity

    [<Property>]
    let ``S <- 'unit``(i : int) =
        let actual = S<int>.Apply (i)
        verifyReduced actual.Reduce.Value |> Assert.True

    [<Property>]
    let ``S <- 'coeff * 'unit``(c : Complex, i : int) =
        let actual = S<int>.Apply (c, i)
        verifyReduced actual.Reduce.Value |> Assert.True

    [<Property>]
    let ``S <- C``(unit : C<int>) =
        let actual = S<int>.Apply (unit)
        verifyReduced actual.Reduce.Value |> Assert.True

    [<Property>]
    let ``S <- C[]``(units : C<int>[]) =
        let actual = S<int>.Apply (units)
        verifyReduced actual.Reduce.Value |> Assert.True

    [<Property>]
    let ``S <- 'coeff * C``(c : Complex, unit : C<int>) =
        let actual = S<int>.Apply (c, unit)
        verifyReduced actual.Reduce.Value |> Assert.True

    [<Property>]
    let ``S <- 'coeff * C[]``(c : Complex, units : C<int>[]) =
        let actual = S<int>.Apply (c, units)
        verifyReduced actual.Reduce.Value |> Assert.True

    [<Property>]
    let ``S <- 'coeff * P``(c : Complex, unit : P<int>) =
        let actual = S<int>.Apply (c, unit)
        verifyReduced actual.Reduce.Value |> Assert.True

    [<Property>]
    let ``S <- 'coeff * P[]``(c : Complex, units : P<int>[]) =
        let actual = S<int>.Apply (c, units)
        verifyReduced actual.Reduce.Value |> Assert.True