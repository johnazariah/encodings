namespace Tests

module Terms_P =
    open System.Collections
    open Encodings
    open Xunit
    open FsCheck.Xunit
    open System.Numerics

    [<Property>]
    let ``P <- C + C``(l : C<int>, r : C<int>) =
        let actual = l + r
        let expectedCount = if (l.Item = r.Item) then 1 else 2
        let expectedCoeff = if (l.Item = r.Item) then (l.Coeff + r.Coeff) else Complex.One
        Assert.Equal(expectedCount, actual.Units.Length)
        Assert.Equal(expectedCoeff.Reduce, actual.Coeff)

    [<Property>]
    let ``P <- C * C``(l : C<int>, r : C<int>) =
        let actual = l * r
        let expectedCount = 2
        let expectedCoeff = l.Coeff * r.Coeff
        Assert.Equal(expectedCount, actual.Units.Length)
        Assert.Equal(expectedCoeff.Reduce, actual.Coeff.Reduce)
        Assert.Equal<IEnumerable>([| l.Item; r.Item |], actual.Units |> Array.map (fun t -> t.Item))
        if actual.Coeff.IsZero then
            Assert.Equal<IEnumerable>([| l.Coeff.Reduce; r.Coeff.Reduce |], actual.Units |> Array.map (fun t -> t.Coeff.Reduce))
        else
            Assert.Equal<IEnumerable>([| Complex.One; Complex.One |], actual.Units |> Array.map (fun t -> t.Coeff.Reduce))

    [<Property>]
    let ``P <- 'unit``(i : int) =
        let actual = P<_>.Apply (i)
        Assert.True actual.Reduce.Value.VerifyReduced

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
        Assert.True actual.Reduce.Value.VerifyReduced

    [<Property>]
    let ``P <- 'coeff * C[]``(c : Complex, units : C<int>[]) =
        let actual = P<int>.Apply (c, units)
        Assert.Equal (c.Reduce, actual.Coeff.Reduce)
        Assert.Equal (units.Length, actual.Units.Length)
        Assert.True  (units.Length >= actual.Reduce.Value.Units.Length)
        Assert.True actual.Reduce.Value.VerifyReduced
