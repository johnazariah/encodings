namespace Tests

module Terms_P =
    open System.Collections
    open Encodings
    open Xunit
    open FsCheck.Xunit
    open System.Numerics

    let verifyReduced (this : P<_>) =
        let coefficientIsZeroOnlyWhenNoProductTerms =
            if this.Units = [||] then
                this.Coeff = Complex.Zero
            else
                this.Coeff <> Complex.Zero

        let everyUnitInProductTermHasUnitCoefficient =
            this.Units
            |> Seq.exists (fun u -> u.Coeff <> Complex.One)
            |> not

        let result =
            coefficientIsZeroOnlyWhenNoProductTerms &&
            everyUnitInProductTermHasUnitCoefficient
    #if DEBUG
        if not result then
            System.Diagnostics.Debugger.Break ()
    #endif
        result

    [<Property>]
    let ``P <- C + C``(l : C<int>, r : C<int>) =
        let actual = l + r
        let combinedCoeff = (l.Coeff + r.Coeff).Reduce
        if l.Item = r.Item then
            if combinedCoeff.IsZero then
                Assert.Equal(0, actual.Units.Length)
                Assert.Equal(Complex.Zero, actual.Coeff)
            else
                Assert.Equal(1, actual.Units.Length)
                Assert.Equal(combinedCoeff, actual.Coeff)
        else
            Assert.Equal(2, actual.Units.Length)
            Assert.Equal(Complex.One, actual.Coeff)

    [<Property>]
    let ``P <- C * C``(l : C<int>, r : C<int>) =
        let actual = l * r
        let expectedCount = 2
        let expectedCoeff = l.Coeff * r.Coeff
        Assert.Equal(expectedCount, actual.Units.Length)
        Assert.Equal(expectedCoeff.Reduce, actual.Coeff.Reduce)
        Assert.Equal<IEnumerable>([| l.Item; r.Item |], actual.Units |> Array.map (fun t -> t.Item))

        let expectedUnitCoefficients =
            if actual.Coeff.IsZero then
                [| l.Coeff.Reduce; r.Coeff.Reduce |]
            else
                [| Complex.One;    Complex.One    |]
        let actualUnitCoefficients = actual.Units |> Array.map (fun t -> t.Coeff.Reduce)
        Assert.Equal<IEnumerable>(expectedUnitCoefficients, actualUnitCoefficients)

    [<Property>]
    let ``P <- 'unit``(i : int) =
        let actual = P<_>.Apply (i)
        verifyReduced actual.Reduce.Value |> Assert.True

    [<Property>]
    let ``P <- 'coeff * 'unit``(c : Complex, i : int) =
        let unit = C<_>.Apply (c, i)
        let actual = P<int>.Apply unit
        Assert.Equal (Complex.One, actual.Coeff.Reduce)
        Assert.Equal (unit.Coeff.Reduce, actual.Reduce.Value.Coeff.Reduce)
        Assert.Equal (1, actual.Units.Length)

    [<Property>]
    let ``P <- C``(unit : C<int>) =
        let actual = P<int>.Apply (unit)
        Assert.Equal (Complex.One, actual.Coeff.Reduce)
        Assert.Equal (unit.Coeff.Reduce, actual.Reduce.Value.Coeff.Reduce)
        Assert.Equal (1, actual.Units.Length)

    [<Property>]
    let ``P <- 'coeff * C``(c : Complex, unit : C<int>) =
        let actual = P<int>.Apply (c, unit)
        Assert.Equal (c.Reduce, actual.Coeff.Reduce)
        Assert.Equal ((c * unit.Coeff).Reduce, actual.Reduce.Value.Coeff.Reduce)
        Assert.Equal (1, actual.Units.Length)

    [<Property>]
    let ``P <- C[]``(units : C<int>[]) =
        let actual = P<int>.Apply (units)
        Assert.Equal (Complex.One, actual.Coeff.Reduce)
        Assert.Equal (units.Length, actual.Units.Length)
        Assert.True  (units.Length >= actual.Reduce.Value.Units.Length)
        verifyReduced actual.Reduce.Value |> Assert.True

    [<Property>]
    let ``P <- 'coeff * C[]``(c : Complex, units : C<int>[]) =
        let actual = P<int>.Apply (c, units)
        Assert.Equal (c.Reduce, actual.Coeff.Reduce)
        Assert.Equal (units.Length, actual.Units.Length)
        Assert.True  (units.Length >= actual.Reduce.Value.Units.Length)
        verifyReduced actual.Reduce.Value |> Assert.True
