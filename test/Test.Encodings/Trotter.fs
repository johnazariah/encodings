namespace Tests

module Trotter =
    open System.Numerics
    open Encodings
    open Encodings.Trotterization
    open Xunit

    let private prs (terms : (string * Complex) list) =
        terms
        |> List.map (fun (ops, coeff) -> PauliRegister(ops, coeff))
        |> List.toArray
        |> PauliRegisterSequence

    let private c x = Complex(x, 0.0)

    // ── Trotter decomposition ───────────────────────────────────────

    [<Fact>]
    let ``first-order of single term produces one rotation`` () =
        let h = prs [ ("ZI", c 1.0) ]
        let step = firstOrderTrotter 0.1 h
        Assert.Equal(1, step.Rotations.Length)
        Assert.Equal(First, step.Order)
        Assert.Equal(0.1, step.TimeStep)

    [<Fact>]
    let ``second-order of single term produces two rotations`` () =
        let h = prs [ ("ZI", c 1.0) ]
        let step = secondOrderTrotter 0.1 h
        Assert.Equal(2, step.Rotations.Length)
        Assert.Equal(Second, step.Order)

    [<Fact>]
    let ``rotation angle equals coefficient times step size`` () =
        let h = prs [ ("ZI", c 2.0) ]
        let step = firstOrderTrotter 0.5 h
        Assert.Equal(1.0, step.Rotations.[0].Angle)

    [<Fact>]
    let ``second-order uses half-angles`` () =
        let h = prs [ ("ZI", c 4.0) ]
        let step = secondOrderTrotter 1.0 h
        Assert.Equal(2.0, step.Rotations.[0].Angle)

    [<Fact>]
    let ``second-order step is palindromic`` () =
        let h = prs [ ("XZ", c 1.0); ("YY", c -0.5); ("ZI", c 0.3) ]
        let step = secondOrderTrotter 0.1 h
        let n = step.Rotations.Length
        for i in 0 .. n / 2 - 1 do
            let fwd = step.Rotations.[i]
            let rev = step.Rotations.[n - 1 - i]
            Assert.Equal(fwd.Operator.Signature, rev.Operator.Signature)
            Assert.Equal(fwd.Angle, rev.Angle)

    [<Fact>]
    let ``second-order has twice the rotations of first-order`` () =
        let h = prs [ ("XZ", c 1.0); ("YY", c -0.5) ]
        let first  = firstOrderTrotter 0.1 h
        let second = secondOrderTrotter 0.1 h
        Assert.Equal(first.Rotations.Length * 2, second.Rotations.Length)

    [<Fact>]
    let ``trotterize dispatches correctly`` () =
        let h = prs [ ("ZI", c 1.0) ]
        let s1 = trotterize First 0.1 h
        let s2 = trotterize Second 0.1 h
        Assert.Equal(First, s1.Order)
        Assert.Equal(Second, s2.Order)

    [<Fact>]
    let ``rotation operator has unit coefficient`` () =
        let h = prs [ ("XZ", c 3.5) ]
        let step = firstOrderTrotter 0.1 h
        Assert.Equal(Complex.One, step.Rotations.[0].Operator.Coefficient)

    // ── Gate decomposition ──────────────────────────────────────────

    [<Fact>]
    let ``identity term produces no gates`` () =
        let r = { Operator = PauliRegister("II", Complex.One); Angle = 0.5 }
        Assert.Empty(decomposeRotation r)

    [<Fact>]
    let ``weight-1 Z produces 0 CNOTs and 1 Rz`` () =
        let r = { Operator = PauliRegister("ZI", Complex.One); Angle = 0.5 }
        let gates = decomposeRotation r
        let cnots = gates |> Array.filter (function Gate.CNOT _ -> true | _ -> false)
        let rzs   = gates |> Array.filter (function Gate.Rz _   -> true | _ -> false)
        Assert.Equal(0, cnots.Length)
        Assert.Equal(1, rzs.Length)

    [<Fact>]
    let ``weight-2 produces 2 CNOTs`` () =
        let r = { Operator = PauliRegister("XZ", Complex.One); Angle = 0.5 }
        let gates = decomposeRotation r
        let cnots = gates |> Array.filter (function Gate.CNOT _ -> true | _ -> false)
        Assert.Equal(2, cnots.Length)

    [<Theory>]
    [<InlineData(1, 0)>]
    [<InlineData(2, 2)>]
    [<InlineData(3, 4)>]
    [<InlineData(4, 6)>]
    let ``weight-w produces 2(w-1) CNOTs`` (w : int, expected : int) =
        let paulis = String.replicate w "X" + String.replicate (4 - w) "I"
        let r = { Operator = PauliRegister(paulis, Complex.One); Angle = 0.1 }
        let cnots =
            decomposeRotation r
            |> Array.filter (function Gate.CNOT _ -> true | _ -> false)
        Assert.Equal(expected, cnots.Length)

    [<Fact>]
    let ``X position gets H basis change`` () =
        let r = { Operator = PauliRegister("XI", Complex.One); Angle = 0.5 }
        let gates = decomposeRotation r
        Assert.Equal(Gate.H 0, gates.[0])

    [<Fact>]
    let ``Y position gets Sdg then H basis change`` () =
        let r = { Operator = PauliRegister("YI", Complex.One); Angle = 0.5 }
        let gates = decomposeRotation r
        Assert.Equal(Gate.Sdg 0, gates.[0])
        Assert.Equal(Gate.H 0,   gates.[1])

    [<Fact>]
    let ``Rz angle is twice the rotation angle`` () =
        let r = { Operator = PauliRegister("ZI", Complex.One); Angle = 0.25 }
        let gates = decomposeRotation r
        match gates |> Array.find (function Gate.Rz _ -> true | _ -> false) with
        | Gate.Rz(_, angle) -> Assert.Equal(0.5, angle)
        | _ -> failwith "unreachable"

    // ── Cost analysis ───────────────────────────────────────────────

    [<Fact>]
    let ``trotterCnotCount matches decomposed count`` () =
        let h = prs [ ("XZ", c 1.0); ("YY", c -0.5); ("ZI", c 0.3) ]
        let step = firstOrderTrotter 0.1 h
        let quick  = trotterCnotCount step
        let actual =
            decomposeTrotterStep step
            |> Array.filter (function Gate.CNOT _ -> true | _ -> false)
            |> Array.length
        Assert.Equal(actual, quick)

    [<Fact>]
    let ``trotterStepStats reports correct rotation count`` () =
        let h = prs [ ("XZ", c 1.0); ("ZI", c -0.5) ]
        let stats = firstOrderTrotter 0.1 h |> trotterStepStats
        Assert.Equal(2, stats.RotationCount)

    [<Fact>]
    let ``trotterStepStats reports correct CNOT count`` () =
        let h = prs [ ("XZ", c 1.0); ("ZI", c -0.5) ]
        let stats = firstOrderTrotter 0.1 h |> trotterStepStats
        // XZ weight 2 → 2 CNOTs; ZI weight 1 → 0 CNOTs
        Assert.Equal(2, stats.CnotCount)

    [<Fact>]
    let ``trotterStepStats reports correct max weight`` () =
        let h = prs [ ("XZYI", c 1.0); ("ZIII", c -0.5) ]
        let stats = firstOrderTrotter 0.1 h |> trotterStepStats
        Assert.Equal(3, stats.MaxWeight)

    [<Fact>]
    let ``pauliWeight counts non-identity positions`` () =
        Assert.Equal(0, pauliWeight (PauliRegister("IIII", Complex.One)))
        Assert.Equal(1, pauliWeight (PauliRegister("ZIII", Complex.One)))
        Assert.Equal(3, pauliWeight (PauliRegister("XZYI", Complex.One)))
        Assert.Equal(4, pauliWeight (PauliRegister("XYZX", Complex.One)))

    [<Fact>]
    let ``compareTrotterCosts returns one entry per encoding`` () =
        let h1 = prs [ ("XZ", c 1.0) ]
        let h2 = prs [ ("YY", c -0.5) ]
        let results = compareTrotterCosts [| ("enc1", h1); ("enc2", h2) |] 0.1
        Assert.Equal(2, results.Length)
        Assert.Equal("enc1", fst results.[0])
        Assert.Equal("enc2", fst results.[1])
