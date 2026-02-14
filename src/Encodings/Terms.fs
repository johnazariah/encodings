namespace Encodings

/// <summary>
/// Core algebraic types for representing quantum operator expressions.
/// </summary>
/// <remarks>
/// <para>
/// This module defines a three-level algebraic hierarchy for building Hamiltonian expressions:
/// </para>
/// <list type="bullet">
///   <item>
///     <term><see cref="T:Encodings.C`1"/></term>
///     <description>
///       A coefficient-operator pair representing a complex scalar times a single operator unit.
///       For example: 0.5 × a†₂ (half times creation operator on mode 2).
///     </description>
///   </item>
///   <item>
///     <term><see cref="T:Encodings.P`1"/></term>
///     <description>
///       A product of coefficient-operator pairs, representing an ordered sequence of operators.
///       For example: a†₁ a₂ a†₃ a₄ (a string of creation/annihilation operators).
///     </description>
///   </item>
///   <item>
///     <term><see cref="T:Encodings.S`1"/></term>
///     <description>
///       A sum of products, representing a full Hamiltonian: H = Σᵢ hᵢ Pᵢ.
///       This is the standard form for quantum chemistry Hamiltonians.
///     </description>
///   </item>
/// </list>
/// <para>
/// The type parameter <c>'unit</c> represents the atomic operator type—either
/// <c>LadderOperatorUnit</c> for fermionic second-quantized operators (a†, a) or
/// <c>PauliRegister</c> for qubit Pauli strings (I, X, Y, Z).
/// </para>
/// <para>
/// Operators support tensor product (*) and sum (+) operations to build complex expressions.
/// </para>
/// </remarks>
[<AutoOpen>]
module Terms =
    open System.Numerics

    /// <summary>
    /// A coefficient-operator pair: a complex coefficient times a single operator unit.
    /// </summary>
    /// <typeparam name="unit">
    /// The type of the operator unit (e.g., <c>LadderOperatorUnit</c> or <c>PauliRegister</c>).
    /// Must support equality for term combination.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// <c>C</c> represents the simplest term in the operator algebra: a scalar coefficient
    /// multiplied by a single operator. Mathematically: c × Ô where c ∈ ℂ and Ô is an operator.
    /// </para>
    /// <para>
    /// Examples:
    /// <list type="bullet">
    ///   <item><description>1.0 × a†₂ — creation operator on mode 2</description></item>
    ///   <item><description>0.5 × X₀ — half times Pauli-X on qubit 0</description></item>
    ///   <item><description>i × Z₁ — imaginary unit times Pauli-Z on qubit 1</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The (*) operator performs tensor product, yielding a <see cref="T:Encodings.P`1"/>.
    /// The (+) operator combines like terms or creates a product.
    /// </para>
    /// </remarks>
    type C<'unit when 'unit : equality> =
        {
          /// <summary>The complex coefficient multiplying the operator.</summary>
          Coeff : Complex
          /// <summary>The operator unit (e.g., a single ladder operator or Pauli).</summary>
          Item : 'unit
        }
    with
        /// <summary>
        /// Creates a coefficient-operator pair with coefficient 1.
        /// </summary>
        /// <param name="unit">The operator unit.</param>
        /// <returns>A <c>C</c> with <c>Coeff = 1</c> and the given unit.</returns>
        static member Apply (unit : 'unit) =
            { Coeff = Complex.One;  Item = unit }

        /// <summary>
        /// Creates a coefficient-operator pair with the specified coefficient.
        /// </summary>
        /// <param name="coeff">The complex coefficient (will be reduced if non-finite).</param>
        /// <param name="unit">The operator unit.</param>
        /// <returns>A <c>C</c> with the given coefficient and unit.</returns>
        static member Apply (coeff : Complex, unit : 'unit) =
            { Coeff = coeff.Reduce; Item = unit }

        /// <summary>
        /// Tensor product operator: multiplies two coefficient-operator pairs.
        /// </summary>
        /// <param name="l">The left operand.</param>
        /// <param name="r">The right operand.</param>
        /// <returns>
        /// A <see cref="T:Encodings.P`1"/> containing both operators in sequence,
        /// with combined coefficient. Returns zero product if coefficient is zero.
        /// </returns>
        /// <remarks>
        /// Computes (c₁ × Ô₁) ⊗ (c₂ × Ô₂) = (c₁c₂) × (Ô₁ Ô₂).
        /// </remarks>
        // TENSOR operator
        static member (*) (l : C<'unit>, r : C<'unit>) =
            let coeff = (l.Coeff * r.Coeff).Reduce
            if (coeff.IsZero) then
                P<'unit>.Apply (Complex.Zero, [| l; r |])
            else
                P<'unit>.Apply (coeff, [| {l with Coeff = Complex.One}; {r with Coeff = Complex.One} |])

        /// <summary>
        /// Sum operator: adds two coefficient-operator pairs.
        /// </summary>
        /// <param name="l">The left operand.</param>
        /// <param name="r">The right operand.</param>
        /// <returns>
        /// If both operands have the same unit, returns a single <see cref="T:Encodings.P`1"/>
        /// with combined coefficient. Otherwise, returns a product containing both terms.
        /// </returns>
        /// <remarks>
        /// For like terms: (c₁ × Ô) + (c₂ × Ô) = (c₁ + c₂) × Ô.
        /// For unlike terms: creates a product term [c₁Ô₁ | c₂Ô₂].
        /// </remarks>
        // SUM operator: combine like terms or create a product term
        static member (+) (l : C<'unit>, r : C<'unit>) =
            if (l.Item = r.Item) then
                let coeff = (l.Coeff + r.Coeff).Reduce
                if coeff.IsZero then
                    P<'unit>.Zero
                else
                    P<'unit>.Apply (coeff, [| { l with Coeff = Complex.One } |])
            else
                P<'unit>.Apply (Complex.One, [| l; r |])

        /// <summary>
        /// Indicates whether the coefficient is zero (making the entire term zero).
        /// </summary>
        member this.IsZero = this.Coeff.IsZero

        /// <summary>
        /// Returns a reduced form with the coefficient sanitized (non-finite values become zero).
        /// </summary>
        member this.Reduce = { this with Coeff = this.Coeff.Reduce }

        /// <summary>
        /// Formats the coefficient-operator pair as a string.
        /// </summary>
        /// <returns>
        /// A string representation showing the coefficient and operator,
        /// with special formatting for common coefficients (±1, ±i).
        /// </returns>
        override this.ToString() =
            let itemString = sprintf "%O" this.Item
            if this.Coeff = Complex.Zero then
                ""
            else if this.Coeff = Complex.One then
                sprintf "%s" itemString
            else if this.Coeff = - Complex.One then
                sprintf "(- %s)" itemString
            else if this.Coeff = Complex.ImaginaryOne then
                sprintf "(i %s)" itemString
            else if this.Coeff = - Complex.ImaginaryOne then
                sprintf "(-i %s)" itemString
            else if this.Coeff.Imaginary = 0. then
                sprintf "(%O %s)" this.Coeff.Real itemString
            else if this.Coeff.Imaginary = 1. then
                sprintf "(%Oi %s)" this.Coeff.Real itemString
            else if this.Coeff.Imaginary = -1. then
                sprintf "(-%Oi %s)" this.Coeff.Real itemString
            else
                sprintf "{Coeff = %O;\n Item = %A;}" this.Coeff this.Item

    /// <summary>
    /// A product of coefficient-operator pairs: an ordered sequence of operators with an overall coefficient.
    /// </summary>
    /// <typeparam name="unit">
    /// The type of the operator unit (e.g., <c>LadderOperatorUnit</c> or <c>PauliRegister</c>).
    /// Must support equality for term operations.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// <c>P</c> represents a product term: c × (Ô₁ Ô₂ ⋯ Ôₙ), where c is a complex coefficient
    /// and each Ôᵢ is an operator unit. The ordering is significant for non-commuting operators.
    /// </para>
    /// <para>
    /// Examples:
    /// <list type="bullet">
    ///   <item><description>a†₁ a₂ — two-operator product (hopping term)</description></item>
    ///   <item><description>0.5 × a†₁ a†₂ a₃ a₄ — four-operator product (two-electron integral)</description></item>
    ///   <item><description>X₀ Z₁ X₂ — three-qubit Pauli string</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The (*) operator performs tensor product of two products.
    /// Products can be collected into sums <see cref="T:Encodings.S`1"/> to form Hamiltonians.
    /// </para>
    /// </remarks>
    and  P<'unit when 'unit : equality> =
        {
          /// <summary>The overall complex coefficient for the product.</summary>
          Coeff : Complex
          /// <summary>The ordered array of coefficient-operator pairs in the product.</summary>
          Units : C<'unit>[]
        }
    with
        /// <summary>
        /// The zero product (additive identity): coefficient zero, empty units.
        /// </summary>
        static member Zero : P<'unit> = { Coeff = Complex.Zero; Units = [||] }

        /// <summary>
        /// Internal constructor that reduces coefficients for numerical stability.
        /// </summary>
        static member private ApplyInternal (coeff : Complex) (units : C<'unit>[]) =
            {
                P.Coeff = coeff.Reduce
                P.Units = (units |> Array.map (fun u -> u.Reduce))
            }

        /// <summary>
        /// Creates a product from an overall coefficient and array of coefficient-operator pairs.
        /// </summary>
        /// <param name="coeff">The overall coefficient.</param>
        /// <param name="units">The array of coefficient-operator pairs.</param>
        /// <returns>A product with reduced coefficients.</returns>
        static member Apply (coeff : Complex, units : C<'unit>[]) : P<'unit> =
            P<'unit>.ApplyInternal coeff units

        /// <summary>
        /// Returns a fully reduced (normalized) form of the product.
        /// </summary>
        /// <remarks>
        /// Reduction moves all coefficients from individual units into the overall coefficient,
        /// leaving each unit with coefficient 1. Zero terms cause the entire product to become zero.
        /// </remarks>
        member this.Reduce =
            let normalize (resultCoeff : Complex, resultUnits) (currentUnit : C<'unit>) =
                if (resultCoeff.IsZero || currentUnit.IsZero) then
                    (Complex.Zero, [||])
                else
                    (resultCoeff * currentUnit.Coeff, Array.append resultUnits [| { currentUnit with Coeff = Complex.One }|])
            lazy
                this.Units
                |> Array.fold normalize (this.Coeff, [||])
                |> (fun (c, u) -> if ((c.IsZero) || (u = [||])) then (Complex.Zero, [||]) else (c, u))
                |> P<'unit>.Apply

        /// <summary>Creates a product from a single unit with coefficient 1.</summary>
        /// <param name="item">The operator unit.</param>
        static member Apply (       item  : 'unit)      : P<'unit> = P<'unit>.Apply(Complex.One, [| item |> C<_>.Apply |])

        /// <summary>Creates a product from a single unit with the given coefficient.</summary>
        /// <param name="coeff">The coefficient.</param>
        /// <param name="item">The operator unit.</param>
        static member Apply (coeff, item  : 'unit)      : P<'unit> = P<'unit>.Apply(coeff,       [| item |> C<_>.Apply |])

        /// <summary>Creates a product from an array of units with coefficient 1.</summary>
        /// <param name="items">The operator units.</param>
        static member Apply (       items : 'unit[])    : P<'unit> = P<'unit>.Apply(Complex.One, (items |> Array.map C<_>.Apply))

        /// <summary>Creates a product from an array of units with the given coefficient.</summary>
        /// <param name="coeff">The coefficient.</param>
        /// <param name="items">The operator units.</param>
        static member Apply (coeff, items : 'unit[])    : P<'unit> = P<'unit>.Apply(coeff,       (items |> Array.map C<_>.Apply))

        /// <summary>Creates a product from a single coefficient-operator pair with overall coefficient 1.</summary>
        /// <param name="unit">The coefficient-operator pair.</param>
        static member Apply (       unit  : C<'unit>)   : P<'unit> = P<'unit>.Apply(Complex.One, [| unit |])

        /// <summary>Creates a product from a single coefficient-operator pair with the given overall coefficient.</summary>
        /// <param name="coeff">The overall coefficient.</param>
        /// <param name="unit">The coefficient-operator pair.</param>
        static member Apply (coeff, unit  : C<'unit>)   : P<'unit> = P<'unit>.Apply(coeff,       [| unit |])

        /// <summary>Creates a product from an array of coefficient-operator pairs with overall coefficient 1.</summary>
        /// <param name="units">The coefficient-operator pairs.</param>
        static member Apply (       units : C<'unit>[]) : P<'unit> = P<'unit>.Apply(Complex.One, units)

        /// <summary>
        /// Tensor product operator: concatenates two products.
        /// </summary>
        /// <param name="l">The left product.</param>
        /// <param name="r">The right product.</param>
        /// <returns>
        /// A new product with coefficient (l.Coeff × r.Coeff) and units [l.Units; r.Units].
        /// </returns>
        /// <remarks>
        /// Computes P₁ ⊗ P₂ where the units of P₁ appear before P₂ in the result.
        /// Order matters for non-commuting operators.
        /// </remarks>
        // TENSOR
        static member (*) (l : P<'unit>, r : P<'unit>) =
            P<'unit>.Apply ((l.Coeff * r.Coeff).Reduce, Array.concat [| l.Units; r.Units |])

        /// <summary>
        /// Indicates whether any coefficient is zero (making the entire product zero).
        /// </summary>
        member this.IsZero =
            (not this.Coeff.IsNonZero) || (this.Units |> Seq.exists (fun item -> item.IsZero))

        /// <summary>
        /// Returns a new product with the overall coefficient scaled by the given factor.
        /// </summary>
        /// <param name="scale">The scaling factor.</param>
        /// <returns>A product with Coeff = this.Coeff × scale.</returns>
        member this.ScaleCoefficient scale =
            { P.Coeff = this.Coeff * scale; Units = this.Units }

        /// <summary>
        /// Attempts to parse a product from a string representation.
        /// </summary>
        /// <param name="unitFactory">A function that parses individual unit strings.</param>
        /// <param name="s">The string to parse, in format "[unit1|unit2|...]".</param>
        /// <returns><c>Some product</c> if parsing succeeds; <c>None</c> otherwise.</returns>
        static member TryCreateFromString
            (unitFactory : string -> 'unit option)
            (s : System.String) : P<'unit> option =
            try
                s.Trim().TrimStart('[').TrimEnd(']').Split('|')
                |> Array.choose (unitFactory)
                |> P<'unit>.Apply
                |> Some
            with
            | _ -> None

        /// <summary>
        /// Formats the product as a string in bracket notation.
        /// </summary>
        /// <returns>A string like "[unit1 | unit2 | ...]".</returns>
        override this.ToString() =
            this.Units
            |> Array.map (sprintf "%O")
            |> (fun rg -> System.String.Join (" | ", rg))
            |> sprintf "[%s]"

        /// <summary>
        /// Appends an operator unit to the end of this product.
        /// </summary>
        /// <param name="u">The unit to append.</param>
        /// <returns>A new product with the unit added at the end.</returns>
        member this.AppendToTerm (u : 'unit) =
            { this with Units = Array.concat [| this.Units; [|C<_>.Apply u|]|]}

    /// <summary>
    /// A sum of products: the standard form for expressing quantum Hamiltonians.
    /// </summary>
    /// <typeparam name="unit">
    /// The type of the operator unit (e.g., <c>LadderOperatorUnit</c> or <c>PauliRegister</c>).
    /// Must support equality for term combination.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// <c>S</c> represents a sum expression: H = Σᵢ cᵢ Pᵢ, where each Pᵢ is a product term.
    /// This is the standard form for molecular Hamiltonians in quantum chemistry.
    /// </para>
    /// <para>
    /// Examples:
    /// <list type="bullet">
    ///   <item><description>h₁₂ a†₁ a₂ + h₂₁ a†₂ a₁ — one-body hopping terms</description></item>
    ///   <item><description>Σᵢⱼ Jᵢⱼ ZᵢZⱼ — Ising model Hamiltonian</description></item>
    ///   <item><description>Full molecular Hamiltonians with hundreds of terms</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Terms are stored in a map keyed by their string representation to facilitate
    /// combining like terms. The (*) operator distributes over sums; (+) collects terms.
    /// </para>
    /// </remarks>
    and S<'unit when 'unit : equality> =
        {
          /// <summary>An overall coefficient applied to the entire sum.</summary>
          Coeff : Complex
          /// <summary>Map from string representation to product terms, enabling like-term combination.</summary>
          Terms : Map<string, P<'unit>>
        }
    with
        /// <summary>
        /// Lazily evaluated array of all product terms in this sum.
        /// </summary>
        member this.ProductTerms = lazy (this.Terms |> Map.toArray |> Array.map snd)

        /// <summary>
        /// The multiplicative identity: coefficient 1, no terms.
        /// </summary>
        static member Unity : S<'unit> = { Coeff = Complex.One; Terms = Map.empty}

        /// <summary>
        /// Internal constructor that normalizes terms and combines like terms via the map key.
        /// </summary>
        static member private ApplyInternal coeff terms =
            terms
            |> Seq.map (fun (t : P<'unit>) ->
                let scaled = t.ScaleCoefficient coeff
                let reduced = scaled.Reduce.Value
                (reduced.ToString(), reduced))
            |> Map.ofSeq
            |> (fun terms' -> { S.Coeff = Complex.One; S.Terms = terms' })

        /// <summary>
        /// Returns a normalized form with the overall coefficient distributed into each term.
        /// </summary>
        member this.NormalizeTermCoefficient =
            S<'unit>.ApplyInternal this.Coeff this.ProductTerms.Value

        /// <summary>Creates a sum containing a single unit with coefficient 1.</summary>
        static member Apply (item         : 'unit)       = S<'unit>.ApplyInternal Complex.One  [| item  |> P<'unit>.Apply |]

        /// <summary>Creates a sum containing a single coefficient-operator pair.</summary>
        static member Apply (unit         : C<'unit>)    = S<'unit>.ApplyInternal Complex.One  [| unit  |> P<'unit>.Apply |]

        /// <summary>Creates a sum containing a single product made from the given units.</summary>
        static member Apply (units        : C<'unit>[])  = S<'unit>.ApplyInternal Complex.One  [| units |> P<'unit>.Apply |]

        /// <summary>Creates a sum containing a single product term.</summary>
        static member Apply (term         : P<'unit>)    = S<'unit>.ApplyInternal Complex.One  [| term                    |]

        /// <summary>Creates a sum from an array of product terms.</summary>
        static member Apply (terms        : P<'unit> []) = S<'unit>.ApplyInternal Complex.One  terms

        /// <summary>Creates a sum containing a single unit with the given coefficient.</summary>
        static member Apply (coeff, item  : 'unit)       = S<'unit>.ApplyInternal coeff        [| item  |> P<'unit>.Apply |]

        /// <summary>Creates a sum containing a single coefficient-operator pair with additional scaling.</summary>
        static member Apply (coeff, unit  : C<'unit>)    = S<'unit>.ApplyInternal coeff        [| unit  |> P<'unit>.Apply |]

        /// <summary>Creates a sum containing a single product with the given overall coefficient.</summary>
        static member Apply (coeff, units : C<'unit>[])  = S<'unit>.ApplyInternal coeff        [| units |> P<'unit>.Apply |]

        /// <summary>Creates a sum containing a single product with the given overall coefficient.</summary>
        static member Apply (coeff, term  : P<'unit>)    = S<'unit>.ApplyInternal coeff        [| term                    |]

        /// <summary>Creates a sum from an array of products with an overall coefficient.</summary>
        static member Apply (coeff, terms : P<'unit>[])  = S<'unit>.ApplyInternal coeff        terms

        /// <summary>
        /// Tensor product operator: distributes multiplication over both sums.
        /// </summary>
        /// <param name="l">The left sum.</param>
        /// <param name="r">The right sum.</param>
        /// <returns>
        /// A sum containing all pairwise products: (Σᵢ Pᵢ)(Σⱼ Qⱼ) = Σᵢⱼ (Pᵢ × Qⱼ).
        /// </returns>
        static member (*) (l : S<'unit>, r : S<'unit>) =
            [|
                for lt in l.NormalizeTermCoefficient.ProductTerms.Value do
                    for gt in r.NormalizeTermCoefficient.ProductTerms.Value do
                        yield lt * gt
            |]
            |> S<'unit>.Apply

        /// <summary>
        /// Sum operator: combines two sums into one.
        /// </summary>
        /// <param name="l">The left sum.</param>
        /// <param name="r">The right sum.</param>
        /// <returns>A sum containing all terms from both operands, with like terms combined.</returns>
        static member (+) (l : 'unit S, r : 'unit S) =
            Array.concat
                [|
                    l.NormalizeTermCoefficient.ProductTerms.Value
                    r.NormalizeTermCoefficient.ProductTerms.Value
                |]
            |> S<'unit>.Apply

        /// <summary>
        /// Returns a reduced form with zero terms removed.
        /// </summary>
        member this.Reduce =
            lazy
                if this.Coeff.IsZero then
                    [||]
                else
                    [|
                        for pt in this.ProductTerms.Value do
                            let pt' = pt.Reduce.Value
                            if pt'.Units <> [||] then
                                yield pt'
                    |]
                |> S<'unit>.Apply

        /// <summary>
        /// Indicates whether the entire sum is zero (either zero coefficient or no non-zero terms).
        /// </summary>
        member this.IsZero =
            let nonZeroTermCount =
                this.ProductTerms.Value
                |> Seq.filter (fun c -> not c.IsZero)
                |> Seq.length
            (not this.Coeff.IsNonZero) || (nonZeroTermCount = 0)

        /// <summary>
        /// Attempts to parse a sum from a string representation.
        /// </summary>
        /// <param name="unitFactory">A function that parses individual unit strings.</param>
        /// <param name="s">The string to parse, in format "{[term1]; [term2]; ...}".</param>
        /// <returns><c>Some sum</c> if parsing succeeds; <c>None</c> otherwise.</returns>
        static member TryCreateFromString
            (unitFactory : string -> 'unit option)
            (s : System.String) : S<'unit> option =
            let f = P<'unit>.TryCreateFromString unitFactory
            try
                s.Trim().TrimStart('{').TrimEnd('}').Split(';')
                |> Array.choose (f)
                |> S<'unit>.Apply
                |> Some
            with
            | _ -> None

        /// <summary>
        /// Formats the sum as a string in brace notation.
        /// </summary>
        /// <returns>A string like "{[term1]; [term2]; ...}".</returns>
        override this.ToString() =
            this.ProductTerms.Value
            |> Array.map (sprintf "%O")
            |> (fun rg -> System.String.Join ("; ", rg))
            |> sprintf "{%s}"
