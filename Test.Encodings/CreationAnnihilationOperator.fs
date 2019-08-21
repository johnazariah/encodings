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

