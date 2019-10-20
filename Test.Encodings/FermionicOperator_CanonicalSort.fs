namespace Tests

open FsCheck.Xunit
open System.Numerics
open Encodings.Operators
open Encodings.Operators.FermionicOperator_Order
open Encodings.SparseRepresentation
open Encodings.TypeExtensions
open Xunit

[<Properties (Arbitrary = [|typeof<ComplexGenerator>|], QuietOnSuccess = true) >]
module FermionicOperator_CanonicalSort =

    let ``FindNextIndex finds the next index`` apply compare indices =
        let ixops =
            indices
            |> Array.map apply

        let expected =
            indices
            |> Array.fold
                (fun (index, result) curr ->
                    let result' =
                        match result with
                        | None -> Some (curr, index)
                        | Some (r, _) -> if compare curr r then Some (curr, index) else result
                    (index + 1, result'))
                (0, None)
            |> snd

        let actual =
            ixops
            |> findNextIndex
            |> Option.map (fun (ixop, index) -> (ixop.Index, index))

        Assert.Equal (expected, actual)

    [<Property>]
    let ``FindNextIndex finds the smallest Cr as the next index`` (indices : uint32[]) =
        ``FindNextIndex finds the next index``
            (fun ix -> IxOp<_,_>.Apply(ix, FermionicOperator.Cr))
            (<=)
            indices

    [<Property>]
    let ``FindNextIndex finds the largest An as the next index`` (indices : uint32[]) =
        ``FindNextIndex finds the next index``
            (fun ix -> IxOp<_,_>.Apply(ix, FermionicOperator.An))
            (>=)
            indices

    [<Property>]
    let ``FindNextIndex finds Cr before An as the next index`` (indices : uint32[]) =
        let ixops =
            [|
                yield! indices |> Array.map (fun ix -> IxOp<_,_>.Apply(ix, FermionicOperator.An))
                yield IxOp<_,_>.Apply(27u, FermionicOperator.Cr)
            |]

        let expected = Some (27u, indices.Length)
        let actual =
            ixops
            |> findNextIndex
            |> Option.map (fun (ixop, index) -> (ixop.Index, index))

        Assert.Equal (expected, actual)

    [<Property>]
    let ``BringToFront brings the desired item to the front`` (ixops : IxOp<uint32, FermionicOperator>[]) =
        ixops
        |> findNextIndex
        |> Option.iter
            (fun (expectedItem, expectedIndex) ->
                let swapped = ixops |> bringToFront expectedIndex
                let expectedCoeff = if expectedIndex % 2 = 0 then Complex.One else Complex.MinusOne
                Assert.Equal(expectedCoeff, swapped.Coeff)
                Assert.Equal(expectedItem, swapped.Thunk.[0]))

    [<Fact>]
    let ``BringToFront brings the desired item to the front (Regression 1)`` () =
        let ixops =
            [|
                IxOp<_,_>.Apply (0u, Cr)
            |]
        ``BringToFront brings the desired item to the front`` ixops