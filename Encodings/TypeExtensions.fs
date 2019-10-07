namespace Encodings

module AssemblyInfo =
    open System.Runtime.CompilerServices
    [<assembly: InternalsVisibleTo("Test.Encodings")>]
    do()

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
[<RequireQualifiedAccess>]
module Array =
    let inline last (arr:_[]) = arr.[arr.Length - 1]
    let inline allButLast (arr:_[]) = 
        if arr.Length = 1 then [| |] else arr.[0..arr.Length - 2]

[<AutoOpen>]
module TypeExtensions =
    open System
    open System.Numerics

    let uncurry (f : 'x -> 'y -> 'r) =
        (fun (x, y) -> f x y)

    let curry (f : ('x * 'y) -> 'r) =
        (fun x y -> f (x, y))

    let equalsOn f x (objY : obj) =
        match objY with
        | :? 'T as y -> (f x = f y)
        | _ -> false

    let hashOn f x =  hash (f x)

    let compareOn f x (objY: obj) =
        match objY with
        | :? 'T as y -> compare (f x) (f y)
        | _ -> invalidArg "yobj" "cannot compare values of different types"

    type Double
    with
        // https://docs.microsoft.com/en-us/dotnet/api/system.double?view=netframework-4.8#Equality
        static member ApproximatelyEqual (l : Double, r : Double) =
            if l.Equals r then true
            else if Double.IsPositiveInfinity l && Double.IsPositiveInfinity r then true
            else if Double.IsNegativeInfinity l && Double.IsNegativeInfinity r then true
            else if Double.IsNaN l && Double.IsNaN r then true
            else
                let epsilon = 1e-10
                let max = Math.Max (l, r)
                let divisor = if (max.Equals 0.) then Math.Min (l, r) else max
                Math.Abs ((l - r) / divisor) <= epsilon

    type Complex
    with
        static member SwapSignMultiple n (c : Complex) =
            [0..(n - 1)] |> Seq.fold (fun c' _ -> -c') c

        static member MinusOne = Complex.One          |> Complex.Negate
        static member MinusI   = Complex.ImaginaryOne |> Complex.Negate

        member this.IsNonZero =
            Complex.IsFinite this && (this <> Complex.Zero)

        member this.IsZero =
            not this.IsNonZero

        static member ApproximatelyEqual (l : Complex, r : Complex) =
            Double.ApproximatelyEqual (l.Real, r.Real) && Double.ApproximatelyEqual (l.Imaginary, r.Imaginary)

        member this.Reduce =
            lazy
                let round (d : Double) = Math.Round(d, 10, MidpointRounding.AwayFromZero)

                if Complex.IsFinite this then
                    Complex(round this.Real, round this.Imaginary)
                else
                    Complex.Zero

        member this.TimesI = new Complex (-this.Imaginary, this.Real)

        member this.ToPhasePrefix =
            match (this.Real, this.Imaginary) with
            | (+1., 0.) -> ""
            | (-1., 0.) -> " -"
            | (0., +1.) -> "( i) "
            | (0., -1.) -> "(-i) "
            | (r, 0.)   -> sprintf "%A " r
            | (0., i)   -> sprintf "(%A i) " i
            | _ -> sprintf "%A" this

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

    type Map<'Key, 'Value when 'Key : comparison>
    with
        member this.Key =
            this |> Map.fold (fun s k _ ->Array.concat [| s; [| k |] |]) ([| |])

        member this.Values =
            this |> Map.fold (fun s _ v ->Array.concat [| s; [| v |] |]) ([| |])
