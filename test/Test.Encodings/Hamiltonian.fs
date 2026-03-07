namespace Tests

module Hamiltonian =
    open System.Numerics
    open Encodings
    open Encodings.Hamiltonian
    open Encodings.JordanWigner
    open Encodings.BravyiKitaev
    open Encodings.TreeEncoding
    open Xunit

    [<Theory>]
    [<InlineData(2u, "II - 0.5 IZ + 0.5 XX + 0.5 YY - 0.5 ZI")>]
    [<InlineData(4u, "2.0 IIII - 0.5 IIIZ + 0.5 IIXX + 0.5 IIYY - 0.5 IIZI + 0.5 IXXI + 0.5 IXZX + 0.5 IYYI + 0.5 IYZY - 0.5 IZII + 0.5 XXII + 0.5 XZXI + 0.5 XZZX + 0.5 YYII + 0.5 YZYI + 0.5 YZZY - 0.5 ZIII")>]
    let ``Hamiltonian : Compute Jordan-Wigner string for Hamiltonian``(n, expected) =
        let hamiltonian = computeHamiltonian (fun _ -> Some Complex.One) n
        let actual = hamiltonian.ToString()
        Assert.Equal (expected, actual)

    [<Fact>]
    let ``Hamiltonian : computeHamiltonianWith matches computeHamiltonian for Jordan-Wigner`` () =
        let coefficientFactory _ = Some Complex.One
        let withGeneric = computeHamiltonianWith jordanWignerTerms coefficientFactory 2u
        let withJw = computeHamiltonian coefficientFactory 2u

        Assert.Equal(withJw.ToString(), withGeneric.ToString())

    [<Fact>]
    let ``Hamiltonian : missing coefficients produce empty sequence`` () =
        let hamiltonian = computeHamiltonianWith jordanWignerTerms (fun _ -> None) 3u
        Assert.Empty(hamiltonian.SummandTerms)

    // ── Parallel API ──────────────────────────────────────────────────

    [<Theory>]
    [<InlineData(2u)>]
    [<InlineData(4u)>]
    let ``Hamiltonian : parallel matches sequential for Jordan-Wigner`` (n : uint32) =
        let factory _ = Some Complex.One
        let sequential = computeHamiltonianWith jordanWignerTerms factory n
        let parallel'  = computeHamiltonianWithParallel jordanWignerTerms factory n
        Assert.Equal(sequential.ToString(), parallel'.ToString())

    [<Theory>]
    [<InlineData(2u)>]
    [<InlineData(4u)>]
    let ``Hamiltonian : parallel matches sequential for Bravyi-Kitaev`` (n : uint32) =
        let factory _ = Some Complex.One
        let sequential = computeHamiltonianWith bravyiKitaevTerms factory n
        let parallel'  = computeHamiltonianWithParallel bravyiKitaevTerms factory n
        Assert.Equal(sequential.ToString(), parallel'.ToString())

    [<Fact>]
    let ``Hamiltonian : computeHamiltonianParallel matches computeHamiltonian`` () =
        let factory _ = Some Complex.One
        let sequential = computeHamiltonian factory 2u
        let parallel'  = computeHamiltonianParallel factory 2u
        Assert.Equal(sequential.ToString(), parallel'.ToString())

    [<Fact>]
    let ``Hamiltonian : parallel with missing coefficients produces empty sequence`` () =
        let hamiltonian = computeHamiltonianWithParallel jordanWignerTerms (fun _ -> None) 3u
        Assert.Empty(hamiltonian.SummandTerms)

    [<Theory>]
    [<InlineData(2u)>]
    [<InlineData(4u)>]
    let ``Hamiltonian : parallel with sparse coefficients matches sequential`` (n : uint32) =
        let factory key =
            if key = "0,1" || key = "1,0" || key = "0,1,1,0" then Some Complex.One
            else None
        let sequential = computeHamiltonianWith jordanWignerTerms factory n
        let parallel'  = computeHamiltonianWithParallel jordanWignerTerms factory n
        Assert.Equal(sequential.ToString(), parallel'.ToString())

    // ── Skeleton API ──────────────────────────────────────────────────

    [<Theory>]
    [<InlineData(2u)>]
    [<InlineData(4u)>]
    let ``Hamiltonian : skeleton + apply matches sequential for Jordan-Wigner`` (n : uint32) =
        let factory _ = Some Complex.One
        let sequential = computeHamiltonianWith jordanWignerTerms factory n
        let skeleton   = computeHamiltonianSkeleton jordanWignerTerms n
        let fromSkel   = applyCoefficients skeleton factory
        Assert.Equal(sequential.ToString(), fromSkel.ToString())

    [<Theory>]
    [<InlineData(2u)>]
    [<InlineData(4u)>]
    let ``Hamiltonian : skeleton + apply matches sequential for Bravyi-Kitaev`` (n : uint32) =
        let factory _ = Some Complex.One
        let sequential = computeHamiltonianWith bravyiKitaevTerms factory n
        let skeleton   = computeHamiltonianSkeleton bravyiKitaevTerms n
        let fromSkel   = applyCoefficients skeleton factory
        Assert.Equal(sequential.ToString(), fromSkel.ToString())

    [<Theory>]
    [<InlineData(2u)>]
    [<InlineData(4u)>]
    let ``Hamiltonian : skeleton + apply matches sequential for ternary tree`` (n : uint32) =
        let factory _ = Some Complex.One
        let sequential = computeHamiltonianWith ternaryTreeTerms factory n
        let skeleton   = computeHamiltonianSkeleton ternaryTreeTerms n
        let fromSkel   = applyCoefficients skeleton factory
        Assert.Equal(sequential.ToString(), fromSkel.ToString())

    [<Fact>]
    let ``Hamiltonian : skeleton with missing coefficients produces empty sequence`` () =
        let skeleton = computeHamiltonianSkeleton jordanWignerTerms 3u
        let result   = applyCoefficients skeleton (fun _ -> None)
        Assert.Empty(result.SummandTerms)

    [<Fact>]
    let ``Hamiltonian : skeleton reused with different coefficients`` () =
        let skeleton = computeHamiltonianSkeleton jordanWignerTerms 2u

        let factory1 _ = Some Complex.One
        let factory2 key =
            if key = "0,1" || key = "1,0" then Some Complex.One else None

        let ham1 = applyCoefficients skeleton factory1
        let ham2 = applyCoefficients skeleton factory2

        // Different coefficients ⟹ different Hamiltonians
        Assert.NotEqual<string>(ham1.ToString(), ham2.ToString())
        // But reusing the same skeleton gives the same result each time
        let ham1b = applyCoefficients skeleton factory1
        Assert.Equal(ham1.ToString(), ham1b.ToString())

    [<Theory>]
    [<InlineData(2u)>]
    [<InlineData(4u)>]
    let ``Hamiltonian : skeleton with sparse coefficients matches sequential`` (n : uint32) =
        let factory key =
            if key = "0,1" || key = "1,0" || key = "0,1,1,0" then Some Complex.One
            else None
        let sequential = computeHamiltonianWith jordanWignerTerms factory n
        let skeleton   = computeHamiltonianSkeleton jordanWignerTerms n
        let fromSkel   = applyCoefficients skeleton factory
        Assert.Equal(sequential.ToString(), fromSkel.ToString())

    [<Fact>]
    let ``Hamiltonian : skeleton has expected structure for n=2 JW`` () =
        let skeleton = computeHamiltonianSkeleton jordanWignerTerms 2u
        // One-body: 4 possible (i,j) pairs for n=2, but only those
        // producing non-empty Pauli strings are stored
        Assert.True(skeleton.OneBody.Length > 0)
        Assert.True(skeleton.OneBody.Length <= 4)
        Assert.Equal(2u, skeleton.NumQubits)
        // Every entry should have a non-empty Terms array
        for entry in skeleton.OneBody do
            Assert.NotEmpty(entry.Terms)
        for entry in skeleton.TwoBody do
            Assert.NotEmpty(entry.Terms)

    // ── Sparse Skeleton API ───────────────────────────────────────────

    [<Theory>]
    [<InlineData(2u)>]
    [<InlineData(4u)>]
    let ``Hamiltonian : sparse skeleton matches full skeleton for all-ones factory`` (n : uint32) =
        let factory _ = Some Complex.One
        let fullSkel   = computeHamiltonianSkeleton jordanWignerTerms n
        let sparseSkel = computeHamiltonianSkeletonFor jordanWignerTerms factory n
        let hamFull   = applyCoefficients fullSkel factory
        let hamSparse = applyCoefficients sparseSkel factory
        Assert.Equal(hamFull.ToString(), hamSparse.ToString())

    [<Theory>]
    [<InlineData(2u)>]
    [<InlineData(4u)>]
    let ``Hamiltonian : sparse skeleton matches sequential for Jordan-Wigner`` (n : uint32) =
        let factory key =
            if key = "0,1" || key = "1,0" || key = "0,1,1,0" then Some Complex.One
            else None
        let sequential = computeHamiltonianWith jordanWignerTerms factory n
        let skeleton   = computeHamiltonianSkeletonFor jordanWignerTerms factory n
        let fromSkel   = applyCoefficients skeleton factory
        Assert.Equal(sequential.ToString(), fromSkel.ToString())

    [<Fact>]
    let ``Hamiltonian : sparse skeleton has fewer entries than full skeleton`` () =
        let sparseFactory key =
            if key = "0,1" || key = "1,0" then Some Complex.One else None
        let fullSkel   = computeHamiltonianSkeleton jordanWignerTerms 4u
        let sparseSkel = computeHamiltonianSkeletonFor jordanWignerTerms sparseFactory 4u
        Assert.True(sparseSkel.OneBody.Length < fullSkel.OneBody.Length)
        // Sparse should only have entries for the provided keys
        Assert.True(sparseSkel.TwoBody.Length = 0)
