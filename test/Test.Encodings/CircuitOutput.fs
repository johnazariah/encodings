namespace Tests

module CircuitOutput =
    open System.Numerics
    open System.Text.Json
    open Encodings
    open Encodings.Trotterization
    open Encodings.CircuitOutput
    open Xunit

    // ── Helpers ─────────────────────────────────────────────────────

    let private prs (terms : (string * Complex) list) =
        terms
        |> List.map (fun (ops, coeff) -> PauliRegister(ops, coeff))
        |> List.toArray
        |> PauliRegisterSequence

    let private c x = Complex(x, 0.0)

    // ── OpenQASM: single gates ──────────────────────────────────────

    [<Fact>]
    let ``QASM: H gate renders correctly`` () =
        let qasm = toOpenQasm { defaultOpenQasmOptions with IncludeHeader = false } 2 [| Gate.H 0 |]
        Assert.Contains("h q[0];", qasm)

    [<Fact>]
    let ``QASM: CNOT gate renders correctly`` () =
        let qasm = toOpenQasm { defaultOpenQasmOptions with IncludeHeader = false } 3 [| Gate.CNOT(0, 2) |]
        Assert.Contains("cx q[0], q[2];", qasm)

    [<Fact>]
    let ``QASM: Rz gate respects precision`` () =
        let opts = { defaultOpenQasmOptions with IncludeHeader = false; Precision = 3 }
        let qasm = toOpenQasm opts 1 [| Gate.Rz(0, 1.23456789) |]
        Assert.Contains("rz(1.235)", qasm)
        Assert.DoesNotContain("1.234567", qasm)

    // ── OpenQASM: full output ───────────────────────────────────────

    [<Fact>]
    let ``QASM: full output includes header and qubit declaration`` () =
        let gates = [| Gate.H 0; Gate.CNOT(0, 1); Gate.Rz(1, 0.5) |]
        let qasm = toOpenQasm defaultOpenQasmOptions 2 gates
        Assert.Contains("OPENQASM 3.0;", qasm)
        Assert.Contains("include \"stdgates.inc\";", qasm)
        Assert.Contains("qubit[2] q;", qasm)
        Assert.Contains("h q[0];", qasm)
        Assert.Contains("cx q[0], q[1];", qasm)

    [<Fact>]
    let ``QASM: header can be omitted`` () =
        let opts = { defaultOpenQasmOptions with IncludeHeader = false }
        let qasm = toOpenQasm opts 2 [| Gate.H 0 |]
        Assert.DoesNotContain("OPENQASM", qasm)
        Assert.DoesNotContain("include", qasm)
        Assert.Contains("qubit[2] q;", qasm)

    // ── Q# output ───────────────────────────────────────────────────

    [<Fact>]
    let ``QSharp: output has correct namespace and operation structure`` () =
        let qs = toQSharp defaultQSharpOptions 2 [| Gate.H 0 |]
        Assert.Contains("namespace FockMap.Generated {", qs)
        Assert.Contains("open Microsoft.Quantum.Intrinsic;", qs)
        Assert.Contains("operation TrotterStep(qs : Qubit[]) : Unit is Adj + Ctl {", qs)

    [<Fact>]
    let ``QSharp: gate mapping is correct`` () =
        let gates = [| Gate.H 0; Gate.S 1; Gate.Sdg 2; Gate.Rz(0, 0.5); Gate.CNOT(0, 1) |]
        let qs = toQSharp defaultQSharpOptions 3 gates
        Assert.Contains("H(qs[0]);", qs)
        Assert.Contains("S(qs[1]);", qs)
        Assert.Contains("Adjoint S(qs[2]);", qs)
        Assert.Contains("Rz(0.500000, qs[0]);", qs)
        Assert.Contains("CNOT(qs[0], qs[1]);", qs)

    // ── JSON output ─────────────────────────────────────────────────

    [<Fact>]
    let ``JSON: output has correct numQubits and gate count`` () =
        let gates = [| Gate.H 0; Gate.CNOT(0, 1); Gate.Rz(1, 0.5) |]
        let json = toCircuitJson 2 Map.empty gates
        let doc = JsonDocument.Parse(json)
        let root = doc.RootElement
        Assert.Equal(2, root.GetProperty("numQubits").GetInt32())
        Assert.Equal(3, root.GetProperty("gateCount").GetInt32())
        Assert.Equal(3, root.GetProperty("gates").GetArrayLength())

    [<Fact>]
    let ``JSON: output includes metadata`` () =
        let meta = Map.ofList [ ("encoding", "JordanWigner"); ("dt", "0.1") ]
        let json = toCircuitJson 2 meta [| Gate.H 0 |]
        let doc = JsonDocument.Parse(json)
        let md = doc.RootElement.GetProperty("metadata")
        Assert.Equal("JordanWigner", md.GetProperty("encoding").GetString())
        Assert.Equal("0.1", md.GetProperty("dt").GetString())

    // ── TrotterStep convenience functions ───────────────────────────

    [<Fact>]
    let ``trotterStepToOpenQasm produces valid QASM`` () =
        let h = prs [ ("XZ", c 1.0) ]
        let step = firstOrderTrotter 0.1 h
        let qasm = trotterStepToOpenQasm defaultOpenQasmOptions step
        Assert.Contains("OPENQASM 3.0;", qasm)
        Assert.Contains("h q[", qasm)
        Assert.Contains("cx q[", qasm)
        Assert.Contains("rz(", qasm)

    [<Fact>]
    let ``trotterStepToQSharp produces valid Q# operation`` () =
        let h = prs [ ("XZ", c 1.0) ]
        let step = firstOrderTrotter 0.1 h
        let qs = trotterStepToQSharp defaultQSharpOptions step
        Assert.Contains("namespace FockMap.Generated {", qs)
        Assert.Contains("H(qs[", qs)
        Assert.Contains("CNOT(qs[", qs)
        Assert.Contains("Rz(", qs)

    // ── JSON: individual gate types ─────────────────────────────────

    [<Fact>]
    let ``JSON: S gate serializes correctly`` () =
        let json = toCircuitJson 2 Map.empty [| Gate.S 0 |]
        let doc = JsonDocument.Parse(json)
        let gate = doc.RootElement.GetProperty("gates").[0]
        Assert.Equal("S", gate.GetProperty("gate").GetString())

    [<Fact>]
    let ``JSON: Sdg gate serializes correctly`` () =
        let json = toCircuitJson 2 Map.empty [| Gate.Sdg 1 |]
        let doc = JsonDocument.Parse(json)
        let gate = doc.RootElement.GetProperty("gates").[0]
        Assert.Equal("Sdg", gate.GetProperty("gate").GetString())

    [<Fact>]
    let ``JSON: Rz gate includes angle`` () =
        let json = toCircuitJson 2 Map.empty [| Gate.Rz(0, 1.5) |]
        let doc = JsonDocument.Parse(json)
        let gate = doc.RootElement.GetProperty("gates").[0]
        Assert.Equal("Rz", gate.GetProperty("gate").GetString())
        Assert.Equal(1.5, gate.GetProperty("angle").GetDouble())

    [<Fact>]
    let ``JSON: CNOT gate has control and target`` () =
        let json = toCircuitJson 3 Map.empty [| Gate.CNOT(0, 2) |]
        let doc = JsonDocument.Parse(json)
        let gate = doc.RootElement.GetProperty("gates").[0]
        Assert.Equal("CNOT", gate.GetProperty("gate").GetString())
        Assert.Equal(0, gate.GetProperty("control").GetInt32())
        Assert.Equal(2, gate.GetProperty("target").GetInt32())

    [<Fact>]
    let ``JSON: empty gate array produces valid JSON`` () =
        let json = toCircuitJson 2 Map.empty [||]
        let doc = JsonDocument.Parse(json)
        Assert.Equal(0, doc.RootElement.GetProperty("gateCount").GetInt32())
        Assert.Equal(0, doc.RootElement.GetProperty("gates").GetArrayLength())

    // ── QASM: S / Sdg gates ─────────────────────────────────────────

    [<Fact>]
    let ``QASM: S gate renders correctly`` () =
        let qasm = toOpenQasm { defaultOpenQasmOptions with IncludeHeader = false } 2 [| Gate.S 0 |]
        Assert.Contains("s q[0];", qasm)

    [<Fact>]
    let ``QASM: Sdg gate renders correctly`` () =
        let qasm = toOpenQasm { defaultOpenQasmOptions with IncludeHeader = false } 2 [| Gate.Sdg 1 |]
        Assert.Contains("sdg q[1];", qasm)
