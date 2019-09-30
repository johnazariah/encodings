namespace Encodings

[<AutoOpen>]
module StringInterop =
    open System.Numerics

    let inline IndexedOpFromString< ^op when ^op : equality and ^op : (static member InNormalOrder : ^op -> ^op -> bool)>
        (unitFactory : string ->  ^op  option)
        (s : System.String) =
        try
            s.Trim().TrimStart('(').TrimEnd(')').Split(',')
            |> Array.map (fun s -> s.Trim ())
            |> (fun rg ->
                unitFactory (rg.[0])
                |> Option.map (fun op ->
                    (System.UInt32.Parse rg.[1], op)
                    |> IxOp<uint32, ^op >.Apply
                    |> (curry CIxOp<_,_>.Apply) Complex.One))
        with
        | _ -> None

    let inline ProductTermFromString< ^op when ^op : equality and ^op : (static member InNormalOrder : ^op -> ^op -> bool)>
        (unitFactory : string ->  ^op  option)
        (s : System.String) : PIxOp<uint32, ^op > option =
        try
            s.Trim().TrimStart('[').TrimEnd(']').Split('|')
            |> Array.choose (IndexedOpFromString unitFactory)
            |> (curry PIxOp<_,_>.Apply) Complex.One
            |> Some
        with
        | _ -> None

    let inline SumTermFromString< ^op when ^op : equality and ^op : (static member InNormalOrder : ^op -> ^op -> bool)>
        (unitFactory : string ->  ^op  option)
        (s : System.String) : SIxOp<uint32, ^op > option =
        try
            s.Trim().TrimStart('{').TrimEnd('}').Split(';')
            |> Array.choose (ProductTermFromString unitFactory)
            |> (curry SIxOp<_,_>.Apply) Complex.One
            |> Some
        with
        | _ -> None