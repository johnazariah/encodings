namespace Encodings

open System
open System.Numerics
open System.Text.RegularExpressions

/// <summary>
/// FCIDUMP file parser and coefficient factory builder.
/// </summary>
/// <remarks>
/// <para>
/// Reads molecular integrals in the standard FCIDUMP format (Knowles &amp; Handy, 1989)
/// used by MOLPRO, PySCF, Psi4, and other quantum chemistry packages. Produces a
/// <c>coefficientFactory</c> function compatible with <see cref="Hamiltonian.computeHamiltonianWith"/>.
/// </para>
/// <para>
/// <b>Index convention:</b> FCIDUMP files use 1-based indices with zero as a sentinel.
/// Two-electron integrals are stored in chemist's notation <c>(ij|kl)</c>.
/// The parser converts to 0-based indices internally.
/// </para>
/// <para>
/// <b>Integral notation:</b> The chemist's integral <c>(ij|kl)</c> relates to the
/// physicist's integral <c>⟨pq|rs⟩</c> by <c>⟨pq|rs⟩ = (pr|qs)</c>.
/// The coefficient factory performs this conversion so that the Hamiltonian
/// module assembles the correct second-quantized Hamiltonian.
/// </para>
/// </remarks>
module Fcidump =

    // ── Types ───────────────────────────────────────────────────────

    /// <summary>
    /// Parsed FCIDUMP data containing molecular integrals and metadata.
    /// </summary>
    type FcidumpData =
        { /// <summary>Number of spatial orbitals (NORB).</summary>
          Norb : int
          /// <summary>Number of electrons (NELEC).</summary>
          Nelec : int
          /// <summary>Twice the spin projection, 2·Mₛ (MS2).</summary>
          Ms2 : int
          /// <summary>Orbital symmetry labels (ORBSYM), 0-based.</summary>
          OrbSym : int[]
          /// <summary>Target state symmetry (ISYM).</summary>
          ISym : int
          /// <summary>
          /// One-electron integrals h[i,j] (0-based), symmetric: h[i,j] = h[j,i].
          /// </summary>
          H1e : float[,]
          /// <summary>
          /// Two-electron integrals in chemist's notation (ij|kl) (0-based),
          /// fully symmetrized with 8-fold permutation symmetry.
          /// </summary>
          H2e : float[,,,]
          /// <summary>
          /// Core energy: nuclear repulsion plus any frozen-core contributions.
          /// </summary>
          CoreEnergy : float }

    // ── Header Parsing ──────────────────────────────────────────────

    /// Parse the &amp;FCI namelist header, returning (header fields, body start index).
    let private parseHeader (lines : string[]) =
        let mutable headerEnd = 0
        for i in 0 .. min (lines.Length - 1) 20 do
            let upper = lines.[i].Trim().ToUpperInvariant()
            if upper.Contains("&END") || upper = "/" then
                headerEnd <- i

        let headerText =
            lines.[0 .. headerEnd]
            |> String.concat " "
            |> fun s -> s.ToUpperInvariant()
            |> fun s -> s.Replace("&FCI", "").Replace("&END", "").Replace("/", "")
            |> fun s -> Regex.Replace(s, @"\s+", " ")

        let getInt (name : string) =
            let m = Regex.Match(headerText, name + @"\s*=\s*(\d+)")
            if m.Success then int m.Groups.[1].Value else 0

        let norb  = getInt "NORB"
        let nelec = getInt "NELEC"
        let ms2   = getInt "MS2"
        let isym  = getInt "ISYM"

        let orbsym =
            let m = Regex.Match(headerText, @"ORBSYM\s*=\s*([\d,\s]+)")
            if m.Success then
                m.Groups.[1].Value.Split([| ','; ' ' |], StringSplitOptions.RemoveEmptyEntries)
                |> Array.map int
            else
                Array.create norb 1

        (norb, nelec, ms2, orbsym, isym, headerEnd + 1)

    // ── Integral Parsing ────────────────────────────────────────────

    /// Symmetrize a one-electron integral into the h1e array.
    let private storeH1e (h1e : float[,]) (i : int) (j : int) (v : float) =
        h1e.[i, j] <- v
        h1e.[j, i] <- v

    /// Symmetrize a two-electron integral into the h2e array (8-fold).
    let private storeH2e (h2e : float[,,,]) (i : int) (j : int) (k : int) (l : int) (v : float) =
        // (ij|kl) = (ji|kl) = (ij|lk) = (ji|lk) = (kl|ij) = (lk|ij) = (kl|ji) = (lk|ji)
        h2e.[i,j,k,l] <- v
        h2e.[j,i,k,l] <- v
        h2e.[i,j,l,k] <- v
        h2e.[j,i,l,k] <- v
        h2e.[k,l,i,j] <- v
        h2e.[l,k,i,j] <- v
        h2e.[k,l,j,i] <- v
        h2e.[l,k,j,i] <- v

    /// Parse a data line: "value  i  j  k  l"
    let private parseDataLine (line : string) =
        let parts =
            line.Trim().Split([| ' '; '\t' |], StringSplitOptions.RemoveEmptyEntries)
        if parts.Length >= 5 then
            Some (float parts.[0], int parts.[1], int parts.[2], int parts.[3], int parts.[4])
        else
            None

    // ── Public API ──────────────────────────────────────────────────

    /// <summary>
    /// Parse an FCIDUMP file from its text content.
    /// </summary>
    /// <param name="content">The full text content of an FCIDUMP file.</param>
    /// <returns>A <see cref="FcidumpData"/> record with parsed integrals and metadata.</returns>
    /// <remarks>
    /// <para>Handles both <c>&amp;END</c> and <c>/</c> header terminators.</para>
    /// <para>Indices are converted from 1-based (FCIDUMP) to 0-based internally.</para>
    /// <para>Two-electron integrals are fully symmetrized (8-fold permutation symmetry).</para>
    /// <para>One-electron integrals are symmetrized: h[i,j] = h[j,i].</para>
    /// </remarks>
    let parse (content : string) : FcidumpData =
        let lines =
            content.Split([| '\n'; '\r' |], StringSplitOptions.RemoveEmptyEntries)

        let (norb, nelec, ms2, orbsym, isym, bodyStart) = parseHeader lines

        let h1e = Array2D.zeroCreate norb norb
        let h2e = Array4D.zeroCreate norb norb norb norb
        let mutable coreEnergy = 0.0

        for lineIdx in bodyStart .. lines.Length - 1 do
            match parseDataLine lines.[lineIdx] with
            | Some (v, i, j, k, l) when abs v > 1e-15 ->
                if k > 0 && l > 0 then
                    // Two-electron integral (ij|kl), 1-based → 0-based
                    storeH2e h2e (i-1) (j-1) (k-1) (l-1) v
                elif j > 0 then
                    // One-electron integral h_ij, 1-based → 0-based
                    storeH1e h1e (i-1) (j-1) v
                elif i = 0 && j = 0 && k = 0 && l = 0 then
                    // Core energy (nuclear repulsion + frozen core)
                    coreEnergy <- v
                // else: i > 0, j = 0, k = 0, l = 0 — non-standard, skip
            | _ -> ()

        { Norb       = norb
          Nelec      = nelec
          Ms2        = ms2
          OrbSym     = orbsym
          ISym       = isym
          H1e        = h1e
          H2e        = h2e
          CoreEnergy = coreEnergy }

    /// <summary>
    /// Build a coefficient factory from parsed FCIDUMP data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The returned function is compatible with
    /// <see cref="Hamiltonian.computeHamiltonianWith"/> and the Skeleton API.
    /// </para>
    /// <para>
    /// <b>Convention mapping:</b> The Hamiltonian module assembles
    /// <c>H = Σ f("p,q") a†_p a_q + Σ f("p,q,r,s") a†_p a†_q a_r a_s</c>.
    /// To reproduce the physics
    /// <c>H₂ = ½ Σ ⟨pq|rs⟩ a†_p a†_q a_s a_r</c>,
    /// the factory returns <c>½ × ⟨pq|sr⟩ = ½ × (ps|qr)</c> in chemist's notation.
    /// The ½ factor is included because the Hamiltonian module does not apply it.
    /// </para>
    /// <para>
    /// The nuclear repulsion energy is <b>not</b> included in the factory output.
    /// Add it separately as an identity-weighted term if needed.
    /// </para>
    /// </remarks>
    /// <param name="data">Parsed FCIDUMP data.</param>
    /// <returns>A coefficient factory function <c>string → Complex option</c>.</returns>
    let toCoefficientFactory (data : FcidumpData) : (string -> Complex option) =
        let norb = data.Norb
        let h1e  = data.H1e
        let h2e  = data.H2e

        fun (key : string) ->
            let parts = key.Split(',')
            match parts.Length with
            | 2 ->
                let p = int parts.[0]
                let q = int parts.[1]
                if p < norb && q < norb then
                    let v = h1e.[p, q]
                    if abs v > 1e-15 then Some (Complex(v, 0.0)) else None
                else None
            | 4 ->
                let p = int parts.[0]
                let q = int parts.[1]
                let r = int parts.[2]
                let s = int parts.[3]
                if p < norb && q < norb && r < norb && s < norb then
                    // Factory key "p,q,r,s" → coefficient of a†_p a†_q a_r a_s
                    // = ½ ⟨pq|sr⟩ = ½ (ps|qr) in chemist notation
                    let v = 0.5 * h2e.[p, s, q, r]
                    if abs v > 1e-15 then Some (Complex(v, 0.0)) else None
                else None
            | _ -> None

    /// <summary>
    /// Parse an FCIDUMP file and return a ready-to-use coefficient factory.
    /// </summary>
    /// <param name="content">The full text content of an FCIDUMP file.</param>
    /// <returns>
    /// A tuple of (coefficientFactory, coreEnergy, norb) where norb is the
    /// number of spatial orbitals (use as the <c>n</c> parameter to
    /// <see cref="Hamiltonian.computeHamiltonianWith"/>).
    /// </returns>
    let parseToFactory (content : string) : (string -> Complex option) * float * int =
        let data = parse content
        (toCoefficientFactory data, data.CoreEnergy, data.Norb)
