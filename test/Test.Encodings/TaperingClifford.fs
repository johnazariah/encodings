namespace Tests

/// <summary>
/// Rigorous tests for Clifford conjugation in the tapering pipeline:
/// all 16 two-qubit Pauli conjugations under CNOT (exact letters + phases),
/// dense-matrix spectral preservation, and H₂ tapering sector correctness.
/// These tests are designed to FAIL under the pre-fix CNOT phase rule
/// (`cx && tz`), which wrongly flipped XY→−YZ and YZ→−XY.
/// </summary>
module TaperingClifford =
    open System
    open System.Numerics
    open Encodings
    open Encodings.Tapering
    open Encodings.Hamiltonian
    open Encodings.JordanWigner
    open Xunit

    // ── Dense complex linear-algebra helpers (self-contained, no deps) ──
    module private Linalg =
        let cI = [| [| Complex.One; Complex.Zero |]; [| Complex.Zero; Complex.One |] |]
        let cX = [| [| Complex.Zero; Complex.One |]; [| Complex.One; Complex.Zero |] |]
        let cY = [| [| Complex.Zero; Complex(0.0, -1.0) |]; [| Complex(0.0, 1.0); Complex.Zero |] |]
        let cZ = [| [| Complex.One; Complex.Zero |]; [| Complex.Zero; Complex(-1.0, 0.0) |] |]

        let pauliMat c =
            match c with
            | 'I' -> cI | 'X' -> cX | 'Y' -> cY | 'Z' -> cZ
            | _ -> failwithf "unknown Pauli letter %c" c

        let kron (a: Complex[][]) (b: Complex[][]) =
            let rb, cb = b.Length, b.[0].Length
            Array.init (a.Length * rb) (fun i ->
                Array.init (a.[0].Length * cb) (fun j ->
                    a.[i / rb].[j / cb] * b.[i % rb].[j % cb]))

        // qubit 0 = leftmost character = outermost (most-significant) tensor factor
        let sigToMatrix (signature: string) =
            signature |> Seq.map pauliMat |> Seq.reduce kron

        let scale (s: Complex) (m: Complex[][]) = m |> Array.map (Array.map (fun x -> s * x))
        let addM (a: Complex[][]) (b: Complex[][]) =
            Array.init a.Length (fun i -> Array.init a.[0].Length (fun j -> a.[i].[j] + b.[i].[j]))
        let matmul (a: Complex[][]) (b: Complex[][]) =
            let n, m, k = a.Length, b.[0].Length, b.Length
            Array.init n (fun i -> Array.init m (fun j ->
                let mutable s = Complex.Zero
                for p in 0 .. k - 1 do s <- s + a.[i].[p] * b.[p].[j]
                s))
        let dagger (a: Complex[][]) =
            Array.init a.[0].Length (fun i -> Array.init a.Length (fun j -> Complex.Conjugate a.[j].[i]))

        let seqToMatrix (h: PauliRegisterSequence) =
            let terms = h.DistributeCoefficient.SummandTerms
            let n = terms.[0].Signature.Length
            let dim = 1 <<< n
            let mutable acc = Array.init dim (fun _ -> Array.zeroCreate dim)
            for t in terms do
                acc <- addM acc (scale t.Coefficient (sigToMatrix t.Signature))
            acc

        let maxDiff (a: Complex[][]) (b: Complex[][]) =
            let mutable d = 0.0
            for i in 0 .. a.Length - 1 do
                for j in 0 .. a.[0].Length - 1 do
                    d <- max d (a.[i].[j] - b.[i].[j]).Magnitude
            d

        // Gate unitaries with qubit 0 as the most-significant tensor factor.
        let private sMat = [| [| Complex.One; Complex.Zero |]; [| Complex.Zero; Complex(0.0, 1.0) |] |]
        let private hMat =
            let r = 1.0 / sqrt 2.0
            [| [| Complex(r, 0.0); Complex(r, 0.0) |]; [| Complex(r, 0.0); Complex(-r, 0.0) |] |]
        let private single (g: Complex[][]) target n =
            Array.init n (fun i -> if i = target then g else cI) |> Array.reduce kron
        let private cnot c t n =
            let dim = 1 <<< n
            let bit (x: int) q = (x >>> (n - 1 - q)) &&& 1
            Array.init dim (fun row -> Array.init dim (fun col ->
                let targetOut = (bit col t) ^^^ (bit col c)
                let mutable ok = true
                for q in 0 .. n - 1 do
                    let inb = if q = t then targetOut else bit col q
                    if bit row q <> inb then ok <- false
                if ok then Complex.One else Complex.Zero))
        let private gateMat g n =
            match g with
            | Had i -> single hMat i n
            | Sgate i -> single sMat i n
            | CNOT (c, t) -> cnot c t n

        /// Unitary U such that applyClifford gates H = U H U†.
        let cliffordUnitary (gates: CliffordGate list) n =
            let dim = 1 <<< n
            let mutable u = Array.init dim (fun i -> Array.init dim (fun j -> if i = j then Complex.One else Complex.Zero))
            for g in gates do u <- matmul (gateMat g n) u
            u

        /// Eigenvalues of a small Hermitian matrix via cyclic Jacobi on the
        /// real 2n×2n embedding M = [[Re, -Im],[Im, Re]]; each eigenvalue of H
        /// appears twice, so we return every other one after sorting.
        let hermitianEigenvalues (h: Complex[][]) : float[] =
            let n = h.Length
            let m = 2 * n
            let a = Array2D.init m m (fun i j ->
                let bi, bj = i % n, j % n
                if i < n && j < n then h.[bi].[bj].Real
                elif i < n then -h.[bi].[bj].Imaginary
                elif j < n then h.[bi].[bj].Imaginary
                else h.[bi].[bj].Real)
            let mutable off = 1.0
            let mutable iter = 0
            while off > 1e-14 && iter < 500 do
                iter <- iter + 1
                for p in 0 .. m - 2 do
                    for q in p + 1 .. m - 1 do
                        let apq = a.[p, q]
                        if abs apq > 1e-300 then
                            let phi = (a.[q, q] - a.[p, p]) / (2.0 * apq)
                            let t = if phi = 0.0 then 1.0 else float (sign phi) / (abs phi + sqrt (phi * phi + 1.0))
                            let c = 1.0 / sqrt (t * t + 1.0)
                            let s = t * c
                            for k in 0 .. m - 1 do
                                let akp, akq = a.[k, p], a.[k, q]
                                a.[k, p] <- c * akp - s * akq
                                a.[k, q] <- s * akp + c * akq
                            for k in 0 .. m - 1 do
                                let apk, aqk = a.[p, k], a.[q, k]
                                a.[p, k] <- c * apk - s * aqk
                                a.[q, k] <- s * apk + c * aqk
                off <- 0.0
                for p in 0 .. m - 2 do
                    for q in p + 1 .. m - 1 do off <- off + a.[p, q] * a.[p, q]
            [ for i in 0 .. m - 1 -> a.[i, i] ]
            |> List.sort
            |> List.mapi (fun i v -> i, v)
            |> List.filter (fun (i, _) -> i % 2 = 0)
            |> List.map snd
            |> List.toArray

    open Linalg

    let private multisetApproxEqual tol (a: float[]) (b: float[]) =
        a.Length = b.Length
        && Array.forall2 (fun x y -> abs (x - y) < tol) (Array.sort a) (Array.sort b)

    // ═══════════════════════════════════════════════════════════════
    //  1. All 16 two-qubit Pauli conjugations under CNOT(0,1)
    //     Verified against the Aaronson–Gottesman table: only XZ→−YY
    //     and YY→−XZ pick up a −1 phase.
    // ═══════════════════════════════════════════════════════════════
    [<Theory>]
    [<InlineData("II", "II", 1)>]
    [<InlineData("IX", "IX", 1)>]
    [<InlineData("IY", "ZY", 1)>]
    [<InlineData("IZ", "ZZ", 1)>]
    [<InlineData("XI", "XX", 1)>]
    [<InlineData("XX", "XI", 1)>]
    [<InlineData("XY", "YZ", 1)>]
    [<InlineData("XZ", "YY", -1)>]
    [<InlineData("YI", "YX", 1)>]
    [<InlineData("YX", "YI", 1)>]
    [<InlineData("YY", "XZ", -1)>]
    [<InlineData("YZ", "XY", 1)>]
    [<InlineData("ZI", "ZI", 1)>]
    [<InlineData("ZX", "ZX", 1)>]
    [<InlineData("ZY", "IY", 1)>]
    [<InlineData("ZZ", "IZ", 1)>]
    let ``CNOT(0,1) conjugation: exact letters and phase`` (input: string) (expected: string) (expectedSign: int) =
        let h = PauliRegisterSequence([| PauliRegister(input, Complex.One) |])
        let res = applyClifford [ CNOT (0, 1) ] h
        Assert.Single(res.SummandTerms) |> ignore
        let t = res.SummandTerms.[0]
        Assert.Equal(expected, t.Signature)
        Assert.Equal(Complex(float expectedSign, 0.0), t.Coefficient)

    [<Fact>]
    let ``CNOT(0,1): XY conjugates to +YZ, not -YZ (regression)`` () =
        let res = applyClifford [ CNOT (0, 1) ] (PauliRegisterSequence([| PauliRegister("XY", Complex.One) |]))
        let t = res.SummandTerms.[0]
        Assert.Equal("YZ", t.Signature)
        Assert.Equal(Complex(1.0, 0.0), t.Coefficient)

    [<Fact>]
    let ``CNOT(0,1): YZ conjugates to +XY, not -XY (regression)`` () =
        let res = applyClifford [ CNOT (0, 1) ] (PauliRegisterSequence([| PauliRegister("YZ", Complex.One) |]))
        let t = res.SummandTerms.[0]
        Assert.Equal("XY", t.Signature)
        Assert.Equal(Complex(1.0, 0.0), t.Coefficient)

    // ═══════════════════════════════════════════════════════════════
    //  2. Dense-matrix spectral preservation: applyClifford = U H U†.
    //     A unitary conjugation preserves the full eigenvalue multiset,
    //     so this simultaneously validates the phase tracking exactly.
    // ═══════════════════════════════════════════════════════════════
    [<Fact>]
    let ``applyClifford equals U H Udagger to machine precision`` () =
        // Includes XY and YZ terms with a bare CNOT(0,1) so the result is
        // sensitive to the CNOT phase rule (the old rule flips XY↔−YZ, YZ↔−XY).
        let h =
            PauliRegisterSequence(
                [| PauliRegister("XYZ", Complex(0.7, 0.0))
                   PauliRegister("YZI", Complex(-1.3, 0.0))
                   PauliRegister("XZI", Complex(0.5, 0.0))
                   PauliRegister("IYX", Complex(0.9, 0.0)) |])
        let gates = [ CNOT (0, 1); Had 2; Sgate 1; CNOT (1, 2) ]
        let n = 3
        let lhs = seqToMatrix (applyClifford gates h)
        let u = cliffordUnitary gates n
        let rhs = matmul (matmul u (seqToMatrix h)) (dagger u)
        Assert.True(maxDiff lhs rhs < 1e-9, sprintf "max|LHS-RHS| = %e" (maxDiff lhs rhs))

    [<Fact>]
    let ``applyClifford preserves the eigenvalue multiset`` () =
        let h =
            PauliRegisterSequence(
                [| PauliRegister("XY", Complex(0.5, 0.0))
                   PauliRegister("YZ", Complex(0.5, 0.0))
                   PauliRegister("ZI", Complex(0.3, 0.0))
                   PauliRegister("IX", Complex(-0.2, 0.0)) |])
        let gates = [ CNOT (0, 1); Sgate 1 ]
        let before = hermitianEigenvalues (seqToMatrix h)
        let after = hermitianEigenvalues (seqToMatrix (applyClifford gates h))
        Assert.True(multisetApproxEqual 1e-9 before after,
            sprintf "before=%A after=%A" before after)

    // ═══════════════════════════════════════════════════════════════
    //  3. H₂/STO-3G tapering: sector correctness and spectral consistency.
    // ═══════════════════════════════════════════════════════════════
    // Exact H₂/STO-3G integrals (2 spatial orbitals → 4 spin-orbitals).
    let private h2Fcidump = """
 &FCI NORB=   2,NELEC= 2,MS2=0,
  ORBSYM=1,1,
  ISYM=1,
 &END
 0.6747559268144484    1    1    1    1
 0.6637114013508132    1    1    2    2
 0.1812104620151968    2    1    2    1
 0.6637114013508132    2    2    1    1
 0.697651504490461    2    2    2    2
 -1.253309786645977    1    1  0  0
 -0.4750688487721783    2    2  0  0
 0.7151043390810812  0  0  0  0
"""

    let private h2ElectronicHamiltonian () =
        let (factory, _core, nso) = Fcidump.parseToSpinOrbitalFactory h2Fcidump
        computeHamiltonianWith jordanWignerTerms factory (uint32 nso)

    // Electronic ground state (no nuclear repulsion) of H₂/STO-3G.
    let private h2Ground = -1.852388

    [<Fact>]
    let ``H2 electronic ground state is -1.852388 Ha`` () =
        let spec = hermitianEigenvalues (seqToMatrix (h2ElectronicHamiltonian ()))
        Assert.Equal(h2Ground, Array.min spec, 4)

    [<Fact>]
    let ``H2 FullClifford: Clifford rotation preserves the full spectrum`` () =
        let ham = h2ElectronicHamiltonian ()
        let n = 4
        // Rotate with the synthesized Clifford (no sector fixing) and compare spectra.
        let result = taper { defaultTaperingOptions with Sector = [] } ham
        let u = cliffordUnitary result.CliffordGates n
        let rotated = matmul (matmul u (seqToMatrix ham)) (dagger u)
        let before = hermitianEigenvalues (seqToMatrix ham)
        let after = hermitianEigenvalues rotated
        Assert.True(multisetApproxEqual 1e-8 before after,
            sprintf "before=%A after=%A" before after)

    [<Fact>]
    let ``H2 FullClifford: ground-state sector (-1,-1,+1) preserves -1.852388 Ha`` () =
        let ham = h2ElectronicHamiltonian ()
        let result = taper { defaultTaperingOptions with Sector = [ (0, -1); (1, -1); (2, 1) ] } ham
        Assert.Equal(1, result.TaperedQubitCount)
        let spec = hermitianEigenvalues (seqToMatrix result.Hamiltonian)
        Assert.Equal(h2Ground, Array.min spec, 4)

    [<Fact>]
    let ``H2 FullClifford: default +1 sector eigenvalues are a subset of the true spectrum`` () =
        // The corrected rule yields only genuine eigenvalues (no spurious values).
        // The old rule produced -1.8319 Ha, which is NOT in the true spectrum.
        let ham = h2ElectronicHamiltonian ()
        let full = hermitianEigenvalues (seqToMatrix ham)
        let result = taper defaultTaperingOptions ham
        let spec = hermitianEigenvalues (seqToMatrix result.Hamiltonian)
        for e in spec do
            Assert.True(full |> Array.exists (fun f -> abs (f - e) < 1e-6),
                sprintf "tapered eigenvalue %f not found in true spectrum %A" e full)

    [<Fact>]
    let ``H2 FullClifford: union of all 8 sectors equals the full spectrum`` () =
        let ham = h2ElectronicHamiltonian ()
        let full = hermitianEigenvalues (seqToMatrix ham)
        let union =
            [ for s0 in [ 1; -1 ] do
                for s1 in [ 1; -1 ] do
                  for s2 in [ 1; -1 ] do
                    let r = taper { defaultTaperingOptions with Sector = [ (0, s0); (1, s1); (2, s2) ] } ham
                    yield! hermitianEigenvalues (seqToMatrix r.Hamiltonian) ]
            |> List.toArray
        Assert.True(multisetApproxEqual 1e-6 full union,
            sprintf "full=%A union=%A" (Array.sort full) (Array.sort union))
