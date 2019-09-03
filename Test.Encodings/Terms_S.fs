namespace Tests

module Terms_S =
    open Encodings
    open Xunit
    open FsCheck.Xunit
    open System.Numerics

    [<Property>]
    let ``S <- 'unit``(i : int) =
        let actual = S<int>.Apply (i)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``S <- 'coeff * 'unit``(c : Complex, i : int) =
        let actual = S<int>.Apply (c, i)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``S <- C``(unit : C<int>) =
        let actual = S<int>.Apply (unit)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``S <- C[]``(units : C<int>[]) =
        let actual = S<int>.Apply (units)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``S <- 'coeff * C``(c : Complex, unit : C<int>) =
        let actual = S<int>.Apply (c, unit)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``S <- 'coeff * C[]``(c : Complex, units : C<int>[]) =
        let actual = S<int>.Apply (c, units)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``S <- 'coeff * P``(c : Complex, unit : P<int>) =
        let actual = S<int>.Apply (c, unit)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``S <- 'coeff * P[]``(c : Complex, units : P<int>[]) =
        let actual = S<int>.Apply (c, units)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``S : AppendToTerms``(expr : S<int>, u : int) =
        let actual = expr.AppendToTerms u
        for terms in actual.Terms.Values do
            let lastUnit = terms.Units.[terms.Units.Length - 1].Item
            Assert.Equal (u, lastUnit)
