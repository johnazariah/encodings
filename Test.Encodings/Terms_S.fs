namespace Tests

module Terms_S =
    open Encodings
    open Xunit
    open FsCheck.Xunit
    open System.Numerics

    [<Property>]
    let ``S <- 'unit``(i : int) =
        let actual = Sum<int>.Apply (i)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``S <- 'coeff * 'unit``(c : Complex, i : int) =
        let actual = Sum<int>.Apply (c, i)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``S <- C``(unit : Cf<int>) =
        let actual = Sum<int>.Apply (unit)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``S <- C[]``(units : Cf<int>[]) =
        let actual = Sum<int>.Apply (units)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``S <- 'coeff * C``(c : Complex, unit : Cf<int>) =
        let actual = Sum<int>.Apply (c, unit)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``S <- 'coeff * C[]``(c : Complex, units : Cf<int>[]) =
        let actual = Sum<int>.Apply (c, units)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``S <- 'coeff * P``(c : Complex, unit : Pr<int>) =
        let actual = Sum<int>.Apply (c, unit)
        Assert.True actual.VerifyIsValid

    [<Property>]
    let ``S <- 'coeff * P[]``(c : Complex, units : Pr<int>[]) =
        let actual = Sum<int>.Apply (c, units)
        Assert.True actual.VerifyIsValid
