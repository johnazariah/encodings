namespace Encodings

/// <summary>
/// Sector-aware symbolic utilities for mixed bosonic and fermionic systems.
/// </summary>
/// <remarks>
/// This module adds explicit sector metadata to indexed ladder operators and
/// provides canonical mixed normal ordering:
///
/// 1) all fermionic operators are moved to the left of bosonic operators,
/// 2) fermionic subsequences are normal-ordered with CAR,
/// 3) bosonic subsequences are normal-ordered with CCR.
///
/// Cross-sector swaps are treated as commuting and therefore do not introduce
/// sign changes.
/// </remarks>
[<AutoOpen>]
module MixedSystems =
    open System.Numerics

    /// <summary>
    /// Particle statistics sector.
    /// </summary>
    type ParticleSector =
        | Fermionic
        | Bosonic

    /// <summary>
    /// A ladder operator tagged with particle-statistics sector.
    /// </summary>
    type SectorLadderOperatorUnit =
        { Sector : ParticleSector
          Operator : LadderOperatorUnit }
    with
        static member Fermion operator = { Sector = Fermionic; Operator = operator }
        static member Boson operator = { Sector = Bosonic; Operator = operator }

        override this.ToString() =
            let prefix =
                match this.Sector with
                | Fermionic -> "f"
                | Bosonic -> "b"
            sprintf "%s:%O" prefix this.Operator

    /// <summary>
    /// Creates a fermionic indexed ladder operator.
    /// </summary>
    let fermion (operator : LadderOperatorUnit) (index : uint32) =
        IxOp<uint32, SectorLadderOperatorUnit>.Apply(index, SectorLadderOperatorUnit.Fermion operator)

    /// <summary>
    /// Creates a bosonic indexed ladder operator.
    /// </summary>
    let boson (operator : LadderOperatorUnit) (index : uint32) =
        IxOp<uint32, SectorLadderOperatorUnit>.Apply(index, SectorLadderOperatorUnit.Boson operator)

    let private normalizeSector
        (algebraNormalizer : S<IxOp<uint32, LadderOperatorUnit>> -> LadderOperatorSumExpr<_> option)
        (sectorUnits : IxOp<uint32, LadderOperatorUnit>[]) =
        if sectorUnits.Length = 0 then
            [| (Complex.One, [||]) |]
        else
            sectorUnits
            |> P<IxOp<uint32, LadderOperatorUnit>>.Apply
            |> S<IxOp<uint32, LadderOperatorUnit>>.Apply
            |> algebraNormalizer
            |> Option.map (fun ordered ->
                ordered.ProductTerms.Value
                |> Array.map (fun term ->
                    let reduced = term.Reduce.Value
                    (reduced.Coeff, reduced.Units |> Array.map (fun u -> u.Item))))
            |> Option.defaultValue [||]

    let private remapSector (sector : ParticleSector) (units : IxOp<uint32, LadderOperatorUnit>[]) =
        units
        |> Array.map (fun unit ->
            let tagged = { Sector = sector; Operator = unit.Op }
            IxOp<uint32, SectorLadderOperatorUnit>.Apply(unit.Index, tagged))

    /// <summary>
    /// Checks whether all fermionic operators appear before all bosonic operators.
    /// </summary>
    let isSectorBlockOrdered (productTerm : P<IxOp<uint32, SectorLadderOperatorUnit>>) =
        let comparer (previous : SectorLadderOperatorUnit) (current : SectorLadderOperatorUnit) =
            match previous.Sector, current.Sector with
            | Bosonic, Fermionic -> false
            | _ -> true

        productTerm.Units
        |> Seq.map (fun c -> c.Item.Op)
        |> isOrdered comparer

    /// <summary>
    /// Reorders a product term so that fermionic operators appear before bosonic operators.
    /// </summary>
    /// <remarks>
    /// Relative order is preserved within each sector. Cross-sector swaps are assumed
    /// commuting and do not change the coefficient.
    /// </remarks>
    let toSectorBlockOrder (productTerm : P<IxOp<uint32, SectorLadderOperatorUnit>>) =
        let reduced = productTerm.Reduce.Value

        let fermionicUnits =
            reduced.Units
            |> Array.choose (fun c ->
                if c.Item.Op.Sector = Fermionic then Some c else None)

        let bosonicUnits =
            reduced.Units
            |> Array.choose (fun c ->
                if c.Item.Op.Sector = Bosonic then Some c else None)

        let reorderedUnits = Array.append fermionicUnits bosonicUnits
        P<IxOp<uint32, SectorLadderOperatorUnit>>.Apply(reduced.Coeff, reorderedUnits)

    /// <summary>
    /// Canonical mixed normal ordering for sector-tagged expressions.
    /// </summary>
    /// <remarks>
    /// For each product term, this function:
    /// <list type="number">
    /// <item><description>Reorders by sector (fermions left, bosons right) without sign.</description></item>
    /// <item><description>Normal-orders fermionic subsequences with CAR.</description></item>
    /// <item><description>Normal-orders bosonic subsequences with CCR.</description></item>
    /// <item><description>Combines the resulting sector terms into a canonical mixed expression.</description></item>
    /// </list>
    /// </remarks>
    let constructMixedNormalOrdered (candidate : S<IxOp<uint32, SectorLadderOperatorUnit>>) =
        let normalizeProduct (productTerm : P<IxOp<uint32, SectorLadderOperatorUnit>>) =
            let blocked = toSectorBlockOrder productTerm
            let reduced = blocked.Reduce.Value

            let fermionicUnits =
                reduced.Units
                |> Array.choose (fun c ->
                    if c.Item.Op.Sector = Fermionic then
                        Some (IxOp<uint32, LadderOperatorUnit>.Apply(c.Item.Index, c.Item.Op.Operator))
                    else
                        None)

            let bosonicUnits =
                reduced.Units
                |> Array.choose (fun c ->
                    if c.Item.Op.Sector = Bosonic then
                        Some (IxOp<uint32, LadderOperatorUnit>.Apply(c.Item.Index, c.Item.Op.Operator))
                    else
                        None)

            let fermionicTerms =
                normalizeSector
                    LadderOperatorSumExpr<FermionicAlgebra>.ConstructNormalOrdered
                    fermionicUnits

            let bosonicTerms =
                normalizeSector
                    LadderOperatorSumExpr<BosonicAlgebra>.ConstructNormalOrdered
                    bosonicUnits

            [|
                for (fermionCoeff, fermionTerm) in fermionicTerms do
                    for (bosonCoeff, bosonTerm) in bosonicTerms do
                        let coeff = (reduced.Coeff * fermionCoeff * bosonCoeff).Reduce
                        let allUnits =
                            Array.append
                                (remapSector Fermionic fermionTerm)
                                (remapSector Bosonic bosonTerm)
                        yield P<IxOp<uint32, SectorLadderOperatorUnit>>.Apply(coeff, allUnits)
            |]

        candidate.Reduce.Value.ProductTerms.Value
        |> Array.collect normalizeProduct
        |> S<IxOp<uint32, SectorLadderOperatorUnit>>.Apply
        |> Some
