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

    [<Fact>]
    let ``S addition keeps the last duplicate term`` () =
        let left = S<int>.Apply(Complex(2.0, 0.0), 7)
        let right = S<int>.Apply(Complex(3.0, 0.0), 7)

        let actual = left + right
        let terms = actual.ProductTerms.Value

        Assert.Equal(1, terms.Length)
        Assert.Equal(Complex(3.0, 0.0), terms.[0].Coeff)

    [<Fact>]
    let ``S multiplication distributes over product terms`` () =
        let left =
            [|
                P<int>.Apply(Complex.One, [| 1 |])
                P<int>.Apply(Complex.One, [| 2 |])
            |]
            |> S<int>.Apply

        let right = S<int>.Apply(Complex.One, 3)

        let actual = left * right

        Assert.Equal(2, actual.ProductTerms.Value.Length)

    [<Fact>]
    let ``NormalizeTermCoefficient pushes top-level coefficient into terms`` () =
        let term = P<int>.Apply(Complex(2.0, 0.0), [| 5 |])
        let sum = S<int>.Apply(Complex(3.0, 0.0), [| term |])

        let normalized = sum.NormalizeTermCoefficient

        Assert.Equal(Complex.One, normalized.Coeff)
        Assert.Equal(Complex(6.0, 0.0), normalized.ProductTerms.Value.[0].Coeff)

    [<Fact>]
    let ``Reduce removes zero product terms`` () =
        let zeroTerm = P<int>.Apply(Complex.Zero, [| 1 |])
        let nonZeroTerm = P<int>.Apply(Complex.One, [| 2 |])

        let reduced = S<int>.Apply(Complex.One, [| zeroTerm; nonZeroTerm |]).Reduce.Value

        Assert.Equal(1, reduced.ProductTerms.Value.Length)
        Assert.Equal(Complex.One, reduced.ProductTerms.Value.[0].Coeff)

    [<Fact>]
    let ``IsZero detects zero coefficient and empty expressions`` () =
        let zeroCoeff = S<int>.Apply(Complex.Zero, 1)
        let empty = S<int>.Apply([||] : P<int> [])
        let nonZero = S<int>.Apply(Complex.One, 1)

        Assert.True(zeroCoeff.IsZero)
        Assert.True(empty.IsZero)
        Assert.False(nonZero.IsZero)

    [<Fact>]
    let ``TryCreateFromString round-trips S expressions`` () =
        let parser =
            fun (value : string) ->
            match System.Int32.TryParse value with
            | true, parsedValue -> Some parsedValue
            | false, _ -> None

        let source =
            [|
                P<int>.Apply(Complex(2.0, 0.0), [| 1; 2 |])
                P<int>.Apply(Complex(3.0, 0.0), [| 4 |])
            |]
            |> S<int>.Apply

        let input = source.ToString()
        let parsed = S<int>.TryCreateFromString parser input

        Assert.True(parsed.IsSome)
        Assert.Equal(input, parsed.Value.ToString())