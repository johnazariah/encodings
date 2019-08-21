namespace Tests

module Hamiltonian =
    open System.Numerics
    open Encodings
    open Xunit

    [<Theory>]
    [<InlineData(2u, "")>]
    let ``Hamiltonian : Compute Jordan-Wigner string for Hamiltonian``(n, expected) =
        let hamiltonian = Hamiltonian.ComputeHamiltonian (fun _ -> Some Complex.One) n
        let actual = hamiltonian.ToString()
        Assert.Equal (expected, actual)

