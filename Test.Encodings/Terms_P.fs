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
        Assert.Equal(expectedCoeff, actual.Coeff)

    [<Property>]
    let ``P <- C * C``(l : C<int>, r : C<int>) =
        let actual = l * r
        let expectedCount = 2
        let expectedCoeff = l.Coeff * r.Coeff
        Assert.Equal(expectedCount, actual.Units.Length)
        Assert.Equal(expectedCoeff, actual.Coeff)
        Assert.Equal<IEnumerable>([|l ; r|], actual.Units)

    [<Property>]
    let ``P <- 'unit``(i : int) =
        let actual = P<_>.Apply (i)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``P <- 'coeff * 'unit``(c : Complex, i : int) =
        let unit = C<_>.Apply (c, i)
        let actual = P<int>.Apply unit
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``P <- C``(unit : C<int>) =
        let actual = P<int>.Apply (unit)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``P <- 'coeff * C``(c : Complex, unit : C<int>) =
        let actual = P<int>.Apply (c, unit)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``P <- C[]``(units : C<int>[]) =
        let actual = P<int>.Apply (units)
        let expected = units |> Array.fold (fun c curr -> c * curr.Coeff) Complex.One
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``P <- 'coeff * C[]``(c : Complex, units : C<int>[]) =
        let actual = P<int>.Apply (c, units)
        Assert.True actual.VerifyIsValid
