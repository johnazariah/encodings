namespace Tests

/// <summary>
/// State-resolved tests for the index/label ordering conventions.
/// </summary>
/// <remarks>
/// Two conventions are in play and must be wired consistently:
///
///  1. <b>PauliRegister strings put mode/qubit 0 as the LEFTMOST character.</b>
///     "XZI" means X on mode 0, Z on mode 1, I on mode 2; JW gives
///     a†₂ (n=4) = ½·ZZXI − (i/2)·ZZYI, and the number operator n_j has its
///     Z at string position j (n_0 = ½I − ½·ZIII, n_3 = ½I − ½·IIIZ).
///
///  2. <b>Occupation-number basis states use mode j as bit 2^j</b> (mode 0 =
///     least-significant bit). The H₂ Hartree–Fock state with modes 0 and 1
///     occupied is the integer 3 = 0b0011.
///
/// To read a FockMap Pauli string in the occupation basis (mode j → bit 2^j),
/// REVERSE the string before taking the Kronecker product, so mode 0 (the
/// leftmost character) becomes the least-significant bit. This reversal is
/// exactly the Qiskit/OpenQASM label convention (mode 0 rightmost).
///
/// Spectrum-only checks cannot detect a mode-order (bit) reversal: bit reversal
/// is a permutation similarity that preserves the eigenvalue multiset. These
/// tests are therefore <i>state-resolved</i> — they check specific diagonal
/// entries in the occupation basis, not just the spectrum.
/// </remarks>
module Ordering =
    open System.Numerics
    open Encodings
    open Encodings.JordanWigner
    open Xunit

    module private M =
        let private cI = [| [| Complex.One; Complex.Zero |]; [| Complex.Zero; Complex.One |] |]
        let private cX = [| [| Complex.Zero; Complex.One |]; [| Complex.One; Complex.Zero |] |]
        let private cY = [| [| Complex.Zero; Complex(0.0, -1.0) |]; [| Complex(0.0, 1.0); Complex.Zero |] |]
        let private cZ = [| [| Complex.One; Complex.Zero |]; [| Complex.Zero; Complex(-1.0, 0.0) |] |]
        let private pm c =
            match c with
            | 'I' -> cI | 'X' -> cX | 'Y' -> cY | 'Z' -> cZ
            | _ -> failwithf "unknown Pauli %c" c
        let private kron (a: Complex[][]) (b: Complex[][]) =
            let rb, cb = b.Length, b.[0].Length
            Array.init (a.Length * rb) (fun i ->
                Array.init (a.[0].Length * cb) (fun j -> a.[i / rb].[j / cb] * b.[i % rb].[j % cb]))

        /// Occupation-basis matrix: mode j (index j from the LEFT) → bit 2^j.
        /// Reverse the signature so mode 0 (leftmost char) is the least-significant bit.
        let occMatrixOfSignature (signature: string) =
            signature |> Seq.rev |> Seq.map pm |> Seq.reduce kron

        /// Naive (non-reversed) matrix: mode 0 (leftmost char) → most-significant bit.
        let naiveMatrixOfSignature (signature: string) =
            signature |> Seq.map pm |> Seq.reduce kron

        /// Dense matrix of a Pauli sum in the chosen basis.
        let seqMatrix (matOfSig: string -> Complex[][]) (h: PauliRegisterSequence) (n: int) =
            let dim = 1 <<< n
            let mutable acc = Array.init dim (fun _ -> Array.zeroCreate dim)
            for t in h.DistributeCoefficient.SummandTerms do
                let m = matOfSig t.Signature
                for i in 0 .. dim - 1 do
                    for j in 0 .. dim - 1 do
                        acc.[i].[j] <- acc.[i].[j] + t.Coefficient * m.[i].[j]
            acc

    /// Number operator n_j = a†_j a_j (Jordan–Wigner): equals ½(I − Z_j).
    let private numberOp j n =
        (jordanWignerTerms Raise (uint32 j) (uint32 n)) * (jordanWignerTerms Lower (uint32 j) (uint32 n))

    let private combine (a: PauliRegisterSequence) (b: PauliRegisterSequence) =
        PauliRegisterSequence(Array.append a.SummandTerms b.SummandTerms)

    // ── The raising operator's exact string locks the index-0-leftmost / Z-string
    //    convention: a†₂ (n=4) = ½·ZZXI − (i/2)·ZZYI (Z-string on modes 0,1;
    //    X/Y on mode 2; mode 0 is the leftmost character). ──
    [<Fact>]
    let ``JW a-dagger_2 (n=4) is exactly 0.5 ZZXI - 0.5i ZZYI`` () =
        let d = (jordanWignerTerms Raise 2u 4u).DistributeCoefficient
        Assert.Equal(2, d.SummandTerms.Length)
        let xf, xr = d.["ZZXI"]
        let yf, yr = d.["ZZYI"]
        Assert.True(xf, "expected a ZZXI term")
        Assert.True(yf, "expected a ZZYI term")
        Assert.True(abs (xr.Coefficient.Real - 0.5) < 1e-9 && abs xr.Coefficient.Imaginary < 1e-9,
            sprintf "ZZXI should be +0.5, got %A" xr.Coefficient)
        Assert.True(abs yr.Coefficient.Real < 1e-9 && abs (yr.Coefficient.Imaginary + 0.5) < 1e-9,
            sprintf "ZZYI should be -0.5i, got %A" yr.Coefficient)

    // ── The JW number operator carries Z at string position j (mode 0 leftmost). ──
    [<Theory>]
    [<InlineData(0, "ZIII")>]
    [<InlineData(1, "IZII")>]
    [<InlineData(2, "IIZI")>]
    [<InlineData(3, "IIIZ")>]
    let ``JW number operator has its Z at string position j (mode 0 leftmost)`` (j: int) (zSig: string) =
        // n_j = ½ IIII − ½ (Z at position j)
        let d = (numberOp j 4).DistributeCoefficient
        let found, reg = d.[zSig]
        Assert.True(found, sprintf "expected a term with signature %s" zSig)
        Assert.True(abs (reg.Coefficient.Real + 0.5) < 1e-9, sprintf "expected −0.5, got %A" reg.Coefficient)

    // ── State-resolved: n_j reads mode j at bit 2^j across the whole basis. ──
    [<Theory>]
    [<InlineData(2)>]
    [<InlineData(3)>]
    [<InlineData(4)>]
    let ``JW number operator reads mode j as bit 2^j across the occupation basis`` (n: int) =
        for j in 0 .. n - 1 do
            let m = M.seqMatrix M.occMatrixOfSignature (numberOp j n) n
            for k in 0 .. (1 <<< n) - 1 do
                let expected = float ((k >>> j) &&& 1)
                Assert.True(abs (m.[k].[k].Real - expected) < 1e-9,
                    sprintf "n_%d on occupation |%d⟩: expected %g got %g" j k expected m.[k].[k].Real)

    // ── H₂ Hartree–Fock: modes 0,1 occupied ⇒ integer 3 = 0b0011, total number 2. ──
    [<Fact>]
    let ``H2 Hartree-Fock state (modes 0,1 occupied) is occupation integer 3 with N=2`` () =
        let n = 4
        let hf = 0b0011
        let occ j = (M.seqMatrix M.occMatrixOfSignature (numberOp j n) n).[hf].[hf].Real
        Assert.Equal(1.0, occ 0, 9)
        Assert.Equal(1.0, occ 1, 9)
        Assert.Equal(0.0, occ 2, 9)
        Assert.Equal(0.0, occ 3, 9)
        let total = [ for j in 0 .. n - 1 -> numberOp j n ] |> List.reduce combine
        let m = M.seqMatrix M.occMatrixOfSignature total n
        Assert.Equal(2.0, m.[hf].[hf].Real, 9)

    // ── Why spectrum tests are insufficient: bit reversal keeps the spectrum but
    //    flips which occupation bit a number operator reads. ──
    [<Fact>]
    let ``mode-order reversal preserves spectrum but changes the occupation reading`` () =
        let n = 4
        let correct = M.seqMatrix M.occMatrixOfSignature (numberOp 0 n) n
        let naive   = M.seqMatrix M.naiveMatrixOfSignature (numberOp 0 n) n
        let diag (m: Complex[][]) = [ for k in 0 .. (1 <<< n) - 1 -> m.[k].[k].Real ] |> List.sort
        // Identical spectra — a spectrum-only test cannot tell the conventions apart.
        Assert.Equal<float list>(diag correct, diag naive)
        // |0001⟩ = integer 1 has mode 0 occupied. The correct (mode j → bit 2^j)
        // convention reads 1; the naive (mode 0 → most-significant bit) reads 0.
        Assert.Equal(1.0, correct.[1].[1].Real, 9)
        Assert.Equal(0.0, naive.[1].[1].Real, 9)
