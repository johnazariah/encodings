namespace Encodings

[<AutoOpen>]
module StringInterop =
    open System.Numerics
    let shrinkString (s : System.String) = s.Replace(" ", "")

    let inline IxOpFromString< ^op
                        when ^op : equality
                        and  ^op : comparison>
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

    let inline PIxOpFromString< ^op
                        when ^op : equality
                        and  ^op : (member IsIdentity  : bool)
                        and  ^op : comparison>
        (unitFactory : string ->  ^op  option)
        (s : System.String) : PIxOp<uint32, ^op > option =
        try
            s.Trim().TrimStart('[').TrimEnd(']').Split('|')
            |> Array.choose (IxOpFromString unitFactory)
            |> (curry PIxOp<_,_>.Apply) Complex.One
            |> Some
        with
        | _ -> None

    let inline SIxOpFromString< ^op
                        when ^op : equality
                        and  ^op : (member IsIdentity  : bool)
                        and  ^op : comparison>
        (unitFactory : string ->  ^op  option)
        (s : System.String) : SIxOp<uint32, ^op > option =
        try
            s.Trim().TrimStart('{').TrimEnd('}').Split(';')
            |> Array.choose (PIxOpFromString unitFactory)
            |> (curry SIxOp<_,_>.Apply) Complex.One
            |> Some
        with
        | _ -> None

    let inline RegisterFromString< ^op
                        when ^op : (static member Identity : ^op)
                        and  ^op : (static member Multiply : ^op -> ^op -> C< ^op >)
                        and ^op : equality>
        (unitFactory : char ->  ^op  option) (s : System.String) : R< ^op > option =
        try
            s.Trim().TrimStart().TrimEnd().ToCharArray()
            |> Array.choose (unitFactory)
            |> Array.map (curry C< ^op >.Apply Complex.One)
            |> (curry R< ^op >.Apply) Complex.One
            |> Some
        with
        | _ -> None
