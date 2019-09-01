namespace Tests

module Terms_C =
    open Encodings
    open Xunit
    open FsCheck.Xunit
    open System.Numerics

    [<Property>]
    let ``C <- 'unit``(i : int) =
        let actual = C<_>.Apply i
        Assert.Equal(Complex.One, actual.Coeff)
        Assert.Equal(i, actual.Item)

    [<Property>]
    let ``C <- 'coeff * 'unit``(c : Complex, i : int) =
        let actual = C<_>.Apply (c, i)
        Assert.Equal(c.Reduce, actual.Coeff.Reduce)
        Assert.Equal(i, actual.Item)

    [<Theory>]
    [<InlineData ( 0,   0, 'a', "")>]
    [<InlineData ( 1,   0, 'a', "a")>]
    [<InlineData (-1,   0, 'a', "(- a)")>]
    [<InlineData ( 0,   1, 'a', "(i a)")>]
    [<InlineData ( 0,  -1, 'a', "(-i a)")>]
    [<InlineData ( 42,  0, 'a', "(42 a)")>]
    [<InlineData (-2,   0, 'a', "(-2 a)")>]
    [<InlineData ( 42,  1, 'a', "(42i a)")>]
    [<InlineData ( 42, -1, 'a', "(-42i a)")>]
    [<InlineData ( 41, 10, 'a', "{Coeff = (41, 10);\n Item = 'a';}")>]
    [<InlineData ( 40, 10, 'a', "{Coeff = (40, 10);\n Item = 'a';}")>]
    [<InlineData (System.Math.PI, -3, 'a', "_ToString_")>]
    let ``C -> string`` (cr, ci, i, expected) =
        let ci = C<_>.Apply(Complex(float cr, float ci), i)
        let expected' = if expected = "_ToString_" then ci.ToString() else expected
        Assert.Equal(expected', ci.ToString())
