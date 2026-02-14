namespace Tests
module Complex =
    open Encodings
    open Xunit
    open FsCheck.Xunit
    open System.Numerics

    [<Property>]
    let ``TimesI flips Re and Im and fixes up sign``(re : double, im : double) =
        let expected = Complex(-im, re)
        let actual   = Complex(re, im).TimesI
        Assert.Equal (expected, actual)
