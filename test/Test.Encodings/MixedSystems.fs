namespace Tests

module MixedSystems =
    open Encodings
    open Xunit
    open System.Numerics

    let private mkProduct (units : IxOp<uint32, SectorLadderOperatorUnit>[]) =
        units |> P<IxOp<uint32, SectorLadderOperatorUnit>>.Apply

    let private toOpPairs (productTerm : P<IxOp<uint32, SectorLadderOperatorUnit>>) =
        productTerm.Units |> Array.map (fun c -> (c.Item.Op.Sector, c.Item.Op.Operator, c.Item.Index))

    [<Fact>]
    let ``Sector block ordering moves bosons right without changing coefficient`` () =
        let input =
            mkProduct
                [|
                    boson Lower 10u
                    fermion Raise 1u
                    boson Raise 12u
                |]

        let blocked = toSectorBlockOrder input

        Assert.Equal(Complex.One, blocked.Coeff)
        Assert.Equal<(ParticleSector * LadderOperatorUnit * uint32)[]>(
            [|
                (Fermionic, Raise, 1u)
                (Bosonic, Lower, 10u)
                (Bosonic, Raise, 12u)
            |],
            toOpPairs blocked)

    [<Fact>]
    let ``Mixed normal ordering applies CAR and CCR within sectors`` () =
        let candidate =
            mkProduct
                [|
                    fermion Lower 3u
                    boson Lower 10u
                    fermion Raise 1u
                    boson Raise 10u
                |]
            |> S<IxOp<uint32, SectorLadderOperatorUnit>>.Apply

        let ordered = constructMixedNormalOrdered candidate
        let terms = ordered.Value.ProductTerms.Value

        Assert.Equal(2, terms.Length)

        let reduced = terms |> Array.map (fun t -> t.Reduce.Value)

        reduced
        |> Array.iter (fun t -> Assert.Equal(Complex.MinusOne, t.Coeff))

        let hasIdentityBosonTerm =
            reduced
            |> Array.exists (fun t ->
                toOpPairs t
                |> Array.exists (fun (sector, op, _) -> sector = Bosonic && op = Identity))

        let hasReorderedBosonTerm =
            reduced
            |> Array.exists (fun t ->
                let ops = toOpPairs t
                ops |> Array.exists (fun (sector, op, idx) -> sector = Bosonic && op = Raise && idx = 10u) &&
                ops |> Array.exists (fun (sector, op, idx) -> sector = Bosonic && op = Lower && idx = 10u))

        Assert.True(hasIdentityBosonTerm)
        Assert.True(hasReorderedBosonTerm)

        reduced
        |> Array.iter (fun t ->
            Assert.True(isSectorBlockOrdered t))
