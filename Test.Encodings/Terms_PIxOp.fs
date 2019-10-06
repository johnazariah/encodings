namespace Tests

open System.Collections
open Encodings
open Xunit
open FsCheck.Xunit
open System.Numerics

[<Properties (Arbitrary = [|typeof<ComplexGenerator>|]) >]
module Terms_PIxOp =

    [<Property>]
    let ``P constructor properly extracts coefficients from arguments``(args : (uint32 * char * Complex)[]) =
        let cixops = args |> Array.map (fun (index, op, coeff) -> CIxOp<uint32,CChar>.Apply(coeff, IxOp<uint32, CChar>.Apply(index, CChar.New op)))
        let pixop = PIxOp<uint32,CChar>.Apply(Complex.One, cixops)
        let expectedCoeff = args |> Array.fold (fun result (_, _, coeff) -> result * coeff) Complex.One
        let actualCoeff = pixop.Coeff
        Assert.Equal(expectedCoeff, actualCoeff)

    [<Property>]
    let ``Coefficient of the product of two P's is the product of the coefficients`` (left : PIxOp<uint32, CChar>, right : PIxOp<uint32, CChar>) =
        let product = left <*> right
        Assert.Equal (left.Coeff * right.Coeff, product.Coeff)

    [<Property>]
    let ``Terms of the product of two P's is the concatenation of the terms`` (left : PIxOp<uint32, CChar>, right : PIxOp<uint32, CChar>) =
        let product = left <*> right
        Assert.Equal<IEnumerable>([| yield! left.IndexedOps; yield! right.IndexedOps|], product.IndexedOps)

    [<Property>]
    let ``P constructor preserves terms even when coefficient is Zero``(cixops) =
        let p = PIxOp<uint32, CChar>.Apply(Complex.Zero, cixops)
        Assert.Equal(Complex.Zero, p.Coeff)
        Assert.Equal(cixops.Length, p.IndexedOps.Length)

    [<Property>]
    let ``Reduce returns Zero when Coeff is Zero``(cixops) =
        let p = PIxOp<uint32, CChar>.Apply(Complex.Zero, cixops)
        Assert.Equal(PIxOp<_,_>.Zero, p.Reduce)
        Assert.Equal(Complex.Zero, p.Reduce.Coeff)
        Assert.Empty(p.Reduce.IndexedOps)

    [<Property>]
    let ``P + P -> S`` (left : PIxOp<uint32, CChar>, right : PIxOp<uint32, CChar>) =
        let zeroCount =
            let zero (p : PIxOp<_,_>) = if p.IsZero then 1 else 0
            zero left + zero right
        let degeneracyCount =
            if (not left.IsZero) && (left.Signature = right.Signature) then 1 else 0

        let expectedTermCount =
            2 - zeroCount - degeneracyCount
        let expectedSignatures =
            [|
                if (not left.IsZero)  then yield left.Signature
                if (not right.IsZero) then yield right.Signature
            |]

        (*
        (ProductTerm { Coeff = (0, 0)
         Thunk = [|{ Index = 0u
                    Op = CC { Coeff = (0, 0)
                              Thunk = 'a' } }|] },
         ProductTerm { Coeff = (-1, 1)
         Thunk = [| |] })
        *)

        let sum = left + right
        Assert.Equal(expectedTermCount, sum.Terms.Length)
        Assert.Equal(Complex.One, sum.Coeff)

        let ops = sum.Terms |> Seq.map (fun t -> t.Signature)
        Assert.All(expectedSignatures, (fun c -> Assert.Contains(c, ops)))

    [<Fact>]
    let ``P + P -> S (Regression 1)``() =
        let left  = PIxOp<_,_>.ProductTerm { Coeff = Complex.Zero; Thunk = [| |] }
        let right = PIxOp<_,_>.ProductTerm { Coeff = Complex.Zero; Thunk = [| |] }
        ``P + P -> S`` (left, right)

    [<Fact>]
    let ``P + P -> S (Regression 2)``() =
        let left  = PIxOp<_,_>.ProductTerm { Coeff = Complex.One;  Thunk = [| |] }
        let right = PIxOp<uint32, CChar>.Apply(Complex.Zero, [| CIxOp.Apply(Complex.Zero, IxOp.Apply(0u, CChar.Apply(Complex.Zero, 'a'))) |])
        ``P + P -> S`` (left, right)

    [<Fact>]
    let ``P + P -> S (Regression 3)``() =
        let left  = PIxOp<_,_>.ProductTerm { Coeff = Complex.One;  Thunk = [| |] }
        let right = PIxOp<_,_>.ProductTerm { Coeff = Complex.Zero; Thunk = [| |] }
        ``P + P -> S`` (left, right)

    [<Theory>]
    [<InlineData("[(R,1)|(L,2)]", "R1L2")>]
    [<InlineData("[(R,1)|(L,1)|(R,2)]", "R1L1R2")>]
    [<InlineData("[(R,1)|(R,1)|(L,1)|(L,1)]", "R1R1L1L1")>]
    let ``P Signature is generated correctly``(input, expected) =
        match PIxOpFromString FermionicOperator.FromString input with
        | Some pixop -> Assert.Equal (expected, pixop.Signature)
        | None -> Assert.True (false)

    [<Theory>]
    [<InlineData("[(R,1)|(R,2)]", "[(R,1)|(R,2)]", "[(R,1)|(R,2)|(R,1)|(R,2)]")>]
    [<InlineData("[(L,1)|(L,2)]", "[(L,1)|(L,2)]", "[(L,1)|(L,2)|(L,1)|(L,2)]")>]
    [<InlineData("[(R,1)|(R,2)]", "[(L,1)|(L,2)]", "[(R,1)|(R,2)|(L,1)|(L,2)]")>]
    let ``P * P is computed correctly``(leftStr, rightStr, expected) =
        let left  = PIxOpFromString FermionicOperator.FromString leftStr
        let right = PIxOpFromString FermionicOperator.FromString rightStr
        match (left, right) with
        | Some l, Some r -> Assert.Equal(expected, prettyPrintPIxOp (l <*> r) |> shrinkString)
        | _, _ -> Assert.True (false)

    [<Theory>]
    [<InlineData("[(R,1)|(R,2)]", "[(R,1)|(R,2)]", "{[(R,1)|(R,2)]}")>]
    [<InlineData("[(L,1)|(L,2)]", "[(L,1)|(L,2)]", "{[(L,1)|(L,2)]}")>]
    [<InlineData("[(R,1)|(R,2)]", "[(L,1)|(L,2)]", "{[(L,1)|(L,2)];[(R,1)|(R,2)]}")>]
    let ``P + P is computed correctly``(leftStr, rightStr, expected) =
        let left  = PIxOpFromString FermionicOperator.FromString leftStr
        let right = PIxOpFromString FermionicOperator.FromString rightStr
        match (left, right) with
        | Some l, Some r -> Assert.Equal(expected, prettyPrintSIxOp (l + r) |> shrinkString)
        | _, _ -> Assert.True (false)
