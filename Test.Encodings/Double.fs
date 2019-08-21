namespace Tests
module Double =
    open Encodings
    open System
    open Xunit

    [<Theory>]
    [<InlineData(0.0, Double.NaN, false)>]
    [<InlineData(Double.NaN, 0.0, false)>]
    [<InlineData(Double.NaN, Double.NaN, true)>]
    let ``Double : approximate equality works with NaN`` (l, r, expected) =
        Assert.Equal(expected, l =? r)


