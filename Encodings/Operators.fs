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

        override this.ToString() =
            this.AsLadderOperatorString.Value

        static member inline Identity = I

        static member inline FromString (s : string) =
            FermionicOperator.Apply <| s.Chars 0

        static member InIndexOrder (a : IxOp<uint32, FermionicOperator>, b : IxOp<uint32, FermionicOperator>) =
            match (a.Op, b.Op) with
            | _, I   -> true
            | I, _   -> true
            | Cr, An -> true
            | An, Cr -> false
            | Cr, Cr -> a.Index <= b.Index
            | An, An -> a.Index >= b.Index

        static member InOperatorOrder (a : IxOp<uint32, FermionicOperator>, b : IxOp<uint32, FermionicOperator>) =
            match (a.Op, b.Op) with
            | _, I   -> true
            | I, _   -> true
            | Cr, An -> true
            | An, Cr -> false
            | Cr, Cr -> true
            | An, An -> true

        static member ToOperatorOrder (a : IxOp<uint32, FermionicOperator>, b : IxOp<uint32, FermionicOperator>) : C<IxOp<uint32, FermionicOperator>[]>[] =
            match (a.Op, b.Op) with
            | _, I ->
                [|
                    C<_>.Apply(Complex.One, [| a |])
                |]
            | I, _ ->
                [|
                    C<_>.Apply(Complex.One, [| b |])
                |]
            | An, Cr ->
                if a.Index = b.Index then
                    [|
                        C<_>.Apply(Complex.One, [| IxOp<_,_>.Apply(0u, I) |])
                        C<_>.Apply(Complex.MinusOne, [| b; a |])
                    |]
                else
                    [|
                        C<_>.Apply(Complex.MinusOne, [| b; a |])
                    |]
            | _, _ ->
                [|
                    C<_>.Apply(Complex.MinusOne, [| b; a |])
                |]

        static member ToIndexOrder (a : IxOp<uint32, FermionicOperator>, b : IxOp<uint32, FermionicOperator>) : C<IxOp<uint32, FermionicOperator>[]>[] =
            match (a.Op, b.Op) with
            | _, I ->
                [|
                    C<_>.Apply(Complex.One, [| a |])
                |]
            | I, _ ->
                [|
                    C<_>.Apply(Complex.One, [| b |])
                |]
            | Cr, An ->
                [|
                    C<_>.Apply(Complex.One, [| a; b |])
                |]
            | An, Cr ->
                if a.Index = b.Index then
                    [|
                        C<_>.Apply(Complex.One, [| IxOp<_,_>.Apply(0u, I) |])
                        C<_>.Apply(Complex.MinusOne, [| b; a |])
                    |]
                else
                    [|
                        C<_>.Apply(Complex.MinusOne, [| b; a |])
                    |]
            | Cr, Cr ->
                if a.Index > b.Index then
                    [|
                        C<_>.Apply(Complex.MinusOne, [| b; a |])
                    |]
                else
                    [|
                        C<_>.Apply(Complex.One, [| a; b |])
                    |]
            | An, An ->
                if a.Index < b.Index then
                    [|
                        C<_>.Apply(Complex.MinusOne, [| b; a |])
                    |]
                else
                    [|
                        C<_>.Apply(Complex.One, [| a; b |])
                    |]

        static member NextIndexLocation (target : FermionicOperator, items : IxOp<uint32, FermionicOperator>[]) : uint32 option =
            let findLocationOfMatched pred t =
                Array.fold
                    (fun (matched, index) curr ->
                        let matched' =
                            if (curr.Op = t) then
                                matched
                                |> Option.map (fun (item, location) -> if (pred curr.Index item.Index) then (curr, index) else (item, location))
                                |> Option.defaultValue (curr, index)
                                |> Some
                            else
                                matched
                        (matched', index + 1u))
                    (None, 0u)

            let findLocationOfMaximumIndex = findLocationOfMatched (>)
            let findLocationOfMinimumIndex = findLocationOfMatched (<)
            let findLocationOfNextIndex    = findLocationOfMatched (fun _ _ -> true)

            match target with
            | An -> findLocationOfMaximumIndex target items |> (fun (f, s) -> f |> Option.map (fun _ -> s))
            | Cr -> findLocationOfMinimumIndex target items |> (fun (f, s) -> f |> Option.map (fun _ -> s))
            | I  -> findLocationOfNextIndex    target items |> (fun (f, s) -> f |> Option.map (fun _ -> s))

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

module FermionicOperator_Order =
    open System.Numerics

    let internal findNextIndex (rg : IxOp<uint32, FermionicOperator>[]) =
        let folder (index, best) (curr : IxOp<uint32, FermionicOperator>)=
            let here = Some (curr, index)
            let best' =
                match best with
                | None -> here
                | Some (bestItem, _) ->
                    match bestItem.Op, curr.Op with
                    | _, I -> best
                    | I, _ -> here
                    | Cr, Cr -> if (bestItem.Index < curr.Index) then best else here
                    | Cr, An -> best
                    | An, Cr -> here
                    | An, An -> if (bestItem.Index > curr.Index) then best else here
            (index + 1, best')
        rg
        |> Array.fold folder (0, None)
        |> snd

    let internal behead (index : int) (rg : IxOp<uint32, FermionicOperator>[]) =
        let pre  = if index = 0 then [| |] else rg.[ .. (index - 1)]
        let post = if index = rg.Length then [| |] else rg.[(index + 1) .. ]
        let remainder =
            [|
                yield! pre
                yield! post
            |]
        let coeff = if index % 2 = 0 then Complex.One else Complex.MinusOne
        let head = C<_>.Apply(coeff, rg.[index])
        (head, remainder)

    let internal findLocationOfNextItemWithIndex (target : uint32) (rg : IxOp<uint32, FermionicOperator>[]) =
        rg
        |> Array.fold
            (fun (index, result) curr ->
                match result with
                | Some _ -> (index + 1, result)
                | None ->
                    if (target = curr.Index) then
                        (index + 1, Some index)
                    else
                        (index + 1, None))
            (0, None)
        |> snd

    let private toPixOp cixops =
        cixops
        |> Array.map (fun c -> CIxOp<_,_>.Apply(c.Coeff, c.Thunk))
        |> curry PIxOp<_,_>.Apply Complex.One

    let internal chunkByIndex (rg : IxOp<_,_>[]) =
        let getChunkWithIndex input (headItem, _) =
            let rec buildChunk target (chunk, remainder) =
                match findLocationOfNextItemWithIndex target remainder with
                | None -> (chunk, remainder)
                | Some location ->
                    let (head, tail) = behead location remainder
                    buildChunk target ([| yield! chunk; head |], tail)

            buildChunk headItem.Index ([||], input)
            |> (fun (h, t) -> (toPixOp h, t))

        let findNextChunk input =
            input
            |> findNextIndex
            |> Option.map (getChunkWithIndex input)

        let rec makeChunks (chunks, remainder) =
            match findNextChunk remainder with
            | None -> (chunks, [||])
            | Some (chunk, rest) -> makeChunks ([| yield! chunks; chunk |], rest)

        match rg with
        | [| |] ->
            [| PIxOp<_,_>.Unit |]
        | _ ->
            makeChunks ([| |], rg)
            |> fst

    let internal sortChunk (chunk : PIxOp<_,_>) =
        let sortOpsWithSameIndex (left : IxOp<_,_>, right : IxOp<_,_>) =
            if (left.Index <> right.Index) then
                failwith "A chunk cannot have heterogenous indices"
            else
                match (left.Op, right.Op) with
                | _, I ->
                    [|
                        PIxOp<_,_>.Apply(
                            Complex.One,
                            [|
                                CIxOp<_,_>.Apply(Complex.One, left)
                            |])
                    |]
                | I, _ ->
                    [|
                        PIxOp<_,_>.Apply(
                            Complex.One,
                            [|
                                CIxOp<_,_>.Apply(Complex.One, right)
                            |])
                    |]
                | An, Cr ->
                    [|
                        PIxOp<_,_>.Apply(
                            Complex.One,
                            [|
                                CIxOp<_,_>.Apply(Complex.One, IxOp<_,_>.Apply (0u, I))
                            |])

                        PIxOp<_,_>.Apply(
                            Complex.MinusOne,
                            [|
                                CIxOp<_,_>.Apply(Complex.One, right)
                                CIxOp<_,_>.Apply(Complex.One, left)
                            |])
                    |]
                | Cr, An ->
                    [|
                        PIxOp<_,_>.Apply(
                            Complex.One,
                            [|
                                CIxOp<_,_>.Apply(Complex.One, left)
                                CIxOp<_,_>.Apply(Complex.One, right)
                            |])
                    |]
                | _, _ ->
                    [| |]

        let sortedChunk =
            match chunk.IndexedOps.Length with
            | 0 ->
                [|
                |]
            | 1 ->
                [|
                    chunk
                |]
            | 2 ->
                (chunk.IndexedOps.[0], chunk.IndexedOps.[1])
                |> sortOpsWithSameIndex
                |> Array.map (fun p -> p.ScaleCoefficient chunk.Coeff)
            | _ -> failwith "what do we do here?"

        SIxOp<_,_>.Apply (Complex.One, sortedChunk)

    let internal sortChunks chunks =
        chunks
        |> Array.map sortChunk
        |> Array.fold (<*>) SIxOp<_,_>.Unit

    let toCanonicalOrder (input : IxOp<uint32, FermionicOperator>[]) : SIxOp<uint32, FermionicOperator> =
        chunkByIndex input
        |> sortChunks
