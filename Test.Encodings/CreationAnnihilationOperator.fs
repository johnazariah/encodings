namespace Tests

module CreationAnnihilationOperator =
    open Encodings
    open Xunit

    [<Theory>]
    [<InlineData(8, 0, "(0.5, 0)XIIIIIII + (0, -0.5)YIIIIIII")>]
    [<InlineData(8, 1, "(0.5, 0)ZXIIIIII + (0, -0.5)ZYIIIIII")>]
    [<InlineData(8, 2, "(0.5, 0)ZZXIIIII + (0, -0.5)ZZYIIIII")>]
    [<InlineData(8, 3, "(0.5, 0)ZZZXIIII + (0, -0.5)ZZZYIIII")>]
    [<InlineData(8, 4, "(0.5, 0)ZZZZXIII + (0, -0.5)ZZZZYIII")>]
    [<InlineData(8, 5, "(0.5, 0)ZZZZZXII + (0, -0.5)ZZZZZYII")>]
    [<InlineData(8, 6, "(0.5, 0)ZZZZZZXI + (0, -0.5)ZZZZZZYI")>]
    [<InlineData(8, 7, "(0.5, 0)ZZZZZZZX + (0, -0.5)ZZZZZZZY")>]
    let ``CreationOperator generates correct JW string``(n : uint32, j : uint32, expected) =
        let actual = (Cr j).ToJordanWignerTerms(n).ToString()
        Assert.Equal(expected, actual)

    [<Theory>]
    [<InlineData(8, 0, "(0.5, 0)XIIIIIII + (0, 0.5)YIIIIIII")>]
    [<InlineData(8, 1, "(0.5, 0)ZXIIIIII + (0, 0.5)ZYIIIIII")>]
    [<InlineData(8, 2, "(0.5, 0)ZZXIIIII + (0, 0.5)ZZYIIIII")>]
    [<InlineData(8, 3, "(0.5, 0)ZZZXIIII + (0, 0.5)ZZZYIIII")>]
    [<InlineData(8, 4, "(0.5, 0)ZZZZXIII + (0, 0.5)ZZZZYIII")>]
    [<InlineData(8, 5, "(0.5, 0)ZZZZZXII + (0, 0.5)ZZZZZYII")>]
    [<InlineData(8, 6, "(0.5, 0)ZZZZZZXI + (0, 0.5)ZZZZZZYI")>]
    [<InlineData(8, 7, "(0.5, 0)ZZZZZZZX + (0, 0.5)ZZZZZZZY")>]
    let ``AnnihilationOperator generates correct JW string``(n : uint32, j : uint32, expected) =
        let actual = (An j).ToJordanWignerTerms(n).ToString()
        Assert.Equal(expected, actual)

    [<Theory>]
    [<InlineData(8, 0, "(0.5, 0)IIIIIIII + (-0.5, 0)ZIIIIIII")>]
    [<InlineData(8, 1, "(0.5, 0)IIIIIIII + (-0.5, 0)IZIIIIII")>]
    [<InlineData(8, 2, "(0.5, 0)IIIIIIII + (-0.5, 0)IIZIIIII")>]
    [<InlineData(8, 3, "(0.5, 0)IIIIIIII + (-0.5, 0)IIIZIIII")>]
    [<InlineData(8, 4, "(0.5, 0)IIIIIIII + (-0.5, 0)IIIIZIII")>]
    [<InlineData(8, 5, "(0.5, 0)IIIIIIII + (-0.5, 0)IIIIIZII")>]
    [<InlineData(8, 6, "(0.5, 0)IIIIIIII + (-0.5, 0)IIIIIIZI")>]
    [<InlineData(8, 7, "(0.5, 0)IIIIIIII + (-0.5, 0)IIIIIIIZ")>]
    let ``NumberOperator generates correct JW string``(n : uint32, j : uint32, expected) =
        let cre = (Cr j).ToJordanWignerTerms(n)
        let anh = (An j).ToJordanWignerTerms(n)
        let num = cre * anh
        let actual = num.ToString()
        Assert.Equal(expected, actual)