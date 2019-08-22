namespace Tests

module Hamiltonian =
    open System.Numerics
    open Encodings
    open Xunit

    [<Theory>]
    [<InlineData(2u, "II - 0.5 IZ + 0.5 XX + 0.5 YY - 0.5 ZI")>]
    [<InlineData(4u, "2.0 IIII - 0.5 IIIZ + 0.5 IIXX + 0.5 IIYY - 0.5 IIZI + 0.5 IXXI + 0.5 IXZX + 0.5 IYYI + 0.5 IYZY - 0.5 IZII + 0.5 XXII + 0.5 XZXI + 0.5 XZZX + 0.5 YYII + 0.5 YZYI + 0.5 YZZY - 0.5 ZIII")>]
    let ``Hamiltonian : Compute Jordan-Wigner string for Hamiltonian``(n, expected) =
        let hamiltonian = computeHamiltonian (fun _ -> Some Complex.One) n
        let actual = hamiltonian.ToString()
        Assert.Equal (expected, actual)

