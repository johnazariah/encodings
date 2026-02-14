namespace Tests

module TypeExtensions =
    open Encodings
    open Xunit
    open FsCheck
    open FsCheck.Xunit
    open System
    open System.Numerics

    [<Property>]
    let ``uncurry applies curried functions correctly`` (leftValue : int) (rightValue : int) =
        let addCurried first second = first + second
        let addTupled = uncurry addCurried
        Assert.Equal(addCurried leftValue rightValue, addTupled (leftValue, rightValue))

    [<Property>]
    let ``curry applies tupled functions correctly`` (leftValue : int) (rightValue : int) =
        let subtractTupled (first, second) = first - second
        let subtractCurried = curry subtractTupled
        Assert.Equal(subtractTupled (leftValue, rightValue), subtractCurried leftValue rightValue)

    [<Property>]
    let ``SwapSignMultiple follows parity`` (NonNegativeInt swapCount) (coefficient : Complex) =
        let expected = if swapCount % 2 = 0 then coefficient else -coefficient
        let actual = Complex.SwapSignMultiple swapCount coefficient
        Assert.Equal(expected, actual)

    [<Theory>]
    [<InlineData(0.0, 0.0, false)>]
    [<InlineData(2.0, 0.0, true)>]
    [<InlineData(0.0, -3.0, true)>]
    [<InlineData(System.Double.PositiveInfinity, 0.0, false)>]
    [<InlineData(0.0, System.Double.NaN, false)>]
    let ``IsNonZero excludes zero and non-finite values`` (realPart : float, imaginaryPart : float, expected : bool) =
        let value = Complex(realPart, imaginaryPart)
        Assert.Equal(expected, value.IsNonZero)

    [<Theory>]
    [<InlineData(0.0, 0.0, true)>]
    [<InlineData(2.0, 0.0, false)>]
    [<InlineData(0.0, -3.0, false)>]
    [<InlineData(System.Double.PositiveInfinity, 0.0, true)>]
    [<InlineData(0.0, System.Double.NaN, true)>]
    let ``IsZero treats non-finite values as zero-like`` (realPart : float, imaginaryPart : float, expected : bool) =
        let value = Complex(realPart, imaginaryPart)
        Assert.Equal(expected, value.IsZero)

    [<Theory>]
    [<InlineData(2.0, -3.0, 2.0, -3.0)>]
    [<InlineData(System.Double.PositiveInfinity, 0.0, 0.0, 0.0)>]
    [<InlineData(0.0, System.Double.NaN, 0.0, 0.0)>]
    let ``Reduce keeps finite values and zeros non-finite values``
        (realPart : float, imaginaryPart : float, expectedReal : float, expectedImaginary : float) =
        let value = Complex(realPart, imaginaryPart)
        let reduced = value.Reduce
        Assert.Equal(Complex(expectedReal, expectedImaginary), reduced)

    [<Property>]
    let ``TimesI matches multiplication by imaginary unit for finite values`` (realPart : float) (imaginaryPart : float) =
        if Double.IsFinite realPart && Double.IsFinite imaginaryPart then
            let value = Complex(realPart, imaginaryPart)
            Assert.Equal(value * Complex.ImaginaryOne, value.TimesI)

    [<Fact>]
    let ``Map Keys and Values expose sorted map contents`` () =
        let sampleMap = Map.ofList [ ("b", 2); ("a", 1); ("c", 3) ]
        Assert.Equal<string>([| "a"; "b"; "c" |], sampleMap.Keys |> Seq.toArray)
        Assert.Equal<int>([| 1; 2; 3 |], sampleMap.Values |> Seq.toArray)

    [<Fact>]
    let ``ToPhasePrefix formats common phases`` () =
        let cases =
            [
                Complex.One, ""
                Complex.MinusOne, " -"
                Complex.ImaginaryOne, "( i) "
                -Complex.ImaginaryOne, "(-i) "
                Complex(2.5, 0.0), "2.5 "
            ]

        for (value, expected) in cases do
            Assert.Equal(expected, value.ToPhasePrefix)

    [<Fact>]
    let ``ToPhaseConjunction formats common phases`` () =
        let cases =
            [
                Complex.One, " + "
                Complex.MinusOne, " - "
                Complex.ImaginaryOne, " + i "
                -Complex.ImaginaryOne, " - i "
                Complex(2.5, 0.0), " + 2.5 "
                Complex(-2.5, 0.0), " - 2.5 "
            ]

        for (value, expected) in cases do
            Assert.Equal(expected, value.ToPhaseConjunction)
