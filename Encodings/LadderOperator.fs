namespace Encodings

[<AutoOpen>]
module LadderOperator =
    open System.Numerics

    type LadderOperatorUnit =
    | Raise of uint32
    | Lower of uint32
    with
        member this.Index =
            match this with
            | Raise x -> x
            | Lower x -> x

        static member FromString (s : System.String) =
            try
                s.Trim().TrimStart('(').TrimEnd(')').Split(',')
                |> Array.map (fun s -> s.Trim ())
                |> (fun rg ->
                    let index = System.UInt32.Parse rg.[1]
                    match rg.[0] with
                    | "u" -> Raise index |> Some
                    | "d" -> Lower index |> Some
                    | _ -> None)
            with
            | _ -> None

        override this.ToString() =
            match this with
            | Raise n -> sprintf "(u, %u)" n
            | Lower n -> sprintf "(d, %u)" n

    type LadderOperator private (operatorUnits : LadderOperatorUnit [], coefficient : Complex) =
        class
            let isOrdered (comparer : 'a -> 'a -> bool) (xs : 'a seq) =
                let compareWithPrev (isOrdered, (prev : 'a option)) (curr : 'a) =
                    let currAndPrevAreOrdered = prev |> Option.map (fun p -> comparer p curr) |> Option.defaultValue true
                    ((isOrdered && currAndPrevAreOrdered), Some curr)
                xs |> Seq.fold compareWithPrev (true, None) |> fst

            let indicesInOrder ascending ops =
                let comparer (prev : LadderOperatorUnit) (curr : LadderOperatorUnit) =
                    if ascending then
                        curr.Index >= prev.Index
                    else
                        curr.Index <= prev.Index
                ops |> isOrdered comparer

            let isInNormalOrder =
                let comparer p c =
                    match (p, c) with
                    | Lower _, Raise _ -> false
                    | _, _       -> true
                operatorUnits |> isOrdered comparer

            let indicesAreAscending  = indicesInOrder true
            let indicesAreDescending = indicesInOrder false

            let raisingIndicesAreAscending =
                Seq.filter (function | Raise _ -> true | _ -> false) >> indicesAreAscending

            let loweringIndicesAreDescending =
                Seq.filter (function | Lower _ -> true | _ -> false) >> indicesAreDescending

            let isInIndexOrder =
                raisingIndicesAreAscending   operatorUnits &&
                loweringIndicesAreDescending operatorUnits

            new (operatorUnits : LadderOperatorUnit []) =
                LadderOperator (operatorUnits, Complex.One)

            member val private OperatorUnits = operatorUnits
            member val Coefficient = coefficient
            member val IsInNormalOrder = isInNormalOrder
            member val IsInIndexOrder  = isInIndexOrder

            static member FromString (s : System.String) =
                try
                    s.Trim().TrimStart('[').TrimEnd(']').Split(';')
                    |> Array.choose (LadderOperatorUnit.FromString)
                    |> (fun ops -> LadderOperator (ops, Complex.One))
                    |> Some
                with
                | _ -> None

            override this.ToString() =
                this.OperatorUnits
                |> Array.map (sprintf "%O")
                |> (fun rg -> System.String.Join ("; ", rg))
                |> sprintf "[%s]"

            static member (*) (l : LadderOperator, r : LadderOperator) =
                LadderOperator ((Array.concat [l.OperatorUnits; r.OperatorUnits]), l.Coefficient * r.Coefficient)
        end

    type LadderOperatorSequence private (operators : LadderOperator [], coefficient : Complex) =
        class
        end
