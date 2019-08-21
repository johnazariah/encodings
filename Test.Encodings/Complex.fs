namespace Tests
module Complex =
    open Encodings
    open System
    open Xunit
    open FsCheck.Xunit

    [<Property>]
    let ``Complex : Addition computed correctly`` (left : Complex, right : Complex) =
        let actual = left + right
        let expected = { Re = left.Re + right.Re; Im = left.Im + right.Im }
        Assert.Equal (expected, actual)

    [<Property>]
    let ``Complex : Subtraction computed correctly`` (left : Complex, right : Complex) =
        let actual = left - right
        let expected = { Re = left.Re - right.Re; Im = left.Im - right.Im }
        Assert.Equal (expected, actual)

    [<Property>]
    let ``Complex : Conjugate computed correctly`` (value : Complex) =
        let actual = ~~value
        let expected = { value with Im = -value.Im }
        Assert.Equal (expected, actual)

    [<Property>]
    let ``Complex : Polar coordinate conversion round-trips correctly`` (value : Complex) =
        let actual = value.ToPolar.ToCartesian
        let expected = value
        Assert.Equal (expected, actual)

    [<Theory>]
    [<InlineData(0.0, Double.NaN)>]
    [<InlineData(Double.NaN, 0.0)>]
    [<InlineData(0.0, 1.0)>]
    [<InlineData(0.0, Double.NegativeInfinity)>]
    [<InlineData(-1.79769313E+308, 0.0)>]
    [<InlineData(-4.820197328, 4.10669264)>]
    [<InlineData(0.8368364414, -0.8300013547)>]
    [<InlineData(-0.00402111212, -0.1554150641)>]
    [<InlineData(-5.0, 7.0)>]
    let ``Complex : Polar coordinate conversion round-trip succeeds`` (re : double, im : double) =
        let expected = { Re = re; Im = im}
        let actual   = expected.ToPolar.ToCartesian
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)

    [<Theory>]
    [<InlineData(0.0, Double.NaN, "(0.0000 + NaN i)")>]
    [<InlineData(Double.NaN, 0.0, "(NaN)")>]
    [<InlineData(0.0, 1.0, "(0.0000 + 1.0000 i)")>]
    [<InlineData(1.0, 1.0, "(1.0000 + 1.0000 i)")>]
    [<InlineData(1.0, 0.0, "(1.0000)")>]
    [<InlineData(1.0, -1.0, "(1.0000 - 1.0000 i)")>]
    let ``Complex : ToString() formats correctly`` (re : double, im : double, expected) =
        let actual = { Re = re; Im = im }.ToString()
        Assert.Equal (expected, actual)
        Assert.Equal (actual, expected)

    [<Property>]
    let ``Complex : TimesI flips Re and Im and fixes up sign``(re : double, im : double) =
        let c = { Re = re; Im = im}
        let expected = { Re = -im; Im = re}
        let actual   = c.TimesI
        Assert.Equal (actual, expected)