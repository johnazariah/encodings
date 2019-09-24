namespace Tests

module Terms_C =
    open Encodings
    open Xunit
    open FsCheck.Xunit
    open System.Numerics

    [<Property (Arbitrary = [|typeof<ComplexGenerator>|]) >]
    let ``IsZero true only when coeff is zero`` (c : Complex) (i : int) =
        let u = C<_>.Apply (c, i)
        Assert.Equal(c.Reduce, u.Coeff.Reduce)

        if (c = Complex.Zero) then
            Assert.True(u.IsZero)
        else
            Assert.False(u.IsZero)

        let v = C<_>.Apply (Complex.Zero, i)
        Assert.Equal(Complex.Zero, v.Coeff)
        Assert.True(v.IsZero)

    [<Property (Arbitrary = [|typeof<ComplexGenerator>|]) >]
    let ``Normalize sets coeff to One`` (c : C<int>) =
        if (c.Coeff <> Complex.One) then
            Assert.False(c.Coeff = Complex.One)
            Assert.True(c.Normalize.Coeff = Complex.One)
        else
            Assert.True(c.Coeff = Complex.One)
            Assert.True(c.Normalize.Coeff = Complex.One)

    [<Property (Arbitrary = [|typeof<ComplexGenerator>|]) >]
    let ``Negate negates coeff`` (c : C<int>) =
        Assert.Equal(-(c.Coeff.Reduce), (-c).Reduce.Coeff)

    [<Property (Arbitrary = [|typeof<ComplexGenerator>|]) >]
    let ``ScaleCoefficient multiplies coefficient`` (s : Complex) (c : C<int>) =
        Assert.Equal((c.Coeff * s), (c.ScaleCoefficient s).Coeff)

    [<Property (Arbitrary = [|typeof<ComplexGenerator>|]) >]
    let ``AddCoefficient adds coefficient`` (s : Complex) (c : C<int>) =
        Assert.Equal((c.Coeff + s), (c.AddCoefficient s).Coeff)

    [<Property>]
    let ``C <- 'unit``(i : int) =
        let actual = (curry C<_>.Apply Complex.One) i
        Assert.Equal(Complex.One, actual.Coeff)
        Assert.Equal(i, actual.Item)

    [<Property (Arbitrary = [|typeof<ComplexGenerator>|]) >]
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
    [<InlineData ( 41, 10, 'a', "{ Coeff = (41, 10)\n  Item = 'a' }")>]
    [<InlineData ( 40, 10, 'a', "{ Coeff = (40, 10)\n  Item = 'a' }")>]
    [<InlineData (System.Math.PI, -3, 'a', "_ToString_")>]
    let ``C -> string`` (cr, ci, i, specified) =
        let ci = C<_>.Apply(Complex(float cr, float ci), i)
        let actual = prettyPrintC ci
        let expected = if specified = "_ToString_" then ci.ToString() else specified
        Assert.Equal(expected, actual)
