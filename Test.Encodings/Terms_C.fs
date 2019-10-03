namespace Tests

open Encodings
open Xunit
open FsCheck.Xunit
open System.Numerics

[<Properties (Arbitrary = [|typeof<ComplexGenerator>|], QuietOnSuccess = true) >]
module Terms_C =

    [<Property>]
    let ``IsZero true only when coeff is zero`` (c : Complex) (i : int) =
        let u = C<_>.Apply (c, i)
        Assert.True(Complex.ApproximatelyEqual(c, u.C))

        if (c = Complex.Zero) then
            Assert.True(u.IsZero)
        else
            Assert.False(u.IsZero)

        let v = C<_>.Apply (Complex.Zero, i)
        Assert.Equal(Complex.Zero, v.C)
        Assert.True(v.IsZero)

    [<Property>]
    let ``Normalize sets coeff to One`` (c : C<int>) =
        if (c.C <> Complex.One) then
            Assert.False(c.C = Complex.One)
            Assert.True(c.Normalize.C = Complex.One)
        else
            Assert.True(c.C = Complex.One)
            Assert.True(c.Normalize.C = Complex.One)

    [<Property>]
    let ``Negate negates coeff`` (c : C<int>) =
        Assert.True(Complex.ApproximatelyEqual(-c.C, (-c).C))

    [<Property>]
    let ``ScaleCoefficient multiplies coefficient`` (s : Complex) (c : C<int>) =
        Assert.Equal((c.C * s), (c.ScaleCoefficient s).C)

    [<Property>]
    let ``AddCoefficient adds coefficient`` (s : Complex) (c : C<int>) =
        Assert.Equal((c.C + s), (c.AddCoefficient s).C)

    [<Property>]
    let ``C <- 'unit``(i : int) =
        let actual = (curry C<_>.Apply Complex.One) i
        Assert.Equal(Complex.One, actual.C)
        Assert.Equal(i, actual.U)

    [<Property>]
    let ``C <- 'coeff * 'unit``(c : Complex, i : int) =
        let actual = C<_>.Apply (c, i)
        Assert.Equal(c, actual.C)
        Assert.Equal(i, actual.U)

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
    [<InlineData ( 41, 10, 'a', "{ C = (41, 10)\n  U = 'a' }")>]
    [<InlineData ( 40, 10, 'a', "{ C = (40, 10)\n  U = 'a' }")>]
    [<InlineData (System.Math.PI, -3, 'a', "_ToString_")>]
    let ``C -> string`` (cr, ci, i, specified) =
        let ci = C<_>.Apply(Complex(float cr, float ci), i)
        let actual = prettyPrintC ci
        let expected = if specified = "_ToString_" then ci.ToString() else specified
        Assert.Equal(expected, actual)
