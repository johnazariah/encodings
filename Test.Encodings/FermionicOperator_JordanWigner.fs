namespace Tests

module FermionicOperator_JordanWigner =
    open Encodings
    open Xunit

    [<Theory>]
    [<InlineData(8, 0, "0.5 XIIIIIII - (0.5 i) YIIIIIII")>]
    [<InlineData(8, 1, "0.5 ZXIIIIII - (0.5 i) ZYIIIIII")>]
    [<InlineData(8, 2, "0.5 ZZXIIIII - (0.5 i) ZZYIIIII")>]
    [<InlineData(8, 3, "0.5 ZZZXIIII - (0.5 i) ZZZYIIII")>]
    [<InlineData(8, 4, "0.5 ZZZZXIII - (0.5 i) ZZZZYIII")>]
    [<InlineData(8, 5, "0.5 ZZZZZXII - (0.5 i) ZZZZZYII")>]
    [<InlineData(8, 6, "0.5 ZZZZZZXI - (0.5 i) ZZZZZZYI")>]
    [<InlineData(8, 7, "0.5 ZZZZZZZX - (0.5 i) ZZZZZZZY")>]
    let ``CreationOperator generates correct JW string``(n : uint32, j : uint32, expected) =
        let actual = (Cr, j) |> IndexedFermionicOperator.Apply |> (fun iop -> iop.JordanWignerEncodeToDensePauliTerm (n)) |> Option.get
        Assert.Equal(expected, prettyPrintSR actual)

    [<Theory>]
    [<InlineData(8, 0, "0.5 XIIIIIII + (0.5 i) YIIIIIII")>]
    [<InlineData(8, 1, "0.5 ZXIIIIII + (0.5 i) ZYIIIIII")>]
    [<InlineData(8, 2, "0.5 ZZXIIIII + (0.5 i) ZZYIIIII")>]
    [<InlineData(8, 3, "0.5 ZZZXIIII + (0.5 i) ZZZYIIII")>]
    [<InlineData(8, 4, "0.5 ZZZZXIII + (0.5 i) ZZZZYIII")>]
    [<InlineData(8, 5, "0.5 ZZZZZXII + (0.5 i) ZZZZZYII")>]
    [<InlineData(8, 6, "0.5 ZZZZZZXI + (0.5 i) ZZZZZZYI")>]
    [<InlineData(8, 7, "0.5 ZZZZZZZX + (0.5 i) ZZZZZZZY")>]
    let ``AnnihilationOperator generates correct JW string``(n : uint32, j : uint32, expected) =
        let actual = (An, j) |> IndexedFermionicOperator.Apply |> (fun iop -> iop.JordanWignerEncodeToDensePauliTerm (n)) |> Option.get
        Assert.Equal(expected, prettyPrintSR actual)

    [<Theory>]
    [<InlineData(8, 0, "0.5 IIIIIIII - 0.5 ZIIIIIII")>]
    [<InlineData(8, 1, "0.5 IIIIIIII - 0.5 IZIIIIII")>]
    [<InlineData(8, 2, "0.5 IIIIIIII - 0.5 IIZIIIII")>]
    [<InlineData(8, 3, "0.5 IIIIIIII - 0.5 IIIZIIII")>]
    [<InlineData(8, 4, "0.5 IIIIIIII - 0.5 IIIIZIII")>]
    [<InlineData(8, 5, "0.5 IIIIIIII - 0.5 IIIIIZII")>]
    [<InlineData(8, 6, "0.5 IIIIIIII - 0.5 IIIIIIZI")>]
    [<InlineData(8, 7, "0.5 IIIIIIII - 0.5 IIIIIIIZ")>]
    let ``NumberOperator generates correct JW string``(n : uint32, j : uint32, expected) =
        let cre = (Cr, j) |> IndexedFermionicOperator.Apply |> (fun iop -> iop.JordanWignerEncodeToDensePauliTerm (n)) |> Option.get
        let anh = (An, j) |> IndexedFermionicOperator.Apply |> (fun iop -> iop.JordanWignerEncodeToDensePauliTerm (n)) |> Option.get
        let num = cre * anh
        let actual = prettyPrintSR num
        Assert.Equal(expected, actual)