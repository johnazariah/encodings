namespace Encodings

[<AutoOpen>]
module Operators =
    open System.Numerics

    type Pauli =
        | I
        | X
        | Y
        | Z
    with
        static member Apply = function
            | 'I' -> Some I
            | 'X' -> Some X
            | 'Y' -> Some Y
            | 'Z' -> Some Z
            | _ -> None

        member this.AsString =
            lazy
                match this with
                | I -> "I"
                | X -> "X"
                | Y -> "Y"
                | Z -> "Z"

        override this.ToString() =
            this.AsString.Value

        static member Identity = I

        static member Multiply (x, y) =
            match (x, y) with
            | (I, s)
            | (s, I) -> C<Pauli>.P1 s
            | (X, X)
            | (Y, Y)
            | (Z, Z) -> C<Pauli>.P1 I
            | (X, Y) -> C<Pauli>.Pi Z
            | (Y, X) -> C<Pauli>.Mi Z
            | (Y, Z) -> C<Pauli>.Pi X
            | (Z, Y) -> C<Pauli>.Mi X
            | (Z, X) -> C<Pauli>.Pi Y
            | (X, Z) -> C<Pauli>.Mi Y

    type FermionicOperator =
        | I
        | Cr
        | An
    with
        static member inline Apply = function
            | 'I' -> Some I
            | 'C' -> Some Cr // Create
            | 'U' -> Some Cr // Up
            | 'D' -> Some An // Down or Destroy
            | 'R' -> Some Cr // Raise
            | 'L' -> Some An // Lower
            | _ -> None

        member inline this.AsLadderOperatorString =
            lazy
                match this with
                | I -> "I"
                | Cr -> "R"
                | An -> "L"

        member inline this.IsIdentity = match this with | I  -> true | _ -> false
        member inline this.IsRaising  = match this with | Cr -> true | _ -> false
        member inline this.IsLowering = match this with | An -> true | _ -> false

        override this.ToString() =
            this.AsLadderOperatorString.Value

        static member inline Identity = I

        static member inline InNormalOrder (l, r) =
            match (l, r) with
            | An, Cr -> false
            | _, _ -> true

        static member inline FromString (s : string) =
            FermionicOperator.Apply <| s.Chars 0

        static member Combine
            (productTerm : PIxWkOp<uint32, FermionicOperator>, nextUnit : IxOp<uint32, FermionicOperator>) =
            let nUnits = productTerm.IndexedOps.Length

            printfn "%s" productTerm.Signature

            let prefix =
                if nUnits > 2 then
                    productTerm.IndexedOps.[0..(nUnits - 2)]
                else if nUnits > 1 then
                    [| productTerm.IndexedOps.[0] |]
                else
                    [| IxOp<_,_>.Apply(0u, I) |]

            let lastUnit = productTerm.IndexedOps.[nUnits - 1]

            match (lastUnit.Op, nextUnit.Op) with
            | An, Cr ->
                if lastUnit.Index <> nextUnit.Index then
                    [|
                        yield! prefix
                        yield nextUnit
                        yield lastUnit
                    |]
                    |> curry PIxWkOp.Apply (productTerm.Coeff * Complex.MinusOne)
                    |> (fun term -> [| term |])
                else
                    let leadingTerm =
                        [|
                            yield! prefix
                        |] |> curry PIxWkOp<_,_>.Apply productTerm.Coeff
                    let trailingTerm =
                        [|
                            yield! prefix
                            yield nextUnit
                            yield lastUnit
                        |] |> curry PIxWkOp.Apply (productTerm.Coeff * Complex.MinusOne)
                    [| leadingTerm; trailingTerm |]
            | _, _ ->
                [|
                    yield! productTerm.IndexedOps
                    yield nextUnit
                |]
                |> curry PIxWkOp<_,_>.Apply productTerm.Coeff
                |> (fun term -> [| term |])

    type IndexedFermionicOperator =
        | InFmOp of IxOp<uint32, FermionicOperator>
    with
        member this.Unapply = match this with InFmOp x -> x

        static member Apply (op, idx) =
            IxOp<_,_>.Apply (idx, op) |> InFmOp

        member this.JordanWignerEncodeToDensePauliTerm (n : uint32) =
            if this.Unapply.Index >= n then
                None
            else
                let jw_components j =
                    let _zs_ = if j = 0u then "" else System.String ('Z', (int j))
                    let _is_ = if j = n  then "" else System.String ('I', int (n - j - 1u))

                    let jw_x = sprintf "%sX%s" _zs_ _is_
                    let jw_y = sprintf "%sY%s" _zs_ _is_
                    [|jw_x; jw_y|]
                    |> Array.map (RegisterFromString Pauli.Apply >> Option.get)

                let coeffs =
                    match this.Unapply.Op with
                    | Cr -> [|Complex(0.5, 0.); (Complex(0., -0.5))|]
                    | An -> [|Complex(0.5, 0.); (Complex(0., +0.5))|]
                    | I ->  [|Complex.Zero; Complex.One|]

                jw_components this.Unapply.Index
                |> Array.zip coeffs
                |> Array.map (fun (a, b) ->  b.ScaleCoefficient a)
                |> curry SR<Pauli>.Apply Complex.One
                |> Some
