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
        Assert.Equal(c, actual.Coeff)
        Assert.Equal(i, actual.Item)

    [<Theory>]
    [<InlineData (1, 0, 'a', "{Coeff = (1, 0);\n Item = 'a';}")>]
    [<InlineData (0, 0, 'a', "{Coeff = (0, 0);\n Item = 'a';}")>]
    let ``C : ToString()`` (cr, ci, i, expected) =
        let ci = C<_>.Apply(Complex(float cr, float ci), i)
        Assert.Equal(expected, ci.ToString())
