namespace Tests

module Terms_PIxOp =
    open System.Collections
    open Encodings
    open Xunit
    open FsCheck.Xunit
    open System.Numerics

    [<Property (Arbitrary = [|typeof<ComplexGenerator>|]) >]
    let ``P constructor properly extracts coefficients from arguments``(args : (uint32 * char * Complex)[]) =
        let cixops = args |> Array.map (fun (index, op, coeff) -> CIxOp<uint32,char>.Apply(coeff, IxOp<uint32, char>.Apply(index, op)))
        let pixop = PIxOp<uint32,char>.Apply(Complex.One, cixops)
        let expectedCoeff = args |> Array.fold (fun result (_, _, coeff) -> result * coeff) Complex.One
        let actualCoeff = pixop.Coeff
        Assert.Equal(expectedCoeff, actualCoeff)

    [<Property (Arbitrary = [|typeof<ComplexGenerator>|]) >]
    let ``Coefficient of the product of two P's is the product of the coefficients`` (left : PIxOp<uint32, char>, right : PIxOp<uint32, char>) =
        let product = left * right
        Assert.Equal (left.Coeff * right.Coeff, product.Coeff)

    [<Property (Arbitrary = [|typeof<ComplexGenerator>|]) >]
    let ``Terms of the product of two P's is the concatenation of the terms`` (left : PIxOp<uint32, char>, right : PIxOp<uint32, char>) =
        let product = left * right
        Assert.Equal<IEnumerable>([| yield! left.IndexedOps; yield! right.IndexedOps|], product.IndexedOps)

    [<Property (Arbitrary = [|typeof<ComplexGenerator>|]) >]
    let ``P constructor preserves terms even when coefficient is Zero``(cixops) =
        let p = PIxOp<uint32, char>.Apply(Complex.Zero, cixops)
        Assert.Equal(Complex.Zero, p.Coeff)
        Assert.Equal(cixops.Length, p.IndexedOps.Length)

    [<Property (Arbitrary = [|typeof<ComplexGenerator>|]) >]
    let ``Reduce returns Zero when Coeff is Zero``(cixops) =
        let p = PIxOp<uint32, char>.Apply(Complex.Zero, cixops)
        Assert.Equal(PIxOp<_,_>.Zero, p.Reduce)
        Assert.Equal(Complex.Zero, p.Reduce.Coeff)
        Assert.Empty(p.Reduce.IndexedOps)

    [<Property (Arbitrary = [|typeof<ComplexGenerator>|]) >]
    let ``Sum of two P's is an S with those terms`` (left : PIxOp<uint32, char>, right : PIxOp<uint32, char>) =
        let sum = left + right
        if (left.IndexedOps.ToString() <> right.IndexedOps.ToString()) then
            Assert.Equal(2, sum.Terms.Length)
        else
            Assert.Equal(1, sum.Terms.Length)
            Assert.Equal(Complex.One, sum.Coeff)
            //let ops = sum.Terms |> Seq.map (fun t -> t.Unapply.ToString())
            //Assert.All([|left.IndexedOps.ToString(); right.IndexedOps.ToString()|], (fun c -> Assert.Contains(c, ops)))