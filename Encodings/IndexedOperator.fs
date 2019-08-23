namespace Encodings

[<AutoOpen>]
module IndexedOperator =
    open System.Numerics

    type IIndexedOperatorUnit =
        interface
            abstract Index : uint32
            abstract AsString   : string Lazy
        end

    type IndexedOperator<'a when 'a :> IIndexedOperatorUnit>
        (operatorUnits : 'a[], coefficient : Complex) =
        class
            override __.ToString() =
                operatorUnits
                |> Array.map (sprintf "%O")
                |> (fun rg -> System.String.Join (" | ", rg))
                |> sprintf "[%s]"

            member val OperatorUnits = operatorUnits
            member val Coefficient = coefficient

            member __.ResetPhase newPhase =
                IndexedOperator (operatorUnits, newPhase)

            static member (*) (l : IndexedOperator<'a>, r : IndexedOperator<'a>) =
                IndexedOperator ((Array.concat [l.OperatorUnits; r.OperatorUnits]), l.Coefficient * r.Coefficient)
        end


    type IndexedOperatorSequence<'a when 'a :> IIndexedOperatorUnit>
        (operators : IndexedOperator<'a>[], coefficient : Complex) =
        class
            new (operators : IndexedOperator<'a>[]) =
                IndexedOperatorSequence (operators, Complex.One)

            member val SummandTerms =
                operators
                |> Array.where (fun t -> t.Coefficient <> Complex.Zero)

            member __.DistributeCoefficient =
                operators
                |> Array.map (fun r -> r.ResetPhase (r.Coefficient * coefficient))
                |> IndexedOperatorSequence

            static member (*) (l : IndexedOperatorSequence<'a>, r : IndexedOperatorSequence<'a>) =
                let (l_normal, r_normal) = (l.DistributeCoefficient, r.DistributeCoefficient)
                [|
                    for lt in l_normal.SummandTerms do
                        for rt in r_normal.SummandTerms do
                            yield (lt * rt)
                |]
                |> IndexedOperatorSequence
        end

    let TryCreateIndexedOperatorUnit<'a>
        (unitFactory : string -> uint32 -> 'a option) (s : System.String) =
        try
            s.Trim().TrimStart('(').TrimEnd(')').Split(',')
            |> Array.map (fun s -> s.Trim ())
            |> (fun rg ->
                let index = System.UInt32.Parse rg.[1]
                unitFactory (rg.[0]) (index))
        with
        | _ -> None

    let TryCreateIndexedOperator<'a when 'a :> IIndexedOperatorUnit>
        (unitFactory : string -> uint32 -> 'a option) (s : System.String) =
        let f = TryCreateIndexedOperatorUnit<'a> unitFactory
        try
            s.Trim().TrimStart('[').TrimEnd(']').Split('|')
            |> Array.choose (f)
            |> (fun ops -> IndexedOperator<'a> (ops, Complex.One))
            |> Some
        with
        | _ -> None

    let TryCreateIndexedOperatorSequence<'a when 'a :> IIndexedOperatorUnit>
        (unitFactory : string -> uint32 -> 'a option) (s : System.String) =
        let f = TryCreateIndexedOperator<'a> unitFactory
        try
            s.Trim().TrimStart('{').TrimEnd('}').Split(';')
            |> Array.choose (f)
            |> (fun ops -> IndexedOperatorSequence<'a> (ops, Complex.One))
            |> Some
        with
        | _ -> None

[<AutoOpen>]
module IndexedOperatorExtensions =
    let inline (.>=.) (l : IIndexedOperatorUnit) (r : IIndexedOperatorUnit) =
        l.Index >= r.Index

    let inline (.<=.) (l : IIndexedOperatorUnit) (r : IIndexedOperatorUnit) =
        l.Index <= r.Index

