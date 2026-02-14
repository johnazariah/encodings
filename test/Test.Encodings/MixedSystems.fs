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

    [<Fact>]
    let ``Sector block ordering detects invalid boson-before-fermion sequence`` () =
        let invalidOrder =
            mkProduct
                [|
                    boson Raise 20u
                    fermion Lower 2u
                |]

        Assert.False(isSectorBlockOrdered invalidOrder)

    [<Fact>]
    let ``Mixed normal ordering handles fermion-only candidate`` () =
        let fermionOnly =
            mkProduct
                [|
                    fermion Lower 3u
                    fermion Raise 1u
                |]
            |> S<IxOp<uint32, SectorLadderOperatorUnit>>.Apply

        let ordered = constructMixedNormalOrdered fermionOnly
        let terms = ordered.Value.ProductTerms.Value |> Array.map (fun t -> t.Reduce.Value)

        Assert.NotEmpty(terms)
        terms
        |> Array.iter (fun t ->
            Assert.True(isSectorBlockOrdered t)
            let bosonCount = t.Units |> Array.filter (fun c -> c.Item.Op.Sector = Bosonic) |> Array.length
            Assert.True(bosonCount = 0 || t.Units |> Array.exists (fun c -> c.Item.Op.Operator = Identity)))

    [<Fact>]
    let ``Mixed normal ordering handles boson-only candidate`` () =
        let bosonOnly =
            mkProduct
                [|
                    boson Lower 8u
                    boson Raise 8u
                |]
            |> S<IxOp<uint32, SectorLadderOperatorUnit>>.Apply

        let ordered = constructMixedNormalOrdered bosonOnly
        let terms = ordered.Value.ProductTerms.Value |> Array.map (fun t -> t.Reduce.Value)

        Assert.Equal(2, terms.Length)
        terms
        |> Array.iter (fun t ->
            Assert.True(isSectorBlockOrdered t)
            let fermionCount = t.Units |> Array.filter (fun c -> c.Item.Op.Sector = Fermionic) |> Array.length
            Assert.True(fermionCount = 0 || t.Units |> Array.exists (fun c -> c.Item.Op.Operator = Identity)))
