namespace Encodings

open System.Numerics

/// <summary>
/// General-purpose utility functions and type extensions used throughout the encoding library.
/// </summary>
/// <remarks>
/// This module provides:
/// <list type="bullet">
///   <item><description>Currying and uncurrying combinators for function transformation</description></item>
///   <item><description>Extensions to <see cref="T:System.Numerics.Complex"/> for quantum coefficient manipulation</description></item>
///   <item><description>Extensions to <see cref="T:Microsoft.FSharp.Collections.Map`2"/> for convenient key/value access</description></item>
/// </list>
/// These utilities simplify the implementation of encoding logic and coefficient arithmetic.
/// </remarks>
[<AutoOpen>]
module TypeExtensions =
    open System

    /// <summary>
    /// Converts a curried two-argument function into a function that takes a tuple.
    /// </summary>
    /// <param name="f">A curried function of type <c>'x -> 'y -> 'r</c>.</param>
    /// <returns>A function that takes a tuple <c>('x * 'y)</c> and returns <c>'r</c>.</returns>
    /// <example>
    /// <code>
    /// let add x y = x + y
    /// let addTuple = uncurry add
    /// addTuple (2, 3) // returns 5
    /// </code>
    /// </example>
    let uncurry (f : 'x -> 'y -> 'r) =
        (fun (x, y) -> f x y)

    /// <summary>
    /// Converts a function that takes a tuple into a curried two-argument function.
    /// </summary>
    /// <param name="f">A function of type <c>('x * 'y) -> 'r</c>.</param>
    /// <returns>A curried function of type <c>'x -> 'y -> 'r</c>.</returns>
    /// <example>
    /// <code>
    /// let addTuple (x, y) = x + y
    /// let add = curry addTuple
    /// add 2 3 // returns 5
    /// </code>
    /// </example>
    let curry (f : ('x * 'y) -> 'r) =
        (fun x y -> f (x, y))

    /// <summary>
    /// Extensions to <see cref="T:System.Numerics.Complex"/> for quantum coefficient manipulation.
    /// </summary>
    /// <remarks>
    /// Complex numbers serve as coefficients in quantum operator expressions.
    /// These extensions provide:
    /// <list type="bullet">
    ///   <item><description>Sign manipulation for fermionic anti-commutation</description></item>
    ///   <item><description>Finiteness and zero checks for numerical stability</description></item>
    ///   <item><description>Multiplication by i for phase transformations</description></item>
    ///   <item><description>String formatting for coefficient display</description></item>
    /// </list>
    /// </remarks>
    type Complex
    with
        /// <summary>
        /// Negates a complex number n times, effectively multiplying by (-1)^n.
        /// </summary>
        /// <param name="n">The number of sign swaps to apply.</param>
        /// <param name="c">The complex number to transform.</param>
        /// <returns>The complex number with sign swapped n times: (-1)^n × c.</returns>
        /// <remarks>
        /// Used in fermionic encodings where anti-commutation relations introduce
        /// sign factors based on the number of operator swaps.
        /// </remarks>
        static member SwapSignMultiple n (c : Complex) =
            [0..(n - 1)] |> Seq.fold (fun c' _ -> -c') c

        /// <summary>
        /// Returns the complex number -1 + 0i.
        /// </summary>
        static member MinusOne = Complex.One |> Complex.Negate

        /// <summary>
        /// Checks whether the complex number has finite (non-NaN, non-infinite) real and imaginary parts.
        /// </summary>
        /// <returns><c>true</c> if both components are finite; otherwise <c>false</c>.</returns>
        member this.IsFinite =
            not (System.Double.IsNaN this.Real || System.Double.IsInfinity this.Real) &&
            not (System.Double.IsNaN this.Imaginary || System.Double.IsInfinity this.Imaginary)

        /// <summary>
        /// Checks whether the complex number is finite and not equal to zero.
        /// </summary>
        /// <returns><c>true</c> if the number is finite and non-zero; otherwise <c>false</c>.</returns>
        member this.IsNonZero =
            let isFinite =
                not (System.Double.IsNaN this.Real || System.Double.IsInfinity this.Real) &&
                not (System.Double.IsNaN this.Imaginary || System.Double.IsInfinity this.Imaginary)
            isFinite && (this <> Complex.Zero)

        /// <summary>
        /// Checks whether the complex number is zero or non-finite (NaN/Infinity).
        /// </summary>
        /// <returns><c>true</c> if the number is zero or non-finite; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// This is the logical negation of <see cref="P:Encodings.TypeExtensions.Complex.IsNonZero"/>.
        /// Non-finite values are treated as zero for numerical safety.
        /// </remarks>
        member this.IsZero =
            let isFinite =
                not (System.Double.IsNaN this.Real || System.Double.IsInfinity this.Real) &&
                not (System.Double.IsNaN this.Imaginary || System.Double.IsInfinity this.Imaginary)
            let isNonZero = isFinite && (this <> Complex.Zero)
            not isNonZero

        /// <summary>
        /// Returns the complex number if finite, or <see cref="P:System.Numerics.Complex.Zero"/> if non-finite.
        /// </summary>
        /// <returns>The original value if finite; otherwise zero.</returns>
        /// <remarks>
        /// Used to sanitize coefficients after arithmetic operations that might produce
        /// NaN or Infinity, ensuring numerical stability in subsequent computations.
        /// </remarks>
        member this.Reduce =
            let isFinite =
                not (System.Double.IsNaN this.Real || System.Double.IsInfinity this.Real) &&
                not (System.Double.IsNaN this.Imaginary || System.Double.IsInfinity this.Imaginary)
            if isFinite then
                this
            else
                Complex.Zero

        /// <summary>
        /// Multiplies the complex number by the imaginary unit i.
        /// </summary>
        /// <returns>A new complex number equal to i × this = (-Im, +Re).</returns>
        /// <remarks>
        /// Useful for phase transformations in quantum mechanics where multiplication
        /// by i corresponds to a π/2 phase shift.
        /// </remarks>
        member this.TimesI = new Complex (-this.Imaginary, this.Real)

        /// <summary>
        /// Formats the complex number as a coefficient prefix for display.
        /// </summary>
        /// <returns>
        /// A string representation suitable for prefixing an operator term:
        /// "" for +1, " -" for -1, "( i) " for +i, "(-i) " for -i, or the numeric value.
        /// </returns>
        member this.ToPhasePrefix =
            match (this.Real, this.Imaginary) with
            | (+1., 0.) -> ""
            | (-1., 0.) -> " -"
            | (0., +1.) -> "( i) "
            | (0., -1.) -> "(-i) "
            | (r, 0.)   -> sprintf "%A " r
            | (0., i)   -> sprintf "(%A i) " i
            | _ -> sprintf "%A" this

        /// <summary>
        /// Formats the complex number as a conjunction operator (+ or -) with coefficient for display.
        /// </summary>
        /// <returns>
        /// A string like " + ", " - ", " + i ", " - i ", or " + value " / " - value "
        /// suitable for joining terms in a sum expression.
        /// </returns>
        member this.ToPhaseConjunction =
            match (this.Real, this.Imaginary) with
            | (+1., 0.) -> " + "
            | (-1., 0.) -> " - "
            | (0., +1.) -> " + i "
            | (0., -1.) -> " - i "
            | (r, 0.) when r >= 0. -> sprintf " + %A "     <| Math.Abs r
            | (r, 0.) when r <  0. -> sprintf " - %A "     <| Math.Abs r
            | (0., i) when i >= 0. -> sprintf " + (%A i) " <| Math.Abs i
            | (0., i) when i <  0. -> sprintf " - (%A i) " <| Math.Abs i
            | _ -> sprintf " + %A" this

    /// <summary>
    /// Extensions to <see cref="T:Microsoft.FSharp.Collections.Map`2"/> for convenient access.
    /// </summary>
    type Map<'Key, 'Value when 'Key : comparison>
    with
        /// <summary>
        /// Gets all keys in the map as an array.
        /// </summary>
        /// <returns>An array containing all keys in the map.</returns>
        member this.Keys =
            this |> Map.toArray |> Array.map fst

        /// <summary>
        /// Gets all values in the map as an array.
        /// </summary>
        /// <returns>An array containing all values in the map.</returns>
        member this.Values =
            this |> Map.toArray |> Array.map snd
